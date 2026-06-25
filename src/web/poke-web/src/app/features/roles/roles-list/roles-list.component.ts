import {
  Component, inject, signal, computed, OnInit, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RolesStore } from '../roles.store';
import { RoleDto, CreateRoleRequest, UpdateRoleRequest, PermissionDto } from '../roles.models';
import { RoleFormComponent } from '../role-form/role-form.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

type ModalState = 'closed' | 'create' | 'edit';

interface PermCategoryGroup {
  category: string;
  label: string;
  perms: PermissionDto[];
}

const PERM_DESCS: Record<string, string> = {
  'cards:read':           'Parcourir le catalogue de cartes et sets Pokémon',
  'cards:sync':           'Lancer une mise à jour du catalogue depuis la source officielle',
  'users:read':           'Voir la liste des membres et leurs informations',
  'users:write':          'Ajouter de nouveaux membres et modifier leurs informations',
  'users:delete':         'Retirer définitivement un membre de la plateforme',
  'users:assign_roles':   'Changer le rôle attribué à un membre',
  'users:reset_password': 'Envoyer un lien de réinitialisation de mot de passe à un membre',
  'roles:read':           'Voir les rôles existants et leurs permissions',
  'roles:write':          'Créer de nouveaux rôles et modifier ceux qui existent',
  'roles:delete':         'Supprimer un rôle personnalisé',
  'settings:read':        'Consulter la configuration de la plateforme',
  'settings:write':       'Modifier la configuration de la plateforme',
};

const CAT_LABELS: Record<string, string> = {
  cards:      'Cartes',
  users:      'Utilisateurs',
  roles:      'Rôles',
  settings:   'Paramètres',
  collection: 'Collection',
  alerts:     'Alertes',
  messages:   'Messagerie',
};

@Component({
  selector: 'app-roles-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, RoleFormComponent, ModalComponent, ConfirmDialogComponent,
    PageHeaderComponent, SpinnerComponent, EmptyStateComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Rôles & permissions" subtitle="Définissez les accès de chaque profil">
        <button *hasPermission="'roles:write'" class="btn-primary" (click)="openCreate()">
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

        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th class="th th--name">Rôle</th>
                <th class="th th--type">Type</th>
                <th class="th th--perms">Permissions</th>
                <th class="th th--actions"></th>
              </tr>
            </thead>
            <tbody>
              @for (r of sortedRoles(); track r.id) {
                <tr class="row">
                  <!-- Nom + code -->
                  <td class="td td--name">
                    <div class="role-icon" [class]="'role-icon--' + (r.isSystem ? 'system' : 'custom')">
                      @if (r.isSystem) {
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                        </svg>
                      } @else {
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <circle cx="12" cy="12" r="3"/>
                          <path d="M19.07 4.93a10 10 0 0 1 0 14.14M4.93 4.93a10 10 0 0 0 0 14.14"/>
                        </svg>
                      }
                    </div>
                    <div class="role-meta">
                      <span class="role-name">{{ r.name }}</span>
                      <code class="role-code">{{ r.code }}</code>
                    </div>
                  </td>

                  <!-- Badge type -->
                  <td class="td">
                    @if (r.isSystem) {
                      <span class="badge badge--system">Système</span>
                    } @else {
                      <span class="badge badge--custom">Personnalisé</span>
                    }
                  </td>

                  <!-- Permissions -->
                  <td class="td td--perms">
                    @if (isSuperAdmin(r)) {
                      <!-- Super Admin : accès total -->
                      <span class="full-access">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                          <polyline points="20 6 9 17 4 12"/>
                        </svg>
                        Accès total
                      </span>
                    } @else if (r.permissions.length === 0) {
                      <span class="no-perms">Aucune permission</span>
                    } @else {
                      <!-- Chips catégories -->
                      <div class="cat-chips">
                        @for (g of groupedPerms(r.permissions); track g.category) {
                          <span class="cat-chip" [attr.data-cat]="g.category"
                                (click)="toggleExpand(r.id, g.category)" title="{{ g.label }}">
                            {{ g.label }}
                            <span class="cat-chip-count">{{ g.perms.length }}</span>
                          </span>
                        }
                      </div>

                      <!-- Expanded : détail d'une catégorie -->
                      @if (expandedKey() && expandedRoleId() === r.id) {
                        @for (g of groupedPerms(r.permissions); track g.category) {
                          @if (g.category === expandedKey()) {
                            <div class="perm-detail">
                              <div class="perm-detail-head">
                                <span class="perm-detail-title">{{ g.label }}</span>
                                <button class="perm-detail-close" (click)="closeExpand()">
                                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                                  </svg>
                                </button>
                              </div>
                              @for (p of g.perms; track p.code) {
                                <div class="perm-row">
                                  <span class="perm-action" [attr.data-action]="actionOf(p.code)">
                                    {{ actionLabelOf(p.code) }}
                                  </span>
                                  <span class="perm-desc">{{ descOf(p.code, p.description) }}</span>
                                </div>
                              }
                            </div>
                          }
                        }
                      }
                    }
                  </td>

                  <!-- Actions -->
                  <td class="td td--actions">
                    @if (!r.isSystem) {
                      <div class="actions">
                        <button *hasPermission="'roles:write'" class="icon-btn" title="Modifier" (click)="openEdit(r)">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                          </svg>
                        </button>
                        <button *hasPermission="'roles:delete'" class="icon-btn icon-btn--danger" title="Supprimer" (click)="confirmDel(r)">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <polyline points="3 6 5 6 21 6"/>
                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                            <path d="M10 11v6M14 11v6M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                          </svg>
                        </button>
                      </div>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Modale création -->
    @if (modal() === 'create') {
      <app-modal title="Nouveau rôle" width="680px" (closed)="closeModal()">
        <app-role-form mode="create" [saving]="store.saving()"
          (submitted)="onCreate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    <!-- Modale édition / consultation -->
    @if (modal() === 'edit' && editTarget()) {
      <app-modal [title]="editTarget()!.isSystem ? 'Consulter le rôle' : 'Modifier le rôle'"
                 width="680px" (closed)="closeModal()">
        <app-role-form mode="edit" [initialData]="editTarget()" [saving]="store.saving()"
          (submitted)="onUpdate($event)" (cancelled)="closeModal()" />
      </app-modal>
    }

    <!-- Confirmation suppression -->
    @if (delTarget()) {
      <app-confirm-dialog
        title="Supprimer le rôle"
        [message]="'Supprimer « ' + delTarget()!.name + ' » ? Les utilisateurs qui ont ce rôle perdront les permissions associées.'"
        confirmLabel="Supprimer" variant="danger"
        (confirmed)="doDel()" (cancelled)="delTarget.set(null)" />
    }
  `,
  styles: [`
    .page  { display: flex; flex-direction: column; gap: var(--space-6); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }

    /* ── Table ──────────────────────────────── */
    .table-wrap {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      overflow: hidden;
    }
    .table { width: 100%; border-collapse: collapse; }

    .th {
      padding: var(--space-3) var(--space-4);
      text-align: left;
      font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold);
      color: var(--color-text-muted);
      text-transform: uppercase; letter-spacing: 0.05em;
      background: var(--color-surface-raised);
      border-bottom: 1px solid var(--color-border-default);
      &--name    { width: 28%; }
      &--type    { width: 14%; }
      &--perms   { width: 44%; }
      &--actions { width: 14%; }
    }

    .row {
      border-bottom: 1px solid var(--color-border-subtle);
      transition: background var(--duration-fast);
      &:last-child { border-bottom: none; }
      &:hover { background: var(--color-neutral-50); }
    }

    .td {
      padding: var(--space-3) var(--space-4);
      vertical-align: top;
      padding-top: var(--space-4);
      font-size: var(--font-size-sm); color: var(--color-text-primary);
      &--name { display: flex; align-items: center; gap: var(--space-3); vertical-align: middle; padding-top: var(--space-3); }
      &--actions { vertical-align: middle; text-align: right; padding-top: var(--space-3); }
    }

    /* ── Role identity ──────────────────────── */
    .role-icon {
      width: 32px; height: 32px; flex-shrink: 0;
      border-radius: var(--radius-md);
      display: flex; align-items: center; justify-content: center;
      &--system { background: var(--color-brand-primary-subtle); color: var(--color-brand-primary); }
      &--custom { background: var(--color-neutral-100); color: var(--color-text-secondary); }
    }
    .role-meta { display: flex; flex-direction: column; gap: 2px; }
    .role-name { font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
    .role-code {
      font-family: var(--font-family-mono); font-size: var(--font-size-xs);
      color: var(--color-text-muted); background: var(--color-neutral-100);
      padding: 1px 5px; border-radius: var(--radius-sm); display: inline-block;
    }

    /* ── Type badges ────────────────────────── */
    .badge {
      display: inline-flex; align-items: center;
      height: 22px; padding: 0 var(--space-2);
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs); font-weight: var(--font-weight-medium);
      &--system { background: var(--color-brand-primary-subtle); color: var(--color-brand-primary); }
      &--custom { background: var(--color-neutral-100); color: var(--color-text-secondary); border: 1px solid var(--color-border-default); }
    }

    /* ── Permission display ─────────────────── */
    .full-access {
      display: inline-flex; align-items: center; gap: var(--space-1);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      color: var(--color-semantic-success, #16a34a);
    }
    .no-perms { font-size: var(--font-size-xs); color: var(--color-text-muted); font-style: italic; }

    /* Category chips */
    .cat-chips { display: flex; flex-wrap: wrap; gap: var(--space-1); }
    .cat-chip {
      display: inline-flex; align-items: center; gap: 5px;
      height: 24px; padding: 0 var(--space-2);
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs); font-weight: var(--font-weight-medium);
      cursor: pointer; border: 1px solid transparent;
      transition: all var(--duration-fast);

      /* Default */
      background: var(--color-neutral-100); color: var(--color-text-secondary);
      border-color: var(--color-border-default);

      /* Per-category tints */
      &[data-cat="cards"]      { background: #fef3c7; color: #92400e; border-color: #fcd34d; }
      &[data-cat="users"]      { background: #dbeafe; color: #1e40af; border-color: #93c5fd; }
      &[data-cat="roles"]      { background: var(--color-brand-primary-subtle); color: var(--color-brand-primary); border-color: color-mix(in srgb, var(--color-brand-primary) 30%, transparent); }
      &[data-cat="settings"]   { background: #f3f4f6; color: #374151; border-color: #d1d5db; }
      &[data-cat="collection"] { background: #d1fae5; color: #065f46; border-color: #6ee7b7; }
      &[data-cat="alerts"]     { background: #fee2e2; color: #991b1b; border-color: #fca5a5; }
      &[data-cat="messages"]   { background: #e0f2fe; color: #0c4a6e; border-color: #7dd3fc; }

      &:hover { opacity: 0.8; }
    }
    .cat-chip-count {
      display: inline-flex; align-items: center; justify-content: center;
      width: 16px; height: 16px;
      border-radius: 50%;
      font-size: 10px; font-weight: var(--font-weight-semibold);
      background: rgba(0,0,0,0.10);
    }

    /* Expanded permission detail */
    .perm-detail {
      margin-top: var(--space-2);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-md);
      overflow: hidden;
      background: var(--color-surface-base);
    }
    .perm-detail-head {
      display: flex; align-items: center; justify-content: space-between;
      padding: var(--space-2) var(--space-3);
      background: var(--color-surface-raised);
      border-bottom: 1px solid var(--color-border-subtle);
    }
    .perm-detail-title {
      font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold);
      color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.04em;
    }
    .perm-detail-close {
      width: 20px; height: 20px; display: flex; align-items: center; justify-content: center;
      background: none; border: none; cursor: pointer; color: var(--color-text-muted);
      border-radius: var(--radius-sm);
      &:hover { background: var(--color-neutral-100); color: var(--color-text-primary); }
    }
    .perm-row {
      display: flex; align-items: center; gap: var(--space-3);
      padding: var(--space-2) var(--space-3);
      font-size: var(--font-size-xs);
      &:not(:last-child) { border-bottom: 1px solid var(--color-border-subtle); }
    }
    .perm-action {
      flex-shrink: 0; min-width: 80px;
      display: inline-flex; align-items: center; justify-content: center;
      height: 20px; padding: 0 var(--space-2);
      border-radius: var(--radius-sm);
      font-weight: var(--font-weight-semibold); font-size: 10px;
      text-transform: uppercase; letter-spacing: 0.04em;

      &[data-action="read"]   { background: #dbeafe; color: #1e40af; }
      &[data-action="write"]  { background: #d1fae5; color: #065f46; }
      &[data-action="delete"] { background: #fee2e2; color: #991b1b; }
      &[data-action="other"]  { background: #f3f4f6; color: #374151; }
    }
    .perm-desc { color: var(--color-text-secondary); }

    /* ── Actions ────────────────────────────── */
    .actions { display: flex; justify-content: flex-end; gap: var(--space-1); }
    .icon-btn {
      width: 30px; height: 30px; display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent; border-radius: var(--radius-md);
      background: transparent; color: var(--color-text-secondary); cursor: pointer;
      &:hover { background: var(--color-neutral-100); border-color: var(--color-border-default); color: var(--color-text-primary); }
      &--danger:hover { background: var(--color-semantic-danger-subtle); border-color: var(--color-semantic-danger); color: var(--color-semantic-danger); }
    }

    /* ── Header button ──────────────────────── */
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

  readonly modal      = signal<ModalState>('closed');
  readonly editTarget = signal<RoleDto | null>(null);
  readonly delTarget  = signal<RoleDto | null>(null);

  /** ID du rôle dont on a ouvert le détail de catégorie */
  readonly expandedRoleId = signal<string | null>(null);
  /** Catégorie actuellement développée */
  readonly expandedKey    = signal<string | null>(null);

  /** Tri décroissant : plus de permissions en premier.
   *  Super Admin (isSystem + 0 permissions = bypass total) → toujours en premier. */
  readonly sortedRoles = computed(() =>
    [...this.store.roles()].sort((a, b) => {
      const score = (r: RoleDto) => this.isSuperAdmin(r) ? Infinity : r.permissions.length;
      return score(b) - score(a);
    })
  );

  /** Map code → PermissionDto pour accès O(1) */
  private readonly permMap = computed(() =>
    new Map(this.store.permissions().map(p => [p.code, p]))
  );

  ngOnInit(): void { this.store.load(); }

  isSuperAdmin(r: RoleDto): boolean {
    return r.isSystem && r.permissions.length === 0;
  }

  /** Groupe les codes de permissions d'un rôle par catégorie */
  groupedPerms(codes: string[]): PermCategoryGroup[] {
    const map = new Map<string, PermCategoryGroup>();
    for (const code of codes) {
      const perm = this.permMap().get(code);
      if (!perm) continue;
      if (!map.has(perm.category)) {
        map.set(perm.category, {
          category: perm.category,
          label: CAT_LABELS[perm.category] ?? perm.category,
          perms: [],
        });
      }
      map.get(perm.category)!.perms.push(perm);
    }
    return Array.from(map.values());
  }

  /** Extrait l'action du code (`users:read` → `read`) */
  actionOf(code: string): string {
    const part = code.split(':')[1] ?? '';
    if (part === 'read')   return 'read';
    if (part === 'write')  return 'write';
    if (part === 'delete') return 'delete';
    return 'other';
  }

  descOf(code: string, fallback: string): string {
    return PERM_DESCS[code] ?? fallback;
  }

  actionLabelOf(code: string): string {
    const LABELS: Record<string, string> = {
      'users:read':           'Consulter',
      'users:write':          'Gérer',
      'users:delete':         'Supprimer',
      'users:assign_roles':   'Attribuer des rôles',
      'users:reset_password': 'Réinit. mot de passe',
      'roles:read':           'Consulter',
      'roles:write':          'Gérer',
      'roles:delete':         'Supprimer',
      'cards:read':           'Consulter',
      'cards:sync':           'Synchroniser',
      'settings:read':        'Consulter',
      'settings:write':       'Configurer',
    };
    return LABELS[code] ?? code.split(':')[1] ?? code;
  }

  toggleExpand(roleId: string, category: string): void {
    if (this.expandedRoleId() === roleId && this.expandedKey() === category) {
      this.closeExpand();
    } else {
      this.expandedRoleId.set(roleId);
      this.expandedKey.set(category);
    }
  }
  closeExpand(): void { this.expandedRoleId.set(null); this.expandedKey.set(null); }

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
