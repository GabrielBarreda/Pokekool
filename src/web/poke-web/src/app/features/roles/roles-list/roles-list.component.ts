import {
  Component, inject, signal, OnInit, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RolesStore } from '../roles.store';
import { RoleDto, CreateRoleRequest, UpdateRoleRequest } from '../roles.models';
import { RoleFormComponent } from '../role-form/role-form.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

type ModalState = 'closed' | 'create' | 'edit';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, RoleFormComponent, ModalComponent, ConfirmDialogComponent,
    BadgeComponent, PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Rôles & permissions" subtitle="Définissez les accès de chaque profil">
        <button *hasPermission="'roles.write'" class="btn-primary" (click)="openCreate()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M12 5v14M5 12h14"/>
          </svg>
          Nouveau rôle
        </button>
      </app-page-header>

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (store.roles().length === 0) {
        <app-empty-state icon="🔑" title="Aucun rôle"
          description="Créez des rôles pour contrôler les accès de votre équipe."
          actionLabel="Créer un rôle" (actionClicked)="openCreate()" />
      } @else {

        <!-- Rôles système -->
        @if (store.systemRoles().length) {
          <section class="section">
            <div class="section-head">
              <h2 class="section-title">Rôles système</h2>
              <span class="section-sub">Prédéfinis par la plateforme — permissions non modifiables.</span>
            </div>
            <div class="grid">
              @for (r of store.systemRoles(); track r.id) {
                <div class="card card--system">
                  <div class="card-head">
                    <div>
                      <div class="role-name">{{ r.name }}</div>
                      <code class="role-code">{{ r.code }}</code>
                    </div>
                    <app-badge variant="neutral">Système</app-badge>
                  </div>
                  @if (r.description) { <p class="role-desc">{{ r.description }}</p> }
                  <div class="perm-preview">
                    @for (p of r.permissions.slice(0, 6); track p) {
                      <span class="perm-tag">{{ p }}</span>
                    }
                    @if (r.permissions.length > 6) {
                      <span class="perm-more">+{{ r.permissions.length - 6 }}</span>
                    }
                  </div>
                </div>
              }
            </div>
          </section>
        }

        <!-- Rôles personnalisés -->
        <section class="section">
          <div class="section-head">
            <h2 class="section-title">Rôles personnalisés</h2>
          </div>
          @if (!store.customRoles().length) {
            <p class="empty-msg">Aucun rôle personnalisé. Cliquez sur « Nouveau rôle » pour en créer un.</p>
          } @else {
            <div class="grid">
              @for (r of store.customRoles(); track r.id) {
                <div class="card">
                  <div class="card-head">
                    <div>
                      <div class="role-name">{{ r.name }}</div>
                      <code class="role-code">{{ r.code }}</code>
                    </div>
                    <div class="card-actions">
                      <button *hasPermission="'roles.write'" class="icon-btn" title="Modifier" (click)="openEdit(r)">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                        </svg>
                      </button>
                      <button *hasPermission="'roles.delete'" class="icon-btn icon-btn--danger" title="Supprimer" (click)="confirmDel(r)">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <polyline points="3 6 5 6 21 6"/>
                          <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                          <path d="M10 11v6M14 11v6M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                        </svg>
                      </button>
                    </div>
                  </div>
                  @if (r.description) { <p class="role-desc">{{ r.description }}</p> }
                  <div class="perm-preview">
                    @for (p of r.permissions.slice(0, 6); track p) {
                      <span class="perm-tag">{{ p }}</span>
                    }
                    @if (r.permissions.length > 6) {
                      <span class="perm-more">+{{ r.permissions.length - 6 }}</span>
                    }
                    @if (!r.permissions.length) {
                      <span class="no-perms">Aucune permission</span>
                    }
                  </div>
                </div>
              }
            </div>
          }
        </section>
      }
    </div>

    <!-- Create modal -->
    @if (modal() === 'create') {
      <app-modal title="Nouveau rôle" width="680px" (closed)="closeModal()">
        <app-role-form mode="create" [saving]="store.saving()"
          (submitted)="onCreate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    <!-- Edit modal -->
    @if (modal() === 'edit' && editTarget()) {
      <app-modal title="Modifier le rôle" width="680px" (closed)="closeModal()">
        <app-role-form mode="edit" [initialData]="editTarget()" [saving]="store.saving()"
          (submitted)="onUpdate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    <!-- Delete confirm -->
    @if (delTarget()) {
      <app-confirm-dialog
        title="Supprimer le rôle"
        [message]="'Supprimer « ' + delTarget()!.name + ' » ? Les membres qui ont ce rôle perdront les permissions associées.'"
        confirmLabel="Supprimer" variant="danger"
        (confirmed)="doDel()" (cancelled)="delTarget.set(null)" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-8); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }

    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .section-head { display: flex; flex-direction: column; gap: var(--space-1); }
    .section-title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }
    .section-sub { font-size: var(--font-size-sm); color: var(--color-text-muted); }
    .empty-msg { font-size: var(--font-size-sm); color: var(--color-text-muted); font-style: italic; }

    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: var(--space-4); }

    .card {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg); padding: var(--space-4);
      display: flex; flex-direction: column; gap: var(--space-3);
      transition: box-shadow var(--duration-fast);
      &:hover { box-shadow: var(--shadow-md); }
      &--system { background: var(--color-surface-raised); }
    }
    .card-head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--space-2); }
    .card-actions { display: flex; gap: var(--space-1); flex-shrink: 0; }

    .role-name { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .role-code {
      font-family: var(--font-family-mono); font-size: var(--font-size-xs);
      color: var(--color-text-muted); background: var(--color-neutral-100);
      padding: 1px 6px; border-radius: var(--radius-sm); display: inline-block; margin-top: 2px;
    }
    .role-desc { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin: 0; }

    .perm-preview { display: flex; flex-wrap: wrap; gap: var(--space-1); margin-top: auto; }
    .perm-tag {
      font-size: var(--font-size-xs); font-family: var(--font-family-mono);
      background: var(--color-surface-raised); border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-sm); padding: 2px 6px; color: var(--color-text-secondary);
    }
    .perm-more { font-size: var(--font-size-xs); color: var(--color-brand-primary); padding: 2px 0; }
    .no-perms { font-size: var(--font-size-xs); color: var(--color-text-muted); font-style: italic; }

    .icon-btn {
      width: 28px; height: 28px; display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent; border-radius: var(--radius-md);
      background: transparent; color: var(--color-text-secondary); cursor: pointer;
      &:hover { background: var(--color-neutral-100); border-color: var(--color-border-default); color: var(--color-text-primary); }
      &--danger:hover { background: var(--color-semantic-danger-subtle); border-color: var(--color-semantic-danger); color: var(--color-semantic-danger); }
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
export class RolesListComponent implements OnInit {
  readonly store = inject(RolesStore);
  readonly modal = signal<ModalState>('closed');
  readonly editTarget = signal<RoleDto | null>(null);
  readonly delTarget  = signal<RoleDto | null>(null);

  ngOnInit(): void { this.store.load(); }

  openCreate() { this.modal.set('create'); }
  openEdit(r: RoleDto) { this.editTarget.set(r); this.modal.set('edit'); }
  confirmDel(r: RoleDto) { this.delTarget.set(r); }
  closeModal() { this.modal.set('closed'); this.editTarget.set(null); }

  onCreate(body: CreateRoleRequest | UpdateRoleRequest) {
    this.store.create({ body: body as CreateRoleRequest, onSuccess: () => this.closeModal() });
  }
  onUpdate(body: CreateRoleRequest | UpdateRoleRequest) {
    const id = this.editTarget()?.id; if (!id) return;
    this.store.update({ id, body: body as UpdateRoleRequest, onSuccess: () => this.closeModal() });
  }
  doDel() {
    const t = this.delTarget(); if (!t) return;
    this.store.remove({ id: t.id, name: t.name });
    this.delTarget.set(null);
  }
}
