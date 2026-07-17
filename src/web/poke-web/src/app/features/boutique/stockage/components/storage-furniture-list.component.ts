import { Component, inject, input, output, signal } from '@angular/core';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { StorageFurnitureDto, FurnitureType, FURNITURE_LABELS } from '../storage.models';

/**
 * Meubles (étagères) de la salle. Liste + création (choix du modèle). Chaque meuble est
 * draggable vers la vue 3D (payload "furniture:{id}").
 */
@Component({
  selector: 'app-storage-furniture-list',
  standalone: true,
  imports: [HasPermissionDirective],
  styleUrls: ['../controls.scss'],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Meubles</h3>
        <div *hasPermission="'storage:write'" class="fu-create">
          <button class="btn-primary sm" [disabled]="saving()" (click)="create('ShelfClassic6')"
                  title="Ajouter une étagère 6 niveaux (nom auto)">+ Étagère</button>
          <button class="btn-secondary sm" [disabled]="saving()" (click)="create('ShelfWall')"
                  title="Ajouter une étagère murale (nom auto)">+ Murale</button>
        </div>
      </header>

      @if (furniture().length === 0) {
        <p class="empty">Aucun meuble. Créez-en un, puis glissez-le dans la vue 3D.</p>
      } @else {
        <ul class="list">
          @for (f of furniture(); track f.id) {
            <li class="fu" draggable="true"
                [class.fu--sel]="f.id === selectedId()"
                (dragstart)="onDragStart($event, f)"
                (click)="picked.emit(f)">
              <span class="fu__grip" title="Glisser dans la vue 3D">⠿</span>
              <span class="fu__body">
                <span class="fu__name">{{ f.name || label(f.type) }}</span>
                <span class="fu__meta">{{ label(f.type) }} · {{ f.levelCount }} niv.</span>
              </span>
              @if (isPlaced(f)) { <span class="fu__dot fu__dot--on" title="Placé"></span> }
              @else { <span class="fu__dot" title="Non placé"></span> }
              <button *hasPermission="'storage:write'" class="fu__del" title="Supprimer"
                      (click)="del($event, f)">✕</button>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [`
    .panel { border: 1px solid #e2e2e2; border-radius: 12px; padding: .85rem; display: flex; flex-direction: column; gap: .6rem; }
    .panel__head { display: flex; justify-content: space-between; align-items: center; }
    h3 { margin: 0; font-size: .95rem; }
    .form { display: flex; flex-direction: column; gap: .4rem; background: #fafafa; padding: .5rem; border-radius: 8px; }
    .form__actions { display: flex; justify-content: flex-end; gap: .4rem; }
    .empty { color: #999; font-size: .82rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .35rem; }
    .fu { display: flex; align-items: center; gap: .5rem; padding: .5rem .55rem; border: 1px solid #e6e6e6; border-radius: 9px; cursor: grab; background: #fff; }
    .fu:hover { border-color: #bcd; }
    .fu--sel { border-color: #4CAF50; background: #f2fbf2; }
    .fu__grip { color: #bbb; font-size: 1rem; }
    .fu__body { display: flex; flex-direction: column; flex: 1; min-width: 0; }
    .fu__name { font-weight: 600; font-size: .85rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .fu__meta { font-size: .72rem; color: #888; }
    .fu__dot { width: .55rem; height: .55rem; border-radius: 50%; background: #d7d7d7; flex: 0 0 auto; }
    .fu__dot--on { background: #4CAF50; }
    .fu__del { border: none; background: none; color: #c33; cursor: pointer; font-size: .8rem; padding: 0 .2rem; }
    .fu-create { display: flex; gap: .35rem; }
  `],
})
export class StorageFurnitureListComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly roomId = input.required<string>();
  readonly furniture = input<StorageFurnitureDto[]>([]);
  readonly selectedId = input<string | null>(null);

  readonly picked = output<StorageFurnitureDto>();
  readonly changed = output<void>();

  readonly saving = signal(false);
  /** Création directe, sans formulaire : nom auto-incrémenté « Meuble N ». */
  create(type: FurnitureType): void {
    this.saving.set(true);
    const n = this.furniture().length + 1;
    this.service.createFurniture({ roomId: this.roomId(), type, name: `Meuble ${n}` }).subscribe({
      next: () => { this.saving.set(false); this.toast.success('Meuble créé.'); this.changed.emit(); },
      error: () => { this.saving.set(false); this.toast.error('Création impossible.'); },
    });
  }

  del(event: Event, f: StorageFurnitureDto): void {
    event.stopPropagation();
    this.service.deleteFurniture(f.id).subscribe({
      next: () => { this.toast.success('Meuble supprimé (contenu détaché).'); this.changed.emit(); },
      error: () => this.toast.error('Suppression impossible.'),
    });
  }

  onDragStart(event: DragEvent, f: StorageFurnitureDto): void {
    event.dataTransfer?.setData('text/plain', `furniture:${f.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  isPlaced(f: StorageFurnitureDto): boolean { return !!f.position3dJson; }
  label(t: FurnitureType): string { return FURNITURE_LABELS[t]; }
}
