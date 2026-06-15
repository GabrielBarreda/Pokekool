import { Component, signal, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';
import { ToastContainerComponent } from '@shared/ui/toast/toast-container.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { LoaderService } from '@core/services/loader.service';
import { PermissionStore } from '@core/auth/permission.store';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, ToastContainerComponent, SpinnerComponent],
  template: `
    <!-- Global loading bar -->
    @if (loader.isLoading()) {
      <div class="global-loader" aria-live="polite" aria-label="Chargement">
        <div class="global-loader__bar"></div>
      </div>
    }

    <div class="admin-shell">
      <app-sidebar [collapsed]="sidebarCollapsed()" />

      <div class="admin-shell__main">
        <app-topbar
          [sidebarCollapsed]="sidebarCollapsed()"
          (toggleSidebar)="toggleSidebar()"
        />
        <main class="admin-shell__content" id="main-content" tabindex="-1">
          <router-outlet />
        </main>
      </div>
    </div>

    <!-- Toast notifications -->
    <app-toast-container />
  `,
  styles: [`
    /* Global loading bar */
    .global-loader {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      height: 3px;
      z-index: calc(var(--z-toast) + 1);
      overflow: hidden;
    }
    .global-loader__bar {
      height: 100%;
      background: var(--color-brand-primary);
      animation: loaderSlide 1.5s ease-in-out infinite;
      transform-origin: left;
    }
    @keyframes loaderSlide {
      0%   { transform: scaleX(0); transform-origin: left; }
      50%  { transform: scaleX(0.7); transform-origin: left; }
      100% { transform: scaleX(0); transform-origin: right; }
    }

    /* Shell layout */
    .admin-shell {
      display: flex;
      height: 100vh;
      overflow: hidden;
    }
    .admin-shell__main {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;
      overflow: hidden;
    }
    .admin-shell__content {
      flex: 1;
      overflow-y: auto;
      padding: var(--space-6);
      background: var(--color-surface-sunken);
      display: flex;
      flex-direction: column;
      /* Permet aux enfants qui veulent prendre toute la hauteur de le faire */
      &:has(> .full-height-page) {
        padding: 0;
        overflow: hidden;
      }
    }

    /* Responsive : sur mobile la sidebar devient un drawer */
    @media (max-width: 768px) {
      .admin-shell__content {
        padding: var(--space-4);
      }
    }
  `],
})
export class AdminLayoutComponent implements OnInit {
  readonly loader = inject(LoaderService);
  private readonly permStore = inject(PermissionStore);

  readonly sidebarCollapsed = signal(false);

  ngOnInit(): void {
    // Charger les permissions au montage du layout
    if (!this.permStore.loaded()) {
      this.permStore.load();
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }
}
