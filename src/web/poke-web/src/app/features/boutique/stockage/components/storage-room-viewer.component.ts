import {
  Component, ElementRef, inject, input, OnDestroy, output, signal, viewChild, effect,
} from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { environment } from '@env/environment';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import {
  StorageBoxDto, StorageRoomDto, StorageCompileJobDto, Box3dPosition,
} from '../storage.models';

/**
 * Viewer 3D three.js d'une salle de stockage.
 * - Charge room.model3dUrl (.glb) via GLTFLoader, sinon affiche un état vide.
 * - Rend chaque carton comme un cube positionné via position3dJson, cliquable (raycasting).
 * - Upload .blend → POST model → poll compile-job toutes les 3s → recharge le .glb à `succeeded`.
 * Toute la logique three.js est encapsulée dans ce composant standalone.
 */
@Component({
  selector: 'app-storage-room-viewer',
  standalone: true,
  imports: [HasPermissionDirective],
  template: `
    <div class="viewer">
      <div class="viewer__toolbar">
        <span class="viewer__title">Vue 3D — {{ room()?.name }}</span>
        <div class="viewer__actions">
          @if (jobStatus()) {
            <span class="badge" [class.badge--run]="jobStatus() === 'Running' || jobStatus() === 'Queued'"
                  [class.badge--ok]="jobStatus() === 'Succeeded'"
                  [class.badge--ko]="jobStatus() === 'Failed'">
              @switch (jobStatus()) {
                @case ('Queued')    { En file d'attente… }
                @case ('Running')   { Compilation Blender en cours… }
                @case ('Succeeded') { Modèle mis à jour ✓ }
                @case ('Failed')    { Échec compilation }
              }
            </span>
          }
          <label *hasPermission="'storage:write'" class="btn-upload">
            Uploader un modèle 3D (.blend)
            <input type="file" accept=".blend" hidden (change)="onFileSelected($event)" [disabled]="uploading()" />
          </label>
        </div>
      </div>

      @if (jobError(); as err) {
        <div class="viewer__error">Erreur de compilation : {{ err }}</div>
      }

      <div class="viewer__stage">
        @if (!hasModel() && !loading()) {
          <div class="viewer__empty">
            <div class="viewer__empty-icon">🧊</div>
            <p>Aucun modèle 3D — uploader un fichier .blend pour générer la vue.</p>
          </div>
        }
        <div #host class="viewer__canvas" [class.hidden]="!hasModel()"></div>
        @if (loading()) { <div class="viewer__loading">Chargement du modèle…</div> }
      </div>

      @if (selectedBox(); as b) {
        <div class="viewer__selection">
          Carton sélectionné :
          <strong>{{ b.barcode || b.internalCode }}</strong>
          <button class="link" (click)="clearSelection()">fermer</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .viewer { display: flex; flex-direction: column; gap: .5rem; }
    .viewer__toolbar { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .viewer__title { font-weight: 600; }
    .viewer__actions { display: flex; align-items: center; gap: .75rem; }
    .btn-upload { cursor: pointer; padding: .45rem .8rem; border-radius: 8px; background: #4CAF50; color: #fff; font-size: .85rem; }
    .btn-upload input:disabled { cursor: not-allowed; }
    .badge { font-size: .75rem; padding: .25rem .5rem; border-radius: 6px; background: #eee; }
    .badge--run { background: #fff3cd; color: #7a5b00; }
    .badge--ok  { background: #d4edda; color: #155724; }
    .badge--ko  { background: #f8d7da; color: #721c24; }
    .viewer__error { background: #f8d7da; color: #721c24; padding: .5rem .75rem; border-radius: 8px; font-size: .85rem; }
    .viewer__stage { position: relative; height: 460px; border: 1px solid #e2e2e2; border-radius: 12px; overflow: hidden; background: #1b1b22; }
    .viewer__canvas { width: 100%; height: 100%; }
    .viewer__canvas.hidden { display: none; }
    .viewer__empty { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #9aa; gap: .5rem; text-align: center; padding: 1rem; }
    .viewer__empty-icon { font-size: 2.5rem; }
    .viewer__loading { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #ccd; }
    .viewer__selection { font-size: .85rem; display: flex; gap: .5rem; align-items: center; }
    .link { background: none; border: none; color: #4CAF50; cursor: pointer; text-decoration: underline; }
  `],
})
export class StorageRoomViewerComponent implements OnDestroy {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly room = input<StorageRoomDto | null>(null);
  readonly boxes = input<StorageBoxDto[]>([]);
  readonly boxSelected = output<StorageBoxDto>();

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly hasModel = signal(false);
  readonly selectedBox = signal<StorageBoxDto | null>(null);
  readonly jobStatus = signal<string | null>(null);
  readonly jobError = signal<string | null>(null);

  // three.js internals
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private controls?: OrbitControls;
  private raycaster = new THREE.Raycaster();
  private pointer = new THREE.Vector2();
  private frameId = 0;
  private resizeObserver?: ResizeObserver;
  private boxMeshes = new Map<string, THREE.Mesh>();
  private pollHandle?: ReturnType<typeof setInterval>;

  constructor() {
    // Recharge la scène quand la salle ou les cartons changent.
    effect(() => {
      const room = this.room();
      const boxes = this.boxes();
      if (!room) return;
      queueMicrotask(() => this.rebuild(room, boxes));
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.disposeScene();
  }

  // ── Scène ────────────────────────────────────────────────────────────────
  private rebuild(room: StorageRoomDto, boxes: StorageBoxDto[]): void {
    const url = room.model3dUrl;
    if (!url) {
      this.hasModel.set(false);
      this.disposeScene();
      return;
    }
    this.hasModel.set(true);
    this.ensureScene();
    this.loading.set(true);

    const loader = new GLTFLoader();
    loader.load(
      this.absoluteUrl(url),
      gltf => {
        this.clearModel();
        gltf.scene.name = '__model';
        this.scene!.add(gltf.scene);
        this.addBoxMeshes(boxes);
        this.loading.set(false);
      },
      undefined,
      err => {
        this.loading.set(false);
        this.toast.error('Impossible de charger le modèle 3D.');
        console.error('GLTF load error', err);
      },
    );
  }

  private ensureScene(): void {
    if (this.renderer) return;
    const el = this.host().nativeElement;
    const width = el.clientWidth || 640;
    const height = el.clientHeight || 460;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x1b1b22);

    this.camera = new THREE.PerspectiveCamera(50, width / height, 0.1, 1000);
    this.camera.position.set(6, 6, 10);

    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.setSize(width, height);
    el.appendChild(this.renderer.domElement);

    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;

    this.scene.add(new THREE.AmbientLight(0xffffff, 0.8));
    const dir = new THREE.DirectionalLight(0xffffff, 0.9);
    dir.position.set(5, 10, 7);
    this.scene.add(dir);
    this.scene.add(new THREE.GridHelper(20, 20, 0x444455, 0x2a2a33));

    this.renderer.domElement.addEventListener('click', this.onCanvasClick);

    this.resizeObserver = new ResizeObserver(() => this.onResize());
    this.resizeObserver.observe(el);

    const loop = () => {
      this.frameId = requestAnimationFrame(loop);
      this.controls?.update();
      this.renderer!.render(this.scene!, this.camera!);
    };
    loop();
  }

  private addBoxMeshes(boxes: StorageBoxDto[]): void {
    if (!this.scene) return;
    const geometry = new THREE.BoxGeometry(1, 1, 1);
    for (const box of boxes) {
      const pos = this.parsePosition(box.position3dJson);
      if (!pos) continue;
      const material = new THREE.MeshStandardMaterial({ color: 0x4caf50, transparent: true, opacity: 0.85 });
      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set(pos.x, pos.y, pos.z);
      mesh.rotation.y = pos.rotation ?? 0;
      mesh.userData['boxId'] = box.id;
      this.scene.add(mesh);
      this.boxMeshes.set(box.id, mesh);
    }
  }

  private readonly onCanvasClick = (event: MouseEvent): void => {
    if (!this.renderer || !this.camera) return;
    const rect = this.renderer.domElement.getBoundingClientRect();
    this.pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const meshes = Array.from(this.boxMeshes.values());
    const hits = this.raycaster.intersectObjects(meshes, false);
    if (hits.length === 0) return;
    const boxId = hits[0].object.userData['boxId'] as string;
    const box = this.boxes().find(b => b.id === boxId);
    if (box) {
      this.selectedBox.set(box);
      this.boxSelected.emit(box);
    }
  };

  clearSelection(): void { this.selectedBox.set(null); }

  private onResize(): void {
    if (!this.renderer || !this.camera) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth, h = el.clientHeight;
    if (w === 0 || h === 0) return;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h);
  }

  private clearModel(): void {
    if (!this.scene) return;
    const model = this.scene.getObjectByName('__model');
    if (model) this.scene.remove(model);
    for (const mesh of this.boxMeshes.values()) this.scene.remove(mesh);
    this.boxMeshes.clear();
  }

  private disposeScene(): void {
    cancelAnimationFrame(this.frameId);
    this.resizeObserver?.disconnect();
    this.renderer?.domElement.removeEventListener('click', this.onCanvasClick);
    this.controls?.dispose();
    this.renderer?.dispose();
    if (this.renderer?.domElement.parentElement) {
      this.renderer.domElement.parentElement.removeChild(this.renderer.domElement);
    }
    this.renderer = undefined;
    this.scene = undefined;
    this.camera = undefined;
    this.boxMeshes.clear();
  }

  // ── Upload + polling ─────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const room = this.room();
    if (!file || !room) return;

    this.uploading.set(true);
    this.jobError.set(null);
    this.service.uploadRoomModel(room.id, file).subscribe({
      next: job => { this.uploading.set(false); this.pollJob(job); },
      error: () => { this.uploading.set(false); this.toast.error('Échec de l’upload du .blend.'); },
    });
    input.value = '';
  }

  private pollJob(job: StorageCompileJobDto): void {
    this.stopPolling();
    this.jobStatus.set(job.status);
    // Polling toutes les 3s (action ponctuelle, pas de SSE nécessaire).
    this.pollHandle = setInterval(() => {
      this.service.getCompileJob(job.id).subscribe({
        next: j => {
          this.jobStatus.set(j.status);
          if (j.status === 'Succeeded') {
            this.stopPolling();
            this.jobError.set(null);
            this.reloadModel(j.glbOutputPath);
          } else if (j.status === 'Failed') {
            this.stopPolling();
            this.jobError.set(j.errorMessage ?? 'Erreur inconnue');
            this.toast.error('La compilation Blender a échoué.');
          }
        },
        error: () => { this.stopPolling(); this.toast.error('Perte du suivi du job de compilation.'); },
      });
    }, 3000);
  }

  private reloadModel(glbUrl: string | null): void {
    const room = this.room();
    if (!room || !glbUrl) return;
    this.hasModel.set(true);
    this.ensureScene();
    this.loading.set(true);
    const loader = new GLTFLoader();
    loader.load(
      this.absoluteUrl(glbUrl),
      gltf => {
        this.clearModel();
        gltf.scene.name = '__model';
        this.scene!.add(gltf.scene);
        this.addBoxMeshes(this.boxes());
        this.loading.set(false);
        this.toast.success('Modèle 3D chargé.');
      },
      undefined,
      () => { this.loading.set(false); this.toast.error('Impossible de charger le modèle généré.'); },
    );
  }

  private stopPolling(): void {
    if (this.pollHandle) { clearInterval(this.pollHandle); this.pollHandle = undefined; }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  private parsePosition(json: string | null): Box3dPosition | null {
    if (!json) return null;
    try {
      const p = JSON.parse(json);
      return { x: +p.x || 0, y: +p.y || 0, z: +p.z || 0, rotation: +p.rotation || 0 };
    } catch {
      return null;
    }
  }

  /** Les .glb sont servis en statique hors du préfixe /api. */
  private absoluteUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) return path;
    try {
      const origin = new URL(environment.apiBaseUrl, window.location.origin).origin;
      return `${origin}${path.startsWith('/') ? '' : '/'}${path}`;
    } catch {
      return path;
    }
  }
}
