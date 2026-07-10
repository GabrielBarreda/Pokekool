import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { LoaderService } from '@core/services/loader.service';
import { ToastContainerComponent } from '@shared/ui/toast/toast-container.component';

/**
 * Layout de l'espace client — navbar simplifiée, aucun menu admin.
 * [Logo] | Ma Collection | Boutique | Mes RDV | [avatar / déconnexion]
 */
@Component({
  selector: 'app-client-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainerComponent],
  template: `
    @if (loader.isLoading()) {
      <div class="global-loader" aria-label="Chargement"><div class="global-loader__bar"></div></div>
    }

    <div class="client-shell">
      <header class="client-nav">
        <div class="client-nav__inner">
          <a routerLink="/client/collection" class="client-nav__logo">Pokekool</a>

          <nav class="client-nav__links">
            <a routerLink="/client/collection" routerLinkActive="is-active">Ma Collection</a>
            <a routerLink="/client/boutique" routerLinkActive="is-active">Boutique</a>
            <a routerLink="/client/rdv" routerLinkActive="is-active">Mes RDV</a>
          </nav>

          <div class="client-nav__account">
            <a routerLink="/client/profil" class="client-nav__avatar" [title]="authStore.userDisplayName()">
              {{ authStore.userInitials() }}
            </a>
            <button type="button" class="client-nav__logout" (click)="authStore.logout()">Déconnexion</button>
          </div>
        </div>
      </header>

      <main class="client-content" id="main-content" tabindex="-1">
        <div class="client-content__inner">
          <router-outlet />
        </div>
      </main>
    </div>

    <app-toast-container />
  `,
  styles: [`
    .global-loader { position: fixed; top: 0; left: 0; right: 0; height: 3px; z-index: 1000; overflow: hidden; }
    .global-loader__bar { height: 100%; background: var(--color-brand-primary); animation: loaderSlide 1.5s ease-in-out infinite; }
    @keyframes loaderSlide {
      0% { transform: scaleX(0); transform-origin: left; }
      50% { transform: scaleX(0.7); transform-origin: left; }
      100% { transform: scaleX(0); transform-origin: right; }
    }

    .client-shell { min-height: 100vh; display: flex; flex-direction: column; background: var(--color-surface-sunken); }

    .client-nav {
      position: sticky; top: 0; z-index: 20;
      background: var(--color-surface-base);
      border-bottom: 1px solid var(--color-border-subtle);
    }
    .client-nav__inner {
      max-width: 1200px; margin: 0 auto; padding: 0 var(--space-4);
      height: 60px; display: flex; align-items: center; gap: var(--space-6);
    }
    .client-nav__logo {
      font-size: var(--font-size-xl); font-weight: var(--font-weight-bold);
      color: var(--color-brand-primary); text-decoration: none;
    }
    .client-nav__links { display: flex; gap: var(--space-2); flex: 1; }
    .client-nav__links a {
      padding: var(--space-2) var(--space-3); border-radius: var(--radius-md);
      color: var(--color-text-secondary); text-decoration: none;
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      transition: background var(--duration-fast), color var(--duration-fast);
    }
    .client-nav__links a:hover { background: var(--color-surface-sunken); color: var(--color-text-primary); }
    .client-nav__links a.is-active { background: var(--color-brand-primary-subtle, var(--color-surface-sunken)); color: var(--color-brand-primary); }

    .client-nav__account { display: flex; align-items: center; gap: var(--space-3); }
    .client-nav__avatar {
      width: 36px; height: 36px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      background: var(--color-brand-primary); color: #fff;
      font-size: var(--font-size-sm); font-weight: var(--font-weight-bold); text-decoration: none;
    }
    .client-nav__logout {
      background: none; border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); padding: var(--space-2) var(--space-3);
      font-size: var(--font-size-sm); color: var(--color-text-secondary); cursor: pointer;
    }
    .client-nav__logout:hover { border-color: var(--color-semantic-danger); color: var(--color-semantic-danger); }

    .client-content { flex: 1; }
    .client-content__inner { max-width: 1200px; margin: 0 auto; padding: var(--space-6) var(--space-4); }

    @media (max-width: 640px) {
      .client-nav__inner { gap: var(--space-3); }
      .client-nav__links a { padding: var(--space-2); }
      .client-nav__logout { display: none; }
    }
  `],
})
export class ClientLayoutComponent {
  readonly authStore = inject(AuthStore);
  readonly loader = inject(LoaderService);
}
