import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { StorageBinderDto, StorageBoxDto } from '../storage.models';
import { StorageIconComponent } from './storage-icon.component';

/** Payload d'un drop de carte stock sur un classeur (depuis la popup d'assignation). */
export interface StockDropEvent {
  binderId: string;
  stockOwnerId: string;
}

/**
 * Classeurs du carton sélectionné (présentationnel : la liste vient du parent).
 * Cliquer un classeur le sélectionne (mis en avant, et ouvert dans la vue 3D).
 */
@Component({
  selector: 'app-storage-binder-list',
  standalone: true,
  imports: [FormsModule, HasPermissionDirective, StorageIconComponent],
  styleUrls: ['../controls.scss'],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Classeurs</h3>
        @if (box(); as b) { <span class="ctx">{{ b.barcode || b.internalCode }}</span> }
      </header>

      @if (!box()) {
        <p class="empty">Sélectionnez un carton pour voir ses classeurs.</p>
      } @else {
        @if (binders().length === 0) {
          <p class="empty">Aucun classeur dans ce carton.</p>
        } @else {
          <ul class="list">
            @for (b of binders(); track b.id; let i = $index) {
              <li class="bd" draggable="true"
                  [class.bd--sel]="b.id === selectedId()"
                  [class.bd--drop]="b.id === dropTargetId()"
                  (dragstart)="onDragStart($event, b)"
                  (dragover)="onBinderDragOver($event, b)"
                  (dragleave)="onBinderDragLeave()"
                  (drop)="onBinderDrop($event, b)"
                  (click)="select(b)">
                <app-storage-icon class="binder-icon" name="binder" [variant]="i" [size]="22" />
                <span class="lbl">{{ b.label }}</span>
                @if (b.positionInBox != null) { <span class="pos-tag">#{{ b.positionInBox }}</span> }
              </li>
            }
          </ul>
        }

        <form *hasPermission="'storage:write'" class="add" (ngSubmit)="add()">
          <input class="input sm" placeholder="Nouveau classeur" [(ngModel)]="label" name="lbl" required />
          <input class="input sm pos-in" type="number" placeholder="#" [(ngModel)]="position" name="pos" />
          <button type="submit" class="btn-primary sm" [disabled]="saving() || !label">+</button>
        </form>
      }
    </section>
  `,
  styles: [`
    .panel { border: 1px solid #e2e2e2; border-radius: 12px; padding: .85rem; display: flex; flex-direction: column; gap: .55rem; }
    .panel__head { display: flex; justify-content: space-between; align-items: center; }
    h3 { margin: 0; font-size: .95rem; }
    .ctx { font-size: .75rem; color: #888; }
    .empty { color: #999; font-size: .82rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .3rem; }
    .bd { display: flex; align-items: center; gap: .5rem; padding: .35rem .5rem; background: #fafafa; border: 1px solid transparent; border-radius: 7px; font-size: .84rem; cursor: pointer; transition: border-color .15s, background .15s; }
    .bd:hover { background: #f2f2f2; }
    .bd--sel { border-color: #2E5AAC; background: #eef3fc; }
    .bd--drop { border-color: #16a34a; background: #f0fdf4; border-style: dashed; }
    .binder-icon { flex: 0 0 auto; }
    .lbl { flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .pos-tag { flex: 0 0 auto; font-size: .7rem; color: #9aa1ad; font-weight: 600; }
    .add { display: flex; gap: .4rem; }
    .add .input { flex: 1; }
    .pos-in { flex: 0 0 3.2rem; }
  `],
})
export class StorageBinderListComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly box = input<StorageBoxDto | null>(null);
  readonly binders = input<StorageBinderDto[]>([]);
  readonly selectedId = input<string | null>(null);

  readonly binderSelected = output<StorageBinderDto>();
  readonly changed = output<void>();
  readonly stockDropped = output<StockDropEvent>();

  readonly dropTargetId = signal<string | null>(null);

  readonly saving = signal(false);
  label = '';
  position: number | null = null;

  select(b: StorageBinderDto): void { this.binderSelected.emit(b); }

  add(): void {
    const b = this.box();
    if (!b || !this.label.trim()) return;
    this.saving.set(true);
    this.service.createBinder({
      label: this.label.trim(),
      boxId: b.id,
      positionInBox: this.position != null ? Number(this.position) : null,
      furnitureId: null,
      levelIndex: null,
    }).subscribe({
      next: () => {
        this.saving.set(false); this.label = ''; this.position = null;
        this.toast.success('Classeur ajouté.'); this.changed.emit();
      },
      error: () => { this.saving.set(false); this.toast.error('Ajout impossible.'); },
    });
  }

  onDragStart(event: DragEvent, b: StorageBinderDto): void {
    event.dataTransfer?.setData('text/plain', `binder:${b.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onBinderDragOver(event: DragEvent, b: StorageBinderDto): void {
    const payload = event.dataTransfer?.getData('text/plain') ?? '';
    if (!payload.startsWith('stock:')) return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
    this.dropTargetId.set(b.id);
  }

  onBinderDragLeave(): void {
    this.dropTargetId.set(null);
  }

  onBinderDrop(event: DragEvent, b: StorageBinderDto): void {
    this.dropTargetId.set(null);
    const payload = event.dataTransfer?.getData('text/plain') ?? '';
    if (!payload.startsWith('stock:')) return;
    event.preventDefault();
    const stockOwnerId = payload.slice('stock:'.length);
    this.stockDropped.emit({ binderId: b.id, stockOwnerId });
  }
}
