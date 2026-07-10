import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '@core/auth/auth.store';
import { ClientService } from '../../client.service';
import { ToastService } from '@shared/services/toast.service';

@Component({
  selector: 'app-client-profil',
  standalone: true,
  imports: [FormsModule],
  template: `
    <header class="page-head">
      <h1 class="page-head__title">Mon profil</h1>
      <p class="page-head__sub">{{ authStore.user()?.email }}</p>
    </header>

    <!-- Email -->
    <section class="card">
      <h2 class="card__title">Changer d'email</h2>
      <input class="input" type="email" placeholder="Nouvel email" [(ngModel)]="newEmail" />
      <button type="button" class="btn btn--primary" [disabled]="!newEmail || busy()" (click)="changeEmail()">Mettre à jour</button>
    </section>

    <!-- Password -->
    <section class="card">
      <h2 class="card__title">Changer de mot de passe</h2>
      <input class="input" type="password" placeholder="Mot de passe actuel" [(ngModel)]="currentPassword" autocomplete="current-password" />
      <input class="input" type="password" placeholder="Nouveau mot de passe (8+ caractères)" [(ngModel)]="newPassword" autocomplete="new-password" />
      <button type="button" class="btn btn--primary" [disabled]="!canChangePassword() || busy()" (click)="changePassword()">Mettre à jour</button>
    </section>

    <!-- Danger zone -->
    <section class="card card--danger">
      <h2 class="card__title">Supprimer mon compte</h2>
      <p class="muted">Cette action est irréversible.</p>
      @if (!confirmDelete()) {
        <button type="button" class="btn btn--danger" (click)="confirmDelete.set(true)">Supprimer mon compte</button>
      } @else {
        <div class="confirm">
          <span>Confirmer la suppression définitive ?</span>
          <div class="confirm__actions">
            <button type="button" class="btn" (click)="confirmDelete.set(false)">Annuler</button>
            <button type="button" class="btn btn--danger" [disabled]="busy()" (click)="deleteAccount()">Oui, supprimer</button>
          </div>
        </div>
      }
    </section>
  `,
  styles: [`
    .page-head { margin-bottom: var(--space-5); }
    .page-head__title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0; }
    .page-head__sub { color: var(--color-text-muted); margin: var(--space-1) 0 0; }

    .card { max-width: 480px; background: var(--color-surface-base); border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg); padding: var(--space-5); margin-bottom: var(--space-4); display: flex; flex-direction: column; gap: var(--space-3); }
    .card--danger { border-color: var(--color-semantic-danger); }
    .card__title { font-size: var(--font-size-lg); margin: 0; }

    .input { height: 40px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); }
    .btn { padding: var(--space-2) var(--space-4); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); cursor: pointer; font-size: var(--font-size-sm); align-self: flex-start; }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn--primary { background: var(--color-brand-primary); color: #fff; border-color: var(--color-brand-primary); }
    .btn--danger { border-color: var(--color-semantic-danger); color: var(--color-semantic-danger); }
    .muted { color: var(--color-text-muted); font-size: var(--font-size-sm); margin: 0; }
    .confirm { display: flex; flex-direction: column; gap: var(--space-2); }
    .confirm__actions { display: flex; gap: var(--space-3); }
  `],
})
export class ClientProfilComponent {
  readonly authStore = inject(AuthStore);
  private readonly service = inject(ClientService);
  private readonly toast = inject(ToastService);

  newEmail = '';
  currentPassword = '';
  newPassword = '';
  readonly busy = signal(false);
  readonly confirmDelete = signal(false);

  canChangePassword(): boolean {
    return this.currentPassword.length > 0 && this.newPassword.length >= 8;
  }

  changeEmail(): void {
    this.busy.set(true);
    this.service.changeEmail(this.newEmail.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.newEmail = '';
        this.toast.show({ message: 'Email mis à jour. Reconnectez-vous si nécessaire.', type: 'success' });
        this.authStore.loadCurrentUser();
      },
      error: () => { this.busy.set(false); this.toast.show({ message: 'Impossible de changer l’email.', type: 'danger' }); },
    });
  }

  changePassword(): void {
    this.busy.set(true);
    this.service.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.busy.set(false);
        this.currentPassword = ''; this.newPassword = '';
        this.toast.show({ message: 'Mot de passe mis à jour.', type: 'success' });
      },
      error: () => { this.busy.set(false); this.toast.show({ message: 'Mot de passe actuel incorrect.', type: 'danger' }); },
    });
  }

  deleteAccount(): void {
    this.busy.set(true);
    this.service.deleteAccount().subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.show({ message: 'Compte supprimé.', type: 'info' });
        this.authStore.logout();
      },
      error: () => { this.busy.set(false); this.toast.show({ message: 'Suppression impossible.', type: 'danger' }); },
    });
  }
}
