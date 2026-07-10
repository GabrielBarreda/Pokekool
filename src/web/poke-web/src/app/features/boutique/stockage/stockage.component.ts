import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from './storage.service';
import { StorageBoxDto, StorageRoomDto, StorageRoomLayoutDto } from './storage.models';
import { StorageRoomViewerComponent } from './components/storage-room-viewer.component';
import { StorageBoxListComponent } from './components/storage-box-list.component';
import { StorageBinderListComponent } from './components/storage-binder-list.component';
import { StorageMovementAlertsComponent } from './components/storage-movement-alerts.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';

/**
 * Onglet Stockage (sous le module Boutique admin).
 * Salle → vue 3D + cartons + classeurs + alertes déplacements.
 */
@Component({
  selector: 'app-stockage',
  standalone: true,
  imports: [
    FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
    StorageRoomViewerComponent, StorageBoxListComponent, StorageBinderListComponent,
    StorageMovementAlertsComponent,
  ],
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
          description="Créez une première salle pour organiser vos cartons et importer un modèle 3D." />
      } @else {
        <div class="layout">
          <div class="col col--main">
            @if (currentRoom(); as room) {
              <app-storage-room-viewer [room]="room" [boxes]="boxes()" (boxSelected)="onBoxSelected($event)" />
            }
            <app-storage-box-list
              [roomId]="selectedRoomId()"
              [selectedId]="selectedBox()?.id ?? null"
              (boxPicked)="onBoxSelected($event)"
              (boxesChanged)="reloadLayout()" />
          </div>
          <div class="col col--side">
            <app-storage-binder-list [box]="selectedBox()" />
            <app-storage-movement-alerts />
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .header-tools { display: flex; gap: .5rem; align-items: center; }
    .room-form { display: flex; gap: .5rem; margin-bottom: 1rem; }
    .room-form .input { flex: 0 0 320px; }
    .center { display: flex; justify-content: center; padding: 3rem; }
    .layout { display: grid; grid-template-columns: 1fr 340px; gap: 1rem; align-items: start; }
    .col { display: flex; flex-direction: column; gap: 1rem; }
    @media (max-width: 1000px) { .layout { grid-template-columns: 1fr; } }
  `],
})
export class StockageComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly rooms = signal<StorageRoomDto[]>([]);
  readonly selectedRoomId = signal<string>('');
  readonly currentRoom = signal<StorageRoomDto | null>(null);
  readonly boxes = signal<StorageBoxDto[]>([]);
  readonly selectedBox = signal<StorageBoxDto | null>(null);
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

  onBoxSelected(box: StorageBoxDto): void { this.selectedBox.set(box); }

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
