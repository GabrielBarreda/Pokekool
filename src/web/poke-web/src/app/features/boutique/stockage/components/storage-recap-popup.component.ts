import { Component, input, output } from '@angular/core';
import { StorageRecapModel } from '../storage.models';
import { StorageIconComponent } from './storage-icon.component';

/**
 * Carte récapitulative flottante affichée quand un objet 3D est sélectionné
 * (carton / meuble / classeur). Ancrée en haut-droite de la vue 3D.
 * Actions contextuelles : Ouvrir (carton/classeur) et Supprimer (carton/meuble).
 * Purement présentationnel : le modèle et les actions sont gérés par le conteneur.
 */
@Component({
  selector: 'app-storage-recap-popup',
  standalone: true,
  imports: [StorageIconComponent],
  template: `
    <aside class="recap" [attr.data-kind]="model().kind">
      <header class="recap__head">
        <app-storage-icon class="recap__ic"
          [name]="model().kind === 'box' ? 'box' : model().kind === 'binder' ? 'binder' : 'furniture'"
          [size]="22" />
        <div class="recap__titles">
          <strong class="recap__title">{{ model().title }}</strong>
          <span class="recap__sub">{{ model().subtitle }}</span>
        </div>
        <button type="button" class="recap__close" title="Fermer" (click)="close.emit()">✕</button>
      </header>

      <dl class="recap__grid">
        <div class="recap__row">
          <dt>Contenance</dt>
          <dd>{{ model().contenance }}</dd>
        </div>
        <div class="recap__row">
          <dt>Prix estimé</dt>
          <dd>{{ model().estimatedPrice ?? '—' }}</dd>
        </div>
        @if (model().barcode) {
          <div class="recap__row">
            <dt>Code-barre</dt>
            <dd class="mono">{{ model().barcode }}</dd>
          </div>
        }
        <div class="recap__row">
          <dt>État</dt>
          <dd>
            @if (model().placed) {
              <span class="pill pill--on">Placé en salle</span>
            } @else {
              <span class="pill">Non placé</span>
            }
          </dd>
        </div>
      </dl>

      <footer class="recap__actions">
        @if (model().kind === 'box' || model().kind === 'binder') {
          <button type="button" class="btn-open" (click)="openRequested.emit()">
            @if (model().kind === 'box') { Ouvrir le carton } @else { Ouvrir le classeur }
          </button>
        }
        @if (model().kind === 'box' || model().kind === 'furniture') {
          <button type="button" class="btn-del" (click)="deleteRequested.emit()" title="Supprimer">✕ Supprimer</button>
        }
      </footer>
    </aside>
  `,
  styles: [`
    .recap { position: absolute; top: .75rem; right: .75rem; z-index: 6; width: 252px;
             background: rgba(255,255,255,.97); backdrop-filter: blur(2px);
             border: 1px solid #e2e2e2; border-radius: 12px; box-shadow: 0 6px 22px rgba(0,0,0,.14);
             padding: .7rem .8rem; font-size: .82rem; }
    .recap__head { display: flex; align-items: flex-start; gap: .55rem; margin-bottom: .55rem; }
    .recap__ic { flex: 0 0 auto; margin-top: .1rem; }
    .recap__titles { flex: 1; min-width: 0; display: flex; flex-direction: column; }
    .recap__title { font-size: .9rem; line-height: 1.15; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .recap__sub { font-size: .72rem; color: #888; }
    .recap__close { border: none; background: none; color: #999; cursor: pointer; font-size: .85rem;
                    padding: 0 .1rem; flex: 0 0 auto; }
    .recap__close:hover { color: #333; }
    .recap__grid { margin: 0 0 .6rem; display: flex; flex-direction: column; gap: .3rem; }
    .recap__row { display: flex; justify-content: space-between; align-items: center; gap: .5rem; }
    .recap__row dt { color: #888; font-size: .74rem; margin: 0; }
    .recap__row dd { margin: 0; font-weight: 600; text-align: right; }
    .mono { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: .78rem; }
    .pill { display: inline-block; padding: .05rem .45rem; border-radius: 999px; font-size: .7rem;
            font-weight: 600; background: #f0f1f4; color: #6b7280; }
    .pill--on { background: #dcfce7; color: #166534; }
    .recap__actions { display: flex; gap: .4rem; }
    .recap__actions button { flex: 1; height: 30px; border-radius: 8px; font-size: .78rem; cursor: pointer; }
    .btn-open { background: #2E5AAC; border: 1px solid #2E5AAC; color: #fff; }
    .btn-open:hover { background: #24488a; }
    .btn-del { background: #fff; border: 1px solid #e0b4b4; color: #c0392b; flex: 0 0 auto; padding: 0 .6rem; }
    .btn-del:hover { background: #c0392b; color: #fff; border-color: #c0392b; }
  `],
})
export class StorageRecapPopupComponent {
  readonly model = input.required<StorageRecapModel>();
  readonly close = output<void>();
  readonly openRequested = output<void>();
  readonly deleteRequested = output<void>();
}
