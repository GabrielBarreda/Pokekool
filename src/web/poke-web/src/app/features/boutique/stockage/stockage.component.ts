import { Component, inject, signal, viewChild, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from './storage.service';
import {
  StorageBoxDto, StorageBinderDto, StorageRoomDto, StorageRoomLayoutDto, BoxDropEvent,
} from './storage.models';
import { StorageRoomViewerComponent } from './components/storage-room-viewer.component';
import { StorageBoxListComponent } from './components/storage-box-list.component';
import { StorageBinderListComponent } from './components/storage-binder-list.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';

/**
 * Onglet Stockage (module Boutique admin).
 * Colonne gauche : liste des cartons (drag&drop vers la vue 3D) + classeurs du carton sélectionné.
 * Zone principale : viewer 3D de la salle.
 */
@Component({
  selector: 'app-stockage',
  standalone: true,
  imports: [
    FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
    StorageRoomViewerComponent, StorageBoxListComponent, StorageBinderListComponent,
  ],
  styleUrls: ['./controls.scss'],
  template: `
    <div class="page">
      <app-page-header title="Stockage physique" subtitle="Salles, cartons, classeurs et vue 3D">
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
          description="Créez une première salle, importez un modèle 3D, puis glissez vos cartons dedans." />
      } @else {
        <div class="layout">
          <aside class="side">
            <app-storage-box-list
              [roomId]="selectedRoomId()"
              [selectedId]="selectedBox()?.id ?? null"
              (boxPicked)="onBoxSelected($event)"
              (boxesChanged)="reloadLayout()" />
            <app-storage-binder-list
              [box]="selectedBox()"
              [binders]="binders()"
              [selectedId]="selectedBinderId()"
              (binderSelected)="onBinderSelected($event)"
              (changed)="reloadBinders()" />
          </aside>
          <main class="main">
            @if (currentRoom(); as room) {
              <app-storage-room-viewer
                [room]="room"
                [boxes]="boxes()"
                [selectedBoxId]="selectedBox()?.id ?? null"
                [binders]="binders()"
                (boxSelected)="onBoxSelected($event)"
                (boxDropped)="onBoxDropped($event)" />
            }
          </main>
        </div>
      }
    </div>
  `,
  styles: [`
    .header-tools { display: flex; gap: .5rem; align-items: center; }
    .room-form { display: flex; gap: .5rem; margin-bottom: 1rem; }
    .room-form .input { flex: 0 0 320px; }
    .center { display: flex; justify-content: center; padding: 3rem; }
    .layout { display: grid; grid-template-columns: 320px 1fr; gap: 1rem; align-items: stretch; min-height: 560px; }
    .side { display: flex; flex-direction: column; gap: 1rem; }
    .main { display: flex; }
    .main app-storage-room-viewer { width: 100%; }
    @media (max-width: 1000px) { .layout { grid-template-columns: 1fr; } }
  `],
})
export class StockageComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  private readonly boxList = viewChild(StorageBoxListComponent);

  readonly rooms = signal<StorageRoomDto[]>([]);
  readonly selectedRoomId = signal<string>('');
  readonly currentRoom = signal<StorageRoomDto | null>(null);
  readonly boxes = signal<StorageBoxDto[]>([]);
  readonly selectedBox = signal<StorageBoxDto | null>(null);
  readonly binders = signal<StorageBinderDto[]>([]);
  readonly selectedBinderId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly creatingRoom = signal(false);
  roomName = '';

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
    this.binders.set([]);
    this.selectedBinderId.set(null);
    this.reloadLayout();
  }

  reloadLayout(): void {
    const id = this.selectedRoomId();
    if (!id) return;
    this.service.getRoomLayout(id).subscribe({
      next: (layout: StorageRoomLayoutDto) => {
        this.currentRoom.set(layout.room);
        this.boxes.set(layout.boxes.map(b => b.box));
      },
      error: () => this.toast.error('Impossible de charger la salle.'),
    });
  }

  onBoxSelected(box: StorageBoxDto): void {
    this.selectedBox.set(box);
    this.selectedBinderId.set(null);
    this.reloadBinders();
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
    // toggle : re-cliquer désélectionne (referme dans la 3D)
    this.selectedBinderId.update(id => (id === binder.id ? null : binder.id));
  }

  onBoxDropped(evt: BoxDropEvent): void {
    const json = JSON.stringify({ x: evt.x, y: evt.y, z: evt.z, rotation: 0 });
    this.service.updateBoxPosition(evt.boxId, json).subscribe({
      next: () => { this.reloadLayout(); this.boxList()?.refresh(); },
      error: () => this.toast.error('Impossible de positionner le carton.'),
    });
  }

  toggleRoomForm(): void {
    this.creatingRoom.update(v => !v);
    this.roomName = '';
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
