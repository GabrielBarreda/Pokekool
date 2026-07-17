/**
 * Petit moteur de tween basé requestAnimationFrame (aucune dépendance).
 * Utilisé pour les cinématiques du viewer 3D (caméra + objets).
 */
export const easeInOutCubic = (t: number): number =>
  t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;

export interface TweenOptions {
  durationMs: number;
  ease?: (t: number) => number;
  onUpdate: (k: number) => void;   // k ∈ [0,1] (après easing)
  onDone?: () => void;
}

/** Lance un tween ; retourne une fonction d'annulation. */
export function tween(opts: TweenOptions): () => void {
  const ease = opts.ease ?? easeInOutCubic;
  const start = performance.now();
  let raf = 0;
  let cancelled = false;

  const step = (now: number): void => {
    if (cancelled) return;
    const t = Math.min(1, (now - start) / opts.durationMs);
    opts.onUpdate(ease(t));
    if (t < 1) {
      raf = requestAnimationFrame(step);
    } else {
      opts.onDone?.();
    }
  };
  raf = requestAnimationFrame(step);

  return () => { cancelled = true; cancelAnimationFrame(raf); };
}
