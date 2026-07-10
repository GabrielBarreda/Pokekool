import { Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { ButtonComponent } from '@shared/ui/button/button.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-card__brand">
          <h1 class="auth-card__logo">Pokekool</h1>
          <p class="auth-card__tagline">Créer un compte client</p>
        </div>

        @if (authStore.error()) {
          <div class="auth-card__error" role="alert">{{ authStore.error() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="auth-card__form">
          <div class="grid-2">
            <div class="field">
              <label for="firstName" class="field__label">Prénom</label>
              <input id="firstName" type="text" class="field__input" formControlName="firstName" autocomplete="given-name" />
            </div>
            <div class="field">
              <label for="lastName" class="field__label">Nom</label>
              <input id="lastName" type="text" class="field__input" formControlName="lastName" autocomplete="family-name" />
            </div>
          </div>

          <div class="field">
            <label for="email" class="field__label">Email</label>
            <input id="email" type="email" class="field__input" formControlName="email"
                   autocomplete="email" [class.field__input--error]="isInvalid('email')" />
            @if (isInvalid('email')) { <span class="field__error">Email invalide</span> }
          </div>

          <div class="field">
            <label for="password" class="field__label">Mot de passe</label>
            <input id="password" type="password" class="field__input" formControlName="password"
                   autocomplete="new-password" [class.field__input--error]="isInvalid('password')" />
            @if (isInvalid('password')) { <span class="field__error">Au moins 8 caractères</span> }
          </div>

          <button appBtn variant="primary" size="lg" type="submit"
                  [loading]="authStore.isLoading()" [disabled]="form.invalid"
                  style="width: 100%; margin-top: var(--space-2);">
            Créer mon compte
          </button>
        </form>

        <p class="auth-card__foot">
          Déjà un compte ? <a routerLink="/auth/login">Se connecter</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      padding: var(--space-4); background: var(--color-surface-sunken);
    }
    .auth-card {
      width: 100%; max-width: 440px; background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle); border-radius: var(--radius-xl);
      box-shadow: var(--shadow-lg); padding: var(--space-8);
      display: flex; flex-direction: column; gap: var(--space-6);
    }
    .auth-card__brand { text-align: center; }
    .auth-card__logo { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); margin: 0 0 var(--space-1); }
    .auth-card__tagline { font-size: var(--font-size-sm); color: var(--color-text-muted); margin: 0; }
    .auth-card__error {
      background: var(--color-semantic-danger-subtle); color: var(--color-semantic-danger);
      border: 1px solid var(--color-semantic-danger); border-radius: var(--radius-md);
      padding: var(--space-3) var(--space-4); font-size: var(--font-size-sm); text-align: center;
    }
    .auth-card__form { display: flex; flex-direction: column; gap: var(--space-4); }
    .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-3); }
    .field { display: flex; flex-direction: column; gap: var(--space-1); }
    .field__label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
    .field__input {
      height: 40px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); font-size: var(--font-size-sm); color: var(--color-text-primary);
      background: var(--color-surface-base); outline: none; width: 100%;
    }
    .field__input:focus { border-color: var(--color-border-focus); }
    .field__input--error { border-color: var(--color-semantic-danger); }
    .field__error { font-size: var(--font-size-xs); color: var(--color-semantic-danger); }
    .auth-card__foot { text-align: center; font-size: var(--font-size-sm); color: var(--color-text-muted); margin: 0; }
    .auth-card__foot a { color: var(--color-brand-primary); text-decoration: none; }
  `],
})
export class RegisterComponent implements OnInit {
  readonly authStore = inject(AuthStore);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    if (this.authStore.isAuthenticated()) {
      this.router.navigate(['/client/collection']);
    }
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.authStore.register({
      email: v.email,
      password: v.password,
      firstName: v.firstName || undefined,
      lastName: v.lastName || undefined,
    });
  }
}
