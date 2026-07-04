import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';
import { Router } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { SyncProgressOverlayComponent } from '@app/layout/sync-progress-overlay/sync-progress-overlay.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [NgClass, NgIf, SyncProgressOverlayComponent],
  template: `
    <header class="topbar">

      <!-- Burger button -->
      <button
        class="topbar__burger"
        (click)="toggleSidebar.emit()"
        aria-label="Basculer le menu"
        type="button"
      >
        <span class="burger-icon" [class.burger-icon--open]="!sidebarCollapsed">
          <span></span><span></span><span></span>
        </span>
      </button>

      <!-- Sync progress overlay (centre topbar) -->
      <div class="topbar__center">
        <app-sync-progress-overlay />
      </div>

      <!-- Spacer -->
      <div class="topbar__spacer"></div>

      <!-- User avatar + dropdown -->
      <div class="topbar__user">
        <button
          class="user-btn"
          (click)="toggleUserMenu()"
          type="button"
          aria-haspopup="true"
          [attr.aria-expanded]="userMenuOpen()"
          [attr.aria-label]="'Menu utilisateur — ' + authStore.userDisplayName()"
        >
          <span class="user-btn__avatar" aria-hidden="true">
            {{ authStore.userInitials() }}
          </span>
          <span class="user-btn__name">{{ authStore.userDisplayName() }}</span>
          <span class="user-btn__chevron" aria-hidden="true">▾</span>
        </button>

        <div *ngIf="userMenuOpen()" class="user-menu" role="menu">
          <div class="user-menu__info">
            <span class="user-menu__name">{{ authStore.userDisplayName() }}</span>
            <span class="user-menu__email">{{ authStore.user()?.email }}</span>
          </div>
          <div class="user-menu__divider" role="separator"></div>
          <button class="user-menu__item" (click)="goToProfile()" role="menuitem" type="button">
            <span aria-hidden="true">👤</span> Mon profil
          </button>
          <button class="user-menu__item user-menu__item--danger" (click)="logout()" role="menuitem" type="button">
            <span aria-hidden="true">⎋</span> Déconnexion
          </button>
        </div>
      </div>

      <!-- Overlay to close dropdowns -->
      <div *ngIf="userMenuOpen()" class="topbar__overlay" (click)="closeMenus()" aria-hidden="true"></div>

    </header>
  `,
  styles: [`
    .topbar {
      position: relative;
      display: flex;
      align-items: center;
      height: 60px;
      padding: 0 var(--space-4);
      background: var(--color-surface-base);
      border-bottom: 1px solid var(--color-border-subtle);
      gap: var(--space-3);
      flex-shrink: 0;
      z-index: var(--z-sticky);
    }
    .topbar__center {
      display: flex;
      align-items: center;
      justify-content: center;
      flex: 1;
    }
    .topbar__spacer { flex: 0 0 auto; }

    /* Burger */
    .topbar__burger {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: none;
      border: none;
      cursor: pointer;
      border-radius: var(--radius-md);
      &:hover { background: var(--color-surface-raised); }
    }
    .burger-icon {
      display: flex;
      flex-direction: column;
      gap: 4px;
      width: 18px;
      span {
        display: block;
        height: 2px;
        background: var(--color-text-secondary);
        border-radius: var(--radius-full);
        transition: all var(--duration-fast) var(--easing-standard);
      }
    }

    /* Establishment selector */
    .topbar__est-selector { position: relative; }
    .est-btn {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      cursor: pointer;
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
      &:hover { background: var(--color-neutral-200); }
    }
    .est-btn__label { max-width: 140px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .est-btn__chevron { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .est-menu {
      position: absolute;
      top: calc(100% + var(--space-1));
      right: 0;
      min-width: 220px;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      z-index: var(--z-dropdown);
      overflow: hidden;
      padding: var(--space-1) 0;
    }
    .est-menu__item {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      width: 100%;
      padding: var(--space-2) var(--space-4);
      background: none;
      border: none;
      text-align: left;
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      cursor: pointer;
      &:hover { background: var(--color-surface-raised); }
    }
    .est-menu__item--add { color: var(--color-brand-primary); font-weight: var(--font-weight-medium); }
    .est-menu__item--active { background: var(--color-surface-raised); font-weight: var(--font-weight-medium); }
    .est-dot { font-size: 12px; }
    .est-menu__divider { height: 1px; background: var(--color-border-subtle); margin: var(--space-1) 0; }

    /* User */
    .topbar__user { position: relative; }
    .user-btn {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-1) var(--space-2);
      background: none;
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      &:hover { background: var(--color-surface-raised); }
    }
    .user-btn__avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: var(--radius-full);
      background: var(--color-brand-primary);
      color: var(--color-brand-on-primary);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      flex-shrink: 0;
    }
    .user-btn__name { max-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-weight: var(--font-weight-medium); }
    .user-btn__chevron { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .user-menu {
      position: absolute;
      top: calc(100% + var(--space-1));
      right: 0;
      min-width: 200px;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      z-index: var(--z-dropdown);
      overflow: hidden;
    }
    .user-menu__info {
      padding: var(--space-3) var(--space-4);
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }
    .user-menu__name { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); }
    .user-menu__email { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .user-menu__divider { height: 1px; background: var(--color-border-subtle); }
    .user-menu__item {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      width: 100%;
      padding: var(--space-3) var(--space-4);
      background: none;
      border: none;
      text-align: left;
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      cursor: pointer;
      &:hover { background: var(--color-surface-raised); }
    }
    .user-menu__item--danger { color: var(--color-semantic-danger); }

    /* Overlay */
    .topbar__overlay {
      position: fixed;
      inset: 0;
      z-index: calc(var(--z-dropdown) - 1);
    }
  `],
})
export class TopbarComponent {
  @Input() sidebarCollapsed = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  readonly userMenuOpen = signal(false);

  toggleUserMenu(): void {
    this.userMenuOpen.update(v => !v);
  }

  closeMenus(): void {
    this.userMenuOpen.set(false);
  }

  goToProfile(): void {
    this.closeMenus();
    this.router.navigate(['/profil']);
  }

  logout(): void {
    this.closeMenus();
    this.authStore.logout();
  }
}
