import { Component, inject, signal, viewChild, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from './storage.service';
import {
  StorageBoxDto, StorageBinderDto, StorageFurnitureDto, StorageRoomDto, StorageRoomLayoutDto,
  StorageBinderCardDto, StorageRecapModel, StorageEstimateDto, BoxDropEvent, FurnitureDropEvent,
  FurnitureFloorDropEvent, BinderBoxDropEvent, FURNITURE_LABELS,
} from './storage.models';
import { StorageRoomViewerComponent } from './components/storage-room-viewer.component';
import { StorageBoxListComponent } from './components/storage-box-list.component';
import { StorageBinderListComponent } from './components/storage-binder-list.component';
import { StorageFurnitureListComponent } from './components/storage-furniture-list.component';
import { StorageBinderOverlayComponent } from './components/storage-binder-overlay.component';
import { StorageCardDetailOverlayComponent } from './components/storage-card-detail-overlay.component';
import { StorageAssignPopupComponent } from './components/storage-assign-popup.component';
import { StorageScanBinderComponent } from './components/storage-scan-binder.component';
import { StorageExpanderComponent } from './components/storage-expander.component';
import { StorageRecapPopupComponent } from './components/storage-recap-popup.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { StockDropEvent } from './components/storage-binder-list.component';

const LS_FURNITURE_LOCKED = 'storage_furniture_locked';

/**
 * Onglet Stockage (module Boutique admin).
 * Colonne gauche : meubles (masquable/verrouillable), cartons, classeurs.
 * Zone principale : viewer 3D + overlays classeur/carte.
 * Popups : assignation depuis stock boutique, scan caméra double-page.
 */
@Component({
  selector: 'app-stockage',
  standalone: true,
  imports: [
    FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
    StorageRoomViewerComponent, StorageBoxListComponent, StorageBinderListComponent,
    StorageFurnitureListComponent, StorageBinderOverlayComponent, StorageCardDetailOverlayComponent,
    StorageAssignPopupComponent, StorageScanBinderComponent,
    StorageExpanderComponent, StorageRecapPopupComponent,
  ],
  styleUrls: ['./controls.scss'],
  template: `
    <div class="page">
      <app-page-header title="Stockage physique" subtitle="Salles, meubles, cartons, classeurs et vue 3D">
        <div class="header-tools">
          @if (rooms().length > 0) {
            <select class="input" [ngModel]="selectedRoomId()" (ngModelChange)="selectRoom($event)">
              @for (r of rooms(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
            </select>
          }
          <button *hasPermission="'storage:write'" class="btn-secondary" (click)="toggleRoomForm()">+ Salle</button>
        </div>
      </app-page-header>

      @if (creatingRoom()) {
        <form class="room-form" (ngSubmit)="createRoom()">
          <input class="input" placeholder="Nom de la salle (ex. Réserve A)" [(ngModel)]="roomName" name="rn" required />
          <button type="submit" class="btn-primary" [disabled]="!roomName">Créer</button>
        </form>
      }

      @if (loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (rooms().length === 0) {
        <app-empty-state icon="🏬" title="Aucune salle de stockage"
          description="Créez une première salle, importez un modèle 3D, puis glissez vos meubles et cartons dedans." />
      } @else {
        <div class="layout">
          <aside class="side">
            <!-- Meubles -->
            <app-storage-expander title="Meubles" icon="furniture" [count]="furniture().length">
              <div class="exp-toolbar">
                <button class="icon-btn" [class.icon-btn--active]="furnitureLocked()"
                        (click)="toggleFurnitureLock()"
                        [title]="furnitureLocked() ? 'Meubles verrouillés (cliquer pour déverrouiller)' : 'Déplaçables — cliquer pour verrouiller'">
                  @if (furnitureLocked()) {
                    <svg viewBox="0 0 16 16" width="14" height="14"><path d="M4 7V5a4 4 0 0 1 8 0v2h1a1 1 0 0 1 1 1v6a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V8a1 1 0 0 1 1-1h1zm2 0h4V5a2 2 0 1 0-4 0v2z" fill="currentColor"/></svg>
                  } @else {
                    <svg viewBox="0 0 16 16" width="14" height="14"><path d="M4 7V5a4 4 0 0 1 7.748-1.315.75.75 0 1 1-1.496.13A2.5 2.5 0 0 0 5.5 5v2H12a1 1 0 0 1 1 1v6a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V8a1 1 0 0 1 1-1h1z" fill="currentColor"/></svg>
                  }
                </button>
                <button class="icon-btn" (click)="furnitureVisible.update(v => !v)"
                        [title]="furnitureVisible() ? 'Masquer les meubles' : 'Afficher les meubles'">
                  @if (furnitureVisible()) {
                    <svg viewBox="0 0 16 16" width="14" height="14"><path d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8zM8 11a3 3 0 1 1 0-6 3 3 0 0 1 0 6z" fill="currentColor"/></svg>
                  } @else {
                    <svg viewBox="0 0 16 16" width="14" height="14"><path d="M13.359 11.238C15.06 9.72 16 8 16 8s-3-5.5-8-5.5a7.028 7.028 0 0 0-2.79.588l.77.771A5.944 5.944 0 0 1 8 3.5c2.12 0 3.879 1.168 5.168 2.457A13.134 13.134 0 0 1 14.828 8c-.058.087-.122.183-.195.288-.335.48-.83 1.12-1.465 1.755-.165.165-.337.328-.517.486l.708.709z" fill="currentColor"/><path d="M11.297 9.176a3.5 3.5 0 0 0-4.474-4.474l.823.823a2.5 2.5 0 0 1 2.829 2.829l.822.822zm-2.943 1.299.822.822a3.5 3.5 0 0 1-4.474-4.474l.823.823a2.5 2.5 0 0 0 2.829 2.829z" fill="currentColor"/><path d="M3.35 5.47c-.18.16-.353.322-.518.487A13.134 13.134 0 0 0 1.172 8l.195.288c.335.48.83 1.12 1.465 1.755C4.121 11.332 5.881 12.5 8 12.5c.716 0 1.39-.133 2.02-.36l.77.772A7.029 7.029 0 0 1 8 13.5C3 13.5 0 8 0 8s.939-1.721 2.641-3.238l.708.708zm10.296 8.884-12-12 .708-.708 12 12-.708.708z" fill="currentColor"/></svg>
                  }
                </button>
              </div>
              @if (furnitureVisible()) {
                <app-storage-furniture-list
                  [roomId]="selectedRoomId()"
                  [furniture]="furniture()"
                  [selectedId]="selectedFurnitureId()"
                  (picked)="onFurnitureSelected($event)"
                  (changed)="reloadLayout()" />
              }
            </app-storage-expander>

            <!-- Cartons -->
            <app-storage-expander title="Cartons" icon="box" [count]="boxes().length">
              <app-storage-box-list
                [roomId]="selectedRoomId()"
                [selectedId]="selectedBox()?.id ?? null"
                (boxPicked)="onBoxSelected($event)"
                (boxesChanged)="reloadLayout()" />
            </app-storage-expander>

            <!-- Classeurs -->
            <app-storage-expander title="Classeurs" icon="binder" [count]="binders().length">
              <app-storage-binder-list
                [box]="selectedBox()"
                [binders]="binders()"
                [selectedId]="selectedBinderId()"
                (binderSelected)="onBinderSelected($event)"
                (changed)="reloadBinders()"
                (stockDropped)="onStockDroppedOnBinder($event)" />

              <!-- Boutons assignation -->
              @if (selectedBox()) {
                <div class="assign-actions">
                  <button class="btn-secondary sm" (click)="openAssignPopup()">
                    <svg viewBox="0 0 16 16" width="13" height="13"><path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z" fill="currentColor"/><path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z" fill="currentColor"/></svg>
                    Assigner cartes
                  </button>
                  <button class="btn-secondary sm" (click)="openScanBinder()">
                    <svg viewBox="0 0 16 16" width="13" height="13"><path d="M10.5 8.5a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0z" fill="currentColor"/><path d="M2 4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2h-1.172a2 2 0 0 1-1.414-.586l-.828-.828A2 2 0 0 0 9.172 2H6.828a2 2 0 0 0-1.414.586l-.828.828A2 2 0 0 1 3.172 4H2zm.5 2a.5.5 0 1 1 0-1 .5.5 0 0 1 0 1zm9 2.5a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0z" fill="currentColor"/></svg>
                    Scanner classeur
                  </button>
                </div>
              }
            </app-storage-expander>
          </aside>
          <main class="main">
            @if (currentRoom(); as room) {
              <app-storage-room-viewer
                [room]="room"
                [boxes]="boxes()"
                [selectedBoxId]="selectedBox()?.id ?? null"
                [binders]="binders()"
                [furniture]="furniture()"
                [looseBinders]="looseBinders()"
                [furnitureLocked]="furnitureLocked()"
                (boxSelected)="onBoxSelected($event)"
                (binderSelected)="onViewerBinderSelected($event)"
                (boxDropped)="onBoxDropped($event)"
                (furnitureFloorDropped)="onFurnitureFloorDropped($event)"
                (itemDroppedOnFurniture)="onItemDroppedOnFurniture($event)"
                (binderDroppedOnBox)="onBinderDroppedOnBox($event)"
                (binderOpened)="onBinderOpened($event)" />
            }
            @if (openBinderId(); as bid) {
              <app-storage-binder-overlay
                [binderId]="bid"
                [label]="openBinderLabel()"
                (close)="openBinderId.set(null)"
                (cardSelected)="onCardSelected($event)" />
            }
            @if (selectedCard(); as card) {
              <app-storage-card-detail-overlay [card]="card" (close)="selectedCard.set(null)" />
            }
            @if (recap(); as r) {
              <app-storage-recap-popup [model]="r"
                (close)="recap.set(null)"
                (openRequested)="onRecapOpen()"
                (deleteRequested)="onRecapDelete()" />
            }
          </main>
        </div>
        <div class="room-footer">
          <button *hasPermission="'storage:write'" class="btn-danger" (click)="deleteRoom()">
            🗑 Supprimer la pièce « {{ currentRoom()?.name }} »
          </button>
        </div>
      }
    </div>

    <!-- Popup assignation depuis le stock boutique -->
    @if (assignPopupOpen()) {
      <app-storage-assign-popup
        [binders]="binders()"
        [selectedBinder]="selectedBox() ? (binders()[0] || null) : null"
        (close)="assignPopupOpen.set(false)"
        (assigned)="onAssigned()" />
    }

    <!-- Popup scan caméra double-page classeur -->
    @if (scanBinderOpen()) {
      <app-storage-scan-binder
        [binders]="binders()"
        [box]="selectedBox()"
        (close)="scanBinderOpen.set(false)"
        (created)="onScanCreated()" />
    }
  `,
  styles: [`
    .header-tools { display: flex; gap: .5rem; align-items: center; }
    .room-form { display: flex; gap: .5rem; margin-bottom: 1rem; }
    .room-form .input { flex: 0 0 320px; }
    .center { display: flex; justify-content: center; padding: 3rem; }
    .layout { display: grid; grid-template-columns: 320px 1fr; gap: 1rem; align-items: stretch; min-height: 560px; }
    .side { display: flex; flex-direction: column; gap: .75rem; }
    .main { display: flex; position: relative; }
    .main app-storage-room-viewer { width: 100%; }
    @media (max-width: 1000px) { .layout { grid-template-columns: 1fr; } }

    .exp-toolbar { display: flex; justify-content: flex-end; gap: .3rem; margin-bottom: .5rem; }

    .icon-btn { display: inline-flex; align-items: center; justify-content: center;
                width: 28px; height: 28px; border-radius: var(--radius-sm);
                border: 1px solid var(--color-border-default); background: var(--color-surface-base);
                color: var(--color-text-secondary); cursor: pointer; padding: 0; }
    .icon-btn:hover { background: var(--color-surface-raised); color: var(--color-text-primary); }
    .icon-btn--active { background: #fef3c7; border-color: #d97706; color: #92400e; }

    .assign-actions { display: flex; gap: .5rem; }
    .assign-actions .btn-secondary { flex: 1; }

    .room-footer { display: flex; justify-content: flex-end; margin-top: .75rem; }
    .btn-danger { height: 34px; padding: 0 var(--space-4); border-radius: var(--radius-md);
      background: var(--color-semantic-danger-subtle, #fde8e8); color: var(--color-semantic-danger, #c0392b);
      border: 1px solid var(--color-semantic-danger, #c0392b); font-size: var(--font-size-sm); cursor: pointer; }
    .btn-danger:hover { background: var(--color-semantic-danger, #c0392b); color: #fff; }
  `],
})
export class StockageComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  private readonly boxList = viewChild(StorageBoxListComponent);
  private readonly viewer = viewChild(StorageRoomViewerComponent);

  readonly rooms = signal<StorageRoomDto[]>([]);
  readonly selectedRoomId = signal<string>('');
  readonly currentRoom = signal<StorageRoomDto | null>(null);
  readonly boxes = signal<StorageBoxDto[]>([]);
  readonly furniture = signal<StorageFurnitureDto[]>([]);
  readonly looseBinders = signal<StorageBinderDto[]>([]);
  readonly selectedBox = signal<StorageBoxDto | null>(null);
  readonly selectedFurnitureId = signal<string | null>(null);
  readonly binders = signal<StorageBinderDto[]>([]);
  readonly selectedBinderId = signal<string | null>(null);
  readonly openBinderId = signal<string | null>(null);
  readonly selectedCard = signal<StorageBinderCardDto | null>(null);
  readonly loading = signal(true);
  readonly creatingRoom = signal(false);

  /** Popup récapitulatif de l'objet sélectionné (carton/meuble/classeur). */
  readonly recap = signal<StorageRecapModel | null>(null);
  /** Nb de classeurs par carton, issu du layout (pour la contenance du récap). */
  private readonly boxBinderCounts = signal<Record<string, number>>({});

  // Partie A — toggles UI
  readonly furnitureVisible = signal(true);
  readonly furnitureLocked = signal(
    localStorage.getItem(LS_FURNITURE_LOCKED) === 'true',
  );

  // Partie B — assignation + scan
  readonly assignPopupOpen = signal(false);
  readonly scanBinderOpen = signal(false);

  roomName = '';

  readonly openBinderLabel = () => {
    const id = this.openBinderId();
    return id ? (this.binders().find(b => b.id === id)?.label ?? '') : '';
  };

  ngOnInit(): void { this.loadRooms(); }

  private loadRooms(): void {
    this.loading.set(true);
    this.service.getRooms().subscribe({
      next: rooms => {
        this.rooms.set(rooms);
        this.loading.set(false);
        if (rooms.length > 0) this.selectRoom(rooms[0].id);
      },
      error: () => { this.loading.set(false); this.toast.error('Impossible de charger les salles.'); },
    });
  }

  selectRoom(roomId: string): void {
    this.selectedRoomId.set(roomId);
    this.selectedBox.set(null);
    this.selectedFurnitureId.set(null);
    this.binders.set([]);
    this.selectedBinderId.set(null);
    this.recap.set(null);
    this.reloadLayout();
  }

  reloadLayout(): void {
    const id = this.selectedRoomId();
    if (!id) return;
    this.service.getRoomLayout(id).subscribe({
      next: (layout: StorageRoomLayoutDto) => {
        this.currentRoom.set(layout.room);
        this.boxes.set(layout.boxes.map(b => b.box));
        this.furniture.set(layout.furniture);
        this.looseBinders.set(layout.looseBinders);
        this.boxBinderCounts.set(
          Object.fromEntries(layout.boxes.map(b => [b.box.id, b.binders.length])),
        );
      },
      error: () => this.toast.error('Impossible de charger la salle.'),
    });
  }

  onBoxSelected(box: StorageBoxDto): void {
    this.selectedBox.set(box);
    this.selectedFurnitureId.set(null);
    this.selectedBinderId.set(null);
    this.setRecap(this.buildBoxRecap(box));
    this.reloadBinders();
  }

  onFurnitureSelected(f: StorageFurnitureDto): void {
    this.selectedFurnitureId.set(f.id);
    this.setRecap(this.buildFurnitureRecap(f));
  }

  /** Clic sur un classeur posé dans la salle 3D → sélection + récap. */
  onViewerBinderSelected(b: StorageBinderDto): void {
    this.selectedBinderId.set(b.id);
    this.setRecap(this.buildBinderRecap(b));
  }

  /** Applique un modèle de récap et déclenche (async) le calcul du prix estimé pour carton/classeur. */
  private setRecap(model: StorageRecapModel | null): void {
    this.recap.set(model);
    if (model && (model.kind === 'box' || model.kind === 'binder')) {
      this.service.getEstimate(model.kind, model.id).subscribe({
        next: e => {
          const r = this.recap();
          if (r && r.id === model.id && r.kind === model.kind) {
            this.recap.set({ ...r, estimatedPrice: this.formatEstimate(e) });
          }
        },
        error: () => {},
      });
    }
  }

  private formatEstimate(e: StorageEstimateDto): string | null {
    if (e.total == null) return null;
    const cur = e.currency || 'EUR';
    try {
      return new Intl.NumberFormat('fr-FR', { style: 'currency', currency: cur }).format(e.total);
    } catch {
      return `${e.total.toFixed(2)} ${cur}`;
    }
  }

  /** Classeur glissé sur un carton dans la 3D → rangé dans ce carton. */
  onBinderDroppedOnBox(evt: BinderBoxDropEvent): void {
    this.service.placeBinder(evt.binderId, { boxId: evt.boxId }).subscribe({
      next: () => {
        this.toast.success('Classeur rangé dans le carton.');
        this.reloadLayout();
        this.reloadBinders();
      },
      error: () => this.toast.error('Rangement impossible.'),
    });
  }

  // ── Modèles récap ─────────────────────────────────────────────────────────
  private buildBoxRecap(box: StorageBoxDto): StorageRecapModel {
    const supplier = box.labelOrigin === 'Supplier';
    const count = this.boxBinderCounts()[box.id] ?? this.binders().length;
    const sizeSuffix = box.size ? ` · taille ${box.size}` : '';
    return {
      kind: 'box',
      id: box.id,
      title: box.barcode || box.internalCode || 'Carton',
      subtitle: (supplier ? 'Carton fournisseur' : 'Carton interne') + sizeSuffix,
      contenance: `${count} classeur${count > 1 ? 's' : ''}`,
      estimatedPrice: null,
      barcode: supplier ? box.barcode : null,
      placed: !!box.position3dJson || !!box.furnitureId,
    };
  }

  private buildFurnitureRecap(f: StorageFurnitureDto): StorageRecapModel {
    return {
      kind: 'furniture',
      id: f.id,
      title: f.name || FURNITURE_LABELS[f.type],
      subtitle: FURNITURE_LABELS[f.type],
      contenance: `${f.levelCount} niveau${f.levelCount > 1 ? 'x' : ''}`,
      estimatedPrice: null,
      barcode: null,
      placed: !!f.position3dJson,
    };
  }

  private buildBinderRecap(b: StorageBinderDto): StorageRecapModel {
    const pos = b.positionInBox != null ? `Position ${b.positionInBox}` : 'Non numéroté';
    return {
      kind: 'binder',
      id: b.id,
      title: b.label,
      subtitle: 'Classeur',
      contenance: pos,
      estimatedPrice: null,
      barcode: null,
      placed: !!b.boxId || !!b.furnitureId,
    };
  }

  // ── Actions du récap ──────────────────────────────────────────────────────
  onRecapOpen(): void {
    const r = this.recap();
    if (!r) return;
    if (r.kind === 'box') this.viewer()?.openBox(r.id);
    else if (r.kind === 'binder') this.openBinderId.set(r.id);
  }

  onRecapDelete(): void {
    const r = this.recap();
    if (!r) return;
    if (r.kind === 'box') {
      if (!confirm(`Supprimer le carton « ${r.title} » ? Selon vos paramètres Stockage, ses classeurs seront supprimés (cascade) ou détachés (conservés « non rangés »).`)) return;
      this.service.deleteBox(r.id).subscribe({
        next: () => {
          this.toast.success('Carton supprimé.');
          this.recap.set(null);
          this.selectedBox.set(null);
          this.binders.set([]);
          this.boxList()?.refresh();
          this.reloadLayout();
        },
        error: () => this.toast.error('Suppression impossible.'),
      });
    } else if (r.kind === 'furniture') {
      if (!confirm(`Supprimer le meuble « ${r.title} » ? Son contenu sera détaché (non supprimé).`)) return;
      this.service.deleteFurniture(r.id).subscribe({
        next: () => {
          this.toast.success('Meuble supprimé (contenu détaché).');
          this.recap.set(null);
          this.selectedFurnitureId.set(null);
          this.reloadLayout();
        },
        error: () => this.toast.error('Suppression impossible.'),
      });
    }
  }

  reloadBinders(): void {
    const box = this.selectedBox();
    if (!box) { this.binders.set([]); return; }
    this.service.getBinders(box.id).subscribe({
      next: b => this.binders.set(b),
      error: () => this.toast.error('Impossible de charger les classeurs.'),
    });
  }

  onBinderSelected(binder: StorageBinderDto): void {
    const willDeselect = this.selectedBinderId() === binder.id;
    this.selectedBinderId.set(willDeselect ? null : binder.id);
    if (willDeselect) {
      const box = this.selectedBox();
      this.setRecap(box ? this.buildBoxRecap(box) : null);
    } else {
      this.setRecap(this.buildBinderRecap(binder));
    }
  }

  onBinderOpened(binderId: string): void {
    this.openBinderId.set(binderId);
  }

  onCardSelected(card: StorageBinderCardDto): void {
    this.selectedCard.set(card);
  }

  onBoxDropped(evt: BoxDropEvent): void {
    const json = JSON.stringify({ x: evt.x, y: evt.y, z: evt.z, rotation: 0 });
    this.service.placeBox(evt.boxId, { position3dJson: json }).subscribe({
      next: () => { this.reloadLayout(); this.boxList()?.refresh(); },
      error: () => this.toast.error('Impossible de positionner le carton.'),
    });
  }

  onFurnitureFloorDropped(evt: FurnitureFloorDropEvent): void {
    const json = JSON.stringify({ x: evt.x, y: evt.y, z: evt.z, rotation: 0 });
    this.service.updateFurniturePosition(evt.furnitureId, json).subscribe({
      next: () => this.reloadLayout(),
      error: () => this.toast.error('Impossible de positionner le meuble.'),
    });
  }

  onItemDroppedOnFurniture(evt: FurnitureDropEvent): void {
    const done = {
      next: () => { this.reloadLayout(); this.boxList()?.refresh(); this.reloadBinders(); },
      error: () => this.toast.error('Rangement impossible.'),
    };
    if (evt.kind === 'box') {
      this.service.placeBox(evt.itemId, { furnitureId: evt.furnitureId, levelIndex: evt.levelIndex }).subscribe(done);
    } else {
      this.service.placeBinder(evt.itemId, { furnitureId: evt.furnitureId, levelIndex: evt.levelIndex }).subscribe(done);
    }
  }

  // ── Partie A : toggles ────────────────────────────────────────────────────
  toggleFurnitureLock(): void {
    this.furnitureLocked.update(v => !v);
    localStorage.setItem(LS_FURNITURE_LOCKED, String(this.furnitureLocked()));
  }

  // ── Partie B : assignation ────────────────────────────────────────────────
  openAssignPopup(): void { this.assignPopupOpen.set(true); }

  openScanBinder(): void { this.scanBinderOpen.set(true); }

  /** Drag d'une carte stock depuis la popup d'assignation déposée sur un classeur de la liste. */
  onStockDroppedOnBinder(evt: StockDropEvent): void {
    this.service.assignLocation(evt.binderId, 'shop_stock', evt.stockOwnerId, null).subscribe({
      next: () => { this.toast.success('Carte assignée.'); this.reloadBinders(); },
      error: () => this.toast.error('Assignation impossible.'),
    });
  }

  onAssigned(): void {
    this.assignPopupOpen.set(false);
    this.reloadBinders();
    this.toast.success('Cartes assignées au classeur.');
  }

  onScanCreated(): void {
    this.scanBinderOpen.set(false);
    this.reloadBinders();
    this.reloadLayout();
    this.toast.success('Classeur créé depuis le scan.');
  }

  // ── Salle ─────────────────────────────────────────────────────────────────
  toggleRoomForm(): void {
    this.creatingRoom.update(v => !v);
    this.roomName = '';
  }

  deleteRoom(): void {
    const room = this.currentRoom();
    if (!room) return;
    if (!confirm(`Supprimer la pièce « ${room.name} » ? Selon vos paramètres Stockage, son contenu sera supprimé (cascade) ou la suppression sera refusée tant que la pièce n'est pas vide.`)) return;
    this.service.deleteRoom(room.id).subscribe({
      next: () => {
        this.toast.success('Pièce supprimée.');
        this.rooms.update(list => list.filter(r => r.id !== room.id));
        const remaining = this.rooms();
        if (remaining.length > 0) this.selectRoom(remaining[0].id);
        else { this.currentRoom.set(null); this.selectedRoomId.set(''); this.boxes.set([]); this.furniture.set([]); this.recap.set(null); }
      },
      error: () => this.toast.error('Suppression impossible (la pièce n\'est peut-être pas vide — voir Paramètres › Stockage).'),
    });
  }

  createRoom(): void {
    if (!this.roomName.trim()) return;
    this.service.createRoom({ name: this.roomName.trim() }).subscribe({
      next: room => {
        this.creatingRoom.set(false); this.roomName = '';
        this.rooms.update(list => [...list, room]);
        this.toast.success('Salle créée.');
        this.selectRoom(room.id);
      },
      error: () => this.toast.error('Création impossible.'),
    });
  }
}
