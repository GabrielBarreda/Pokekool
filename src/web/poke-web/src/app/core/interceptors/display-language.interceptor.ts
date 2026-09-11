import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CardDisplayLanguageService } from '@core/services/card-display-language.service';

/**
 * Ajoute le header X-Display-Language sur chaque requête API, lu par
 * ICurrentDisplayLanguageService côté backend pour résoudre DisplayName (voir ADR-004,
 * mémoire Obsidian du projet).
 */
export const displayLanguageInterceptor: HttpInterceptorFn = (req, next) => {
  const displayLanguage = inject(CardDisplayLanguageService);

  const cloned = req.clone({
    setHeaders: { 'X-Display-Language': displayLanguage.language() },
  });

  return next(cloned);
};
