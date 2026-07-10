import { Component, inject, input, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { StorageBinderDto, StorageBoxDto } from '../storage.models';

@Component({
  selector: 'app-storage-binder-list',
  standalone: true,
  imports: [FormsModule, HasPermissionDirective],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Classeurs</h3>
        @if (box()) {
          <span class="ctx">Carton {{ box()!.barcode || box()!.internalCode }}</span>
        }
      </header>

      @if (!box()) {
        <p class="empty">Sélectionnez un carton pour voir ses classeurs.</p>
      } @else {
        @if (binders().length === 0) {
          <p class="empty">Aucun classeur dans ce carton.</p>
        } @else {
          <ul class="list">
            @for (b of binders(); track b.id) {
              <li>
                <span class="pos">{{ b.positionInBox ?? '·' }}</span>
                <span>{{ b.label }}</span>
              </li>
            }
          </ul>
        }

        <form *hasPermission="'storage:write'" class="add" (ngSubmit)="add()">
          <input class="input" placeholder="Libellé du classeur" [(ngModel)]="label" name="lbl" required />
          <input class="input input--pos" type="number" placeholder="#" [(ngModel)]="position" name="pos" />
          <button type="submit" class="btn-primary" [disabled]="saving() || !label">Ajouter</button>
        </form>
      }
    </section>
  `,
  styles: [`
    .panel { border: 1px solid #e2e2e2; border-radius: 12px; padding: 1rem; display: flex; flex-direction: column; gap: .75rem; }
    .panel__head { display: flex; justify-content: space-between; align-items: center; }
    .ctx { font-size: .8rem; color: #777; }
    .empty { color: #999; font-size: .85rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .3rem; }
    .list li { display: flex; align-items: center; gap: .6rem; padding: .35rem .5rem; background: #fafafa; border-radius: 6px; font-size: .85rem; }
    .pos { display: inline-flex; width: 1.5rem; height: 1.5rem; align-items: center; justify-content: center; background: #4CAF50; color: #fff; border-radius: 50%; font-size: .75rem; }
    .add { display: flex; gap: .5rem; }
    .add .input { flex: 1; }
    .input--pos { flex: 0 0 4rem; }
  `],
})
export class StorageBinderListComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly box = input<StorageBoxDto | null>(null);

  readonly binders = signal<StorageBinderDto[]>([]);
  readonly saving = signal(false);
  label = '';
  position: number | null = null;

  constructor() {
    effect(() => { const b = this.box(); if (b) this.reload(b.id); else this.binders.set([]); });
  }

  private reload(boxId: string): void {
    this.service.getBinders(boxId).subscribe({
      next: b => this.binders.set(b),
      error: () => this.toast.error('Impossible de charger les classeurs.'),
    });
  }

  add(): void {
    const b = this.box();
    if (!b || !this.label.trim()) return;
    this.saving.set(true);
    this.service.createBinder({
      boxId: b.id, label: this.label.trim(),
      positionInBox: this.position != null ? Number(this.position) : null,
    }).subscribe({
      next: () => {
        this.saving.set(false); this.label = ''; this.position = null;
        this.toast.success('Classeur ajouté.'); this.reload(b.id);
      },
      error: () => { this.saving.set(false); this.toast.error('Ajout impossible.'); },
    });
  }
}
