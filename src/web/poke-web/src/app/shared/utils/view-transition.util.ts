/**
 * Petite abstraction autour de la View Transition API du navigateur, utilisée
 * pour le morph carte de la grille ↔ panneau de détail en thème Pokémon.
 *
 * - Si l'API n'est pas supportée (Firefox/Safari anciens) ou si l'utilisateur
 *   a demandé `prefers-reduced-motion: reduce`, `updateFn` est simplement
 *   exécutée sans transition — le comportement fonctionnel reste identique.
 * - Le projet utilise déjà la View Transition API au niveau du routeur
 *   (`withViewTransitions()` + keyframes `vt-zoom-in/out` dans styles.scss) ;
 *   cette fonction applique la même logique de façon locale, à l'intérieur
 *   d'un composant, pour une transition « carte → détail ».
 * - Défensif : si `startViewTransition` lève une erreur (ex: nom de
 *   `view-transition-name` en doublon), on retombe silencieusement sur un
 *   changement d'état direct plutôt que de casser l'interaction.
 */

export interface RunViewTransitionHandle {
  finished: Promise<void>;
}

type DocumentWithViewTransitions = Document & {
  startViewTransition?: (cb: () => void) => RunViewTransitionHandle;
};

export function runViewTransition(updateFn: () => void): RunViewTransitionHandle | undefined {
  const prefersReducedMotion =
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  const doc = document as DocumentWithViewTransitions;
  const supportsViewTransitions = typeof doc.startViewTransition === 'function';

  if (prefersReducedMotion || !supportsViewTransitions) {
    updateFn();
    return undefined;
  }

  try {
    return doc.startViewTransition!(updateFn);
  } catch {
    // Nom en doublon ou autre échec de l'API — on ne bloque jamais l'UI.
    updateFn();
    return undefined;
  }
}
