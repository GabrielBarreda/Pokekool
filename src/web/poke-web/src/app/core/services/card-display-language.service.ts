import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'pokekool.displayLanguage';
const DEFAULT_LANGUAGE = 'en';

/**
 * Préférence de langue d'affichage des noms de cartes — persistée en localStorage uniquement.
 * Ne résout AUCUN nom lui-même : la résolution (DisplayName) se fait côté backend à partir de
 * la table card_translations, via le header envoyé par displayLanguageInterceptor. Voir
 * ADR-004 (mémoire Obsidian du projet).
 */
@Injectable({ providedIn: 'root' })
export class CardDisplayLanguageService {
  private readonly _language = signal<string>(this.readStored());

  readonly language = this._language.asReadonly();

  setLanguage(language: string): void {
    const normalized = language.trim().toLowerCase() || DEFAULT_LANGUAGE;
    this._language.set(normalized);
    try {
      localStorage.setItem(STORAGE_KEY, normalized);
    } catch {
      // localStorage indisponible (navigation privée, etc.) — la préférence reste en mémoire
      // pour la session en cours, simplement pas persistée entre rechargements.
    }
  }

  private readStored(): string {
    try {
      return localStorage.getItem(STORAGE_KEY) || DEFAULT_LANGUAGE;
    } catch {
      return DEFAULT_LANGUAGE;
    }
  }
}
