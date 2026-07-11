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
  styleUrls: ['../controls.scss'],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Cartons</h3>
        <button *hasPermission="'storage:write'" class="btn-primary sm" (click)="toggleCreate()">+ Carton</button>
      </header>

      <input class="input sm" placeholder="Rechercher par code…"
             [(ngModel)]="lookupCode" (keyup.enter)="doLookup()" />
      @if (lookupResult(); as res) {
        <div class="lookup-result">
          <strong>{{ res.box.barcode || res.box.internalCode }}</strong>
          — {{ res.binders.length }} classeur(s)
          <button class="link" (click)="pick(res.box)">ouvrir</button>
        </div>
      }

      @if (creating()) {
        <form class="form" (ngSubmit)="submit()">
          <select class="input sm" [(ngModel)]="draft.labelOrigin" name="lo">
            <option value="Supplier">Fournisseur (code-barre)</option>
            <option value="Internal">Interne (code maison)</option>
          </select>
          @if (draft.labelOrigin === 'Supplier') {
            <input class="input sm" placeholder="Code-barre" [(ngModel)]="draft.barcode" name="bc" required />
            <input class="input sm" placeholder="Fournisseur" [(ngModel)]="draft.supplierName" name="sn" />
          } @else {
            <input class="input sm" placeholder="Code interne" [(ngModel)]="draft.internalCode" name="ic" required />
          }
          <select class="input sm" [(ngModel)]="draft.size" name="sz">
            <option [ngValue]="null">Taille…</option>
            @for (s of sizes; track s) { <option [ngValue]="s">{{ s }}</option> }
          </select>
          <div class="form__actions">
            <button type="button" class="btn-secondary sm" (click)="toggleCreate()">Annuler</button>
            <button type="submit" class="btn-primary sm" [disabled]="saving()">Créer</button>
          </div>
        </form>
      }

      @if (boxes().length === 0) {
        <p class="empty">Aucun carton. Créez-en un, puis glissez-le dans la vue 3D.</p>
      } @else {
        <ul class="list">
          @for (b of boxes(); track b.id) {
            <li class="ck" draggable="true"
                [class.ck--sel]="b.id === selectedId()"
                (dragstart)="onDragStart($event, b)"
                (click)="pick(b)">
              <span class="ck__grip" title="Glisser dans la vue 3D">⠿</span>
              <span class="ck__body">
                <span class="ck__code">{{ b.barcode || b.internalCode }}</span>
                <span class="ck__meta">{{ originShort(b.labelOrigin) }}{{ b.size ? ' · ' + b.size : '' }}</span>
              </span>
              @if (isPlaced(b)) {
                <span class="ck__dot ck__dot--on" title="Placé dans la salle"></span>
              } @else {
                <span class="ck__dot" title="Non placé — glissez-le dans la vue 3D"></span>
              }
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
    .lookup-result { background: #f4f9f4; padding: .4rem .6rem; border-radius: 8px; font-size: .8rem; }
    .form { display: flex; flex-direction: column; gap: .4rem; background: #fafafa; padding: .5rem; border-radius: 8px; }
    .form__actions { display: flex; justify-content: flex-end; gap: .4rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .35rem; }
    .ck { display: flex; align-items: center; gap: .5rem; padding: .5rem .55rem; border: 1px solid #e6e6e6; border-radius: 9px; cursor: grab; background: #fff; }
    .ck:hover { border-color: #bcd; }
    .ck:active { cursor: grabbing; }
    .ck--sel { border-color: #4CAF50; background: #f2fbf2; }
    .ck__grip { color: #bbb; font-size: 1rem; }
    .ck__body { display: flex; flex-direction: column; flex: 1; min-width: 0; }
    .ck__code { font-weight: 600; font-size: .85rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .ck__meta { font-size: .72rem; color: #888; }
    .ck__dot { width: .55rem; height: .55rem; border-radius: 50%; background: #d7d7d7; flex: 0 0 auto; }
    .ck__dot--on { background: #4CAF50; }
    .empty { color: #999; font-size: .82rem; }
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
    this.service.getBoxes(this.roomId(), null).subscribe({
      next: b => this.boxes.set(b),
      error: () => this.toast.error('Impossible de charger les cartons.'),
    });
  }

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

  /** Rafraîchit la liste depuis l'extérieur (après un drop qui déplace un carton). */
  refresh(): void { this.reload(); }

  onDragStart(event: DragEvent, box: StorageBoxDto): void {
    event.dataTransfer?.setData('text/plain', box.id);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  pick(box: StorageBoxDto): void { this.boxPicked.emit(box); }

  isPlaced(b: StorageBoxDto): boolean { return !!b.position3dJson; }
  originShort(o: LabelOrigin): string { return LABEL_ORIGIN_LABELS[o].split(' ')[0]; }
}
