import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '@shared/ui/card/card.component';
import { ToastService } from '@shared/services/toast.service';
import { ScanHistoryService } from '../../scan-history/scan-history.service';

/** Libellés + description affichés pour chaque modèle proposé — tenus à jour manuellement en
 * miroir de VisionModelSettingsKeys.AllowedModels côté backend (source de vérité pour la liste
 * elle-même, chargée dynamiquement ; ce mapping n'est qu'un affinage d'affichage, avec repli sur
 * le nom brut du modèle si un nouveau modèle est ajouté côté backend sans mise à jour ici). */
const MODEL_INFO: Record<string, { label: string; hint: string }> = {
  'llava:7b': {
    label: 'llava:7b',
    hint: 'Défaut historique — le plus lourd, référence qualité (~7B paramètres).',
  },
  'llava-phi3': {
    label: 'llava-phi3',
    hint: 'Backbone Phi-3-mini (~3.8B) — garde l\'architecture vision + raisonnement de llava, '
      + 'plus léger. Meilleur compromis probable (lecture étiquette gradation incluse).',
  },
  moondream: {
    label: 'moondream',
    hint: 'Le plus léger (~1.8B) — optimisé description/lecture courte. À tester sur les '
      + 'étiquettes de gradation (texte dense/petit), potentiellement moins fiable.',
  },
};

/**
 * Onglet Paramètres › Scan (IA Vision).
 * Modèle Ollama Vision utilisé par le fallback texte/vision du scan (3e tentative et au-delà,
 * voir RerunScanBatchItemsCommandHandler) et par le scan mobile "1 carte à la fois" — comparatif
 * CPU llava:7b / llava-phi3 / moondream demandé le 2026-09-11 (pic à 569% CPU observé avec
 * llava:7b sur un run boutique réel). Persisté via AppSetting côté back (VisionModelSettingsKeys),
 * appliqué immédiatement sans redémarrage de l'API — un seul modèle tourne à la fois.
 */
@Component({
  selector: 'app-scan-vision-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, FormsModule],
  template: `
    <app-card title="Modèle Vision (OCR / lecture de carte)">
      <p class="hint">
        Modèle utilisé pour lire le texte imprimé sur une carte (nom, numéro, étiquette de
        gradation PSA/BGS/CGC) — déclenché à la 3ᵉ tentative de scan sur une même photo, et par le
        scan mobile "1 carte à la fois". Un seul modèle tourne à la fois ; les autres restent
        disponibles au choix ci-dessous sans rien réinstaller (déjà téléchargés côté Ollama).
      </p>

      @if (loading()) {
        <p class="muted">Chargement…</p>
      } @else {
        <label class="field">
          <span class="field__label">Modèle actif</span>
          <select
            class="field__select"
            [ngModel]="model()"
            (ngModelChange)="set($event)"
            [disabled]="saving()"
          >
            @for (m of allowedModels(); track m) {
              <option [value]="m">{{ modelLabel(m) }}</option>
            }
          </select>
        </label>

        @if (modelHint(); as hint) {
          <p class="hint hint--model">{{ hint }}</p>
        }

        @if (saving()) {
          <p class="muted">Enregistrement…</p>
        }
      }
    </app-card>
  `,
  styles: [`
    .hint { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin-bottom: var(--space-4); line-height: 1.5; }
    .hint--model { margin-top: var(--space-3); margin-bottom: 0; color: var(--color-text-muted); font-size: var(--font-size-xs); }
    .muted { font-size: var(--font-size-sm); color: var(--color-text-muted); }

    .field { display: flex; flex-direction: column; gap: var(--space-2); max-width: 360px; }
    .field__label { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .field__select {
      padding: var(--space-3);
      border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      font-family: var(--font-family-sans);
      font-size: var(--font-size-sm);
      cursor: pointer;
    }
    .field__select:disabled { opacity: .6; cursor: default; }
  `],
})
export class ScanVisionSettingsComponent implements OnInit {
  private readonly service = inject(ScanHistoryService);
  private readonly toast = inject(ToastService);

  readonly model = signal<string>('llava:7b');
  readonly allowedModels = signal<string[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.service.getVisionModelSettings().subscribe({
      next: s => {
        this.model.set(s.model);
        this.allowedModels.set(s.allowedModels);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Impossible de charger le réglage du modèle Vision.');
      },
    });
  }

  set(value: string): void {
    if (this.saving() || this.model() === value) return;
    this.saving.set(true);
    this.service.updateVisionModelSettings(value).subscribe({
      next: s => {
        this.model.set(s.model);
        this.saving.set(false);
        this.toast.success(`Modèle Vision : ${this.modelLabel(s.model)}.`);
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Enregistrement impossible.');
      },
    });
  }

  modelLabel(model: string): string {
    return MODEL_INFO[model]?.label ?? model;
  }

  modelHint(): string | null {
    return MODEL_INFO[this.model()]?.hint ?? null;
  }
}
