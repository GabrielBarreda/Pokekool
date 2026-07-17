import {
  Component, ElementRef, inject, input, OnDestroy, output, signal, viewChild, effect,
} from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { environment } from '@env/environment';
import { StorageService } from '../storage.service';
import { loadGltfClone } from '../gltf-cache';
import { tween } from '../anim';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import {
  StorageBoxDto, StorageBinderDto, StorageFurnitureDto, StorageRoomDto, StorageCompileJobDto,
  Box3dPosition, BoxDropEvent, FurnitureDropEvent, FurnitureFloorDropEvent, BinderBoxDropEvent,
  STORAGE_ASSETS, FURNITURE_ASSET, FURNITURE_LEVEL_Y, FURNITURE_HALF_WIDTH, FurnitureType,
} from '../storage.models';

/**
 * Viewer 3D de la salle : caméra à l'intérieur, meubles + cartons + classeurs.
 * - Meubles rendus depuis leurs .glb fixes, positionnés via Position3dJson.
 * - Cartons/classeurs posés au sol (Position3dJson) ou aimantés à un niveau de meuble.
 * - Drag&drop : meuble → sol ; carton/classeur → niveau de meuble (snap) ou sol.
 * - Clic droit sur un carton ou un meuble → déplacement libre (caméra verrouillée),
 *   clic gauche valide, Échap annule.
 */
@Component({
  selector: 'app-storage-room-viewer',
  standalone: true,
  imports: [HasPermissionDirective],
  template: `
    <div class="viewer" #viewerRoot>
      <div class="viewer__stage">
        <!-- Barre d'outils overlay, ancrée en haut-gauche DANS la vue 3D -->
        <div class="viewer__overlay">
          <span class="viewer__title">Vue 3D — {{ room()?.name }}</span>
          <div class="viewer__actions">
            @if (stage() === 'carton') {
              <button class="btn-ghost" (click)="exitCarton()">← Retour salle</button>
            } @else if (hasModel()) {
              <button class="btn-ghost" (click)="resetCamera()" title="Réinitialiser la vue caméra">⟲ Vue</button>
            }
            <label *hasPermission="'storage:write'" class="btn-ghost btn-upload">
              ⤓ Modèle (.blend)
              <input type="file" accept=".blend" hidden (change)="onFileSelected($event)" [disabled]="uploading()" />
            </label>
            <button class="btn-ghost" (click)="toggleFullscreen()" title="Plein écran">
              @if (isFullscreen()) { ⤢ Quitter } @else { ⛶ Plein écran }
            </button>
          </div>
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
          @if (jobError(); as err) { <span class="viewer__error">Erreur : {{ err }}</span> }
        </div>

        @if (!hasModel() && !loading()) {
          <div class="viewer__empty">
            <div class="viewer__empty-icon">🏬</div>
            <p>Aucun modèle 3D — uploadez un fichier .blend de salle.</p>
          </div>
        }
        <div #host class="viewer__canvas" [class.hidden]="!hasModel()" [class.moving]="moving()"
             (dragenter)="onDragEnter($event)" (dragover)="onDragOver($event)"
             (dragleave)="onDragLeave()" (drop)="onDrop($event)"></div>
        @if (loading()) { <div class="viewer__loading">Chargement du modèle…</div> }
        <div class="viewer__hint">
          @if (moving()) { <strong>Déplacement</strong> — relâchez pour poser · Échap pour annuler }
          @else if (stage() === 'carton') { Carton ouvert (vue du dessus) — cliquez un classeur · Échap ou « Retour salle » pour revenir }
          @else { Cliquez-glissez un objet pour le déplacer · clic pour le sélectionner · glisser depuis la liste pour ajouter }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .viewer { display: flex; flex-direction: column; height: 100%; }
    .viewer__stage { position: relative; flex: 1; min-height: 400px; max-height: 620px;
      border: 1px solid var(--color-border-default); border-radius: var(--radius-lg); overflow: hidden; background: #14141a; }
    .viewer__canvas { width: 100%; height: 100%; }
    .viewer__canvas.hidden { display: none; }
    .viewer__canvas.moving { cursor: grabbing; }

    /* Barre d'outils flottante en haut-gauche, DANS la vue 3D */
    .viewer__overlay { position: absolute; top: .6rem; left: .6rem; z-index: 5;
      display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; max-width: calc(100% - 280px); }
    .viewer__title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
      color: #fff; background: rgba(0,0,0,.42); padding: .28rem .55rem; border-radius: var(--radius-md); }
    .viewer__actions { display: flex; align-items: center; gap: .35rem; }
    .badge { font-size: var(--font-size-xs); padding: .22rem .5rem; border-radius: var(--radius-sm); background: rgba(255,255,255,.85); }
    .badge--run { background: #fff3cd; color: #7a5b00; }
    .badge--ok  { background: #d4edda; color: #155724; }
    .badge--ko  { background: #f8d7da; color: #721c24; }
    .viewer__error { background: #f8d7da; color: #721c24; padding: .22rem .5rem; border-radius: var(--radius-sm); font-size: var(--font-size-xs); }
    .viewer__empty { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #9aa; gap: .5rem; text-align: center; padding: 1rem; }
    .viewer__empty-icon { font-size: 2.5rem; }
    .viewer__loading { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #ccd; }
    .viewer__hint { position: absolute; left: 0; right: 0; bottom: 0; padding: .35rem .6rem; font-size: var(--font-size-xs); color: #cdd; background: rgba(0,0,0,.4); text-align: center; }

    /* Boutons overlay : verre clair lisible sur le canvas sombre */
    .btn-ghost { display: inline-flex; align-items: center; height: 30px; padding: 0 .6rem; border-radius: var(--radius-md);
      background: rgba(255,255,255,.92); border: 1px solid rgba(0,0,0,.08); color: #333;
      font-size: var(--font-size-xs); cursor: pointer; white-space: nowrap; }
    .btn-ghost:hover { background: #fff; }
    .btn-upload { cursor: pointer; }
  `],
})
export class StorageRoomViewerComponent implements OnDestroy {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly room = input<StorageRoomDto | null>(null);
  readonly boxes = input<StorageBoxDto[]>([]);
  readonly selectedBoxId = input<string | null>(null);
  readonly binders = input<StorageBinderDto[]>([]);        // classeurs du carton sélectionné (in-box)
  readonly furniture = input<StorageFurnitureDto[]>([]);
  readonly looseBinders = input<StorageBinderDto[]>([]);    // classeurs posés directement sur meuble
  /** Quand true : les meubles ne sont pas déplaçables (hit-test les ignore). */
  readonly furnitureLocked = input(false);

  readonly boxSelected = output<StorageBoxDto>();
  readonly binderSelected = output<StorageBinderDto>();         // clic sur un classeur posé (vue salle)
  readonly boxDropped = output<BoxDropEvent>();                 // carton posé au sol
  readonly furnitureFloorDropped = output<FurnitureFloorDropEvent>(); // meuble posé au sol
  readonly itemDroppedOnFurniture = output<FurnitureDropEvent>();     // carton/classeur aimanté à un niveau
  readonly binderDroppedOnBox = output<BinderBoxDropEvent>();  // classeur déposé sur un carton → rangé dedans
  readonly binderOpened = output<string>();                    // clic sur un classeur dans le carton focus

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');
  private readonly viewerRoot = viewChild.required<ElementRef<HTMLDivElement>>('viewerRoot');
  readonly isFullscreen = signal(false);

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly hasModel = signal(false);
  readonly moving = signal(false);
  readonly stage = signal<'room' | 'carton'>('room');   // niveau de zoom (drill-down)
  readonly jobStatus = signal<string | null>(null);
  readonly jobError = signal<string | null>(null);

  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private controls?: OrbitControls;
  private raycaster = new THREE.Raycaster();
  private pointer = new THREE.Vector2();
  private floorPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  private frameId = 0;
  private resizeObserver?: ResizeObserver;
  private itemsGroup = new THREE.Group();
  private boxMeshes: THREE.Object3D[] = [];
  private binderMeshes: THREE.Object3D[] = [];   // classeurs posés (vue salle), cliquables/déplaçables
  private furnitureMeshes: THREE.Object3D[] = [];
  private furnitureBounds: { id: string; type: FurnitureType; box: THREE.Box3; baseY: number; cx: number; cz: number }[] = [];
  private ghost?: THREE.Group;                        // aperçu du niveau visé pendant le drag
  private buildToken = 0;
  private pollHandle?: ReturnType<typeof setInterval>;

  private roomRoot?: THREE.Object3D;
  private roomBounds?: THREE.Box3;
  private loadedModelUrl: string | null = null;   // évite de recharger/recadrer à chaque reload de layout

  // ── Drill-down (cinématique carton) ──────────────────────────────────────
  private focusGroup?: THREE.Group;
  private focusSource?: THREE.Vector3;
  private cancelAnim?: () => void;
  private savedCam?: { pos: THREE.Vector3; target: THREE.Vector3; min: number; max: number };
  private focusBinderMeshes: THREE.Object3D[] = [];
  private hoveredBinder?: THREE.Object3D;
  private hoverBaseY = 0;
  private pendingBinderClick: { id: string; sx: number; sy: number } | null = null;
  private static readonly WALL_MARGIN = 0.5;       // marge intérieure (murs)

  // move mode
  private movingKind: 'box' | 'furniture' | 'binder' | null = null;
  private movingId: string | null = null;
  private movingObj?: THREE.Object3D;
  private movingOrigin = new THREE.Vector3();
  private movingSnap: { furnitureId: string; level: number } | null = null;
  private movingOntoBoxId: string | null = null;   // classeur survolant un carton pendant le déplacement
  private pendingGrab: { kind: 'box' | 'furniture' | 'binder'; id: string; obj: THREE.Object3D; sx: number; sy: number } | null = null;

  constructor() {
    effect(() => {
      const room = this.room();
      if (!room) return;
      // Ne recharge le modèle (et ne recadre la caméra) que si l'URL change réellement.
      if (this.renderer && room.model3dUrl === this.loadedModelUrl) return;
      queueMicrotask(() => this.loadRoom(room));
    });
    effect(() => {
      this.boxes(); this.selectedBoxId(); this.binders(); this.furniture(); this.looseBinders();
      if (this.scene && !this.movingId) queueMicrotask(() => this.rebuild());
    });
    // Met à jour les classeurs du carton focus quand la liste arrive (chargée après le clic).
    effect(() => {
      const list = this.binders();
      if (this.stage() === 'carton' && this.focusGroup) queueMicrotask(() => this.refreshFocusBinders(list));
    });
    document.addEventListener('fullscreenchange', this.onFsChange);
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.cancelAnim?.();
    window.removeEventListener('keydown', this.onKeyDown);
    window.removeEventListener('pointermove', this.onPointerMove);
    window.removeEventListener('pointerup', this.onPointerUp);
    document.removeEventListener('fullscreenchange', this.onFsChange);
    this.disposeScene();
  }

  // ── Scène ────────────────────────────────────────────────────────────────
  private loadRoom(room: StorageRoomDto): void {
    if (!room.model3dUrl) { this.hasModel.set(false); this.disposeScene(); return; }
    this.hasModel.set(true);
    this.ensureScene();
    this.loading.set(true);
    new GLTFLoader().load(
      this.absoluteUrl(room.model3dUrl),
      gltf => {
        this.clearNamed('__room');
        gltf.scene.name = '__room';
        this.scene!.add(gltf.scene);
        this.roomRoot = gltf.scene;
        this.loadedModelUrl = room.model3dUrl;
        this.frameCameraToRoom(gltf.scene);
        this.loading.set(false);
        this.rebuild();
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
    this.scene.add(this.itemsGroup);

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
    this.controls.maxPolarAngle = Math.PI * 0.52;
    // OrbitControls par défaut : gauche = rotate, droit = pan, molette = zoom.
    // Le grab d'un item est géré en amont sur pointerdown (hit-test bbox) et désactive
    // temporairement les controls, donc pas besoin de toucher aux boutons.

    this.scene.add(new THREE.AmbientLight(0xffffff, 0.9));
    const dir = new THREE.DirectionalLight(0xffffff, 0.8);
    dir.position.set(4, 8, 5);
    this.scene.add(dir);

    const canvas = this.renderer.domElement;
    // capture:true → notre hit-test s'exécute AVANT OrbitControls, pour geler la caméra
    // dès qu'on saisit un item (sinon un micro-rotate se déclencherait).
    canvas.addEventListener('pointerdown', this.onPointerDown, { capture: true });
    window.addEventListener('pointermove', this.onPointerMove);
    window.addEventListener('pointerup', this.onPointerUp);
    window.addEventListener('keydown', this.onKeyDown);

    this.resizeObserver = new ResizeObserver(() => this.onResize());
    this.resizeObserver.observe(el);

    const loop = () => {
      this.frameId = requestAnimationFrame(loop);
      if (this.hoveredBinder) {
        this.hoveredBinder.position.y = this.hoverBaseY + Math.abs(Math.sin(performance.now() * 0.006)) * 0.05;
      }
      this.controls?.update();
      this.renderer!.render(this.scene!, this.camera!);
    };
    loop();
  }

  private frameCameraToRoom(root: THREE.Object3D): void {
    if (!this.camera || !this.controls) return;
    const bbox = new THREE.Box3().setFromObject(root);
    this.roomBounds = bbox;
    const center = bbox.getCenter(new THREE.Vector3());
    const size = bbox.getSize(new THREE.Vector3());
    const eye = Math.min(1.7, size.y * 0.55);
    this.camera.position.set(center.x, eye, bbox.max.z - 0.4);
    this.controls.target.set(center.x, eye, center.z);
    this.controls.maxDistance = Math.max(size.x, size.z) * 1.1;
    this.controls.update();
  }

  private readonly onFsChange = (): void => this.isFullscreen.set(!!document.fullscreenElement);

  toggleFullscreen(): void {
    if (document.fullscreenElement) { void document.exitFullscreen(); return; }
    void this.viewerRoot().nativeElement.requestFullscreen?.();
  }

  /** Réinitialise la caméra à la vue par défaut de la salle. */
  resetCamera(): void {
    if (this.roomRoot) this.frameCameraToRoom(this.roomRoot);
  }

  /** Contraint un point (x,z) à l'intérieur des murs de la salle (marge sur le centre). */
  private clampToRoom(v: THREE.Vector3): THREE.Vector3 {
    const b = this.roomBounds;
    if (!b) return v;
    const m = StorageRoomViewerComponent.WALL_MARGIN;
    v.x = Math.min(b.max.x - m, Math.max(b.min.x + m, v.x));
    v.z = Math.min(b.max.z - m, Math.max(b.min.z + m, v.z));
    return v;
  }

  /**
   * Contraint un objet à l'intérieur des murs en tenant compte de son EMPRISE réelle
   * (bounding box) : le vide restant vers chaque mur = WALL_MARGIN, identique quelle que
   * soit la forme/rotation de l'objet.
   */
  private clampObjectToRoom(obj: THREE.Object3D): void {
    const b = this.roomBounds;
    if (!b) return;
    obj.updateMatrixWorld(true);
    const bb = new THREE.Box3().setFromObject(obj);
    const hx = (bb.max.x - bb.min.x) / 2;
    const hz = (bb.max.z - bb.min.z) / 2;
    const m = StorageRoomViewerComponent.WALL_MARGIN;
    const cx = (b.min.x + b.max.x) / 2;
    const cz = (b.min.z + b.max.z) / 2;
    const minX = b.min.x + m + hx, maxX = b.max.x - m - hx;
    const minZ = b.min.z + m + hz, maxZ = b.max.z - m - hz;
    obj.position.x = maxX >= minX ? Math.min(maxX, Math.max(minX, obj.position.x)) : cx;
    obj.position.z = maxZ >= minZ ? Math.min(maxZ, Math.max(minZ, obj.position.z)) : cz;
  }

  // ── Reconstruction meubles + cartons + classeurs ───────────────────────────
  private async rebuild(): Promise<void> {
    if (!this.scene) return;
    const token = ++this.buildToken;
    this.itemsGroup.clear();
    this.boxMeshes = [];
    this.binderMeshes = [];
    this.furnitureMeshes = [];
    this.furnitureBounds = [];

    // 1) Meubles
    const furnitureById = new Map<string, StorageFurnitureDto>();
    for (const f of this.furniture()) {
      furnitureById.set(f.id, f);
      const pos = this.parsePosition(f.position3dJson);
      if (!pos) continue;
      let mesh: THREE.Object3D;
      try { mesh = await loadGltfClone(FURNITURE_ASSET[f.type]); }
      catch { continue; }
      if (token !== this.buildToken) return;
      mesh.position.set(pos.x, pos.y, pos.z);
      mesh.rotation.y = pos.rotation ?? 0;
      mesh.traverse(o => { o.userData['furnitureId'] = f.id; o.userData['furnitureType'] = f.type; });
      this.itemsGroup.add(mesh);
      this.furnitureMeshes.push(mesh);

      // Boîte englobante monde (cible de dépôt "clip", indépendante du raycast mesh).
      mesh.updateMatrixWorld(true);
      const bb = new THREE.Box3().setFromObject(mesh);
      bb.expandByScalar(0.08); // survol plus tolérant
      const c = bb.getCenter(new THREE.Vector3());
      this.furnitureBounds.push({ id: f.id, type: f.type, box: bb, baseY: pos.y, cx: c.x, cz: c.z });
    }

    // 2) Cartons — répartition par niveau pour éviter le chevauchement
    const selId = this.selectedBoxId();
    const perLevel = new Map<string, number>(); // clé furnitureId|level -> compteur
    for (const box of this.boxes()) {
      const place = this.resolveItemPlacement(box.furnitureId, box.levelIndex, box.position3dJson, furnitureById, perLevel);
      if (!place) continue;
      const isSel = box.id === selId;
      let carton: THREE.Object3D;
      try { carton = await loadGltfClone(isSel ? STORAGE_ASSETS.cartonOpen : STORAGE_ASSETS.cartonClosed); }
      catch {
        carton = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.4, 0.4), new THREE.MeshStandardMaterial({ color: 0x8a5a2b }));
      }
      if (token !== this.buildToken) return;
      carton.position.copy(place.pos);
      carton.rotation.y = place.rotY;
      carton.traverse(o => (o.userData['boxId'] = box.id));
      this.itemsGroup.add(carton);
      this.boxMeshes.push(carton);
      if (isSel) await this.addInBoxBinders(place.pos, token);
    }

    // 3) Classeurs posés directement sur un meuble
    for (const b of this.looseBinders()) {
      const place = this.resolveItemPlacement(b.furnitureId, b.levelIndex, null, furnitureById, perLevel);
      if (!place) continue;
      let binder: THREE.Object3D;
      try { binder = await loadGltfClone(STORAGE_ASSETS.binderClosed); }
      catch { continue; }
      if (token !== this.buildToken) return;
      binder.rotation.x = -Math.PI / 2;   // debout comme un classeur rangé
      binder.rotation.z = place.rotY;
      binder.position.set(place.pos.x, place.pos.y + 0.14, place.pos.z);
      binder.traverse(o => (o.userData['binderId'] = b.id));
      this.itemsGroup.add(binder);
      this.binderMeshes.push(binder);
    }
  }

  /** Classeurs du carton sélectionné, dessinés à l'intérieur du carton ouvert. */
  private async addInBoxBinders(cartonPos: THREE.Vector3, token: number): Promise<void> {
    const list = this.binders();
    if (list.length === 0) return;
    const n = Math.min(list.length, 8);
    for (let i = 0; i < n; i++) {
      let binder: THREE.Object3D;
      try { binder = await loadGltfClone(STORAGE_ASSETS.binderClosed); }
      catch { continue; }
      if (token !== this.buildToken) return;
      this.layoutBinderInCarton(binder, i, n, cartonPos.x, cartonPos.z, cartonPos.y + 0.06);
      this.itemsGroup.add(binder);
    }
  }

  /** Empreinte utile intérieure d'un carton (X × Z, mètres) pour ranger les classeurs à plat. */
  private static readonly CARTON_INNER_W = 0.44;
  private static readonly CARTON_INNER_D = 0.32;
  private static readonly CARTON_COLS = 2;
  private static readonly CARTON_MAX = 8;

  /**
   * Range un classeur à plat dans un carton, en vraie **grille 2 colonnes** remplie
   * de gauche à droite puis de haut en bas (1,2 / 3,4 / …). Chaque classeur est mis
   * à l'échelle pour tenir dans sa cellule → aucun débordement du carton.
   */
  private layoutBinderInCarton(
    binder: THREE.Object3D, i: number, count: number, cx: number, cz: number, baseY: number,
  ): void {
    const V = StorageRoomViewerComponent;
    const n = Math.min(count, V.CARTON_MAX);
    const cols = V.CARTON_COLS;
    const rows = Math.max(1, Math.ceil(n / cols));
    const col = i % cols;
    const row = Math.floor(i / cols);
    const cellW = V.CARTON_INNER_W / cols;
    const cellD = V.CARTON_INNER_D / rows;

    // À plat (couverture vers le haut = rotation par défaut du modèle), orienté portrait
    // (grand axe selon Z = haut→bas de l'écran en vue du dessus).
    binder.rotation.set(0, 0, 0);
    binder.scale.setScalar(1);
    binder.updateMatrixWorld(true);
    let size = new THREE.Box3().setFromObject(binder).getSize(new THREE.Vector3());
    if (size.x > size.z) {
      binder.rotation.y = Math.PI / 2;               // met le grand côté selon Z
      binder.updateMatrixWorld(true);
      size = new THREE.Box3().setFromObject(binder).getSize(new THREE.Vector3());
    }

    // Mise à l'échelle pour tenir dans la cellule (petite marge pour l'espacement).
    const s = Math.min((cellW * 0.86) / (size.x || 1), (cellD * 0.86) / (size.z || 1));
    if (isFinite(s) && s > 0) binder.scale.setScalar(s);

    const gx = (col - (cols - 1) / 2) * cellW;        // gauche → droite
    const gz = (row - (rows - 1) / 2) * cellD;        // haut (fond) → bas (avant)
    binder.position.set(cx + gx, baseY, cz + gz);
  }

  /**
   * Position monde d'un item : sur un meuble (furnitureId+level, réparti en X) ou au sol (position3dJson).
   */
  private resolveItemPlacement(
    furnitureId: string | null,
    levelIndex: number | null,
    position3dJson: string | null,
    furnitureById: Map<string, StorageFurnitureDto>,
    perLevel: Map<string, number>,
  ): { pos: THREE.Vector3; rotY: number } | null {
    if (furnitureId) {
      const f = furnitureById.get(furnitureId);
      if (!f) return null;
      const fpos = this.parsePosition(f.position3dJson);
      if (!fpos) return null;
      const level = levelIndex ?? 0;
      const levelY = FURNITURE_LEVEL_Y[f.type][Math.min(level, FURNITURE_LEVEL_Y[f.type].length - 1)] ?? 0;
      const key = `${furnitureId}|${level}`;
      const slotIdx = perLevel.get(key) ?? 0;
      perLevel.set(key, slotIdx + 1);
      const half = FURNITURE_HALF_WIDTH[f.type];
      const slotX = f.type === 'ShelfWall'
        ? -half + 0.35 + slotIdx * 0.5      // rangée le long de l'étagère murale
        : (slotIdx % 2 === 0 ? -0.16 : 0.16); // 2 cartons par niveau sur la classique
      const r = fpos.rotation ?? 0;
      const wx = slotX * Math.cos(r);
      const wz = -slotX * Math.sin(r);
      return { pos: new THREE.Vector3(fpos.x + wx, fpos.y + levelY, fpos.z + wz), rotY: r };
    }
    const p = this.parsePosition(position3dJson);
    if (!p) return null;
    return { pos: new THREE.Vector3(p.x, p.y, p.z), rotY: p.rotation ?? 0 };
  }

  // ── Interactions (presser-glisser-relâcher) ─────────────────────────────────
  // pointerdown sur un item → on prépare un grab et on gèle la caméra ;
  // au-delà d'un petit seuil de déplacement → vrai drag ; sinon (relâché sans bouger) → sélection.
  // pointerdown dans le vide → OrbitControls (rotation/pan) comme d'habitude.
  private readonly onPointerDown = (event: PointerEvent): void => {
    if (!this.renderer || !this.camera) return;
    if (this.stage() === 'carton') { if (event.button === 0) this.cartonPointerDown(event); return; }
    if (event.button !== 0) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);

    const boxHit = this.raycaster.intersectObjects(this.boxMeshes, true)[0];
    if (boxHit) {
      const id = boxHit.object.userData['boxId'] as string;
      const obj = this.boxMeshes.find(m => m.userData['boxId'] === id);
      if (obj) { this.pendingGrab = { kind: 'box', id, obj, sx: event.clientX, sy: event.clientY }; this.setControlsEnabled(false); }
      return;
    }

    // Classeurs posés (petits, souvent sur un meuble) : à tester avant les meubles.
    const binderHit = this.raycaster.intersectObjects(this.binderMeshes, true)[0];
    if (binderHit) {
      const id = binderHit.object.userData['binderId'] as string;
      const obj = this.binderMeshes.find(m => m.userData['binderId'] === id);
      if (obj) { this.pendingGrab = { kind: 'binder', id, obj, sx: event.clientX, sy: event.clientY }; this.setControlsEnabled(false); }
      return;
    }

    if (!this.furnitureLocked()) {
      const pick = this.pickFurniture();
      if (pick) {
        const obj = this.furnitureMeshes.find(m => m.userData['furnitureId'] === pick.id);
        if (obj) { this.pendingGrab = { kind: 'furniture', id: pick.id, obj, sx: event.clientX, sy: event.clientY }; this.setControlsEnabled(false); }
      }
    }
    // sinon : rien → la caméra tourne (OrbitControls)
  };

  private readonly onPointerUp = (): void => {
    if (this.stage() === 'carton') {
      if (this.pendingBinderClick) {
        const id = this.pendingBinderClick.id;
        this.pendingBinderClick = null;
        this.openBinder(id);
      }
      return;
    }
    if (this.movingId) {
      this.finishMove();
    } else if (this.pendingGrab) {
      // relâché sans avoir bougé → sélection seule (le récap propose « Ouvrir »)
      const grab = this.pendingGrab;
      if (grab.kind === 'box') {
        const box = this.boxes().find(b => b.id === grab.id);
        if (box) this.boxSelected.emit(box);
      } else if (grab.kind === 'binder') {
        const binder = this.looseBinders().find(b => b.id === grab.id);
        if (binder) this.binderSelected.emit(binder);
      }
      this.setControlsEnabled(true);
    }
    this.pendingGrab = null;
  };

  /** Ouvre (drill-down cinématique) le carton d'id donné, commandé depuis le récap. */
  openBox(boxId: string): void {
    const obj = this.boxMeshes.find(m => m.userData['boxId'] === boxId);
    if (obj) this.enterCarton(obj);
  }

  private beginMove(kind: 'box' | 'furniture' | 'binder', id: string, obj: THREE.Object3D): void {
    this.movingKind = kind;
    this.movingId = id;
    this.movingObj = obj;
    this.movingOrigin.copy(obj.position);
    this.movingOntoBoxId = null;
    this.moving.set(true);
    if (kind === 'box') {
      const box = this.boxes().find(b => b.id === id);
      if (box) this.boxSelected.emit(box);
    } else if (kind === 'binder') {
      const binder = this.looseBinders().find(b => b.id === id);
      if (binder) this.binderSelected.emit(binder);
    }
  }

  private setControlsEnabled(v: boolean): void {
    if (this.controls) this.controls.enabled = v;
  }

  private readonly onPointerMove = (event: PointerEvent): void => {
    if (this.stage() === 'carton') { this.cartonPointerMove(event); return; }
    // Passe en vrai déplacement une fois le seuil franchi.
    if (this.pendingGrab && !this.movingId) {
      const dx = event.clientX - this.pendingGrab.sx, dy = event.clientY - this.pendingGrab.sy;
      if (dx * dx + dy * dy > 25) { // ~5 px
        this.beginMove(this.pendingGrab.kind, this.pendingGrab.id, this.pendingGrab.obj);
      }
    }
    if (!this.movingId || !this.movingObj || !this.camera) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);

    // Un classeur en déplacement s'aimante d'abord à un carton survolé (rangement dedans).
    if (this.movingKind === 'binder') {
      const boxHit = this.raycaster.intersectObjects(this.boxMeshes, true)[0];
      if (boxHit) {
        const boxId = boxHit.object.userData['boxId'] as string;
        const target = this.boxMeshes.find(m => m.userData['boxId'] === boxId);
        if (target) {
          this.movingObj.position.set(target.position.x, target.position.y + 0.28, target.position.z);
          this.movingOntoBoxId = boxId;
          this.movingSnap = null;
          return;
        }
      }
      this.movingOntoBoxId = null;
    }

    // Un carton/classeur en déplacement s'aimante à un niveau de meuble s'il le survole.
    if (this.movingKind === 'box' || this.movingKind === 'binder') {
      const pick = this.pickFurniture();
      if (pick) {
        const levels = FURNITURE_LEVEL_Y[pick.type];
        const level = this.nearestLevelIndex(pick.point.y - pick.baseY, pick.type);
        const levelY = levels[Math.min(level, levels.length - 1)] ?? 0;
        this.movingObj.position.set(pick.cx, pick.baseY + levelY, pick.cz);
        this.movingSnap = { furnitureId: pick.id, level };
        return;
      }
      this.movingSnap = null;
    }

    const p = new THREE.Vector3();
    if (this.raycaster.ray.intersectPlane(this.floorPlane, p)) {
      this.movingObj.position.set(p.x, 0, p.z);
      this.clampObjectToRoom(this.movingObj); // clamp sur l'emprise réelle
    }
  };

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    if (event.key !== 'Escape') return;
    if (this.stage() === 'carton') { this.exitCarton(); return; }
    if (this.movingId) this.cancelMove();
  };

  // ── Cinématique carton (drill-down étape 1) ─────────────────────────────────
  private enterCarton(sourceObj: THREE.Object3D): void {
    if (!this.camera || !this.controls || !this.scene || this.stage() === 'carton') return;
    this.cancelAnim?.();
    this.stage.set('carton');

    const b = this.roomBounds;
    const center = new THREE.Vector3(b ? (b.min.x + b.max.x) / 2 : 0, 0, b ? (b.min.z + b.max.z) / 2 : 0);

    // Groupe focus (clone) posé sur le carton source, masque le reste de la scène.
    const group = new THREE.Group();
    group.position.copy(sourceObj.position);
    group.rotation.y = sourceObj.rotation.y;
    this.scene.add(group);
    this.focusGroup = group;
    this.focusSource = sourceObj.position.clone();
    this.itemsGroup.visible = false;

    loadGltfClone(STORAGE_ASSETS.cartonOpen).then(carton => {
      if (this.focusGroup !== group) return;
      carton.name = '__fcarton';
      group.add(carton);
      this.refreshFocusBinders(this.binders());
    }).catch(() => {});

    // Sauvegarde caméra + cinématique.
    this.savedCam = {
      pos: this.camera.position.clone(), target: this.controls.target.clone(),
      min: this.controls.minDistance, max: this.controls.maxDistance,
    };
    this.controls.enabled = false;
    const fromPos = this.camera.position.clone();
    const fromTarget = this.controls.target.clone();
    const fromGroup = group.position.clone();
    const cartonCenter = center.clone().setY(0.18);
    const dest = this.framePose(cartonCenter, new THREE.Vector3(0.54, 0.36, 0.36), 0.85);

    this.cancelAnim = tween({
      durationMs: 650,
      onUpdate: k => {
        group.position.lerpVectors(fromGroup, center, k);
        this.camera!.position.lerpVectors(fromPos, dest.position, k);
        this.controls!.target.lerpVectors(fromTarget, dest.target, k);
        this.controls!.update();
      },
      onDone: () => {
        this.controls!.target.copy(dest.target);
        this.controls!.enabled = false;     // caméra verrouillée dans le carton (vue du dessus figée)
        this.controls!.update();
      },
    });
  }

  exitCarton(): void {
    if (!this.camera || !this.controls || this.stage() !== 'carton') return;
    this.cancelAnim?.();
    const group = this.focusGroup;
    const saved = this.savedCam;
    const fromPos = this.camera.position.clone();
    const fromTarget = this.controls.target.clone();
    const fromGroup = group?.position.clone();
    const toGroup = this.focusSource?.clone();
    this.controls.enabled = false;

    this.cancelAnim = tween({
      durationMs: 550,
      onUpdate: k => {
        if (group && fromGroup && toGroup) group.position.lerpVectors(fromGroup, toGroup, k);
        if (saved) {
          this.camera!.position.lerpVectors(fromPos, saved.pos, k);
          this.controls!.target.lerpVectors(fromTarget, saved.target, k);
          this.controls!.update();
        }
      },
      onDone: () => {
        if (group) this.scene?.remove(group);
        this.focusGroup = undefined;
        this.focusSource = undefined;
        this.focusBinderMeshes = [];
        this.hoveredBinder = undefined;
        this.pendingBinderClick = null;
        this.itemsGroup.visible = true;
        if (saved && this.controls) {
          this.controls.minDistance = saved.min;
          this.controls.maxDistance = saved.max;
          this.controls.enabled = true;
        }
        this.stage.set('room');
      },
    });
  }

  /** (Re)construit les classeurs (cliquables) dans le carton focus. */
  private refreshFocusBinders(list: StorageBinderDto[]): void {
    const group = this.focusGroup;
    if (!group) return;
    for (const child of [...group.children]) {
      if (child.name === '__fbinder') group.remove(child);
    }
    this.focusBinderMeshes = [];
    this.hoveredBinder = undefined;
    const n = Math.min(list.length, 8);
    for (let i = 0; i < n; i++) {
      const binderId = list[i].id;
      loadGltfClone(STORAGE_ASSETS.binderClosed).then(binder => {
        if (this.focusGroup !== group) return;
        binder.name = '__fbinder';
        this.layoutBinderInCarton(binder, i, n, 0, 0, 0.06);
        binder.userData['binderId'] = binderId;
        binder.traverse(o => (o.userData['binderId'] = binderId));
        group.add(binder);
        this.focusBinderMeshes.push(binder);
      }).catch(() => {});
    }
  }

  /** Pose caméra telle que l'objet (bbox `size`) occupe `fill` de la hauteur de vue, vu de dessus. */
  private framePose(center: THREE.Vector3, size: THREE.Vector3, fill: number):
    { position: THREE.Vector3; target: THREE.Vector3; distance: number } {
    const fov = (this.camera!.fov * Math.PI) / 180;
    const objH = Math.max(size.x, size.y, size.z);
    const distance = (objH / 2) / (Math.tan(fov / 2) * fill);
    const dir = new THREE.Vector3(0, 1, 0.0001).normalize(); // pile à la verticale (le ε évite le gimbal lock)
    return { position: center.clone().add(dir.multiplyScalar(distance)), target: center.clone(), distance };
  }

  // ── Classeurs interactifs dans le carton focus ─────────────────────────────
  private cartonPointerDown(event: PointerEvent): void {
    if (!this.renderer || !this.camera || this.focusBinderMeshes.length === 0) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const hit = this.raycaster.intersectObjects(this.focusBinderMeshes, true)[0];
    const id = hit ? (hit.object.userData['binderId'] as string | undefined) : undefined;
    if (id) this.pendingBinderClick = { id, sx: event.clientX, sy: event.clientY };
  }

  private cartonPointerMove(event: PointerEvent): void {
    // un déplacement au-delà du seuil = orbite → annule le clic classeur
    if (this.pendingBinderClick) {
      const dx = event.clientX - this.pendingBinderClick.sx, dy = event.clientY - this.pendingBinderClick.sy;
      if (dx * dx + dy * dy > 25) this.pendingBinderClick = null;
    }
    // hover (bouton relâché) → bounce
    if (event.buttons === 0 && this.renderer && this.camera) {
      this.setPointer(event);
      this.raycaster.setFromCamera(this.pointer, this.camera);
      const hit = this.raycaster.intersectObjects(this.focusBinderMeshes, true)[0];
      const id = hit ? (hit.object.userData['binderId'] as string | undefined) : undefined;
      const mesh = id ? this.focusBinderMeshes.find(m => m.userData['binderId'] === id) : undefined;
      if (mesh !== this.hoveredBinder) {
        if (this.hoveredBinder) this.hoveredBinder.position.y = this.hoverBaseY;
        this.hoveredBinder = mesh;
        if (mesh) this.hoverBaseY = mesh.position.y;
      }
    }
  }

  /** Petit « pop » du classeur puis émission → l'overlay classeur s'ouvre côté conteneur. */
  private openBinder(id: string): void {
    const mesh = this.focusBinderMeshes.find(m => m.userData['binderId'] === id);
    if (!mesh) { this.binderOpened.emit(id); return; }
    if (this.hoveredBinder === mesh) { this.hoveredBinder = undefined; }
    const baseY = this.hoverBaseY || mesh.position.y;
    mesh.position.y = baseY;
    this.cancelAnim?.();
    this.cancelAnim = tween({
      durationMs: 300,
      onUpdate: k => { mesh.position.y = baseY + k * 0.4; mesh.scale.setScalar(1 + k * 0.15); },
      onDone: () => {
        mesh.position.y = baseY; mesh.scale.setScalar(1);
        this.binderOpened.emit(id);
      },
    });
  }

  private finishMove(): void {
    if (!this.movingId || !this.movingObj) return;
    const p = this.movingObj.position;
    if (this.movingKind === 'box') {
      if (this.movingSnap) {
        this.itemDroppedOnFurniture.emit({
          kind: 'box', itemId: this.movingId,
          furnitureId: this.movingSnap.furnitureId, levelIndex: this.movingSnap.level,
        });
      } else {
        this.boxDropped.emit({ boxId: this.movingId, x: +p.x.toFixed(3), y: 0, z: +p.z.toFixed(3) });
      }
    } else if (this.movingKind === 'binder') {
      if (this.movingOntoBoxId) {
        this.binderDroppedOnBox.emit({ binderId: this.movingId, boxId: this.movingOntoBoxId });
      } else if (this.movingSnap) {
        this.itemDroppedOnFurniture.emit({
          kind: 'binder', itemId: this.movingId,
          furnitureId: this.movingSnap.furnitureId, levelIndex: this.movingSnap.level,
        });
      } else {
        // Un classeur ne se pose pas au sol : on annule le déplacement.
        this.toast.info('Un classeur se range dans un carton ou sur un meuble.');
        this.cancelMove();
        return;
      }
    } else {
      this.furnitureFloorDropped.emit({ furnitureId: this.movingId, x: +p.x.toFixed(3), y: 0, z: +p.z.toFixed(3) });
    }
    this.endMove();
  }

  private cancelMove(): void {
    if (this.movingObj) this.movingObj.position.copy(this.movingOrigin);
    this.endMove();
  }

  private endMove(): void {
    this.movingKind = null;
    this.movingId = null;
    this.movingObj = undefined;
    this.movingSnap = null;
    this.movingOntoBoxId = null;
    this.pendingGrab = null;
    this.moving.set(false);
    this.setControlsEnabled(true);
  }

  // ── Drag & drop ────────────────────────────────────────────────────────────
  // dragenter : indispensable pour que la zone soit une cible de dépôt valide (sinon dragover/drop ne fire pas).
  onDragEnter(event: DragEvent): void {
    event.preventDefault();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    if (!this.renderer || !this.camera) return;

    // Aperçu : positionne un fantôme sur le niveau du meuble survolé.
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    const pick = this.pickFurniture();
    const ghost = this.ensureGhost();
    if (!pick) { ghost.visible = false; return; }

    const levels = FURNITURE_LEVEL_Y[pick.type];
    const level = this.nearestLevelIndex(pick.point.y - pick.baseY, pick.type);
    const levelY = levels[Math.min(level, levels.length - 1)] ?? 0;
    ghost.position.set(pick.cx, pick.baseY + levelY + 0.18, pick.cz); // +0.18 = demi-hauteur carton
    ghost.rotation.y = 0;
    ghost.visible = true;
  }

  /** Meuble le plus proche traversé par le rayon courant (intersection rayon ↔ bbox). */
  private pickFurniture(): { id: string; type: FurnitureType; baseY: number; cx: number; cz: number; point: THREE.Vector3 } | null {
    let best: { id: string; type: FurnitureType; baseY: number; cx: number; cz: number; point: THREE.Vector3 } | null = null;
    let bestDist = Infinity;
    const target = new THREE.Vector3();
    for (const fb of this.furnitureBounds) {
      if (this.raycaster.ray.intersectBox(fb.box, target)) {
        const d = this.raycaster.ray.origin.distanceTo(target);
        if (d < bestDist) {
          bestDist = d;
          best = { id: fb.id, type: fb.type, baseY: fb.baseY, cx: fb.cx, cz: fb.cz, point: target.clone() };
        }
      }
    }
    return best;
  }

  private nearestLevelIndex(localY: number, type: FurnitureType): number {
    const levels = FURNITURE_LEVEL_Y[type];
    let best = 0, bestD = Infinity;
    for (let i = 0; i < levels.length; i++) {
      const d = Math.abs(levels[i] - localY);
      if (d < bestD) { bestD = d; best = i; }
    }
    return best;
  }

  onDragLeave(): void {
    if (this.ghost) this.ghost.visible = false;
  }

  private ensureGhost(): THREE.Group {
    if (this.ghost) return this.ghost;
    const g = new THREE.Group();
    const geo = new THREE.BoxGeometry(0.56, 0.38, 0.38);
    g.add(new THREE.Mesh(geo, new THREE.MeshBasicMaterial({
      color: 0x4caf50, transparent: true, opacity: 0.28, depthWrite: false,
    })));
    g.add(new THREE.LineSegments(new THREE.EdgesGeometry(geo),
      new THREE.LineBasicMaterial({ color: 0x4caf50 })));
    g.name = '__ghost';
    g.visible = false;
    this.scene?.add(g);
    this.ghost = g;
    return g;
  }

  private hideGhost(): void {
    if (this.ghost) this.ghost.visible = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.hideGhost();
    const payload = event.dataTransfer?.getData('text/plain');
    if (!payload || !this.renderer || !this.camera) return;
    const [kind, id] = payload.split(':');
    if (!id) return;
    this.setPointer(event);
    this.raycaster.setFromCamera(this.pointer, this.camera);

    if (kind === 'furniture') {
      const p = new THREE.Vector3();
      if (this.raycaster.ray.intersectPlane(this.floorPlane, p)) {
        this.clampToRoom(p);
        this.furnitureFloorDropped.emit({ furnitureId: id, x: +p.x.toFixed(3), y: 0, z: +p.z.toFixed(3) });
      }
      return;
    }

    // classeur : d'abord tester un dépôt sur un carton (rangement dedans)
    if (kind === 'binder') {
      const boxHit = this.raycaster.intersectObjects(this.boxMeshes, true)[0];
      if (boxHit) {
        this.binderDroppedOnBox.emit({ binderId: id, boxId: boxHit.object.userData['boxId'] as string });
        return;
      }
    }

    // carton / classeur : ensuite tester un dépôt sur un meuble (intersection rayon ↔ bbox)
    const pick = this.pickFurniture();
    if (pick) {
      const level = this.nearestLevelIndex(pick.point.y - pick.baseY, pick.type);
      this.itemDroppedOnFurniture.emit({ kind: kind as 'box' | 'binder', itemId: id, furnitureId: pick.id, levelIndex: level });
      return;
    }

    // sinon : carton au sol (les classeurs ne se posent pas au sol)
    if (kind === 'box') {
      const p = new THREE.Vector3();
      if (this.raycaster.ray.intersectPlane(this.floorPlane, p)) {
        this.clampToRoom(p);
        this.boxDropped.emit({ boxId: id, x: +p.x.toFixed(3), y: 0, z: +p.z.toFixed(3) });
      }
    } else {
      this.toast.info('Un classeur se range dans un carton ou sur un meuble.');
    }
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
      canvas.removeEventListener('pointerdown', this.onPointerDown, { capture: true } as EventListenerOptions);
    }
    this.controls?.dispose();
    this.renderer?.dispose();
    if (canvas?.parentElement) canvas.parentElement.removeChild(canvas);
    this.renderer = undefined;
    this.scene = undefined;
    this.camera = undefined;
    this.itemsGroup = new THREE.Group();
    this.boxMeshes = [];
    this.binderMeshes = [];
    this.furnitureMeshes = [];
    this.furnitureBounds = [];
    this.ghost = undefined;
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
            this.stopPolling(); this.jobError.set(null);
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
