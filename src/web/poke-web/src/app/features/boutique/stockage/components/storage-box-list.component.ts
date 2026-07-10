import { Component, inject, input, output, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import {
  StorageBoxDto, LabelOrigin, LABEL_ORIGIN_LABELS, BOX_SIZES,
  CreateStorageBoxRequest, StorageBoxLookupDto,
} from '../storage.models';

const EMPTY = (roomId: string): CreateStorageBoxRequest => ({
  roomId, labelOrigin: 'Supplier', barcode: null, internalCode: null,
  supplierName: null, size: null, position3dJson: null, notes: null,
});

@Component({
  selector: 'app-storage-box-list',
  standalone: true,
  imports: [FormsModule, HasPermissionDirective],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Cartons</h3>
        <div class="panel__filters">
          <select class="input" [ngModel]="labelFilter()" (ngModelChange)="onFilter($event)">
            <option value="">Toutes origines</option>
            <option value="Supplier">Fournisseur</option>
            <option value="Internal">Interne</option>
          </select>
          <button *hasPermission="'storage:write'" class="btn-primary" (click)="toggleCreate()">+ Carton</button>
        </div>
      </header>

      <!-- Recherche par code (lookup "scan théorique") -->
      <div class="lookup">
        <input class="input" placeholder="Rechercher par code-barre ou code interne…"
               [(ngModel)]="lookupCode" (keyup.enter)="doLookup()" />
        <button class="btn-secondary" (click)="doLookup()" [disabled]="!lookupCode">Rechercher</button>
      </div>
      @if (lookupResult(); as res) {
        <div class="lookup-result">
          <strong>{{ res.box.barcode || res.box.internalCode }}</strong>
          — {{ res.binders.length }} classeur(s),
          {{ totalLocations(res) }} item(s) attendu(s)
          <button class="link" (click)="pick(res.box)">ouvrir</button>
        </div>
      }

      <!-- Formulaire création -->
      @if (creating()) {
        <form class="form" (ngSubmit)="submit()">
          <div class="row">
            <label>Origine
              <select class="input" [(ngModel)]="draft.labelOrigin" name="lo">
                <option value="Supplier">Fournisseur (code-barre)</option>
                <option value="Internal">Interne (code maison)</option>
              </select>
            </label>
            @if (draft.labelOrigin === 'Supplier') {
              <label>Code-barre
                <input class="input" [(ngModel)]="draft.barcode" name="bc" required />
              </label>
              <label>Fournisseur
                <input class="input" [(ngModel)]="draft.supplierName" name="sn" />
              </label>
            } @else {
              <label>Code interne
                <input class="input" [(ngModel)]="draft.internalCode" name="ic" required />
              </label>
            }
            <label>Taille
              <select class="input" [(ngModel)]="draft.size" name="sz">
                <option [ngValue]="null">—</option>
                @for (s of sizes; track s) { <option [ngValue]="s">{{ s }}</option> }
              </select>
            </label>
          </div>
          <label>Notes
            <input class="input" [(ngModel)]="draft.notes" name="nt" />
          </label>
          <div class="form__actions">
            <button type="button" class="btn-secondary" (click)="toggleCreate()">Annuler</button>
            <button type="submit" class="btn-primary" [disabled]="saving()">Créer</button>
          </div>
        </form>
      }

      <!-- Liste -->
      @if (boxes().length === 0) {
        <p class="empty">Aucun carton dans cette salle.</p>
      } @else {
        <table class="tbl">
          <thead><tr><th>Code</th><th>Origine</th><th>Fournisseur</th><th>Taille</th><th></th></tr></thead>
          <tbody>
            @for (b of boxes(); track b.id) {
              <tr [class.selected]="b.id === selectedId()">
                <td>{{ b.barcode || b.internalCode }}</td>
                <td>{{ originLabel(b.labelOrigin) }}</td>
                <td>{{ b.supplierName || '—' }}</td>
                <td>{{ b.size || '—' }}</td>
                <td><button class="link" (click)="pick(b)">Classeurs →</button></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: [`
    .panel { border: 1px solid #e2e2e2; border-radius: 12px; padding: 1rem; display: flex; flex-direction: column; gap: .75rem; }
    .panel__head { display: flex; justify-content: space-between; align-items: center; }
    .panel__filters { display: flex; gap: .5rem; }
    .lookup { display: flex; gap: .5rem; }
    .lookup .input { flex: 1; }
    .lookup-result { background: #f4f9f4; padding: .5rem .75rem; border-radius: 8px; font-size: .85rem; }
    .form { display: flex; flex-direction: column; gap: .5rem; background: #fafafa; padding: .75rem; border-radius: 8px; }
    .row { display: flex; gap: .5rem; flex-wrap: wrap; }
    label { display: flex; flex-direction: column; font-size: .8rem; gap: .2rem; }
    .form__actions { display: flex; justify-content: flex-end; gap: .5rem; }
    .tbl { width: 100%; border-collapse: collapse; font-size: .85rem; }
    .tbl th, .tbl td { text-align: left; padding: .4rem; border-bottom: 1px solid #eee; }
    tr.selected { background: #eef7ee; }
    .empty { color: #999; font-size: .85rem; }
    .link { background: none; border: none; color: #4CAF50; cursor: pointer; }
  `],
})
export class StorageBoxListComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly roomId = input.required<string>();
  readonly selectedId = input<string | null>(null);
  readonly boxPicked = output<StorageBoxDto>();
  readonly boxesChanged = output<void>();

  readonly boxes = signal<StorageBoxDto[]>([]);
  readonly labelFilter = signal<string>('');
  readonly creating = signal(false);
  readonly saving = signal(false);
  readonly lookupResult = signal<StorageBoxLookupDto | null>(null);
  lookupCode = '';

  readonly sizes = BOX_SIZES;
  draft: CreateStorageBoxRequest = EMPTY('');

  constructor() {
    effect(() => { const id = this.roomId(); if (id) this.reload(); });
  }

  private reload(): void {
    this.service.getBoxes(this.roomId(), this.labelFilter() || null).subscribe({
      next: b => this.boxes.set(b),
      error: () => this.toast.error('Impossible de charger les cartons.'),
    });
  }

  onFilter(v: string): void { this.labelFilter.set(v); this.reload(); }

  toggleCreate(): void {
    this.creating.update(v => !v);
    if (this.creating()) this.draft = EMPTY(this.roomId());
  }

  submit(): void {
    this.saving.set(true);
    this.service.createBox({ ...this.draft, roomId: this.roomId() }).subscribe({
      next: () => {
        this.saving.set(false); this.creating.set(false);
        this.toast.success('Carton créé.'); this.reload(); this.boxesChanged.emit();
      },
      error: () => { this.saving.set(false); this.toast.error('Création impossible (code déjà utilisé ?).'); },
    });
  }

  doLookup(): void {
    if (!this.lookupCode.trim()) return;
    this.service.lookup(this.lookupCode.trim()).subscribe({
      next: r => this.lookupResult.set(r),
      error: () => { this.lookupResult.set(null); this.toast.error('Aucun carton pour ce code.'); },
    });
  }

  pick(box: StorageBoxDto): void { this.boxPicked.emit(box); }

  originLabel(o: LabelOrigin): string { return LABEL_ORIGIN_LABELS[o]; }
  totalLocations(res: StorageBoxLookupDto): number {
    return res.binders.reduce((sum, b) => sum + b.locations.length, 0);
  }
}
