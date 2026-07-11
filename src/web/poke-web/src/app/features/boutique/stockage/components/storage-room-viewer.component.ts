import {
  Component, ElementRef, inject, input, OnDestroy, output, signal, viewChild, effect,
} from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { environment } from '@env/environment';
import { StorageService } from '../storage.service';
import { loadGltfClone } from '../gltf-cache';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import {
  StorageBoxDto, StorageBinderDto, StorageRoomDto, StorageCompileJobDto,
  Box3dPosition, BoxDropEvent, STORAGE_ASSETS,
} from '../storage.models';

/**
 * Viewer 3D three.js d'une salle de stockage (caméra à l'intérieur, sans décor extérieur).
 * - Charge room.model3dUrl (.glb compilé).
 * - Cartons = carton_closed.glb (carton_open.glb si sélectionné), positionnés via Position3dJson.
 * - Drag&drop depuis la liste → pose sur le sol (raycast).
 * - Clic droit sur un carton → mode déplacement : la caméra est verrouillée, on suit la souris,
 *   clic gauche pour valider, Échap pour annuler.
 * - Classeurs du carton sélectionné représentés à l'intérieur du carton ouvert.
 * - Upload .blend (salle) → compilation Blender asynchrone.
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
            <span class="badge"
                  [class.badge--run]="jobStatus() === 'Running' || jobStatus() === 'Queued'"
                  [class.badge--ok]="jobStatus() === 'Succeeded'"
                  [class.badge--ko]="jobStatus() === 'Failed'">
              @switch (jobStatus()) {
                @case ('Queued')    { En file d'attente… }
                @case ('Running')   { Compilation Blender… }
                @case ('Succeeded') { Modèle à jour ✓ }
                @case ('Failed')    { Échec compilation }
              }
            </span>
          }
          <label *hasPermission="'storage:write'" class="btn-upload">
            Modèle de salle (.blend)
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
            <div class="viewer__empty-icon">🏬</div>
            <p>Aucun modèle 3D — uploadez un fichier .blend de salle.</p>
          </div>
        }
        <div #host class="viewer__canvas"
             [class.hidden]="!hasModel()"
             [class.moving]="moving()"
             (dragover)="onDragOver($event)"
             (drop)="onDrop($event)"></div>
        @if (loading()) { <div class="viewer__loading">Chargement du modèle…</div> }
        <div class="viewer__hint">
          @if (moving()) {
            <strong>Déplacement</strong> — clic gauche pour valider · Échap pour annuler
          } @else {
            Glissez un carton depuis la liste · clic pour sélectionner · clic droit pour déplacer
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .viewer { display: flex; flex-direction: column; gap: .5rem; height: 100%; }
    .viewer__toolbar { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .viewer__title { font-weight: var(--font-weight-semibold); }
    .viewer__actions { display: flex; align-items: center; gap: .75rem; }
    .badge { font-size: var(--font-size-xs); padding: .22rem .5rem; border-radius: var(--radius-sm); background: var(--color-surface-raised); }
    .badge--run { background: #fff3cd; color: #7a5b00; }
    .badge--ok  { background: #d4edda; color: #155724; }
    .badge--ko  { background: #f8d7da; color: #721c24; }
    .viewer__error { background: #f8d7da; color: #721c24; padding: .5rem .75rem; border-radius: var(--radius-md); font-size: var(--font-size-sm); }
    .viewer__stage { position: relative; flex: 1; min-height: 480px; border: 1px solid var(--color-border-default); border-radius: var(--radius-lg); overflow: hidden; background: #14141a; }
    .viewer__canvas { width: 100%; height: 100%; }
    .viewer__canvas.hidden { display: none; }
    .viewer__canvas.moving { cursor: grabbing; }
    .viewer__empty { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #9aa; gap: .5rem; text-align: center; padding: 1rem; }
    .viewer__empty-icon { font-size: 2.5rem; }
    .viewer__loading { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #ccd; }
    .viewer__hint { position: absolute; left: 0; right: 0; bottom: 0; padding: .35rem .6rem; font-size: var(--font-size-xs); color: #cdd; background: rgba(0,0,0,.4); text-align: center; }

    /* Boutons alignés sur le design-system (tokens) */
    .btn-upload { display: inline-flex; align-items: center; height: 36px; padding: 0 var(--space-4); border-radius: var(--radius-md);
      background: var(--color-surface-base); border: 1px solid var(--color-border-default); color: var(--color-text-primary);
      font-size: var(--font-size-sm); cursor: pointer; }
    .btn-upload:hover { background: var(--color-surface-raised); }
  `],
})
export class StorageRoomViewerComponent implements OnDestroy {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly room = input<StorageRoomDto | null>(null);
  readonly boxes = input<StorageBoxDto[]>([]);
  readonly selectedBoxId = input<string | null>(null);
  readonly binders = input<StorageBinderDto[]>([]);

  readonly boxSelected = output<StorageBoxDto>();
  readonly boxDropped = output<BoxDropEvent>();

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly hasModel = signal(false);
  readonly moving = signal(false);
  readonly jobStatus = signal<string | null>(null);
  readonly jobError = signal<string | null>(null);

  // three.js internals
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private controls?: OrbitControls;
  private raycaster = new THREE.Raycaster();
  private pointer = new THREE.Vector2();
  private floorPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  private frameId = 0;
  private resizeObserver?: ResizeObserver;
  private boxGroup = new THREE.Group();
  private boxMeshes: THREE.Object3D[] = [];
  private buildToken = 0;
  private pollHandle?: ReturnType<typeof setInterval>;

  // move mode
  private movingId: string | null = null;
  private movingObj?: THREE.Object3D;
  private movingOrigin = new THREE.Vector3();

  constructor() {
    effect(() => {
      const room = this.room();
      if (!room) return;
      queueMicrotask(() => this.loadRoom(room));
    });
    effect(() => {
      this.boxes(); this.selectedBoxId(); this.binders();
      if (this.scene && !this.movingId) queueMicrotask(() => this.rebuildBoxes());
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    window.removeEventListener('keydown', this.onKeyDown);
    this.disposeScene();
  }

  // ── Scène ────────────────────────────────────────────────────────────────
  private loadRoom(room: StorageRoomDto): void {
    if (!room.model3dUrl) {
      this.hasModel.set(false);
      this.disposeScene();
      return;
    }
    this.hasModel.set(true);
    this.ensureScene();
    this.loading.set(true);
    const loader = new GLTFLoader();
    loader.load(
      this.absoluteUrl(room.model3dUrl),
      gltf => {
        this.clearNamed('__room');
        gltf.scene.name = '__room';
        this.scene!.add(gltf.scene);
        this.frameCameraToRoom(gltf.scene);
        this.loading.set(false);
        this.rebuildBoxes();
      },
      undefined,
      err => { this.loading.set(false); this.toast.error('Impossible de charger le modèle 3D.'); console.error('GLTF room', err); },
    );
  }

  private ensureScene(): void {
    if (this.renderer) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth || 640, h = el.clientHeight || 480;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x14141a);
    this.scene.add(this.boxGroup);

    this.camera = new THREE.PerspectiveCamera(60, w / h, 0.05, 100);
    this.camera.position.set(0, 1.7, 2.5);

    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.setSize(w, h);
    el.appendChild(this.renderer.domElement);

    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;
    this.controls.target.set(0, 1, 0);
    this.controls.minDistance = 0.6;
    this.controls.maxDistance = 8;
    this.controls.maxPolarAngle = Math.PI * 0.52; // n'autorise pas à passer sous le sol
    // désactive le menu contextuel natif d'OrbitControls (on gère le clic droit nous-mêmes)
    this.controls.mouseButtons = { LEFT: THREE.MOUSE.ROTATE, MIDDLE: THREE.MOUSE.DOLLY, RIGHT: THREE.MOUSE.PAN };

    this.scene.add(new THREE.AmbientLight(0xffffff, 0.9));
    const dir = new THREE.DirectionalLight(0xffffff, 0.8);
    dir.position.set(4, 8, 5);
    this.scene.add(dir);
    // Pas de GridHelper : on ne veut que la salle, aucun décor extérieur.

    const canvas = this.renderer.domElement;
    canvas.addEventListener('click', this.onCanvasClick);
    canvas.addEventListener('contextmenu', this.onContextMenu);
    canvas.addEventListener('mousemove', this.onMouseMove);
    window.addEventListener('keydown', this.onKeyDown);

    this.resizeObserver = new ResizeObserver(() => this.onResize());
    this.resizeObserver.observe(el);

    const loop = () => {
      this.frameId = requestAnimationFrame(loop);
      this.controls?.update();
      this.renderer!.render(this.scene!, this.camera!);
    };
    loop();
  }

  /** Place la caméra à l'intérieur de la salle, centrée, à hauteur d'œil. */
  private frameCameraToRoom(root: THREE.Object3D): void {
    if (!this.camera || !this.controls) return;
    const box = new THREE.Box3().setFromObject(root);
    const center = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3());
    const eye = Math.min(1.7, size.y * 0.55);
    // caméra vers le côté ouvert (max Z), regardant le centre
    this.camera.position.set(center.x, eye, box.max.z - 0.4);
    this.controls.target.set(center.x, eye, center.z);
    this.controls.maxDistance = Math.max(size.x, size.z) * 1.1;
    this.controls.update();
  }

  // ── Cartons + classeurs ────────────────────────────────────────────────────
  private async rebuildBoxes(): Promise<void> {
    if (!this.scene) return;
    const token = ++this.buildToken;
    this.boxGroup.clear();
    this.boxMeshes = [];

    const selId = this.selectedBoxId();
    for (const box of this.boxes()) {
      const pos = this.parsePosition(box.position3dJson);
      if (!pos) continue;
      const isSel = box.id === selId;
      const url = isSel ? STORAGE_ASSETS.cartonOpen : STORAGE_ASSETS.cartonClosed;
      let carton: THREE.Object3D;
      try {
        carton = await loadGltfClone(url);
      } catch {
        carton = new THREE.Mesh(
          new THREE.BoxGeometry(0.6, 0.4, 0.4),
          new THREE.MeshStandardMaterial({ color: 0xb8865b }),
        );
      }
      if (token !== this.buildToken) return;
      carton.position.set(pos.x, pos.y, pos.z);
      carton.rotation.y = pos.rotation ?? 0;
      carton.traverse(o => (o.userData['boxId'] = box.id));
      this.boxGroup.add(carton);
      this.boxMeshes.push(carton);
      if (isSel) await this.addBinders(carton, pos, token);
    }
  }

  private async addBinders(carton: THREE.Object3D, pos: Box3dPosition, token: number): Promise<void> {
    const list = this.binders();
    if (list.length === 0) return;
    const n = Math.min(list.length, 6);
    const spread = 0.42;
    for (let i = 0; i < n; i++) {
      let binder: THREE.Object3D;
      try { binder = await loadGltfClone(STORAGE_ASSETS.binderClosed); }
      catch { continue; }
      if (token !== this.buildToken) return;
      const t = n === 1 ? 0 : (i / (n - 1) - 0.5);
      binder.rotation.x = -Math.PI / 2;
      binder.position.set(pos.x + t * spread, pos.y + 0.14, pos.z);
      this.boxGroup.add(binder);
    }
  }

  // ── Interactions ────────────────────────────────────────────────────────────
  private readonly onCanvasClick = (event: MouseEvent): void => {
    if (this.movingId) { this.finishMove(); return; }
    if (!this.renderer || !this.camera || this.boxMeshes.length === 0) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const hits = this.raycaster.intersectObjects(this.boxMeshes, true);
    if (hits.length === 0) return;
    const boxId = hits[0].object.userData['boxId'] as string | undefined;
    const box = boxId ? this.boxes().find(b => b.id === boxId) : undefined;
    if (box) this.boxSelected.emit(box);
  };

  private readonly onContextMenu = (event: MouseEvent): void => {
    event.preventDefault();
    if (this.movingId || !this.renderer || !this.camera) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const hits = this.raycaster.intersectObjects(this.boxMeshes, true);
    if (hits.length === 0) return;
    const boxId = hits[0].object.userData['boxId'] as string | undefined;
    const obj = this.boxMeshes.find(m => m.userData['boxId'] === boxId);
    if (!boxId || !obj) return;
    // entre en mode déplacement : caméra verrouillée
    this.movingId = boxId;
    this.movingObj = obj;
    this.movingOrigin.copy(obj.position);
    this.moving.set(true);
    if (this.controls) this.controls.enabled = false;
    const box = this.boxes().find(b => b.id === boxId);
    if (box) this.boxSelected.emit(box);
  };

  private readonly onMouseMove = (event: MouseEvent): void => {
    if (!this.movingId || !this.movingObj || !this.camera) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const p = new THREE.Vector3();
    if (this.raycaster.ray.intersectPlane(this.floorPlane, p)) {
      this.movingObj.position.x = p.x;
      this.movingObj.position.z = p.z;
    }
  };

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    if (event.key === 'Escape' && this.movingId) this.cancelMove();
  };

  private finishMove(): void {
    if (!this.movingId || !this.movingObj) return;
    const p = this.movingObj.position;
    this.boxDropped.emit({ boxId: this.movingId, x: +p.x.toFixed(3), y: 0, z: +p.z.toFixed(3) });
    this.endMove();
  }

  private cancelMove(): void {
    if (this.movingObj) this.movingObj.position.copy(this.movingOrigin);
    this.endMove();
  }

  private endMove(): void {
    this.movingId = null;
    this.movingObj = undefined;
    this.moving.set(false);
    if (this.controls) this.controls.enabled = true;
  }

  // ── Drag & drop (dépose un carton sur le plan) ─────────────────────────────
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    const boxId = event.dataTransfer?.getData('text/plain');
    if (!boxId || !this.renderer || !this.camera) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const point = new THREE.Vector3();
    if (!this.raycaster.ray.intersectPlane(this.floorPlane, point)) return;
    this.boxDropped.emit({ boxId, x: +point.x.toFixed(3), y: 0, z: +point.z.toFixed(3) });
  }

  private setPointer(event: MouseEvent): void {
    const rect = this.renderer!.domElement.getBoundingClientRect();
    this.pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
  }

  private onResize(): void {
    if (!this.renderer || !this.camera) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth, h = el.clientHeight;
    if (w === 0 || h === 0) return;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h);
  }

  private clearNamed(name: string): void {
    const obj = this.scene?.getObjectByName(name);
    if (obj) this.scene!.remove(obj);
  }

  private disposeScene(): void {
    cancelAnimationFrame(this.frameId);
    this.resizeObserver?.disconnect();
    const canvas = this.renderer?.domElement;
    if (canvas) {
      canvas.removeEventListener('click', this.onCanvasClick);
      canvas.removeEventListener('contextmenu', this.onContextMenu);
      canvas.removeEventListener('mousemove', this.onMouseMove);
    }
    this.controls?.dispose();
    this.renderer?.dispose();
    if (canvas?.parentElement) canvas.parentElement.removeChild(canvas);
    this.renderer = undefined;
    this.scene = undefined;
    this.camera = undefined;
    this.boxGroup = new THREE.Group();
    this.boxMeshes = [];
  }

  // ── Upload + polling (modèle de salle) ─────────────────────────────────────
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
    this.pollHandle = setInterval(() => {
      this.service.getCompileJob(job.id).subscribe({
        next: j => {
          this.jobStatus.set(j.status);
          if (j.status === 'Succeeded') {
            this.stopPolling();
            this.jobError.set(null);
            const room = this.room();
            if (room && j.glbOutputPath) this.loadRoom({ ...room, model3dUrl: j.glbOutputPath });
            this.toast.success('Modèle 3D chargé.');
          } else if (j.status === 'Failed') {
            this.stopPolling();
            this.jobError.set(j.errorMessage ?? 'Erreur inconnue');
            this.toast.error('La compilation Blender a échoué.');
          }
        },
        error: () => { this.stopPolling(); this.toast.error('Perte du suivi du job.'); },
      });
    }, 3000);
  }

  private stopPolling(): void {
    if (this.pollHandle) { clearInterval(this.pollHandle); this.pollHandle = undefined; }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private parsePosition(json: string | null): Box3dPosition | null {
    if (!json) return null;
    try {
      const p = JSON.parse(json);
      return { x: +p.x || 0, y: +p.y || 0, z: +p.z || 0, rotation: +p.rotation || 0 };
    } catch { return null; }
  }

  private absoluteUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) return path;
    try {
      const origin = new URL(environment.apiBaseUrl, window.location.origin).origin;
      return `${origin}${path.startsWith('/') ? '' : '/'}${path}`;
    } catch { return path; }
  }
}
