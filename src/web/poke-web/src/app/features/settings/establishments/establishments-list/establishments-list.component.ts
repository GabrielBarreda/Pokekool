import {
  Component, inject, signal, OnInit, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { EstablishmentStore } from '../establishment.store';
import { EstablishmentDto, CreateEstablishmentRequest, UpdateEstablishmentRequest } from '../establishment.models';
import { EstablishmentFormComponent } from '../establishment-form/establishment-form.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

type ModalState = 'closed' | 'create' | 'edit';

@Component({
  selector: 'app-establishments-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, EstablishmentFormComponent, ModalComponent, ConfirmDialogComponent,
    BadgeComponent, PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Établissements" subtitle="Gérez les établissements de votre enseigne">
        <button *hasPermission="'establishments.write'" class="btn-primary" (click)="openCreate()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M12 5v14M5 12h14"/>
          </svg>
          Nouvel établissement
        </button>
      </app-page-header>

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (store.establishments().length === 0) {
        <app-empty-state icon="🏪" title="Aucun établissement"
          description="Créez votre premier établissement pour commencer."
          actionLabel="Créer un établissement" (actionClicked)="openCreate()" />
      } @else {
        <div class="grid">
          @for (e of store.establishments(); track e.id) {
            <div class="card" [class.card--inactive]="!e.isActive">
              <div class="card__head">
                <span class="card__name">{{ e.name }}</span>
                <app-badge [variant]="e.isActive ? 'success' : 'neutral'">
                  {{ e.isActive ? 'Actif' : 'Inactif' }}
                </app-badge>
              </div>

              @if (e.address?.city || e.address?.line1) {
                <p class="card__address">{{ formatAddress(e) }}</p>
              }

              <div class="card__meta">
                @if (e.phone) {
                  <span class="meta">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.49 13a19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 3.4 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 9.91a16 16 0 0 0 6.16 6.16l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/>
                    </svg>
                    {{ e.phone }}
                  </span>
                }
                @if (e.email) {
                  <span class="meta">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
                    </svg>
                    {{ e.email }}
                  </span>
                }
              </div>

              <div class="card__footer">
                <button *hasPermission="'establishments.write'" class="act-btn" (click)="openEdit(e)">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                  </svg>
                  Modifier
                </button>
                <button *hasPermission="'establishments.delete'" class="act-btn act-btn--danger" (click)="confirmDelete(e)">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="3 6 5 6 21 6"/>
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                    <path d="M10 11v6M14 11v6M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                  </svg>
                  Supprimer
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>

    @if (modal() === 'create') {
      <app-modal title="Nouvel établissement" (closed)="closeModal()">
        <app-establishment-form mode="create" [saving]="store.saving()"
          (submitted)="onCreate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    @if (modal() === 'edit' && editTarget()) {
      <app-modal title="Modifier l'établissement" width="720px" (closed)="closeModal()">
        <app-establishment-form mode="edit" [initialData]="editTarget()" [saving]="store.saving()"
          (submitted)="onUpdate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    @if (deleteTarget()) {
      <app-confirm-dialog
        title="Supprimer l'établissement"
        [message]="'Supprimer « ' + deleteTarget()!.name + ' » ? Cette action est irréversible.'"
        confirmLabel="Supprimer" variant="danger"
        (confirmed)="doDelete()" (cancelled)="deleteTarget.set(null)" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-6); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: var(--space-4);
    }

    .card {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      padding: var(--space-5);
      display: flex; flex-direction: column; gap: var(--space-3);
      transition: box-shadow var(--duration-fast);
      &:hover { box-shadow: var(--shadow-md); }
      &--inactive { opacity: 0.6; }
    }
    .card__head {
      display: flex; align-items: flex-start;
      justify-content: space-between; gap: var(--space-2);
    }
    .card__name { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .card__address { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin: 0; }
    .card__meta { display: flex; flex-wrap: wrap; gap: var(--space-3); }
    .meta {
      display: flex; align-items: center; gap: var(--space-1);
      font-size: var(--font-size-sm); color: var(--color-text-muted);
    }
    .card__footer {
      display: flex; gap: var(--space-2);
      padding-top: var(--space-3); border-top: 1px solid var(--color-border-subtle);
      margin-top: auto;
    }
    .act-btn {
      display: flex; align-items: center; gap: var(--space-1);
      height: 32px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: transparent; color: var(--color-text-secondary);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover { background: var(--color-neutral-100); color: var(--color-text-primary); }
      &--danger:hover {
        border-color: var(--color-semantic-danger);
        color: var(--color-semantic-danger);
        background: var(--color-semantic-danger-subtle);
      }
    }
    .btn-primary {
      display: flex; align-items: center; gap: var(--space-2);
      height: 38px; padding: 0 var(--space-4); border: none;
      border-radius: var(--radius-md); background: var(--color-brand-primary);
      color: var(--color-brand-on-primary); font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium); cursor: pointer;
      &:hover { background: var(--color-brand-primary-hover); }
    }
  `],
})
export class EstablishmentsListComponent implements OnInit {
  readonly store = inject(EstablishmentStore);
  readonly modal = signal<ModalState>('closed');
  readonly editTarget = signal<EstablishmentDto | null>(null);
  readonly deleteTarget = signal<EstablishmentDto | null>(null);

  ngOnInit(): void { this.store.load(); }

  openCreate(): void { this.modal.set('create'); }
  openEdit(e: EstablishmentDto): void { this.editTarget.set(e); this.modal.set('edit'); }
  confirmDelete(e: EstablishmentDto): void { this.deleteTarget.set(e); }
  closeModal(): void { this.modal.set('closed'); this.editTarget.set(null); }

  onCreate(body: CreateEstablishmentRequest | UpdateEstablishmentRequest): void {
    this.store.create({ body: body as CreateEstablishmentRequest, onSuccess: () => this.closeModal() });
  }
  onUpdate(body: CreateEstablishmentRequest | UpdateEstablishmentRequest): void {
    const id = this.editTarget()?.id;
    if (!id) return;
    this.store.update({ id, body: body as UpdateEstablishmentRequest, onSuccess: () => this.closeModal() });
  }
  doDelete(): void {
    const t = this.deleteTarget();
    if (!t) return;
    this.store.remove({ id: t.id, name: t.name });
    this.deleteTarget.set(null);
  }

  formatAddress(e: EstablishmentDto): string {
    const a = e.address;
    return [a.line1, a.postalCode && a.city ? `${a.postalCode} ${a.city}` : (a.city ?? a.postalCode)].filter(Boolean).join(', ');
  }
}
