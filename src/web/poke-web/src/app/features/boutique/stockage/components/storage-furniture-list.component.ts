import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  imports: [FormsModule, HasPermissionDirective],
  styleUrls: ['../controls.scss'],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Meubles</h3>
        <button *hasPermission="'storage:write'" class="btn-primary sm" (click)="toggleCreate()">+ Meuble</button>
      </header>

      @if (creating()) {
        <form class="form" (ngSubmit)="submit()">
          <select class="input sm" [(ngModel)]="type" name="ty">
            <option value="ShelfClassic6">Étagère (6 niveaux)</option>
            <option value="ShelfWall">Étagère murale</option>
          </select>
          <input class="input sm" placeholder="Nom (optionnel)" [(ngModel)]="name" name="nm" />
          <div class="form__actions">
            <button type="button" class="btn-secondary sm" (click)="toggleCreate()">Annuler</button>
            <button type="submit" class="btn-primary sm" [disabled]="saving()">Créer</button>
          </div>
        </form>
      }

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

  readonly creating = signal(false);
  readonly saving = signal(false);
  type: FurnitureType = 'ShelfClassic6';
  name = '';

  toggleCreate(): void {
    this.creating.update(v => !v);
    if (this.creating()) { this.type = 'ShelfClassic6'; this.name = ''; }
  }

  submit(): void {
    this.saving.set(true);
    this.service.createFurniture({ roomId: this.roomId(), type: this.type, name: this.name.trim() || null })
      .subscribe({
        next: () => { this.saving.set(false); this.creating.set(false); this.toast.success('Meuble créé.'); this.changed.emit(); },
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
