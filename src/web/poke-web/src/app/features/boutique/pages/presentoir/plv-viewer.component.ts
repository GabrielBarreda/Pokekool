import {
  Component, ElementRef, inject, input, OnDestroy, signal, viewChild, afterNextRender, effect,
} from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { environment } from '@env/environment';
import { BoutiqueService } from '../../boutique.service';
import { ShopProductDto, PlvCompileJobDto } from '../../boutique.models';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';

/**
 * Panneau 3D dédié du présentoir (PLV) sur la page Présentoir.
 *
 * - Charge le .glb de la PLV dans un canvas contenu.
 * - Vue produit rapprochée face aux étagères ; la MOLETTE (wheel) déplace la caméra le long
 *   d'une trajectoire calculée sur les étagères (PLV_Shelf_*), du haut vers le bas.
 * - Pose les vraies cartes (ShopProductDto) en plans texturés sur les étagères.
 * - Section admin (shop:write) : upload .blend → compilation Blender → poll → reload.
 */
@Component({
  selector: 'app-presentoir-plv-viewer',
  standalone: true,
  imports: [HasPermissionDirective],
  template: `
    <div class="plv3d">
      <div class="plv3d__toolbar">
        <span class="plv3d__title">Présentoir 3D</span>
        <div class="plv3d__actions">
          @if (jobStatus(); as st) {
            <span class="plv3d__badge"
                  [class.plv3d__badge--run]="st === 'Queued' || st === 'Running'"
                  [class.plv3d__badge--ok]="st === 'Succeeded'"
                  [class.plv3d__badge--ko]="st === 'Failed'">
              @switch (st) {
                @case ('Queued')    { En file d'attente… }
                @case ('Running')   { Compilation Blender… }
                @case ('Succeeded') { PLV à jour ✓ }
                @case ('Failed')    { Échec compilation }
              }
            </span>
          }
          <label *hasPermission="'shop:write'" class="plv3d__upload" [class.plv3d__upload--busy]="uploading()">
            {{ uploading() ? 'Envoi…' : 'Uploader PLV (.blend)' }}
            <input type="file" accept=".blend" hidden (change)="onFileSelected($event)" [disabled]="uploading()" />
          </label>
        </div>
      </div>

      @if (jobError(); as err) {
        <div class="plv3d__error">Erreur de compilation : {{ err }}</div>
      }

      <div class="plv3d__stage">
        <div #host class="plv3d__canvas"></div>
        @if (hasModel() && shelfCount() > 1) {
          <div class="plv3d__hint">↕ Molette pour parcourir les étagères</div>
        }
        @if (!hasModel()) {
          <div class="plv3d__empty">
            <div class="plv3d__empty-icon">📦</div>
            <p>Aucun présentoir 3D. Uploade un fichier .blend pour le générer.</p>
          </div>
        }
        @if (loading()) { <div class="plv3d__loading">Chargement du présentoir…</div> }
      </div>
    </div>
  `,
  styles: [`
    .plv3d { display: flex; flex-direction: column; gap: 8px; }
    .plv3d__toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
    .plv3d__title { font-weight: 700; color: var(--color-text-primary); }
    .plv3d__actions { display: flex; align-items: center; gap: 10px; }
    .plv3d__upload {
      cursor: pointer; padding: 8px 14px; border-radius: 8px;
      background: linear-gradient(90deg, #ffcb05, #f0a500); color: #060a1c;
      font-size: 13px; font-weight: 700; box-shadow: 0 2px 8px rgba(0,0,0,0.2);
    }
    .plv3d__upload--busy { opacity: .6; cursor: not-allowed; }
    .plv3d__badge { font-size: 12px; padding: 4px 10px; border-radius: 6px; background: rgba(0,0,0,0.06); color: #333; font-weight: 600; }
    .plv3d__badge--run { background: #fff3cd; color: #7a5b00; }
    .plv3d__badge--ok  { background: #d4edda; color: #155724; }
    .plv3d__badge--ko  { background: #f8d7da; color: #721c24; }
    .plv3d__error { background: #f8d7da; color: #721c24; padding: 8px 12px; border-radius: 8px; font-size: 13px; }

    .plv3d__stage {
      position: relative;
      width: 100%;
      height: min(68vh, 640px);
      min-height: 380px;
      border-radius: 14px;
      overflow: hidden;
      background: radial-gradient(120% 120% at 50% 0%, #16234f 0%, #0a132e 55%, #060a1c 100%);
      border: 2px solid rgba(255,203,5,0.25);
      box-shadow: inset 0 1px 0 rgba(255,203,5,0.15), 0 8px 32px rgba(0,0,0,0.4);
    }
    .plv3d__canvas { position: absolute; inset: 0; }
    .plv3d__canvas canvas { display: block; width: 100% !important; height: 100% !important; }
    .plv3d__hint {
      position: absolute; bottom: 12px; left: 50%; transform: translateX(-50%);
      background: rgba(6,10,28,0.6); color: #e8ecf7; font-size: 12px; font-weight: 600;
      padding: 5px 12px; border-radius: 999px; pointer-events: none; backdrop-filter: blur(4px);
    }
    .plv3d__empty {
      position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center;
      justify-content: center; gap: 8px; color: #9fb0d0; text-align: center; padding: 1rem; pointer-events: none;
    }
    .plv3d__empty-icon { font-size: 2.5rem; }
    .plv3d__loading {
      position: absolute; inset: 0; display: flex; align-items: center; justify-content: center;
      color: #cdd6f0; background: rgba(6,10,28,0.4);
    }
  `],
})
export class PresentoirPlvViewerComponent implements OnDestroy {
  private readonly service = inject(BoutiqueService);
  private readonly toast = inject(ToastService);

  /** Cartes du présentoir posées sur les étagères 3D. */
  readonly products = input<ShopProductDto[]>([]);

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly hasModel = signal(false);
  readonly shelfCount = signal(0);
  readonly jobStatus = signal<PlvCompileJobDto['status'] | null>(null);
  readonly jobError = signal<string | null>(null);

  // three.js
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private frameId = 0;
  private resizeObserver?: ResizeObserver;
  private readonly textureLoader = new THREE.TextureLoader();

  private shelfBoxes: THREE.Box3[] = [];   // triées du haut vers le bas
  private readonly modelBox = new THREE.Box3(); // bbox globale du modèle
  private frontSign = -1;                    // côté "avant" des étagères (axe Z)

  // Trajectoire caméra pilotée à la molette
  private camKeyframes: { pos: THREE.Vector3; look: THREE.Vector3 }[] = [];
  private readonly desiredPos = new THREE.Vector3();
  private readonly desiredLook = new THREE.Vector3();
  private readonly currentLook = new THREE.Vector3();
  private scrollT = 0; // 0 = étagère du haut, 1 = étagère du bas

  private pollHandle?: ReturnType<typeof setInterval>;
  private initialized = false;

  constructor() {
    afterNextRender(() => this.init());
    effect(() => {
      this.products();
      if (this.shelfBoxes.length) queueMicrotask(() => this.placeCards());
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.resizeObserver?.disconnect();
    this.host()?.nativeElement?.removeEventListener('wheel', this.onWheel);
    this.disposeScene();
  }

  // ── Init ─────────────────────────────────────────────────────────────────
  private init(): void {
    if (this.initialized) return;
    this.initialized = true;
    this.ensureScene();
    const el = this.host().nativeElement;
    this.resizeObserver = new ResizeObserver(() => this.onResize());
    this.resizeObserver.observe(el);
    el.addEventListener('wheel', this.onWheel, { passive: false });
    this.service.getPlvModel().subscribe({
      next: m => { if (m.plvModel3dUrl) this.loadModel(m.plvModel3dUrl); },
      error: () => { /* pas de modèle : état vide, non bloquant */ },
    });
  }

  private ensureScene(): void {
    if (this.renderer) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth || 800, h = el.clientHeight || 480;

    this.scene = new THREE.Scene();

    this.camera = new THREE.PerspectiveCamera(42, w / h, 0.01, 100);
    this.camera.position.set(0, 1.2, -1.2);
    this.currentLook.set(0, 1.2, 0);
    this.camera.lookAt(this.currentLook);

    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(w, h);
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    el.appendChild(this.renderer.domElement);

    this.scene.add(new THREE.AmbientLight(0xffffff, 0.75));
    const dir = new THREE.DirectionalLight(0xffffff, 1.0);
    dir.position.set(0.5, 2, this.frontSign * 2);
    this.scene.add(dir);

    const loop = () => {
      this.frameId = requestAnimationFrame(loop);
      this.updateCamera();
      this.renderer!.render(this.scene!, this.camera!);
    };
    loop();
  }

  // ── Chargement du modèle ───────────────────────────────────────────────────
  private loadModel(url: string): void {
    this.ensureScene();
    this.loading.set(true);
    const loader = new GLTFLoader();
    loader.load(
      this.absoluteUrl(url),
      gltf => {
        this.clearModel();
        gltf.scene.name = '__plv';
        this.scene!.add(gltf.scene);
        gltf.scene.updateMatrixWorld(true);
        this.hasModel.set(true);

        // Déduit le côté "avant" via la caméra embarquée du modèle (PLV_Camera), puis on l'ignore.
        this.frontSign = this.detectFrontSign(gltf);

        this.modelBox.setFromObject(gltf.scene);
        this.collectShelves(gltf.scene);
        this.buildCameraPath();
        this.placeCards();
        this.scrollT = 0;
        this.loading.set(false);
      },
      undefined,
      err => {
        this.loading.set(false);
        this.toast.error('Impossible de charger le présentoir 3D.');
        console.error('GLTF PLV load error', err);
      },
    );
  }

  private detectFrontSign(gltf: { scene: THREE.Object3D; cameras?: THREE.Camera[] }): number {
    const cam = gltf.cameras?.[0];
    if (!cam) return -1;
    const wp = new THREE.Vector3();
    cam.getWorldPosition(wp);
    const center = new THREE.Box3().setFromObject(gltf.scene).getCenter(new THREE.Vector3());
    return Math.sign(wp.z - center.z) || -1;
  }

  private collectShelves(root: THREE.Object3D): void {
    const shelves: THREE.Object3D[] = [];
    root.traverse(o => { if (o.name.startsWith('PLV_Shelf')) shelves.push(o); });
    this.shelfBoxes = shelves
      .map(o => new THREE.Box3().setFromObject(o))
      .sort((a, b) => b.max.y - a.max.y); // plus haut d'abord
    this.shelfCount.set(this.shelfBoxes.length);
  }

  // ── Emplacements par étagère ────────────────────────────────────────────────
  private readonly perShelf = 3;

  /** Pour chaque étagère : centre X, hauteur du plateau, dégagement au-dessus, Z avant, dims carte. */
  private shelfSlots(): {
    cx: number; topY: number; clearance: number; frontZ: number; centerY: number;
    slotW: number; cardW: number; cardH: number;
  }[] {
    const modelTop = this.modelBox.max.y;
    return this.shelfBoxes.map((s, i) => {
      const aboveY = i === 0 ? modelTop : this.shelfBoxes[i - 1].min.y; // dessous de l'étagère au-dessus
      const topY = s.max.y;
      const clearance = Math.max(0.05, aboveY - topY);
      const depth = s.max.z - s.min.z;
      const frontZ = this.frontSign > 0 ? s.max.z - depth * 0.25 : s.min.z + depth * 0.25;
      const shelfWidth = s.max.x - s.min.x;
      const slotW = shelfWidth / this.perShelf;
      let cardW = slotW * 0.86;
      let cardH = cardW * (3.5 / 2.5);
      if (cardH > clearance * 0.9) { cardH = clearance * 0.9; cardW = cardH * (2.5 / 3.5); }
      return { cx: (s.min.x + s.max.x) / 2, topY, clearance, frontZ, centerY: topY + clearance / 2, slotW, cardW, cardH };
    });
  }

  // ── Trajectoire caméra (devant le présentoir, une étagère cadrée par keyframe) ──
  private buildCameraPath(): void {
    this.camKeyframes = [];
    if (!this.camera || !this.shelfBoxes.length) return;

    const fov = (this.camera.fov * Math.PI) / 180;
    // Caméra toujours DEVANT l'ensemble du présentoir (jamais à l'intérieur d'un compartiment).
    const frontZmodel = this.frontSign > 0 ? this.modelBox.max.z : this.modelBox.min.z;

    for (const slot of this.shelfSlots()) {
      const viewH = Math.max(slot.clearance * 1.6, 0.24); // cadrage + contexte, avec un minimum
      const camDist = (viewH / 2) / Math.tan(fov / 2);
      const look = new THREE.Vector3(slot.cx, slot.centerY, slot.frontZ);
      const pos = new THREE.Vector3(slot.cx, slot.centerY, frontZmodel + this.frontSign * camDist);
      this.camKeyframes.push({ pos, look });
    }

    this.camera.position.copy(this.camKeyframes[0].pos);
    this.currentLook.copy(this.camKeyframes[0].look);
  }

  private updateCamera(): void {
    if (!this.camera || this.camKeyframes.length === 0) return;
    const n = this.camKeyframes.length;
    if (n === 1) {
      this.desiredPos.copy(this.camKeyframes[0].pos);
      this.desiredLook.copy(this.camKeyframes[0].look);
    } else {
      const s = this.scrollT * (n - 1);
      const i = Math.min(n - 2, Math.floor(s));
      const t = s - i;
      this.desiredPos.copy(this.camKeyframes[i].pos).lerp(this.camKeyframes[i + 1].pos, t);
      this.desiredLook.copy(this.camKeyframes[i].look).lerp(this.camKeyframes[i + 1].look, t);
    }
    this.camera.position.lerp(this.desiredPos, 0.12);
    this.currentLook.lerp(this.desiredLook, 0.12);
    this.camera.lookAt(this.currentLook);
  }

  private readonly onWheel = (e: WheelEvent): void => {
    if (this.camKeyframes.length < 2) return;
    e.preventDefault(); // pilote la caméra au lieu de scroller la page
    this.scrollT = Math.min(1, Math.max(0, this.scrollT + e.deltaY / 1400));
  };

  // ── Cartes sur les étagères ─────────────────────────────────────────────────
  private placeCards(): void {
    if (!this.scene || !this.shelfBoxes.length) return;
    this.clearCards();

    const products = this.products();
    if (!products.length) return;

    const group = new THREE.Group();
    group.name = '__cards';

    let idx = 0;
    for (const slot of this.shelfSlots()) {
      if (idx >= products.length) break;
      const rowItems = products.slice(idx, idx + this.perShelf);
      idx += this.perShelf;

      const restY = slot.topY + slot.cardH / 2 + 0.002;
      const rowWidth = rowItems.length * slot.slotW;
      const startX = slot.cx - rowWidth / 2 + slot.slotW / 2;

      rowItems.forEach((p, i) => {
        const mesh = this.makeCardMesh(p, slot.cardW, slot.cardH);
        mesh.position.set(startX + i * slot.slotW, restY, slot.frontZ);
        mesh.rotation.y = this.frontSign > 0 ? 0 : Math.PI; // face vers l'avant
        group.add(mesh);
      });
    }

    this.scene.add(group);
  }

  private makeCardMesh(p: ShopProductDto, w: number, h: number): THREE.Mesh {
    const geom = new THREE.PlaneGeometry(w, h);
    const material = new THREE.MeshBasicMaterial({
      color: 0x1a2a55, side: THREE.DoubleSide, transparent: true,
    });
    const mesh = new THREE.Mesh(geom, material);

    const imgUrl = p.photoUrl || p.cardImage;
    if (imgUrl) {
      // Images externes servies sans CORS → on les passe par le proxy same-origin de l'API,
      // sinon WebGL refuse de les utiliser comme texture.
      const src = /^https?:\/\//i.test(imgUrl)
        ? `${environment.apiBaseUrl}/v1/media/image?url=${encodeURIComponent(imgUrl)}`
        : this.absoluteUrl(imgUrl);
      this.textureLoader.setCrossOrigin('anonymous');
      this.textureLoader.load(
        src,
        tex => {
          tex.colorSpace = THREE.SRGBColorSpace;
          material.color.set(0xffffff);
          material.map = tex;
          material.needsUpdate = true;
        },
        undefined,
        () => { /* garde le plan coloré en cas d'échec */ },
      );
    }
    return mesh;
  }

  // ── Resize ──────────────────────────────────────────────────────────────────
  private onResize(): void {
    if (!this.renderer || !this.camera) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth, h = el.clientHeight;
    if (w === 0 || h === 0) return;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h);
  }

  // ── Nettoyage scène ─────────────────────────────────────────────────────────
  private clearCards(): void {
    const g = this.scene?.getObjectByName('__cards');
    if (g && this.scene) {
      g.traverse(o => {
        const m = o as THREE.Mesh;
        if (m.geometry) m.geometry.dispose();
        const mat = m.material as THREE.MeshBasicMaterial | undefined;
        if (mat) { mat.map?.dispose(); mat.dispose(); }
      });
      this.scene.remove(g);
    }
  }

  private clearModel(): void {
    this.clearCards();
    this.shelfBoxes = [];
    this.camKeyframes = [];
    this.shelfCount.set(0);
    const model = this.scene?.getObjectByName('__plv');
    if (model && this.scene) this.scene.remove(model);
  }

  private disposeScene(): void {
    cancelAnimationFrame(this.frameId);
    this.renderer?.dispose();
    if (this.renderer?.domElement.parentElement) {
      this.renderer.domElement.parentElement.removeChild(this.renderer.domElement);
    }
    this.renderer = undefined;
    this.scene = undefined;
    this.camera = undefined;
  }

  // ── Upload + polling ────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.jobError.set(null);
    this.service.uploadPlvModel(file).subscribe({
      next: job => { this.uploading.set(false); this.pollJob(job); },
      error: () => { this.uploading.set(false); this.toast.error('Échec de l’upload du .blend.'); },
    });
    input.value = '';
  }

  private pollJob(job: PlvCompileJobDto): void {
    this.stopPolling();
    this.jobStatus.set(job.status);
    this.pollHandle = setInterval(() => {
      this.service.getPlvCompileJob(job.id).subscribe({
        next: j => {
          this.jobStatus.set(j.status);
          if (j.status === 'Succeeded') {
            this.stopPolling();
            this.jobError.set(null);
            if (j.glbOutputPath) this.loadModel(j.glbOutputPath);
            this.toast.success('Présentoir 3D mis à jour.');
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

  private stopPolling(): void {
    if (this.pollHandle) { clearInterval(this.pollHandle); this.pollHandle = undefined; }
  }

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
