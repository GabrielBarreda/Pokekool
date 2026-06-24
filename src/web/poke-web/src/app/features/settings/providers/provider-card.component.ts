import {
  ChangeDetectionStrategy,
  Component,
  Input,
  inject,
  computed,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProviderConfig } from '../settings.models';
import { SettingsStore } from '../settings.store';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { BadgeComponent, BadgeVariant } from '@shared/ui/badge/badge.component';
import { ButtonComponent } from '@shared/ui/button/button.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

// ── Key label map (union of all providers) ────────────────────────────────────

// Clés non-sensibles gérées via un widget dédié (pas dans la liste classique)
const NON_SECRET_KEYS = new Set(['extraction_mode']);

const KEY_LABELS: Record<string, string> = {
  app_token:     'App Token',
  app_secret:    'App Secret',
  access_token:  'Access Token',
  access_secret: 'Access Secret',
  app_id:        'App ID',
  public_key:    'Public Key',
  private_key:   'Private Key',
  api_key:       'API Key',
};

@Component({
  selector: 'app-provider-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    HasPermissionDirective,
    BadgeComponent,
    ButtonComponent,
    SpinnerComponent,
  ],
  template: `
    <!-- Card wrapper -->
    <div class="prov-card">

      <!-- ── Header ── -->
      <div class="prov-card__header">
        <div class="prov-card__title-row">
          <span class="prov-card__name">{{ config.label }}</span>

          <!-- Tooltip contribution -->
          <span
            class="prov-card__tooltip"
            [attr.data-tooltip]="config.contribution"
            aria-label="Contribution : {{ config.contribution }}"
          >ℹ︎</span>

          <!-- Fixed badge (ex: "API indisponible", "Actif") -->
          @if (config.fixedBadge) {
            <app-badge [variant]="config.fixedBadge.variant">
              {{ config.fixedBadge.text }}
            </app-badge>
          }

          <!-- Pricing badge -->
          @if (config.pricing) {
            <span class="prov-card__pricing" [attr.data-variant]="config.pricing.variant">
              {{ config.pricing.label }}
            </span>
          }

          <!-- Guide PDF -->
          @if (config.guideUrl) {
            <a
              class="prov-card__guide-btn"
              [href]="config.guideUrl"
              target="_blank"
              rel="noopener noreferrer"
              aria-label="Ouvrir le guide de connexion {{ config.label }}"
            >
              📖 Guide
            </a>
          }
        </div>

        <!-- Dynamic status badge -->
        <app-badge [variant]="statusBadge().variant">
          {{ statusBadge().text }}
        </app-badge>
      </div>

      <!-- ── Body ── -->
      <div class="prov-card__body">

        <!-- Fallback notice -->
        @if (config.fallback) {
          <div class="prov-card__fallback" [class.prov-card__fallback--active]="config.fallback.active">
            @if (config.fallback.active) {
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M20 6 9 17l-5-5"/>
              </svg>
            } @else {
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            }
            <span>
              <strong>Fallback :</strong> {{ config.fallback.label }}
            </span>
          </div>
        }

        <!-- Extraction mode select (si le provider propose plusieurs modes) -->
        @if (config.extractionModes) {
          <div class="extract-mode">
            <label class="extract-mode__label" [for]="'em-' + config.provider">
              Mode d'extraction
            </label>
            <select
              class="extract-mode__select"
              [id]="'em-' + config.provider"
              [ngModel]="currentExtractionMode()"
              (ngModelChange)="onExtractionModeChange($event)"
            >
              @for (m of config.extractionModes; track m.value) {
                <option [value]="m.value">{{ m.label }}</option>
              }
            </select>
          </div>
        }

        @if (config.noKeys) {
          <p class="prov-card__no-keys">
            Aucune configuration requise — actif automatiquement.
          </p>
        } @else {
          <div class="keys-list">
            <!-- Filtrer les clés gérées par le bloc extraction mode -->
            @for (key of secretKeys(); track key.key) {
              <div class="key-row">
                <div class="key-row__info">
                  <span class="key-row__name">{{ label(key.key) }}</span>
                  @if (key.isConfigured) {
                    <app-badge variant="success">Configuré ✓</app-badge>
                  } @else {
                    <app-badge variant="neutral">Non configuré</app-badge>
                  }
                </div>

                <div class="key-row__actions">
                  @if (editState(key.key)?.isEditing) {
                    <div class="key-row__edit">
                      <input
                        class="key-input"
                        type="password"
                        placeholder="Nouvelle valeur…"
                        autocomplete="new-password"
                        [ngModel]="editState(key.key)?.value"
                        (ngModelChange)="store.updateEditValue(config.provider, key.key, $event)"
                      />
                      <button appBtn variant="primary" size="sm"
                        [loading]="editState(key.key)?.saving ?? false"
                        [disabled]="!editState(key.key)?.value"
                        *hasPermission="'settings:write'"
                        (click)="save(key.key)"
                      >
                        Enregistrer
                      </button>
                      <button appBtn variant="ghost" size="sm"
                        (click)="store.cancelEdit(config.provider, key.key)"
                      >
                        Annuler
                      </button>
                    </div>
                  } @else {
                    <button appBtn variant="secondary" size="sm"
                      *hasPermission="'settings:write'"
                      (click)="store.startEdit(config.provider, key.key)"
                    >
                      Modifier
                    </button>
                  }
                </div>
              </div>
            }
          </div>
        }
      </div>

      <!-- ── Footer (test connexion CardMarket uniquement) ── -->
      @if (config.hasTestConnection) {
        <div class="prov-card__footer">
          <button appBtn variant="secondary" size="md"
            *hasPermission="'settings:write'"
            [loading]="store.testingConnection()"
            (click)="store.testCardmarketConnection()"
          >
            Tester la connexion
          </button>
        </div>
      }

    </div>
  `,
  styles: [`
    /* ── Card shell ── */
    .prov-card {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-sm);
      overflow: hidden;
    }

    /* ── Header ── */
    .prov-card__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--color-border-subtle);
      flex-wrap: wrap;
    }

    .prov-card__title-row {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .prov-card__name {
      font-size: var(--font-size-md);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
    }

    /* ── Pricing badge ── */
    .prov-card__pricing {
      display: inline-flex;
      align-items: center;
      padding: 2px 8px;
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      white-space: nowrap;
      flex-shrink: 0;

      &[data-variant="free"] {
        background: var(--color-semantic-success-subtle, #dcfce7);
        color: var(--color-semantic-success, #16a34a);
      }
      &[data-variant="limited"] {
        background: var(--color-semantic-warning-subtle, #fef9c3);
        color: var(--color-semantic-warning, #ca8a04);
      }
      &[data-variant="paid"] {
        background: var(--color-semantic-danger-subtle, #fee2e2);
        color: var(--color-semantic-danger, #dc2626);
      }
    }

    /* ── Fallback notice ── */
    .prov-card__fallback {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      margin-bottom: var(--space-4);
      border-radius: var(--radius-md);
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
      background: var(--color-surface-sunken);
      border: 1px solid var(--color-border-subtle);

      svg { flex-shrink: 0; color: var(--color-text-muted); }

      &--active {
        background: var(--color-semantic-success-subtle, #dcfce7);
        border-color: var(--color-semantic-success, #16a34a);
        color: var(--color-semantic-success, #16a34a);
        svg { color: var(--color-semantic-success, #16a34a); }
      }
    }

    /* ── Guide button ── */
    .prov-card__guide-btn {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      padding: 2px var(--space-2);
      border-radius: var(--radius-md);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      color: var(--color-brand-primary);
      background: var(--color-brand-primary-subtle);
      text-decoration: none;
      white-space: nowrap;
      transition: background var(--duration-fast) var(--easing-standard),
                  color var(--duration-fast) var(--easing-standard);
      flex-shrink: 0;
    }
    .prov-card__guide-btn:hover {
      background: var(--color-brand-primary);
      color: #fff;
    }

    /* ── Tooltip contribution ── */
    .prov-card__tooltip {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 18px;
      height: 18px;
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      cursor: default;
      border-radius: var(--radius-full);
      flex-shrink: 0;
    }

    .prov-card__tooltip::after {
      content: attr(data-tooltip);
      position: absolute;
      bottom: calc(100% + 6px);
      left: 50%;
      transform: translateX(-50%);
      background: var(--color-neutral-800, #1e293b);
      color: #fff;
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-normal);
      white-space: nowrap;
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-md);
      pointer-events: none;
      opacity: 0;
      transition: opacity 150ms ease;
      z-index: 100;
      /* small arrow */
      filter: drop-shadow(0 1px 3px rgba(0,0,0,.25));
    }

    .prov-card__tooltip:hover::after {
      opacity: 1;
    }

    /* ── Body ── */
    .prov-card__body {
      padding: var(--space-5);
    }

    .prov-card__no-keys {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin: 0;
    }

    /* ── Key rows ── */
    .keys-list {
      display: flex;
      flex-direction: column;
      gap: 0;
    }

    .key-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-4);
      padding: var(--space-4) 0;
      border-bottom: 1px solid var(--color-border-subtle);
      flex-wrap: wrap;
    }
    .key-row:last-of-type { border-bottom: none; }

    .key-row__info {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      min-width: 200px;
    }

    .key-row__name {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
      font-family: var(--font-family-mono);
    }

    .key-row__actions { flex-shrink: 0; }

    .key-row__edit {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      flex-wrap: wrap;
    }

    .key-input {
      padding: var(--space-1) var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      font-family: var(--font-family-mono);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      width: 240px;
      outline: none;
      transition: border-color var(--duration-fast) var(--easing-standard);
    }
    .key-input:focus {
      border-color: var(--color-brand-primary);
    }

    /* ── Extraction mode select ── */
    .extract-mode {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3) 0 var(--space-4);
      border-bottom: 1px solid var(--color-border-subtle);
      margin-bottom: var(--space-4);
    }

    .extract-mode__label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
      white-space: nowrap;
      flex-shrink: 0;
    }

    .extract-mode__select {
      padding: var(--space-1) var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      cursor: pointer;
      outline: none;
      transition: border-color var(--duration-fast) var(--easing-standard);
      min-width: 220px;
    }
    .extract-mode__select:focus {
      border-color: var(--color-brand-primary);
    }

    /* ── Footer ── */
    .prov-card__footer {
      padding: var(--space-4) var(--space-5);
      border-top: 1px solid var(--color-border-subtle);
      background: var(--color-surface-raised, var(--color-surface-base));
    }
  `],
})
export class ProviderCardComponent {
  @Input({ required: true }) config!: ProviderConfig;

  readonly store = inject(SettingsStore);

  /** Toutes les clés du provider depuis le store. */
  readonly keys = computed(() =>
    this.store.providerKeys()[this.config.provider] ?? []
  );

  /** Clés secrètes uniquement (filtre les clés non-sensibles avec widget dédié). */
  readonly secretKeys = computed(() =>
    this.keys().filter(k => !NON_SECRET_KEYS.has(k.key))
  );

  /** Valeur actuelle du mode d'extraction — lue depuis le store, défaut 'css'. */
  readonly currentExtractionMode = computed(() => {
    const modeKey = this.keys().find(k => k.key === 'extraction_mode');
    return modeKey?.value ?? 'css';
  });

  /** Dynamic status badge. */
  readonly statusBadge = computed((): { text: string; variant: BadgeVariant } => {
    if (this.config.noKeys) {
      return { text: 'Aucune clé requise', variant: 'info' };
    }
    const ks = this.secretKeys();
    if (ks.length === 0) return { text: 'Chargement…', variant: 'neutral' };
    const allConfigured = ks.every(k => k.isConfigured);
    return allConfigured
      ? { text: 'Configuré', variant: 'success' }
      : { text: 'Incomplet', variant: 'warning' };
  });

  label(key: string): string {
    return KEY_LABELS[key] ?? key;
  }

  editState(key: string) {
    return this.store.keyEditState()[`${this.config.provider}:${key}`];
  }

  save(key: string): void {
    const value = this.editState(key)?.value;
    if (value) {
      this.store.saveSetting({ provider: this.config.provider, key, value });
    }
  }

  onExtractionModeChange(value: string): void {
    this.store.saveKeyDirect({ provider: this.config.provider, key: 'extraction_mode', value });
  }
}
