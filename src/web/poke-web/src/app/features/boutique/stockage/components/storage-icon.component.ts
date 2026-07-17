import { Component, computed, input } from '@angular/core';

export type StorageIconName = 'box' | 'binder' | 'furniture';

/** Palette des couvertures de classeur (cover, dos, anneaux). Variantes cyclées par `variant`. */
const BINDER_PALETTE: { cover: string; spine: string; ring: string; page: string }[] = [
  { cover: '#0FA3A3', spine: '#0B7C7C', ring: '#ffffff', page: '#f4f6fb' }, // teal
  { cover: '#3A3F45', spine: '#24272B', ring: '#ffffff', page: '#e9ecf0' }, // anthracite
  { cover: '#C0392B', spine: '#8E271C', ring: '#ffffff', page: '#f7ece9' }, // rouge
  { cover: '#E9ECF0', spine: '#C4CAD2', ring: '#6b7280', page: '#ffffff' }, // clair
  { cover: '#F1C40F', spine: '#C79E06', ring: '#5b4d00', page: '#fffdf3' }, // jaune
];

/**
 * Icônes remplies du module Stockage (carton kraft, classeur, étagère), rendues inline.
 * Le classeur accepte une `variant` (0..) qui choisit une couleur de couverture dans BINDER_PALETTE.
 */
@Component({
  selector: 'app-storage-icon',
  standalone: true,
  template: `
    @switch (name()) {
      @case ('box') {
        <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" aria-hidden="true">
          <polygon points="12,7 4,4.6 4,8 12,10.6" fill="#e3c391"/>
          <polygon points="12,7 20,4.6 20,8 12,10.6" fill="#d7b47c"/>
          <polygon points="4,8 12,10.6 12,21 4,18.4" fill="#c99c60"/>
          <polygon points="20,8 12,10.6 12,21 20,18.4" fill="#b98a4c"/>
          <rect x="11.1" y="10.4" width="1.8" height="10.6" fill="#a5763c"/>
        </svg>
      }
      @case ('binder') {
        <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" aria-hidden="true">
          <rect x="4" y="3" width="16" height="18" rx="1.6" [attr.fill]="palette().cover"/>
          <rect x="9" y="4.5" width="10" height="15" rx=".6" [attr.fill]="palette().page"/>
          <rect x="4" y="3" width="5" height="18" rx="1.6" [attr.fill]="palette().spine"/>
          <circle cx="6.5" cy="8"  r="1.05" [attr.fill]="palette().ring"/>
          <circle cx="6.5" cy="12" r="1.05" [attr.fill]="palette().ring"/>
          <circle cx="6.5" cy="16" r="1.05" [attr.fill]="palette().ring"/>
          <rect x="4" y="3" width="16" height="18" rx="1.6" fill="none" stroke="rgba(0,0,0,.14)" stroke-width=".6"/>
        </svg>
      }
      @default {
        <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" aria-hidden="true">
          <rect x="4" y="3" width="16" height="18" rx="1.2" fill="#eef1f5" stroke="#5b6472" stroke-width="1.6"/>
          <line x1="4" y1="9"  x2="20" y2="9"  stroke="#5b6472" stroke-width="1.5"/>
          <line x1="4" y1="15" x2="20" y2="15" stroke="#5b6472" stroke-width="1.5"/>
          <rect x="6"  y="10.4" width="4" height="3.4" fill="#c99c60"/>
          <rect x="13" y="4.4"  width="4" height="3.4" fill="#2E5AAC"/>
        </svg>
      }
    }
  `,
  styles: [`
    :host { display: inline-flex; line-height: 0; }
    svg { display: block; }
  `],
})
export class StorageIconComponent {
  readonly name = input.required<StorageIconName>();
  readonly size = input<number>(20);
  /** Variante de couleur (classeur uniquement). Cyclée sur la palette. */
  readonly variant = input<number>(0);

  readonly palette = computed(() => {
    const i = ((this.variant() % BINDER_PALETTE.length) + BINDER_PALETTE.length) % BINDER_PALETTE.length;
    return BINDER_PALETTE[i];
  });
}
