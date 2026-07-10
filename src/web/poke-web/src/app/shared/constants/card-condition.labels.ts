// Source unique des libellés d'état de carte (FR).
// Le code (NM/LP/MP/HP/DMG) reste la valeur stockée/technique ; seul le libellé est affiché.
// Traduction EN/ES à venir : voir la note "Internationalisation-i18n" dans le vault.

export interface CardConditionOption {
  /** Code technique persisté (ne pas traduire). */
  readonly code: string;
  /** Libellé affiché à l'utilisateur. */
  readonly label: string;
}

export const CARD_CONDITIONS: readonly CardConditionOption[] = [
  { code: 'NM', label: 'Neuf' },
  { code: 'LP', label: 'Très bon' },
  { code: 'MP', label: 'Bon' },
  { code: 'HP', label: 'Usé' },
  { code: 'DMG', label: 'Abîmé' },
] as const;

const CONDITION_LABELS: Readonly<Record<string, string>> =
  Object.fromEntries(CARD_CONDITIONS.map(c => [c.code, c.label]));

/** Retourne le libellé FR d'un code d'état, ou le code brut si inconnu, ou '' si vide. */
export function conditionLabel(code: string | null | undefined): string {
  if (!code) return '';
  return CONDITION_LABELS[code] ?? code;
}
