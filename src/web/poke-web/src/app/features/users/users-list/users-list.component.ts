import {
  Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AuthStore } from '@core/auth/auth.store';
import { UsersStore } from '../users.store';
import { UserSummaryDto, roleBadgeVariant, userDisplayName, userInitials } from '../users.models';
import { UserDetailModalComponent } from '../user-detail-modal/user-detail-modal.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-users-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    UserDetailModalComponent, BadgeComponent, PageHeaderComponent,
    SpinnerComponent, EmptyStateComponent, ConfirmDialogComponent,
    HasPermissionDirective,
  ],
  template: `
    <div class="page">

      <!-- En-tête -->
      <app-page-header
        title="Utilisateurs"
        [subtitle]="store.totalItems() > 0 ? 'Total : ' + store.totalItems() : ''">
      </app-page-header>

      <!-- Barre de filtres -->
      <div class="filters-bar">
        <div class="filters-row">
          <!-- Recherche -->
          <div class="search-wrap">
            <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
            </svg>
            <input
              class="search-input"
              type="text"
              placeholder="Recherche email / nom…"
              [ngModel]="searchInput()"
              (ngModelChange)="onSearchInput($event)" />
          </div>

          <!-- Filtre rôle -->
          <select class="filter-select"
            [ngModel]="store.filters().roleCode"
            (ngModelChange)="onFilterChange({ roleCode: $event || null })">
            <option value="">Tous les rôles</option>
            @for (r of store.availableRoles(); track r.code) {
              <option [value]="r.code">{{ r.name }}</option>
            }
          </select>

          <!-- Trier -->
          <select class="filter-select"
            [ngModel]="store.filters().sortBy"
            (ngModelChange)="onFilterChange({ sortBy: $event })">
            <option value="created_desc">Plus récents</option>
            <option value="name_asc">Nom A→Z</option>
            <option value="name_desc">Nom Z→A</option>
            <option value="email">Email</option>
          </select>
        </div>

        <!-- Checkboxes filtres -->
        <div class="filters-checks">
          <label class="check-label">
            <input type="checkbox"
              [checked]="store.filters().hasPendingMessages === true"
              (change)="toggleCheck('hasPendingMessages', $event)" />
            Message en attente
          </label>
          <label class="check-label">
            <input type="checkbox"
              [checked]="store.filters().recentOnly === true"
              (change)="toggleCheck('recentOnly', $event)" />
            Inscrits cette semaine
          </label>
        </div>
      </div>

      <!-- Tableau -->
      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (!store.hasResults()) {
        <app-empty-state icon="👥" title="Aucun utilisateur"
          description="Aucun résultat pour les filtres actuels." />
      } @else {
        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th>Utilisateur</th>
                <th>Rôles</th>
                <th>Statut</th>
                <th>Inscrit le</th>
                <th>Dernière connexion</th>
                <th class="col-actions"></th>
              </tr>
            </thead>
            <tbody>
              @for (u of store.users(); track u.id) {
                <tr class="row" (click)="openDetail(u)">

                  <!-- Avatar + nom / email -->
                  <td class="cell-user">
                    <div class="avatar">{{ initials(u) }}</div>
                    <div class="user-info">
                      <div class="user-name">{{ displayName(u) }}</div>
                      <div class="user-email">{{ u.email }}</div>
                    </div>
                  </td>

                  <!-- Rôles -->
                  <td>
                    <div class="badges">
                      @for (code of u.roles; track code) {
                        <app-badge [variant]="badgeVariant(code)">{{ code }}</app-badge>
                      }
                      @if (u.roles.length === 0) {
                        <span class="no-role">—</span>
                      }
                    </div>
                  </td>

                  <!-- Statut + msg en attente -->
                  <td>
                    <div class="badges">
                      <app-badge [variant]="u.isActive ? 'success' : 'neutral'">
                        {{ u.isActive ? 'Actif' : 'Inactif' }}
                      </app-badge>
                      @if (u.hasPendingMessages) {
                        <app-badge variant="warning">✉</app-badge>
                      }
                    </div>
                  </td>

                  <!-- Inscrit le -->
                  <td class="cell-date">{{ u.createdAt | date:'dd/MM/yyyy' }}</td>

                  <!-- Dernière connexion -->
                  <td class="cell-date">
                    {{ u.lastLoginAt ? (u.lastLoginAt | date:'dd/MM/yyyy') : '—' }}
                  </td>

                  <!-- Actions -->
                  <td class="cell-actions" (click)="$event.stopPropagation()">
                    <div class="action-menu" [class.open]="menuOpen() === u.id">
                      <button class="menu-trigger" (click)="toggleMenu(u.id)" title="Actions">⋯</button>
                      @if (menuOpen() === u.id) {
                        <div class="menu-dropdown">
                          <button class="menu-item" (click)="openDetail(u); closeMenu()">
                            Voir le détail
                          </button>

                          @if (!isSelf(u.id)) {
                            @if (u.isActive) {
                              <button *hasPermission="'users:write'" class="menu-item menu-item--warn"
                                (click)="onDeactivate(u); closeMenu()">
                                Désactiver
                              </button>
                            } @else {
                              <button *hasPermission="'users:write'" class="menu-item"
                                (click)="onActivate(u); closeMenu()">
                                Activer
                              </button>
                            }
                            <button *hasPermission="'users:delete'" class="menu-item menu-item--danger"
                              (click)="delTarget.set(u); closeMenu()">
                              Supprimer
                            </button>
                          }
                        </div>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        @if (store.totalPages() > 1) {
          <div class="pagination">
            <button class="page-btn" [disabled]="store.filters().page <= 1"
              (click)="goToPage(store.filters().page - 1)">‹</button>

            @for (p of pageRange(); track p) {
              @if (p === -1) {
                <span class="page-ellipsis">…</span>
              } @else {
                <button class="page-btn" [class.active]="p === store.filters().page"
                  (click)="goToPage(p)">{{ p }}</button>
              }
            }

            <button class="page-btn" [disabled]="store.filters().page >= store.totalPages()"
              (click)="goToPage(store.filters().page + 1)">›</button>
          </div>
        }
      }
    </div>

    <!-- Modale de détail -->
    @if (store.selectedUser()) {
      <app-user-detail-modal />
    }

    <!-- Confirm suppression -->
    @if (delTarget()) {
      <app-confirm-dialog
        title="Supprimer l'utilisateur"
        [message]="'Supprimer ' + displayName(delTarget()!) + ' ?'"
        confirmLabel="Supprimer" variant="danger"
        (confirmed)="doDelete()" (cancelled)="delTarget.set(null)" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-6); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }

    /* ── Filtres ─────────────────────────────────────────── */
    .filters-bar {
      display: flex; flex-direction: column; gap: var(--space-3);
      padding: var(--space-4); background: var(--color-surface-raised);
      border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg);
    }
    .filters-row { display: flex; gap: var(--space-3); flex-wrap: wrap; align-items: center; }

    .search-wrap {
      position: relative; flex: 1; min-width: 200px;
    }
    .search-icon {
      position: absolute; left: 10px; top: 50%; transform: translateY(-50%);
      color: var(--color-text-muted); pointer-events: none;
    }
    .search-input {
      width: 100%; height: 38px; padding: 0 var(--space-3) 0 34px;
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-primary);
      font-size: var(--font-size-sm);
      &:focus { outline: none; border-color: var(--color-brand-primary); }
    }
    .filter-select {
      height: 38px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-primary);
      font-size: var(--font-size-sm); cursor: pointer;
    }

    .filters-checks { display: flex; gap: var(--space-4); }
    .check-label {
      display: flex; align-items: center; gap: var(--space-2);
      font-size: var(--font-size-sm); color: var(--color-text-secondary); cursor: pointer;
      input { accent-color: var(--color-brand-primary); }
    }

    /* ── Tableau ─────────────────────────────────────────── */
    .table-wrap {
      border: 1px solid var(--color-border-default); border-radius: var(--radius-lg);
      overflow: hidden;
    }
    .table {
      width: 100%; border-collapse: collapse;
      font-size: var(--font-size-sm);
    }
    thead { background: var(--color-surface-raised); }
    th {
      padding: var(--space-3) var(--space-4);
      text-align: left; font-weight: var(--font-weight-medium);
      color: var(--color-text-muted); white-space: nowrap;
      border-bottom: 1px solid var(--color-border-default);
    }
    .col-actions { width: 48px; }

    .row {
      cursor: pointer; transition: background var(--duration-fast);
      &:hover { background: var(--color-surface-raised); }
      &:not(:last-child) td { border-bottom: 1px solid var(--color-border-subtle); }
    }
    td { padding: var(--space-3) var(--space-4); vertical-align: middle; }

    /* Cellule utilisateur */
    .cell-user { display: flex; align-items: center; gap: var(--space-3); }
    .avatar {
      width: 36px; height: 36px; border-radius: var(--radius-full); flex-shrink: 0;
      background: var(--color-brand-primary-subtle); color: var(--color-brand-primary);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-bold);
      display: flex; align-items: center; justify-content: center;
    }
    .user-info { display: flex; flex-direction: column; gap: 1px; }
    .user-name { font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
    .user-email { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .badges { display: flex; flex-wrap: wrap; gap: var(--space-1); }
    .no-role { color: var(--color-text-muted); }

    .cell-date { white-space: nowrap; color: var(--color-text-secondary); }

    /* Menu actions */
    .cell-actions { text-align: right; white-space: nowrap; }
    .action-menu { position: relative; display: inline-block; }
    .menu-trigger {
      width: 32px; height: 32px; border-radius: var(--radius-md);
      border: 1px solid transparent; background: transparent;
      color: var(--color-text-muted); cursor: pointer; font-size: 18px;
      display: flex; align-items: center; justify-content: center;
      &:hover { background: var(--color-neutral-100); border-color: var(--color-border-default); }
    }
    .menu-dropdown {
      position: absolute; right: 0; top: calc(100% + 4px); z-index: 100;
      background: var(--color-surface-base); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); box-shadow: var(--shadow-lg);
      min-width: 160px; padding: var(--space-1);
    }
    .menu-item {
      display: block; width: 100%; padding: var(--space-2) var(--space-3);
      text-align: left; background: transparent; border: none; border-radius: var(--radius-sm);
      font-size: var(--font-size-sm); color: var(--color-text-primary); cursor: pointer;
      &:hover { background: var(--color-surface-raised); }
      &--warn { color: var(--color-semantic-warning); }
      &--danger { color: var(--color-semantic-danger); }
    }

    /* ── Pagination ──────────────────────────────────────── */
    .pagination {
      display: flex; align-items: center; justify-content: center; gap: var(--space-1);
    }
    .page-btn {
      min-width: 36px; height: 36px; padding: 0 var(--space-2);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-secondary);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover:not(:disabled) { background: var(--color-surface-raised); color: var(--color-text-primary); }
      &:disabled { opacity: 0.4; cursor: not-allowed; }
      &.active {
        background: var(--color-brand-primary); color: var(--color-brand-on-primary);
        border-color: var(--color-brand-primary);
      }
    }
    .page-ellipsis { color: var(--color-text-muted); padding: 0 var(--space-1); }
  `],
})
export class UsersListComponent implements OnInit, OnDestroy {
  readonly store     = inject(UsersStore);
  readonly authStore = inject(AuthStore);

  readonly menuOpen  = signal<string | null>(null);
  readonly delTarget = signal<UserSummaryDto | null>(null);

  /** Valeur locale du champ recherche — debounce avant envoi au store */
  readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  readonly displayName  = userDisplayName;
  readonly initials     = userInitials;
  readonly badgeVariant = roleBadgeVariant;

  ngOnInit(): void {
    this.store.loadUsers();
    this.store.loadRoles();

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(search => {
      this.store.updateFilters({ search, page: 1 });
      this.store.loadUsers();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchInput(value: string): void {
    this.searchInput.set(value);
    this.searchSubject.next(value);
  }

  onFilterChange(partial: Parameters<typeof this.store.updateFilters>[0]): void {
    this.store.updateFilters(partial);
    this.store.loadUsers();
  }

  toggleCheck(key: 'hasPendingMessages' | 'recentOnly', ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    this.store.updateFilters({ [key]: checked || null, page: 1 });
    this.store.loadUsers();
  }

  goToPage(p: number): void {
    this.store.updateFilters({ page: p });
    this.store.loadUsers();
  }

  openDetail(u: UserSummaryDto): void { this.store.selectUser(u); }
  toggleMenu(id: string): void { this.menuOpen.set(this.menuOpen() === id ? null : id); }
  closeMenu(): void { this.menuOpen.set(null); }

  isSelf(id: string): boolean { return this.authStore.user()?.id === id; }

  onActivate(u: UserSummaryDto): void   { this.store.activateUser(u.id); }
  onDeactivate(u: UserSummaryDto): void { this.store.deactivateUser(u.id); }

  doDelete(): void {
    const t = this.delTarget();
    if (!t) return;
    this.store.deleteUser(t.id);
    this.delTarget.set(null);
  }

  /** Génère la liste des numéros de page à afficher (avec ellipsis = -1) */
  pageRange(): number[] {
    const current = this.store.filters().page;
    const total   = this.store.totalPages();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);

    const pages: number[] = [1];
    if (current > 3) pages.push(-1);
    for (let p = Math.max(2, current - 1); p <= Math.min(total - 1, current + 1); p++)
      pages.push(p);
    if (current < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  }
}
