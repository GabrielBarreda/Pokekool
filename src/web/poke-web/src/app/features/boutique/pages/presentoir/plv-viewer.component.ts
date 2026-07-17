import {
  Component, ElementRef, inject, input, OnDestroy, signal, viewChild, afterNextRender, effect,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { environment } from '@env/environment';
import { BoutiqueService } from '../../boutique.service';
import {
  ShopProductDto, PlvCompileJobDto, ShopProductType, PRODUCT_TYPE_LABELS,
} from '../../boutique.models';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';

interface PlvCategory { type: ShopProductType; label: string; products: ShopProductDto[]; }

/**
 * Présentoir 3D — carrousel par catégorie.
 * - 1 PLV (carton) par type d'article ; les voisins dépassent, un clic dessus recentre la caméra.
 * - Molette = parcours vertical des étagères du PLV actif.
 * - Cartes cliquables (raycasting) → panneau latéral (prix, en vente depuis, nb d'intéressés, Notifier).
 * - Section admin (shop:write) : upload .blend → compilation Blender → poll → reload.
 */
@Component({
  selector: 'app-presentoir-plv-viewer',
  standalone: true,
  imports: [HasPermissionDirective, DatePipe, DecimalPipe],
  template: `
    <div class="plv3d">
      <div class="plv3d__toolbar">
        <div class="plv3d__nav">
          @if (categories().length > 1) {
            <button class="plv3d__arrow" (click)="prev()" [disabled]="activeIndex() === 0">‹</button>
          }
          <span class="plv3d__title">
            {{ categories().length ? categories()[activeIndex()].label : 'Présentoir 3D' }}
            @if (categories().length) { <small>({{ categories()[activeIndex()].products.length }})</small> }
          </span>
          @if (categories().length > 1) {
            <button class="plv3d__arrow" (click)="next()" [disabled]="activeIndex() >= categories().length - 1">›</button>
          }
        </div>
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

      @if (jobError(); as err) { <div class="plv3d__error">Erreur de compilation : {{ err }}</div> }

      <div class="plv3d__stage">
        <div #host class="plv3d__canvas"></div>

        @if (hasModel()) {
          <div class="plv3d__hint">↕ molette · clic carte = détails · clic PLV voisin = changer de rayon</div>
        }
        @if (!hasModel()) {
          <div class="plv3d__empty">
            <div class="plv3d__empty-icon">📦</div>
            <p>Aucun présentoir 3D. Uploade un fichier .blend pour le générer.</p>
          </div>
        }
        @if (loading()) { <div class="plv3d__loading">Chargement du présentoir…</div> }

        <!-- Panneau latéral : détails de la carte cliquée -->
        @if (selected(); as p) {
          <aside class="plv3d__panel">
            <button class="plv3d__panel-close" (click)="closePanel()">✕</button>
            @if (p.photoUrl || p.cardImage) {
              <img class="plv3d__panel-img" [src]="p.photoUrl || p.cardImage" [alt]="p.cardName || p.productName" />
            }
            <h3 class="plv3d__panel-name">{{ p.cardName || p.productName }}</h3>
            <div class="plv3d__panel-price">{{ p.price | number:'1.2-2' }} {{ p.currency }}</div>
            <dl class="plv3d__panel-meta">
              <div><dt>En vente depuis</dt><dd>{{ p.createdAt | date:'dd/MM/yyyy HH:mm' }}</dd></div>
              <div>
                <dt>Intéressés</dt>
                <dd>
                  @if (interestLoading()) { … }
                  @else { {{ interested() ?? 0 }} client{{ (interested() ?? 0) > 1 ? 's' : '' }} }
                </dd>
              </div>
              <div><dt>Disponibilité</dt><dd>{{ p.isAvailable ? 'En vente' : 'Indisponible' }} · ×{{ p.quantity }}</dd></div>
            </dl>
            <button class="plv3d__notify" [disabled]="(interested() ?? 0) === 0" (click)="notifyInterested(p)">
              🔔 Notifier les intéressés
            </button>
          </aside>
        }
      </div>
    </div>
  `,
  styles: [`
    .plv3d { display: flex; flex-direction: column; gap: 8px; }
    .plv3d__toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
    .plv3d__nav { display: flex; align-items: center; gap: 8px; }
    .plv3d__title { font-weight: 700; color: var(--color-text-primary); }
    .plv3d__title small { color: var(--color-text-muted); font-weight: 500; }
    .plv3d__arrow {
      width: 28px; height: 28px; border-radius: 50%; border: 1px solid var(--color-border-default);
      background: var(--color-surface-base); color: var(--color-text-primary); cursor: pointer; font-size: 16px; line-height: 1;
    }
    .plv3d__arrow:disabled { opacity: .4; cursor: not-allowed; }
    .plv3d__actions { display: flex; align-items: center; gap: 10px; }
    .plv3d__upload {
      cursor: pointer; padding: 8px 14px; border-radius: 8px;
      background: linear-gradient(90deg, #ffcb05, #f0a500); color: #060a1c; font-size: 13px; font-weight: 700;
    }
    .plv3d__upload--busy { opacity: .6; cursor: not-allowed; }
    .plv3d__badge { font-size: 12px; padding: 4px 10px; border-radius: 6px; background: rgba(0,0,0,0.06); color: #333; font-weight: 600; }
    .plv3d__badge--run { background: #fff3cd; color: #7a5b00; }
    .plv3d__badge--ok  { background: #d4edda; color: #155724; }
    .plv3d__badge--ko  { background: #f8d7da; color: #721c24; }
    .plv3d__error { background: #f8d7da; color: #721c24; padding: 8px 12px; border-radius: 8px; font-size: 13px; }

    .plv3d__stage {
      position: relative; width: 100%; height: min(68vh, 640px); min-height: 380px;
      border-radius: 14px; overflow: hidden;
      background: radial-gradient(120% 120% at 50% 0%, #16234f 0%, #0a132e 55%, #060a1c 100%);
      border: 2px solid rgba(255,203,5,0.25); box-shadow: inset 0 1px 0 rgba(255,203,5,0.15), 0 8px 32px rgba(0,0,0,0.4);
    }
    .plv3d__canvas { position: absolute; inset: 0; cursor: pointer; }
    .plv3d__canvas canvas { display: block; width: 100% !important; height: 100% !important; }
    .plv3d__hint {
      position: absolute; bottom: 12px; left: 50%; transform: translateX(-50%);
      background: rgba(6,10,28,0.6); color: #e8ecf7; font-size: 12px; font-weight: 600;
      padding: 5px 12px; border-radius: 999px; pointer-events: none; backdrop-filter: blur(4px);
    }
    .plv3d__empty { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; color: #9fb0d0; text-align: center; padding: 1rem; pointer-events: none; }
    .plv3d__empty-icon { font-size: 2.5rem; }
    .plv3d__loading { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #cdd6f0; background: rgba(6,10,28,0.4); }

    /* Panneau latéral détails carte */
    .plv3d__panel {
      position: absolute; top: 12px; right: 12px; bottom: 12px; width: 300px; max-width: 80%;
      background: rgba(10,16,38,0.94); backdrop-filter: blur(8px); border: 1px solid rgba(255,203,5,0.3);
      border-radius: 12px; padding: 16px; color: #e8ecf7; display: flex; flex-direction: column; gap: 10px;
      box-shadow: -8px 0 32px rgba(0,0,0,0.5); overflow-y: auto;
    }
    .plv3d__panel-close { position: absolute; top: 8px; right: 8px; background: none; border: none; color: #9fb0d0; cursor: pointer; font-size: 16px; }
    .plv3d__panel-img { width: 55%; align-self: center; border-radius: 8px; box-shadow: 0 4px 16px rgba(0,0,0,0.5); }
    .plv3d__panel-name { margin: 0; font-size: 16px; font-weight: 700; }
    .plv3d__panel-price { font-size: 20px; font-weight: 800; color: #ffcb05; }
    .plv3d__panel-meta { display: flex; flex-direction: column; gap: 8px; margin: 4px 0; }
    .plv3d__panel-meta > div { display: flex; justify-content: space-between; gap: 8px; font-size: 13px; border-bottom: 1px solid rgba(255,255,255,0.08); padding-bottom: 6px; }
    .plv3d__panel-meta dt { color: #9fb0d0; margin: 0; }
    .plv3d__panel-meta dd { margin: 0; font-weight: 600; text-align: right; }
    .plv3d__notify {
      margin-top: auto; padding: 10px; border: none; border-radius: 8px; cursor: pointer;
      background: linear-gradient(90deg, #ffcb05, #f0a500); color: #060a1c; font-weight: 700; font-size: 13px;
    }
    .plv3d__notify:disabled { opacity: .4; cursor: not-allowed; }
  `],
})
export class PresentoirPlvViewerComponent implements OnDestroy {
  private readonly service = inject(BoutiqueService);
  private readonly toast = inject(ToastService);

  readonly products = input<ShopProductDto[]>([]);

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly hasModel = signal(false);
  readonly categories = signal<PlvCategory[]>([]);
  readonly activeIndex = signal(0);
  readonly selected = signal<ShopProductDto | null>(null);
  readonly interested = signal<number | null>(null);
  readonly interestLoading = signal(false);
  readonly jobStatus = signal<PlvCompileJobDto['status'] | null>(null);
  readonly jobError = signal<string | null>(null);

  // Réglages
  private readonly perShelf = 5;     // nb de cartes par étagère
  private readonly stretchX = 1;     // 1 = on utilise le .blend déjà large (plv-wide)
  private readonly cardZoom = 1.1;   // cadrage : + petit = cartes + grandes (~1/cardZoom de la hauteur)

  // three.js
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private frameId = 0;
  private resizeObserver?: ResizeObserver;
  private readonly textureLoader = new THREE.TextureLoader();
  private readonly raycaster = new THREE.Raycaster();
  private readonly pointer = new THREE.Vector2();

  private templateScene?: THREE.Group;     // modèle .glb (élargi) cloné par catégorie
  private readonly modelBox = new THREE.Box3();
  private frontSign = -1;
  private shelfBoxes: THREE.Box3[] = [];
  private plvSpacing = 1.2;                 // écart X entre 2 PLV
  private pickables: THREE.Object3D[] = []; // meshes cliquables (cartes + structure)
  private cardMeshes: THREE.Mesh[] = [];    // cartes (lévitation + survol curseur)
  private readonly hoverPointer = new THREE.Vector2();
  private pointerInside = false;
  private readonly clock = new THREE.Clock();

  // Caméra
  private camKeyframes: { pos: THREE.Vector3; look: THREE.Vector3 }[] = [];
  private readonly desiredPos = new THREE.Vector3();
  private readonly desiredLook = new THREE.Vector3();
  private readonly currentLook = new THREE.Vector3();
  private scrollT = 0;

  private pollHandle?: ReturnType<typeof setInterval>;
  private initialized = false;

  constructor() {
    afterNextRender(() => this.init());
    effect(() => {
      this.products();
      if (this.templateScene) queueMicrotask(() => this.buildCarousel());
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.resizeObserver?.disconnect();
    const el = this.host()?.nativeElement;
    if (el) {
      el.removeEventListener('wheel', this.onWheel);
      el.removeEventListener('click', this.onClick);
      el.removeEventListener('mousemove', this.onMove);
      el.removeEventListener('mouseleave', this.onLeave);
    }
    this.disposeScene();
  }

  // ── Nav catégories ──────────────────────────────────────────────────────────
  prev(): void { this.setActive(this.activeIndex() - 1); }
  next(): void { this.setActive(this.activeIndex() + 1); }
  private setActive(i: number): void {
    const n = this.categories().length;
    if (n === 0) return;
    this.activeIndex.set(Math.min(n - 1, Math.max(0, i)));
    this.scrollT = 0;
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
    el.addEventListener('click', this.onClick);
    el.addEventListener('mousemove', this.onMove);
    el.addEventListener('mouseleave', this.onLeave);
    this.service.getPlvModel().subscribe({
      next: m => { if (m.plvModel3dUrl) this.loadModel(m.plvModel3dUrl); },
      error: () => { /* pas de modèle : état vide */ },
    });
  }

  private ensureScene(): void {
    if (this.renderer) return;
    const el = this.host().nativeElement;
    const w = el.clientWidth || 800, h = el.clientHeight || 480;

    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(42, w / h, 0.01, 100);
    this.camera.position.set(0, 1.0, -1.5);
    this.currentLook.set(0, 0.9, 0);
    this.camera.lookAt(this.currentLook);

    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(w, h);
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    el.appendChild(this.renderer.domElement);

    this.scene.add(new THREE.AmbientLight(0xffffff, 0.75));
    const dir = new THREE.DirectionalLight(0xffffff, 1.0);
    dir.position.set(0.5, 2, 2);
    this.scene.add(dir);

    const loop = () => {
      this.frameId = requestAnimationFrame(loop);
      this.updateCamera();
      this.updateCards();
      this.renderer!.render(this.scene!, this.camera!);
    };
    loop();
  }

  // ── Chargement modèle (template cloné ensuite par catégorie) ─────────────────
  private loadModel(url: string): void {
    this.ensureScene();
    this.loading.set(true);
    const loader = new GLTFLoader();
    loader.load(
      this.absoluteUrl(url),
      gltf => {
        const tpl = gltf.scene;
        tpl.scale.x = this.stretchX;
        tpl.traverse(o => { if (o.name.startsWith('PLV_SidePanel')) o.scale.x = 1 / this.stretchX; });
        tpl.updateMatrixWorld(true);

        this.frontSign = this.detectFrontSign(gltf);
        this.modelBox.setFromObject(tpl);
        this.collectShelves(tpl);
        // Écart entre PLV : bon espace visible entre les cartons.
        this.plvSpacing = (this.modelBox.max.x - this.modelBox.min.x) * 1.45;

        this.templateScene = tpl;
        this.hasModel.set(true);
        this.buildCameraPath();
        this.buildCarousel();
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
      .sort((a, b) => b.max.y - a.max.y);
  }

  // ── Carrousel : 1 PLV par catégorie ─────────────────────────────────────────
  private buildCarousel(): void {
    if (!this.scene || !this.templateScene) return;
    this.clearCarousel();

    // Regroupe les produits par type.
    const byType = new Map<ShopProductType, ShopProductDto[]>();
    for (const p of this.products()) {
      const t = p.type as ShopProductType;
      (byType.get(t) ?? byType.set(t, []).get(t)!).push(p);
    }
    // Un carton par type connu (même vide) → le carrousel est toujours visible avec ses voisins.
    const cats: PlvCategory[] = (Object.keys(PRODUCT_TYPE_LABELS) as ShopProductType[])
      .map(t => ({ type: t, label: PRODUCT_TYPE_LABELS[t], products: byType.get(t) ?? [] }));

    this.categories.set(cats);
    if (this.activeIndex() >= cats.length) this.activeIndex.set(Math.max(0, cats.length - 1));
    // Se placer sur la 1re catégorie non vide si l'active est vide.
    if (!cats[this.activeIndex()]?.products.length) {
      const firstWithProducts = cats.findIndex(c => c.products.length);
      if (firstWithProducts >= 0) this.activeIndex.set(firstWithProducts);
    }

    const root = new THREE.Group();
    root.name = '__plvs';
    this.pickables = [];

    // Au moins 1 PLV même sans produit (le carton vide).
    const count = Math.max(1, cats.length);
    for (let c = 0; c < count; c++) {
      const offsetX = c * this.plvSpacing;
      const clone = this.templateScene.clone(true);
      clone.position.x = offsetX;
      clone.traverse(o => { o.userData['plvIndex'] = c; if ((o as THREE.Mesh).isMesh) this.pickables.push(o); });
      root.add(clone);

      const cat = cats[c];
      if (cat) this.addCards(root, cat.products, c, offsetX);
    }

    this.scene.add(root);
  }

  private addCards(root: THREE.Group, products: ShopProductDto[], plvIndex: number, offsetX: number): void {
    let idx = 0;
    for (const slot of this.shelfSlots()) {
      if (idx >= products.length) break;
      const rowItems = products.slice(idx, idx + this.perShelf);
      idx += this.perShelf;

      const restY = slot.topY + slot.cardH / 2 + 0.002;
      const rowWidth = rowItems.length * slot.slotW;
      const startX = slot.cx - rowWidth / 2 + slot.slotW / 2;

      const levitate = slot.cardH * 0.14; // la carte flotte au-dessus de l'étagère
      rowItems.forEach((p, i) => {
        const mesh = this.makeCardMesh(p, slot.cardW, slot.cardH);
        const bx = offsetX + startX + i * slot.slotW;
        const by = restY + levitate;
        const bz = slot.frontZ;
        const baseRotY = this.frontSign > 0 ? 0 : Math.PI;
        mesh.position.set(bx, by, bz);
        mesh.rotation.y = baseRotY;
        mesh.userData['product'] = p;
        mesh.userData['plvIndex'] = plvIndex;
        mesh.userData['base'] = { x: bx, y: by, z: bz, rotY: baseRotY, phase: Math.random() * Math.PI * 2 };
        root.add(mesh);
        this.pickables.push(mesh);
        this.cardMeshes.push(mesh);
      });
    }
  }

  private shelfSlots(): {
    cx: number; topY: number; frontZ: number; centerY: number;
    slotW: number; cardW: number; cardH: number;
  }[] {
    // Espacement vertical standard entre étagères (médiane) → taille de carte uniforme
    // (évite la carte minuscule sur l'étagère du haut qui a peu de dégagement).
    const tops = this.shelfBoxes.map(s => s.max.y);
    const gaps: number[] = [];
    for (let i = 1; i < tops.length; i++) gaps.push(tops[i - 1] - tops[i]);
    const spacing = gaps.length ? gaps.slice().sort((a, b) => a - b)[Math.floor(gaps.length / 2)] : 0.28;

    const shelfWidth = this.shelfBoxes[0].max.x - this.shelfBoxes[0].min.x;
    const slotW = shelfWidth / this.perShelf;
    let cardW = slotW * 0.86;
    let cardH = cardW * (3.5 / 2.5);
    const maxH = spacing * 0.85;
    if (cardH > maxH) { cardH = maxH; cardW = cardH * (2.5 / 3.5); }

    return this.shelfBoxes.map(s => {
      const topY = s.max.y;
      const depth = s.max.z - s.min.z;
      const frontZ = this.frontSign > 0 ? s.max.z - depth * 0.25 : s.min.z + depth * 0.25;
      return { cx: (s.min.x + s.max.x) / 2, topY, frontZ, centerY: topY + cardH / 2, slotW, cardW, cardH };
    });
  }

  private makeCardMesh(p: ShopProductDto, w: number, h: number): THREE.Mesh {
    const geom = new THREE.PlaneGeometry(w, h);
    const material = new THREE.MeshBasicMaterial({ color: 0x1a2a55, side: THREE.DoubleSide, transparent: true });
    const mesh = new THREE.Mesh(geom, material);
    const imgUrl = p.photoUrl || p.cardImage;
    if (imgUrl) {
      const src = /^https?:\/\//i.test(imgUrl)
        ? `${environment.apiBaseUrl}/v1/media/image?url=${encodeURIComponent(imgUrl)}`
        : this.absoluteUrl(imgUrl);
      this.textureLoader.setCrossOrigin('anonymous');
      this.textureLoader.load(src, tex => {
        tex.colorSpace = THREE.SRGBColorSpace;
        material.color.set(0xffffff);
        material.map = tex;
        material.needsUpdate = true;
      });
    }
    return mesh;
  }

  // ── Caméra : vertical (étagères) + horizontal (catégorie active) ────────────
  private buildCameraPath(): void {
    this.camKeyframes = [];
    if (!this.camera || !this.shelfBoxes.length) return;
    const fov = (this.camera.fov * Math.PI) / 180;
    const frontZmodel = this.frontSign > 0 ? this.modelBox.max.z : this.modelBox.min.z;

    this.shelfSlots().forEach((slot, i) => {
      // Cadrage basé sur la hauteur d'une carte ; recul progressif vers le bas.
      const viewH = slot.cardH * this.cardZoom * (1 + i * 0.12);
      const distH = (viewH / 2) / Math.tan(fov / 2);
      const camDist = Math.max(distH, 0.3); // reste devant le carton
      const cy = slot.topY + slot.cardH / 2;
      const look = new THREE.Vector3(slot.cx, cy, slot.frontZ);
      const pos = new THREE.Vector3(slot.cx, cy, frontZmodel + this.frontSign * camDist);
      this.camKeyframes.push({ pos, look });
    });
    this.camera.position.copy(this.camKeyframes[0].pos);
    this.currentLook.copy(this.camKeyframes[0].look);
  }

  private updateCamera(): void {
    if (!this.camera || this.camKeyframes.length === 0) return;
    const activeX = this.activeIndex() * this.plvSpacing;
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
    this.desiredPos.x += activeX;
    this.desiredLook.x += activeX;
    this.camera.position.lerp(this.desiredPos, 0.1);
    this.currentLook.lerp(this.desiredLook, 0.1);
    this.camera.lookAt(this.currentLook);
  }

  private readonly onWheel = (e: WheelEvent): void => {
    if (this.camKeyframes.length < 2) return;
    e.preventDefault();
    this.scrollT = Math.min(1, Math.max(0, this.scrollT + e.deltaY / 1400));
  };

  private readonly onMove = (e: MouseEvent): void => {
    if (!this.renderer) return;
    const rect = this.renderer.domElement.getBoundingClientRect();
    this.hoverPointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    this.hoverPointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    this.pointerInside = true;
  };
  private readonly onLeave = (): void => { this.pointerInside = false; };

  /** Lévitation douce de toutes les cartes + la carte survolée sort et suit le curseur. */
  private updateCards(): void {
    if (!this.cardMeshes.length || !this.camera) return;
    const t = this.clock.getElapsedTime();

    let hovered: THREE.Mesh | null = null;
    if (this.pointerInside) {
      this.raycaster.setFromCamera(this.hoverPointer, this.camera);
      const hits = this.raycaster.intersectObjects(this.cardMeshes, false);
      if (hits.length) hovered = hits[0].object as THREE.Mesh;
    }

    for (const m of this.cardMeshes) {
      const b = m.userData['base'] as { x: number; y: number; z: number; rotY: number; phase: number };
      const bob = Math.sin(t * 1.6 + b.phase) * 0.012; // flottement vertical
      let ty = b.y + bob, tz = b.z, rotY = b.rotY, rotX = 0;

      if (m === hovered) {
        ty = b.y + bob + 0.05;                 // se soulève
        tz = b.z + this.frontSign * 0.08;      // sort vers la caméra
        rotY = b.rotY + this.hoverPointer.x * 0.45; // suit le curseur (gauche/droite)
        rotX = -this.hoverPointer.y * 0.30;         // suit le curseur (haut/bas)
      }

      m.position.x += (b.x - m.position.x) * 0.2;
      m.position.y += (ty - m.position.y) * 0.2;
      m.position.z += (tz - m.position.z) * 0.2;
      m.rotation.y += (rotY - m.rotation.y) * 0.2;
      m.rotation.x += (rotX - m.rotation.x) * 0.2;
    }
  }

  // ── Clic : carte → panneau ; PLV voisin → switch ────────────────────────────
  private readonly onClick = (e: MouseEvent): void => {
    if (!this.renderer || !this.camera || !this.pickables.length) return;
    const rect = this.renderer.domElement.getBoundingClientRect();
    this.pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    this.pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const hits = this.raycaster.intersectObjects(this.pickables, false);
    if (!hits.length) return;

    const obj = hits[0].object;
    const plvIndex = this.findUp(obj, 'plvIndex') as number | undefined;
    const product = obj.userData['product'] as ShopProductDto | undefined;

    if (product) {
      if (plvIndex != null && plvIndex !== this.activeIndex()) this.setActive(plvIndex);
      this.openPanel(product);
    } else if (plvIndex != null && plvIndex !== this.activeIndex()) {
      this.setActive(plvIndex);
    }
  };

  private findUp(obj: THREE.Object3D | null, key: string): unknown {
    let o = obj;
    while (o) {
      if (o.userData[key] !== undefined) return o.userData[key];
      o = o.parent;
    }
    return undefined;
  }

  // ── Panneau latéral ─────────────────────────────────────────────────────────
  private openPanel(p: ShopProductDto): void {
    this.selected.set(p);
    this.interested.set(null);
    this.interestLoading.set(true);
    this.service.getProductInterest(p.id).subscribe({
      next: r => { this.interested.set(r.interestedCount); this.interestLoading.set(false); },
      error: () => { this.interested.set(0); this.interestLoading.set(false); },
    });
  }
  closePanel(): void { this.selected.set(null); }

  notifyInterested(p: ShopProductDto): void {
    // Phase B : branchera le système de notifications in-app.
    this.toast.info(`Notifications in-app à venir — ${this.interested() ?? 0} client(s) pour « ${p.cardName || p.productName} ».`);
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
    this.buildCameraPath();
  }

  // ── Nettoyage ────────────────────────────────────────────────────────────────
  private clearCarousel(): void {
    const g = this.scene?.getObjectByName('__plvs');
    if (g && this.scene) {
      g.traverse(o => {
        const m = o as THREE.Mesh;
        if (m.userData['product'] && m.geometry) {
          m.geometry.dispose();
          const mat = m.material as THREE.MeshBasicMaterial;
          mat.map?.dispose();
          mat.dispose();
        }
      });
      this.scene.remove(g);
    }
    this.pickables = [];
    this.cardMeshes = [];
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
