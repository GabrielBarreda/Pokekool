import {
  Component, inject, signal, OnInit, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '@core/auth/auth.store';
import { UsersStore } from '../users.store';
import { UserSummaryDto, roleBadgeVariant, userDisplayName, userInitials } from '../users.models';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-user-detail-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    BadgeComponent, ModalComponent, ConfirmDialogComponent, SpinnerComponent,
    HasPermissionDirective,
  ],
  template: `
    @if (user(); as u) {
      <app-modal [title]="displayName(u)" width="560px" (closed)="store.closeDetail()">

        <!-- Avatar + infos de base -->
        <div class="detail-body">
          <div class="profile-row">
            <div class="avatar-lg">{{ initials(u) }}</div>
            <div class="profile-info">
              <div class="profile-name">{{ displayName(u) }}</div>
              <div class="profile-email">{{ u.email }}</div>
              <div class="profile-meta">
                Inscrit le {{ u.createdAt | date:'dd/MM/yyyy' }}
                @if (u.lastLoginAt) {
                  · Dernière connexion {{ u.lastLoginAt | date:'dd/MM/yyyy HH:mm' }}
                }
              </div>
            </div>
          </div>

          <!-- Statuts -->
          <div class="badges-row">
            <app-badge [variant]="u.isActive ? 'success' : 'neutral'">
              {{ u.isActive ? 'Actif' : 'Inactif' }}
            </app-badge>
            @if (u.hasPendingMessages) {
              <app-badge variant="warning">Message en attente</app-badge>
            }
          </div>

          <!-- Section rôles -->
          <section class="section">
            <h3 class="section-title">Rôles</h3>

            <div class="roles-current">
              @if (u.roles.length === 0) {
                <span class="empty-roles">Aucun rôle assigné</span>
              } @else {
                @for (code of u.roles; track code) {
                  <app-badge [variant]="badgeVariant(code)">{{ code }}</app-badge>
                }
              }
            </div>

            <!-- Assignation multi-select -->
            @if (store.availableRoles().length) {
              <div *hasPermission="'users:assign_roles'" class="role-assign">
                <p class="assign-label">Modifier les rôles :</p>
                <div class="role-checkboxes">
                  @for (r of store.availableRoles(); track r.code) {
                    <label class="role-check">
                      <input
                        type="checkbox"
                        [checked]="selectedRoles().includes(r.code)"
                        (change)="toggleRole(r.code)" />
                      <span class="role-check-label">
                        <app-badge [variant]="badgeVariant(r.code)">{{ r.code }}</app-badge>
                        <span class="role-name">{{ r.name }}</span>
                      </span>
                    </label>
                  }
                </div>
                <button
                  class="btn-secondary"
                  [disabled]="!rolesChanged(u)"
                  (click)="saveRoles(u)">
                  Enregistrer les rôles
                </button>
              </div>
            }
          </section>

          <!-- Actions -->
          <div class="actions-row">
            @if (!isSelf(u.id)) {
              @if (u.isActive) {
                <button *hasPermission="'users:write'" class="btn-warning" (click)="deactivate(u)">
                  Désactiver
                </button>
              } @else {
                <button *hasPermission="'users:write'" class="btn-secondary" (click)="activate(u)">
                  Activer
                </button>
              }
              <button *hasPermission="'users:delete'" class="btn-danger" (click)="confirmDel.set(true)">
                Supprimer
              </button>
            }
          </div>
        </div>
      </app-modal>

      <!-- Confirm suppression -->
      @if (confirmDel()) {
        <app-confirm-dialog
          title="Supprimer l'utilisateur"
          [message]="'Supprimer ' + displayName(u) + ' ? Cette action est irréversible.'"
          confirmLabel="Supprimer" variant="danger"
          (confirmed)="doDelete(u.id)"
          (cancelled)="confirmDel.set(false)" />
      }
    }
  `,
  styles: [`
    .detail-body { display: flex; flex-direction: column; gap: var(--space-5); }

    /* Avatar + profil */
    .profile-row { display: flex; align-items: flex-start; gap: var(--space-4); }
    .avatar-lg {
      width: 52px; height: 52px; border-radius: var(--radius-full);
      background: var(--color-brand-primary-subtle); color: var(--color-brand-primary);
      font-size: var(--font-size-lg); font-weight: var(--font-weight-bold);
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
    }
    .profile-info { display: flex; flex-direction: column; gap: 2px; }
    .profile-name { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .profile-email { font-size: var(--font-size-sm); color: var(--color-text-secondary); }
    .profile-meta { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-top: 2px; }

    /* Badges statut */
    .badges-row { display: flex; gap: var(--space-2); flex-wrap: wrap; }

    /* Section rôles */
    .section { display: flex; flex-direction: column; gap: var(--space-3); padding-top: var(--space-2); border-top: 1px solid var(--color-border-subtle); }
    .section-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }

    .roles-current { display: flex; flex-wrap: wrap; gap: var(--space-2); min-height: 24px; }
    .empty-roles { font-size: var(--font-size-sm); color: var(--color-text-muted); font-style: italic; }

    .role-assign { display: flex; flex-direction: column; gap: var(--space-2); }
    .assign-label { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0; }
    .role-checkboxes { display: flex; flex-direction: column; gap: var(--space-2); }
    .role-check { display: flex; align-items: center; gap: var(--space-2); cursor: pointer; }
    .role-check input { accent-color: var(--color-brand-primary); }
    .role-check-label { display: flex; align-items: center; gap: var(--space-2); }
    .role-name { font-size: var(--font-size-sm); color: var(--color-text-secondary); }

    /* Actions */
    .actions-row {
      display: flex; gap: var(--space-2); justify-content: flex-end;
      padding-top: var(--space-2); border-top: 1px solid var(--color-border-subtle);
    }

    .btn-secondary {
      height: 36px; padding: 0 var(--space-4); border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default); background: var(--color-surface-base);
      color: var(--color-text-primary); font-size: var(--font-size-sm); cursor: pointer;
      &:hover:not(:disabled) { background: var(--color-neutral-100); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
    .btn-warning {
      height: 36px; padding: 0 var(--space-4); border-radius: var(--radius-md); border: none;
      background: var(--color-semantic-warning-subtle); color: var(--color-semantic-warning);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover { opacity: 0.85; }
    }
    .btn-danger {
      height: 36px; padding: 0 var(--space-4); border-radius: var(--radius-md); border: none;
      background: var(--color-semantic-danger); color: #fff;
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover { opacity: 0.85; }
    }
  `],
})
export class UserDetailModalComponent implements OnInit {
  readonly store     = inject(UsersStore);
  readonly authStore = inject(AuthStore);
  readonly confirmDel = signal(false);

  /** Copie locale des rôles sélectionnés pour le multi-select */
  readonly selectedRoles = signal<string[]>([]);

  readonly user = this.store.selectedUser;

  ngOnInit(): void {
    const u = this.user();
    if (u) this.selectedRoles.set([...u.roles]);
  }

  readonly displayName = userDisplayName;
  readonly initials    = userInitials;
  readonly badgeVariant = roleBadgeVariant;

  isSelf(id: string): boolean {
    return this.authStore.user()?.id === id;
  }

  toggleRole(code: string): void {
    const current = this.selectedRoles();
    this.selectedRoles.set(
      current.includes(code) ? current.filter(c => c !== code) : [...current, code]
    );
  }

  rolesChanged(u: UserSummaryDto): boolean {
    const a = [...u.roles].sort().join(',');
    const b = [...this.selectedRoles()].sort().join(',');
    return a !== b;
  }

  saveRoles(u: UserSummaryDto): void {
    this.store.assignRoles({ id: u.id, roleCodes: this.selectedRoles() });
  }

  activate(u: UserSummaryDto): void   { this.store.activateUser(u.id); }
  deactivate(u: UserSummaryDto): void { this.store.deactivateUser(u.id); }

  doDelete(id: string): void {
    this.store.deleteUser(id);
    this.confirmDel.set(false);
    this.store.closeDetail();
  }
}
