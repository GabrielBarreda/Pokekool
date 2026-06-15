import { Component, Input, inject, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgFor, NgIf, NgClass } from '@angular/common';
import { AuthStore } from '@core/auth/auth.store';
import { PermissionStore } from '@core/auth/permission.store';
import { ADMIN_NAV_ITEMS, ADMIN_ONLY_NAV_ITEMS, PLANNING_NAV_ITEMS, NavItem } from '../nav-item.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgFor, NgIf, NgClass],
  template: `
    <nav class="sidebar" [class.sidebar--collapsed]="collapsed" aria-label="Navigation principale">

      <!-- Logo / Brand -->
      <div class="sidebar__brand">
        <span *ngIf="!collapsed" class="sidebar__brand-name">Pokekool</span>
        <span *ngIf="collapsed" class="sidebar__brand-icon" aria-hidden="true">P</span>
      </div>

      <!-- Nav items -->
      <ul class="sidebar__nav" role="list">
        <li *ngFor="let item of visibleItems()" class="sidebar__nav-item">
          <a
            [routerLink]="item.route"
            routerLinkActive="sidebar__link--active"
            class="sidebar__link"
            [attr.title]="collapsed ? item.label : null"
          >
            <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
            <span *ngIf="!collapsed" class="sidebar__label">{{ item.label }}</span>
          </a>
        </li>
      </ul>

      <!-- Séparateur + Nav planning -->
      <ng-container *ngIf="planningItems().length > 0">
        <div class="sidebar__divider" role="separator"></div>
        <span *ngIf="!collapsed" class="sidebar__section-label">Planning</span>
        <ul class="sidebar__nav" role="list">
          <li *ngFor="let item of planningItems()" class="sidebar__nav-item">
            <a
              [routerLink]="item.route"
              routerLinkActive="sidebar__link--active"
              class="sidebar__link"
              [attr.title]="collapsed ? item.label : null"
            >
              <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
              <span *ngIf="!collapsed" class="sidebar__label">{{ item.label }}</span>
            </a>
          </li>
        </ul>
      </ng-container>

      <!-- Séparateur + Nav admin -->
      <ng-container *ngIf="adminItems().length > 0">
        <div class="sidebar__divider" role="separator"></div>
        <span *ngIf="!collapsed" class="sidebar__section-label">Administration</span>
        <ul class="sidebar__nav" role="list">
          <li *ngFor="let item of adminItems()" class="sidebar__nav-item">
            <a
              [routerLink]="item.route"
              routerLinkActive="sidebar__link--active"
              class="sidebar__link"
              [attr.title]="collapsed ? item.label : null"
            >
              <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
              <span *ngIf="!collapsed" class="sidebar__label">{{ item.label }}</span>
            </a>
          </li>
        </ul>
      </ng-container>

    </nav>
  `,
  styles: [`
    .sidebar {
      display: flex;
      flex-direction: column;
      width: 240px;
      height: 100%;
      background: var(--color-surface-base);
      border-right: 1px solid var(--color-border-subtle);
      transition: width var(--duration-normal) var(--easing-standard);
      overflow: hidden;
      flex-shrink: 0;
    }
    .sidebar--collapsed { width: 60px; }

    /* Brand */
    .sidebar__brand {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 60px;
      padding: 0 var(--space-4);
      border-bottom: 1px solid var(--color-border-subtle);
      flex-shrink: 0;
    }
    .sidebar__brand-name {
      font-size: var(--font-size-xl);
      font-weight: var(--font-weight-bold);
      color: var(--color-brand-primary);
      letter-spacing: var(--letter-spacing-tight);
    }
    .sidebar__brand-icon {
      font-size: var(--font-size-xl);
      font-weight: var(--font-weight-bold);
      color: var(--color-brand-primary);
    }

    /* Nav */
    .sidebar__nav {
      list-style: none;
      padding: var(--space-2) 0;
      margin: 0;
      flex-shrink: 0;
    }
    .sidebar__nav-item { margin: 0 var(--space-2); }

    .sidebar__link {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2) var(--space-3);
      border-radius: var(--radius-md);
      color: var(--color-text-secondary);
      text-decoration: none;
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      transition: background var(--duration-fast), color var(--duration-fast);
      white-space: nowrap;

      &:hover {
        background: var(--color-surface-raised);
        color: var(--color-text-primary);
      }
      &.sidebar__link--active {
        background: var(--color-brand-primary-subtle);
        color: var(--color-brand-primary);
      }
    }
    .sidebar--collapsed .sidebar__link { justify-content: center; padding: var(--space-2); }
    .sidebar__icon { font-size: var(--font-size-md); flex-shrink: 0; }
    .sidebar__label { flex: 1; overflow: hidden; text-overflow: ellipsis; }

    /* Divider & section label */
    .sidebar__divider {
      height: 1px;
      background: var(--color-border-subtle);
      margin: var(--space-2) var(--space-4);
    }
    .sidebar__section-label {
      display: block;
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-muted);
      text-transform: uppercase;
      letter-spacing: var(--letter-spacing-wide);
      padding: 0 var(--space-5) var(--space-1);
    }
  `],
})
export class SidebarComponent {
  @Input() collapsed = false;

  private readonly authStore = inject(AuthStore);
  private readonly permStore = inject(PermissionStore);

  readonly isAdmin = computed(() =>
    this.authStore.user()?.roles?.includes('admin') ?? false
  );

  readonly planningItems = computed(() => PLANNING_NAV_ITEMS);

  // Tant que les permissions ne sont pas chargées, on affiche tous les items
  // (évite la sidebar vide juste après login)
  private readonly permsLoaded = computed(() => this.permStore.loaded());

  readonly visibleItems = computed(() => {
    if (!this.permsLoaded()) return ADMIN_NAV_ITEMS;
    return ADMIN_NAV_ITEMS.filter(item =>
      !item.permission || this.permStore.permissions().includes(item.permission)
    );
  });

  readonly adminItems = computed(() => {
    if (!this.isAdmin()) return [];
    if (!this.permsLoaded()) return ADMIN_ONLY_NAV_ITEMS;
    return ADMIN_ONLY_NAV_ITEMS.filter(item =>
      !item.permission || this.permStore.permissions().includes(item.permission)
    );
  });
}
