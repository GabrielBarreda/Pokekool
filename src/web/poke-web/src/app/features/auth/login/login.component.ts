import { Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { ButtonComponent } from '@shared/ui/button/button.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent],
  template: `
    <div class="login-page">
      <div class="login-card">

        <!-- Brand header -->
        <div class="login-card__brand">
          <h1 class="login-card__logo">Pokekool</h1>
          <p class="login-card__tagline">Marché de cartes Pokémon</p>
        </div>

        <!-- Error -->
        @if (authStore.error()) {
          <div class="login-card__error" role="alert">
            {{ authStore.error() }}
          </div>
        }

        <!-- Form -->
        <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="login-card__form">

          <div class="field">
            <label for="email" class="field__label">Email</label>
            <input
              id="email"
              type="email"
              class="field__input"
              formControlName="email"
              placeholder="admin@pokekool.dev"
              autocomplete="email"
              [class.field__input--error]="isInvalid('email')"
            />
            @if (isInvalid('email')) {
              <span class="field__error">Email invalide</span>
            }
          </div>

          <div class="field">
            <label for="password" class="field__label">Mot de passe</label>
            <input
              id="password"
              type="password"
              class="field__input"
              formControlName="password"
              placeholder="••••••••"
              autocomplete="current-password"
              [class.field__input--error]="isInvalid('password')"
            />
            @if (isInvalid('password')) {
              <span class="field__error">Mot de passe requis</span>
            }
          </div>

          <button
            appBtn
            variant="primary"
            size="lg"
            type="submit"
            [loading]="authStore.isLoading()"
            [disabled]="form.invalid"
            style="width: 100%; margin-top: var(--space-2);"
          >
            Se connecter
          </button>

        </form>

      </div>
    </div>
  `,
  styles: [`
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--space-4);
      background: var(--color-surface-sunken);
    }

    .login-card {
      width: 100%;
      max-width: 400px;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-lg);
      padding: var(--space-8);
      display: flex;
      flex-direction: column;
      gap: var(--space-6);
    }

    .login-card__brand { text-align: center; }
    .login-card__logo {
      font-size: var(--font-size-2xl);
      font-weight: var(--font-weight-bold);
      color: var(--color-brand-primary);
      margin: 0 0 var(--space-1);
    }
    .login-card__tagline {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin: 0;
    }

    .login-card__error {
      background: var(--color-semantic-danger-subtle);
      color: var(--color-semantic-danger);
      border: 1px solid var(--color-semantic-danger);
      border-radius: var(--radius-md);
      padding: var(--space-3) var(--space-4);
      font-size: var(--font-size-sm);
      text-align: center;
    }

    .login-card__form { display: flex; flex-direction: column; gap: var(--space-4); }

    /* Field */
    .field { display: flex; flex-direction: column; gap: var(--space-1); }
    .field__label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
    }
    .field__input {
      height: 40px;
      padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      background: var(--color-surface-base);
      transition: border-color var(--duration-fast);
      outline: none;
      width: 100%;

      &:focus { border-color: var(--color-border-focus); }
      &::placeholder { color: var(--color-text-disabled); }
      &--error { border-color: var(--color-semantic-danger); }
    }
    .field__error {
      font-size: var(--font-size-xs);
      color: var(--color-semantic-danger);
    }
  `],
})
export class LoginComponent implements OnInit {
  readonly authStore = inject(AuthStore);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  ngOnInit(): void {
    // Redirect if already authenticated
    if (this.authStore.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
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
    this.authStore.login(this.form.getRawValue());
  }
}
