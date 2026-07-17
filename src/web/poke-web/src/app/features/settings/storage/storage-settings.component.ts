import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { CardComponent } from '@shared/ui/card/card.component';
import { ToastService } from '@shared/services/toast.service';
import { StorageService } from '../../boutique/stockage/storage.service';

/**
 * Onglet Paramètres › Stockage.
 * Réglages non sensibles du module Stockage Physique (persistés via AppSetting côté back).
 */
@Component({
  selector: 'app-storage-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  template: `
    <app-card title="Suppression en chaîne">
      <p class="hint">
        Lorsqu'elle est <strong>activée</strong>, supprimer une pièce ou un carton supprime aussi
        tout son contenu (meubles, classeurs, cartes rangées). Lorsqu'elle est <strong>désactivée</strong>,
        le contenu est <strong>préservé</strong> : les classeurs d'un carton supprimé deviennent
        « non rangés », et une pièce ne peut être supprimée que si elle est vide.
      </p>

      @if (loading()) {
        <p class="muted">Chargement…</p>
      } @else {
        <div class="seg" role="group" aria-label="Suppression en chaîne">
          <button type="button" class="seg__opt"
                  [class.seg__opt--active]="cascade()"
                  [disabled]="saving()"
                  (click)="set(true)">
            <span class="seg__icon">⛓️</span>
            <span class="seg__label">Activée</span>
            <span class="seg__desc">Cascade complète (défaut)</span>
          </button>
          <button type="button" class="seg__opt"
                  [class.seg__opt--active]="!cascade()"
                  [disabled]="saving()"
                  (click)="set(false)">
            <span class="seg__icon">🛡️</span>
            <span class="seg__label">Désactivée</span>
            <span class="seg__desc">Contenu détaché / protégé</span>
          </button>
        </div>
      }
    </app-card>
  `,
  styles: [`
    .hint { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin-bottom: var(--space-4); line-height: 1.5; }
    .muted { font-size: var(--font-size-sm); color: var(--color-text-muted); }
    .seg { display: flex; gap: var(--space-3); }
    .seg__opt {
      display: flex; flex-direction: column; align-items: flex-start; gap: var(--space-1);
      padding: var(--space-3) var(--space-4); flex: 1;
      border: 2px solid var(--color-border-default); border-radius: var(--radius-lg);
      background: var(--color-surface-raised); cursor: pointer; text-align: left;
      transition: border-color var(--duration-fast) var(--easing-standard),
                  background var(--duration-fast) var(--easing-standard);
    }
    .seg__opt:hover:not(:disabled) { border-color: var(--color-brand-primary); background: var(--color-surface-base); }
    .seg__opt--active { border-color: var(--color-brand-primary); background: var(--color-brand-primary-subtle); }
    .seg__opt:disabled { opacity: .6; cursor: default; }
    .seg__icon { font-size: var(--font-size-xl); }
    .seg__label { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .seg__desc { font-size: var(--font-size-xs); color: var(--color-text-muted); }
  `],
})
export class StorageSettingsComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly cascade = signal(true);
  readonly loading = signal(true);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.service.getStorageSettings().subscribe({
      next: s => { this.cascade.set(s.cascadeDelete); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Impossible de charger les paramètres Stockage.'); },
    });
  }

  set(value: boolean): void {
    if (this.saving() || this.cascade() === value) return;
    this.saving.set(true);
    this.service.updateStorageSettings({ cascadeDelete: value }).subscribe({
      next: s => { this.cascade.set(s.cascadeDelete); this.saving.set(false); this.toast.success('Paramètre enregistré.'); },
      error: () => { this.saving.set(false); this.toast.error('Enregistrement impossible.'); },
    });
  }
}
