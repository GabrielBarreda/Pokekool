import {
  Component, Input, Output, EventEmitter, signal, computed, input, inject,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MarketCardDto, CardGradingData, GradeEntry, GradingCompany, GradingCompanyId } from '../market.models';
import { GRADING_COMPANIES } from '../grading-mock.data';
import { ThemeStore } from '@core/theme/theme.store';

@Component({
  selector: 'app-card-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  host: {
    '[class.theme-pokemon]': 'themeStore.isPokemon()',
  },
  template: `
    <!-- Backdrop -->
    <div class="backdrop" (click)="closed.emit()"></div>

    <!-- Drawer -->
    <aside class="drawer" role="dialog" aria-modal="true" [attr.aria-label]="(card.displayName || card.name) + ' — détail'">

      <!-- Header -->
      <div class="drawer-header">
        <div class="drawer-card-info">
          <!-- En thème Pokémon, l'image est déjà affichée en grand (hero card
               tournoyant) par le parent (market-list) — pas besoin de la
               dupliquer ici. -->
          @if (card.imageSmall && !themeStore.isPokemon()) {
            <img [src]="card.imageLarge ?? card.imageSmall" [alt]="card.displayName || card.name" class="header-img" />
          }
          <div class="header-text">
            <h2 class="header-name">{{ card.displayName || card.name }}</h2>
            <div class="header-meta">
              <span class="meta-chip">#{{ card.number }}</span>
              @if (card.supertype) { <span class="meta-chip">{{ card.supertype }}</span> }
              @if (card.rarity)    { <span class="meta-chip meta-chip--rarity">{{ card.rarity }}</span> }
              @for (t of (card.types ?? []); track t) {
                <span class="meta-chip meta-chip--type">{{ t }}</span>
              }
            </div>
            @if (card.priceMarket !== null) {
              <div class="header-price">
                <span class="header-price__value">
                  {{ card.priceMarket | currency:(card.priceCurrency ?? 'USD'):'symbol':'1.2-2' }}
                </span>
                <span class="header-price__label">Prix marché · {{ card.priceSource }}</span>
              </div>
            }
          </div>
        </div>
        <button class="close-btn" aria-label="Fermer" (click)="closed.emit()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M18 6 6 18M6 6l12 12"/>
          </svg>
        </button>
      </div>

      <!-- Section titre -->
      <div class="section-title-bar">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
        </svg>
        Données de gradation
        <span class="title-spacer"></span>

        <!-- Toggle vue tableau / graphique -->
        @if (activeData()) {
          <div class="view-toggle" role="group" aria-label="Mode d'affichage">
            <button class="view-toggle__btn"
                    [class.view-toggle__btn--active]="gradeView() === 'table'"
                    aria-label="Vue tableau"
                    title="Vue tableau"
                    (click)="gradeView.set('table')">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="18" height="18" rx="2"/>
                <line x1="3" y1="9" x2="21" y2="9"/><line x1="3" y1="15" x2="21" y2="15"/>
                <line x1="9" y1="3" x2="9" y2="21"/>
              </svg>
            </button>
            <button class="view-toggle__btn"
                    [class.view-toggle__btn--active]="gradeView() === 'chart'"
                    aria-label="Vue graphique"
                    title="Vue graphique"
                    (click)="gradeView.set('chart')">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="20" x2="12" y2="10"/><line x1="18" y1="20" x2="18" y2="4"/><line x1="6" y1="20" x2="6" y2="16"/>
              </svg>
            </button>
          </div>
        }

        @if (activeData()?.lastUpdated; as dt) {
          <span class="data-date">MAJ {{ dt | date:'dd/MM HH:mm' }}</span>
        }
      </div>

      <!-- Company tabs — seulement les sociétés avec données -->
      <div class="company-tabs">
        @for (company of companies; track company.id) {
          @if (hasDataFor(company.id)) {
            <button class="company-tab"
                    [class.company-tab--active]="activeCompany() === company.id"
                    [style.--tab-color]="company.color"
                    (click)="activeCompany.set(company.id)">
              <span class="tab-dot" [style.background]="company.color"></span>
              {{ company.name }}
            </button>
          }
        }
      </div>

      <!-- Active company content -->
      @if (gradingLoading()) {
        <div class="grading-state">
          <svg class="grading-state__spinner" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
          </svg>
          Chargement…
        </div>
      } @else if (activeData(); as data) {
      <div class="company-panel" [style.--company-color]="activeCompanyObj()?.color">
        <!-- Summary chips -->
        <div class="summary-row">
          <div class="summary-chip">
            <span class="summary-chip__label">Gemme (10)</span>
            <span class="summary-chip__value">
              {{ topEntry()?.marketPrice | currency:'USD':'symbol':'1.0-0' }}
            </span>
          </div>
          <div class="summary-chip">
            <span class="summary-chip__label">Neuf (9)</span>
            <span class="summary-chip__value">
              {{ gradePrice(data, 9) | currency:'USD':'symbol':'1.0-0' }}
            </span>
          </div>
          <div class="summary-chip">
            <span class="summary-chip__label">Population</span>
            <span class="summary-chip__value">{{ totalPop(data) | number }}</span>
          </div>
          <div class="summary-chip">
            <span class="summary-chip__label">Ratio 10/9</span>
            <span class="summary-chip__value">{{ ratio10to9(data) }}×</span>
          </div>
        </div>

        <!-- SVG Bar chart: prix par grade -->
        @if (gradeView() === 'chart') {
        <div class="chart-wrap">
          <div class="chart-label">Prix marché par grade — {{ activeCompanyObj()?.name }}</div>
          <svg class="bar-chart" [attr.viewBox]="'0 0 ' + CHART_W + ' ' + CHART_H" preserveAspectRatio="xMidYMid meet">
            <!-- Y axis grid lines -->
            @for (tick of yTicks(); track tick.value) {
              <line [attr.x1]="MARGIN_LEFT" [attr.x2]="CHART_W - MARGIN_RIGHT"
                    [attr.y1]="tick.y" [attr.y2]="tick.y"
                    stroke="var(--color-border-subtle)" stroke-dasharray="3,3" />
              <text [attr.x]="MARGIN_LEFT - 6" [attr.y]="tick.y + 4"
                    text-anchor="end" font-size="10" fill="var(--color-text-muted)">
                {{ tick.label }}
              </text>
            }

            <!-- Bars -->
            @for (entry of data.entries; track entry.grade) {
              <g [attr.transform]="'translate(' + barX(entry.grade) + ',0)'">
                <!-- Low price bar (lighter) -->
                @if (entry.lowPrice !== null) {
                  <rect
                    [attr.x]="BAR_GAP"
                    [attr.y]="priceToY(entry.lowPrice ?? 0)"
                    [attr.width]="barWidth() - BAR_GAP * 2"
                    [attr.height]="CHART_H - MARGIN_BOTTOM - priceToY(entry.lowPrice ?? 0)"
                    [attr.fill]="activeCompanyObj()?.color ?? '#888'"
                    opacity="0.25"
                    rx="3" />
                }
                <!-- Market price bar -->
                @if (entry.marketPrice !== null) {
                  <rect
                    [attr.x]="BAR_GAP"
                    [attr.y]="priceToY(entry.marketPrice ?? 0)"
                    [attr.width]="barWidth() - BAR_GAP * 2"
                    [attr.height]="CHART_H - MARGIN_BOTTOM - priceToY(entry.marketPrice ?? 0)"
                    [attr.fill]="activeCompanyObj()?.color ?? '#888'"
                    opacity="0.85"
                    rx="3" />
                  <!-- Price label on bar -->
                  @if ((entry.marketPrice ?? 0) > maxPrice() * 0.06) {
                    <text
                      [attr.x]="(barWidth()) / 2"
                      [attr.y]="priceToY(entry.marketPrice ?? 0) - 4"
                      text-anchor="middle" font-size="9"
                      fill="var(--color-text-secondary)" font-weight="600">
                      {{ '$' + formatNum(entry.marketPrice ?? 0) }}
                    </text>
                  }
                }
                <!-- Grade label -->
                <text
                  [attr.x]="barWidth() / 2"
                  [attr.y]="CHART_H - MARGIN_BOTTOM + 14"
                  text-anchor="middle" font-size="11"
                  fill="var(--color-text-primary)" font-weight="600">
                  {{ entry.grade }}
                </text>
                <!-- Pop label -->
                <text
                  [attr.x]="barWidth() / 2"
                  [attr.y]="CHART_H - MARGIN_BOTTOM + 26"
                  text-anchor="middle" font-size="9"
                  fill="var(--color-text-muted)">
                  {{ formatPop(entry.population) }}
                </text>
              </g>
            }

            <!-- Legend: axes labels -->
            <text [attr.x]="MARGIN_LEFT - 4" [attr.y]="16"
                  text-anchor="end" font-size="9" fill="var(--color-text-muted)">Prix</text>
            <text [attr.x]="CHART_W / 2" [attr.y]="CHART_H - 2"
                  text-anchor="middle" font-size="9" fill="var(--color-text-muted)">Grade</text>
          </svg>

          <!-- Legend: barres -->
          <div class="chart-legend">
            <span class="legend-item">
              <span class="legend-dot" [style.background]="activeCompanyObj()?.color" style="opacity:0.85"></span>
              Prix marché
            </span>
            <span class="legend-item">
              <span class="legend-dot" [style.background]="activeCompanyObj()?.color" style="opacity:0.25"></span>
              Prix bas
            </span>
          </div>
        </div>
        }

        <!-- Table détail -->
        @if (gradeView() === 'table') {
        <div class="table-wrap">
          <table class="grade-table">
            <thead>
              <tr>
                <th>Grade</th>
                <th>Marché</th>
                <th>Bas</th>
                <th>Population</th>
                <th>% du tot.</th>
              </tr>
            </thead>
            <tbody>
              @for (grade of ALL_GRADES; track grade) {
                @if (gradeEntry(data, grade); as entry) {
                  <tr [class.row--top]="entry.grade === 10">
                    <td>
                      <span class="grade-badge" [class.grade-badge--gem]="entry.grade === 10">
                        {{ entry.grade }}
                      </span>
                    </td>
                    <td class="price-cell">
                      @if (entry.marketPrice !== null) {
                        {{ entry.marketPrice | currency:'USD':'symbol':'1.2-2' }}
                      } @else { — }
                    </td>
                    <td class="price-cell price-cell--low">
                      @if (entry.lowPrice !== null) {
                        {{ entry.lowPrice | currency:'USD':'symbol':'1.2-2' }}
                      } @else { — }
                    </td>
                    <td>
                      <div class="pop-bar-wrap">
                        <span class="pop-num">{{ entry.population | number }}</span>
                        <div class="pop-bar">
                          <div class="pop-bar__fill"
                               [style.width.%]="popPct(data, entry.population)"
                               [style.background]="activeCompanyObj()?.color"></div>
                        </div>
                      </div>
                    </td>
                    <td class="pct-cell">{{ popPct(data, entry.population) | number:'1.1-1' }}%</td>
                  </tr>
                } @else {
                  <tr class="row--no-data">
                    <td>
                      <span class="grade-badge">{{ grade }}</span>
                    </td>
                    <td colspan="4" class="no-data-cell">
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                        <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                        <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
                      </svg>
                      Pas de données actuellement
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        }
      </div>
      } @else {
        <!-- Aucune donnée pour cette société -->
        <div class="grading-state grading-state--empty">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
          <span>Pas de données actuellement</span>
          <span class="grading-state__sub">{{ activeCompanyObj()?.name }} — aucun prix de gradation disponible</span>
        </div>
      }

      <!-- Cote & ventes — réservé pour le futur suivi des transactions
           individuelles (entité CardSale côté back, pas encore implémentée). -->
      <div class="section-title-bar">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="12" y1="20" x2="12" y2="10"/><line x1="18" y1="20" x2="18" y2="4"/><line x1="6" y1="20" x2="6" y2="16"/>
        </svg>
        Cote & ventes
      </div>
      <div class="ladder-placeholder">
        <span class="ladder-placeholder__icon">📈</span>
        <p class="ladder-placeholder__text">Bientôt disponible</p>
        <p class="ladder-placeholder__sub">
          Fourchette de prix (haut / bas / moyenne) et historique des dernières
          ventes — arrivera avec le suivi des transactions individuelles.
        </p>
      </div>
    </aside>
  `,
  styles: [`
    /* ── Largeur du panneau ──
       Définie sur :host (ancêtre commun du backdrop et du drawer, qui sont
       deux éléments frères) pour que les deux puissent la lire. */
    :host {
      --drawer-w: clamp(420px, calc((100vw - var(--layout-sidebar-w, 240px)) * 0.8), calc(100vw - var(--layout-sidebar-w, 240px) - 360px));
    }
    /* Sous ~960px de large utile : plus assez de place pour garder 2 cartes
       visibles à gauche → le panneau repasse en plein écran façon modal. */
    @media (max-width: 960px) {
      :host { --drawer-w: 100vw; }
    }

    /* ── Thème Pokémon ──
       Le détail n'est plus un drawer plein écran : il est posé par le
       parent (market-list) dans l'espace libéré à côté du classeur, en
       panneau statique. Le morph carte → détail est assuré par la View
       Transition API (view-transition-name partagé, cf. selectCard/
       closeDetail du parent) ; ici on ne fait qu'ôter le positionnement
       fixed et l'animation slideIn, remplacée par un léger scale-in en
       secours pour les navigateurs sans support de la View Transition API. */
    :host(.theme-pokemon) {
      display: block;
      width: 100%;
      --drawer-w: 100%;
    }
    :host(.theme-pokemon) .backdrop { display: none; }
    :host(.theme-pokemon) .drawer {
      position: static;
      width: 100%;
      height: 100%;
      border-left: none;
      border-radius: var(--radius-2xl);
      animation: pokemonDetailIn var(--duration-slow) var(--easing-decelerate);
    }
    @keyframes pokemonDetailIn {
      from { opacity: 0; transform: scale(0.94); }
      to   { opacity: 1; transform: scale(1); }
    }
    @media (prefers-reduced-motion: reduce) {
      :host(.theme-pokemon) .drawer { animation: none; }
    }

    /* ── Backdrop ──
       Ne couvre que la zone du drawer lui-même : la colonne de gauche
       (≈ largeur de 2 cartes) reste visible, cliquable et scrollable
       pendant que le panneau est ouvert. Sur petit écran le drawer repasse
       en plein écran et le backdrop redevient un vrai overlay de fermeture. */
    .backdrop {
      position: fixed; top: 0; bottom: 0; right: 0;
      left: calc(100% - var(--drawer-w));
      z-index: calc(var(--z-modal) - 1);
      background: transparent;
      cursor: default;
    }
    @media (max-width: 960px) {
      .backdrop {
        left: 0; background: var(--color-surface-overlay); cursor: pointer;
        animation: fadeIn var(--duration-normal) var(--easing-decelerate);
      }
      @keyframes fadeIn { from { opacity: 0 } to { opacity: 1 } }
    }

    /* ── Drawer ── */
    .drawer {
      position: fixed; right: 0; top: 0; bottom: 0;
      width: var(--drawer-w); z-index: var(--z-modal);
      background: var(--color-surface-base);
      border-left: 1px solid var(--color-border-default);
      box-shadow: var(--shadow-xl);
      overflow-y: auto; overflow-x: hidden;
      display: flex; flex-direction: column;
      animation: slideIn var(--duration-normal) var(--easing-decelerate);
    }
    @keyframes slideIn { from { transform: translateX(100%) } to { transform: translateX(0) } }

    /* ── Header ── */
    .drawer-header {
      display: flex; align-items: flex-start; gap: var(--space-4);
      padding: var(--space-5) var(--space-6);
      border-bottom: 1px solid var(--color-border-default);
      background: var(--color-surface-raised);
      position: sticky; top: 0; z-index: 1;
    }
    .drawer-card-info { display: flex; align-items: flex-start; gap: var(--space-4); flex: 1; min-width: 0; }
    .header-img { width: 72px; height: auto; border-radius: var(--radius-md); flex-shrink: 0; box-shadow: var(--shadow-md); }
    .header-text { display: flex; flex-direction: column; gap: var(--space-2); flex: 1; min-width: 0; }
    .header-name { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text-primary); margin: 0; }
    .header-meta { display: flex; flex-wrap: wrap; gap: var(--space-1); }
    .meta-chip {
      font-size: var(--font-size-xs); border-radius: var(--radius-full);
      padding: 2px 8px; background: var(--color-surface-sunken);
      border: 1px solid var(--color-border-subtle); color: var(--color-text-secondary);
      &--rarity { background: var(--color-semantic-warning-subtle); border-color: var(--color-semantic-warning); color: #7c5900; }
      &--type { background: var(--color-semantic-info-subtle); border-color: var(--color-semantic-info); color: var(--color-semantic-info); }
    }
    .header-price { margin-top: var(--space-1); }
    .header-price__value { font-size: var(--font-size-lg); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); }
    .header-price__label { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-left: var(--space-2); }
    .close-btn {
      width: 36px; height: 36px; flex-shrink: 0;
      display: flex; align-items: center; justify-content: center;
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: transparent; color: var(--color-text-muted); cursor: pointer;
      &:hover { background: var(--color-neutral-100); color: var(--color-text-primary); }
    }

    /* ── Section title ── */
    .section-title-bar {
      display: flex; align-items: center; gap: var(--space-2);
      padding: var(--space-4) var(--space-6) var(--space-2);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
    }
    .data-date {
      font-size: var(--font-size-xs); font-weight: var(--font-weight-regular);
      color: var(--color-text-muted); white-space: nowrap;
    }
    .title-spacer { flex: 1; }

    /* ── Toggle vue tableau / graphique ── */
    .view-toggle {
      display: flex; gap: 2px; padding: 2px;
      background: var(--color-surface-sunken);
      border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md);
    }
    .view-toggle__btn {
      display: flex; align-items: center; justify-content: center;
      width: 26px; height: 24px; border: none; border-radius: calc(var(--radius-md) - 2px);
      background: transparent; color: var(--color-text-muted); cursor: pointer;
      transition: all var(--duration-fast);
      &:hover { color: var(--color-text-primary); }
      &--active { background: var(--color-surface-base); color: var(--color-brand-primary); box-shadow: var(--shadow-sm); }
    }

    /* ── Company tabs ── */
    .company-tabs {
      display: flex; gap: var(--space-1); padding: 0 var(--space-6);
      overflow-x: auto; scrollbar-width: none;
      &::-webkit-scrollbar { display: none; }
    }
    .company-tab {
      display: flex; align-items: center; gap: 6px;
      padding: var(--space-2) var(--space-3); border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default);
      background: var(--color-surface-base); cursor: pointer;
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      color: var(--color-text-secondary); white-space: nowrap;
      transition: all var(--duration-fast);
      &:hover { border-color: var(--tab-color, var(--color-brand-primary)); color: var(--color-text-primary); }
      &--active { background: var(--tab-color, var(--color-brand-primary)); border-color: var(--tab-color); color: #fff; }
    }
    .tab-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .company-tab--active .tab-dot { background: rgba(255,255,255,0.7) !important; }

    /* ── Company panel ──
       Accent de couleur propre à la société de gradation active, pour bien
       séparer visuellement les données PSA / CGC / SGC / … entre elles. */
    .company-panel {
      border-left: 3px solid var(--company-color, var(--color-brand-primary));
      margin: var(--space-2) 0 0 var(--space-6);
      padding-left: var(--space-4);
    }
    .company-panel .summary-row,
    .company-panel .chart-wrap,
    .company-panel .table-wrap { padding-left: 0; }

    /* ── Summary chips ── */
    .summary-row {
      display: flex; gap: var(--space-3); flex-wrap: wrap;
      padding: var(--space-4) var(--space-6);
    }
    .summary-chip {
      flex: 1; min-width: 110px;
      background: var(--color-surface-raised); border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-lg); padding: var(--space-3) var(--space-4);
      display: flex; flex-direction: column; gap: 2px;
    }
    .summary-chip__label { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .summary-chip__value { font-size: var(--font-size-md); font-weight: var(--font-weight-bold); color: var(--color-text-primary); }

    /* ── Bar chart ── */
    .chart-wrap { padding: 0 var(--space-6) var(--space-2); }
    .chart-label { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-bottom: var(--space-2); }
    .bar-chart { width: 100%; display: block; }
    .chart-legend { display: flex; gap: var(--space-4); margin-top: var(--space-2); }
    .legend-item { display: flex; align-items: center; gap: 5px; font-size: var(--font-size-xs); color: var(--color-text-secondary); }
    .legend-dot { width: 10px; height: 10px; border-radius: 2px; display: inline-block; }

    /* ── Table ── */
    .table-wrap { padding: var(--space-2) var(--space-6) var(--space-8); }
    .grade-table {
      width: 100%; border-collapse: collapse; font-size: var(--font-size-sm);
      th {
        text-align: left; padding: var(--space-2) var(--space-3);
        font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold);
        color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.05em;
        border-bottom: 2px solid var(--color-border-default);
      }
      td { padding: var(--space-2) var(--space-3); border-bottom: 1px solid var(--color-border-subtle); vertical-align: middle; }
      .row--top td { background: var(--color-brand-primary-subtle); font-weight: var(--font-weight-semibold); }
    }
    .grade-badge {
      display: inline-flex; align-items: center; justify-content: center;
      width: 30px; height: 30px; border-radius: var(--radius-md);
      font-weight: var(--font-weight-bold); font-size: var(--font-size-sm);
      background: var(--color-surface-sunken); border: 1px solid var(--color-border-default);
      &--gem { background: linear-gradient(135deg, #f59e0b, #fbbf24); border-color: #f59e0b; color: #7c2d12; }
    }
    .price-cell { font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
    .price-cell--low { color: var(--color-text-secondary); font-weight: var(--font-weight-regular); }
    .pop-bar-wrap { display: flex; align-items: center; gap: var(--space-2); }
    .pop-num { font-size: var(--font-size-xs); color: var(--color-text-secondary); min-width: 40px; }
    .pop-bar { flex: 1; height: 6px; background: var(--color-neutral-200); border-radius: 3px; overflow: hidden; }
    .pop-bar__fill { height: 100%; border-radius: 3px; transition: width var(--duration-normal); }
    .pct-cell { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    /* ── No-data rows ── */
    .row--no-data td { padding: var(--space-2) var(--space-3); }
    .no-data-cell {
      display: flex; align-items: center; gap: var(--space-2);
      font-size: var(--font-size-xs); color: var(--color-text-muted);
      font-style: italic;
      svg { color: var(--color-semantic-warning); flex-shrink: 0; }
    }

    /* ── Loading / empty state ── */
    .grading-state {
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      gap: var(--space-3); padding: var(--space-12) var(--space-6);
      color: var(--color-text-muted); font-size: var(--font-size-sm);
      text-align: center;
      svg { color: var(--color-brand-primary); }
      &__spinner {
        animation: spin 1s linear infinite;
        @keyframes spin { to { transform: rotate(360deg) } }
      }
      &__sub { font-size: var(--font-size-xs); color: var(--color-text-muted); }
      &--empty svg { color: var(--color-semantic-warning); }
    }

    /* ── Cote & ventes (placeholder) ── */
    .ladder-placeholder {
      display: flex; flex-direction: column; align-items: center; text-align: center;
      gap: var(--space-2); padding: var(--space-8) var(--space-6) var(--space-10);
      opacity: 0.65;
    }
    .ladder-placeholder__icon { font-size: 30px; }
    .ladder-placeholder__text { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); margin: 0; }
    .ladder-placeholder__sub { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0; max-width: 340px; }
  `],
})
export class CardDetailComponent {
  @Input({ required: true }) card!: MarketCardDto;
  // Signal inputs — réactifs dans computed() sans ngOnChanges
  readonly gradingData   = input.required<CardGradingData[]>();
  readonly gradingLoading = input<boolean>(false);
  @Output() closed = new EventEmitter<void>();

  readonly themeStore = inject(ThemeStore);

  readonly companies: GradingCompany[] = GRADING_COMPANIES;
  readonly activeCompany = signal<GradingCompanyId>('PSA');
  readonly gradeView = signal<'table' | 'chart'>('table');
  readonly ALL_GRADES = [10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

  // Chart dimensions
  readonly CHART_W = 560;
  readonly CHART_H = 200;
  readonly MARGIN_LEFT = 42;
  readonly MARGIN_RIGHT = 10;
  readonly MARGIN_TOP = 20;
  readonly MARGIN_BOTTOM = 38;
  readonly BAR_GAP = 3;

  readonly activeData = computed(() =>
    this.gradingData().find(d => d.company === this.activeCompany()) ?? null
  );

  readonly activeCompanyObj = computed(() =>
    this.companies.find(c => c.id === this.activeCompany()) ?? null
  );

  readonly topEntry = computed(() =>
    this.activeData()?.entries.find(e => e.grade === 10) ?? null
  );

  readonly maxPrice = computed(() => {
    const entries = this.activeData()?.entries ?? [];
    return Math.max(...entries.map(e => e.marketPrice ?? 0), 0);
  });

  readonly barWidth = computed(() => {
    const usable = this.CHART_W - this.MARGIN_LEFT - this.MARGIN_RIGHT;
    return usable / 10;
  });

  barX(grade: number): number {
    // entries are 10,9,...,1 → index 0=grade10
    const idx = 10 - grade;
    return this.MARGIN_LEFT + idx * this.barWidth();
  }

  priceToY(price: number): number {
    const max = this.maxPrice();
    if (max === 0) return this.CHART_H - this.MARGIN_BOTTOM;
    const ratio = price / (max * 1.1);
    const usableH = this.CHART_H - this.MARGIN_TOP - this.MARGIN_BOTTOM;
    return this.CHART_H - this.MARGIN_BOTTOM - ratio * usableH;
  }

  readonly yTicks = computed(() => {
    const max = this.maxPrice();
    if (max === 0) return [];
    const step = this.niceStep(max * 1.1, 4);
    const ticks = [];
    for (let v = 0; v <= max * 1.1; v += step) {
      ticks.push({ value: v, y: this.priceToY(v), label: this.formatNum(v) });
    }
    return ticks;
  });

  private niceStep(max: number, count: number): number {
    const rough = max / count;
    const magnitude = Math.pow(10, Math.floor(Math.log10(rough)));
    const candidates = [1, 2, 5, 10].map(x => x * magnitude);
    return candidates.find(c => c >= rough) ?? candidates[candidates.length - 1];
  }

  gradeEntry(data: CardGradingData, grade: number): GradeEntry | undefined {
    return data.entries.find(e => e.grade === grade);
  }

  hasDataFor(companyId: GradingCompanyId): boolean {
    return this.gradingData().some(d => d.company === companyId);
  }

  gradePrice(data: CardGradingData, grade: number): number | null {
    return data.entries.find(e => e.grade === grade)?.marketPrice ?? null;
  }

  totalPop(data: CardGradingData): number {
    return data.entries.reduce((s, e) => s + (e.population ?? 0), 0);
  }

  ratio10to9(data: CardGradingData): string {
    const p10 = data.entries.find(e => e.grade === 10)?.marketPrice ?? 0;
    const p9  = data.entries.find(e => e.grade === 9)?.marketPrice ?? 0;
    if (!p9) return '—';
    return (p10 / p9).toFixed(1);
  }

  popPct(data: CardGradingData, pop: number | null): number {
    const total = this.totalPop(data);
    if (!total || !pop) return 0;
    return Math.round((pop / total) * 1000) / 10;
  }

  formatNum(n: number): string {
    if (n >= 1000) return (n / 1000).toFixed(1) + 'k';
    return n.toFixed(0);
  }

  formatPop(pop: number | null): string {
    if (pop === null) return '';
    if (pop >= 1000) return (pop / 1000).toFixed(1) + 'k';
    return String(pop);
  }
}
