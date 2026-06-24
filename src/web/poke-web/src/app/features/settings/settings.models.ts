export interface ProviderKeyDto {
  key: string;
  isConfigured: boolean;
  /** Valeur actuelle — uniquement pour les clés non-sensibles (ex: extraction_mode) */
  value?: string | null;
}

export interface ProviderSettingsDto {
  provider: string;
  keys: ProviderKeyDto[];
}

export interface TestConnectionResultDto {
  success: boolean;
  message: string;
}

// ── Static provider metadata ──────────────────────────────────────────────────

export type ProviderBadgeVariant = 'success' | 'warning' | 'info';

export interface ProviderConfig {
  /** Identifiant utilisé dans les appels API */
  provider: string;
  /** Nom affiché */
  label: string;
  /** Texte du tooltip contribution */
  contribution: string;
  /**
   * Badge fixe affiché à côté du nom (ex: "API indisponible", "Actif").
   * Distinct du badge de statut dynamique (Configuré / Incomplet).
   */
  fixedBadge?: { text: string; variant: ProviderBadgeVariant };
  /** Ce provider n'a aucune clé à configurer (ex: cardmarket-scraping) */
  noKeys?: boolean;
  /** Afficher le bouton "Tester la connexion" */
  hasTestConnection?: boolean;
  /** Chemin vers le PDF de guide utilisateur (relatif à la racine du site) */
  guideUrl?: string;
  /** Tarification de l'accès à ce provider */
  pricing?: {
    label: string;
    /** free = vert · limited = orange · paid = rouge */
    variant: 'free' | 'limited' | 'paid';
  };
  /**
   * Fallback actif quand l'API officielle est indisponible ou non configurée.
   * Ex: scraping public en remplacement d'une API payante.
   */
  fallback?: {
    label: string;
    /** true = fallback actuellement actif et fonctionnel */
    active: boolean;
  };
  /**
   * Si défini, affiche un select dans la card pour choisir le mode d'extraction.
   * Sauvegardé sous la clé "extraction_mode" dans ProviderSettingsService.
   */
  extractionModes?: Array<{ value: string; label: string }>;
}

export const PROVIDERS: ProviderConfig[] = [
  {
    provider: 'cardmarket',
    label: 'CardMarket',
    contribution: 'Prix EUR · marché européen',
    fixedBadge: { text: 'API indisponible', variant: 'warning' },
    hasTestConnection: true,
    guideUrl: '/guides/cardmarket.pdf',
    pricing: { label: 'API fermée', variant: 'paid' },
    fallback: { label: 'Scraping CardMarket actif (provider dédié)', active: true },
  },
  {
    provider: 'ebay',
    label: 'eBay',
    contribution: 'Ventes réalisées · prix gradés PSA/CGC',
    guideUrl: '/guides/ebay.pdf',
    pricing: { label: 'Gratuit · 5 000 req/j', variant: 'free' },
  },
  {
    provider: 'tcgplayer',
    label: 'TCGPlayer',
    contribution: 'Prix marché US',
    guideUrl: '/guides/tcgplayer.pdf',
    pricing: { label: 'Gratuit · approbation requise', variant: 'limited' },
  },
  {
    provider: 'pricecharting',
    label: 'PriceCharting',
    contribution: 'Prix RAW · PSA/CGC · scraping actif sans abonnement',
    guideUrl: '/guides/pricecharting.pdf',
    pricing: { label: 'API ~2$/mois', variant: 'paid' },
    fallback: { label: 'Scraping public actif (sans clé API)', active: true },
    extractionModes: [
      { value: 'css',    label: 'CSS / JS (rapide · 0 GPU)' },
      { value: 'ollama', label: 'Ollama LLM (robuste · GPU requis)' },
    ],
  },
  {
    provider: 'cardmarket-scraping',
    label: 'CardMarket Scraping',
    contribution: 'Prix EUR · scraping public (actif sans config)',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    guideUrl: '/guides/cardmarket-scraping.pdf',
    pricing: { label: 'Gratuit', variant: 'free' },
  },
  {
    provider: 'pokemon-tcg',
    label: 'Pokémon TCG API',
    contribution: 'Sets · cartes · images (API publique gratuite)',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    guideUrl: '/guides/pokemon-tcg.pdf',
    pricing: { label: 'Gratuit · 1 000 req/j', variant: 'free' },
  },

  // ── Scraping public — aucune clé, aucun guide PDF ────────────────────────

  {
    provider: 'cardtrader',
    label: 'CardTrader',
    contribution: 'Prix EUR · stock live · marché européen secondaire',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    pricing: { label: 'Gratuit', variant: 'free' },
  },
  {
    provider: 'mavin',
    label: 'Mavin.io',
    contribution: 'Prix USD · agrégateur ventes eBay complétées (sans compte eBay)',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    pricing: { label: 'Gratuit', variant: 'free' },
  },
  {
    provider: '130point',
    label: '130point',
    contribution: 'Historique ventes eBay complétées · prix bas/haut/moyen',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    pricing: { label: 'Gratuit', variant: 'free' },
  },
  {
    provider: 'psa',
    label: 'PSA',
    contribution: 'Population reports · nb exemplaires gradés · gem rate PSA 10',
    fixedBadge: { text: 'Actif', variant: 'success' },
    noKeys: true,
    pricing: { label: 'Gratuit', variant: 'free' },
  },
];
