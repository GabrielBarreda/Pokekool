import { CardGradingData, GradingCompany, GradingCompanyId } from './market.models';

// ── Sociétés de gradation ─────────────────────────────────────────────────────

export const GRADING_COMPANIES: GradingCompany[] = [
  { id: 'PSA',  name: 'PSA',     color: '#3B82F6', currency: 'USD' },
  { id: 'BGS',  name: 'Beckett', color: '#8B5CF6', currency: 'USD' },
  { id: 'CGC',  name: 'CGC',     color: '#F59E0B', currency: 'USD' },
  { id: 'SGC',  name: 'SGC',     color: '#10B981', currency: 'USD' },
  { id: 'AGS',  name: 'AGS',     color: '#EF4444', currency: 'USD' },
  { id: 'CSG',  name: 'CSG',     color: '#6366F1', currency: 'USD' },
];

// ── Multiplicateurs de prix par grade ─────────────────────────────────────────
// Grade 10 = référence × le plus élevé, grade 1 = fraction

const GRADE_MULTIPLIERS: Record<number, number> = {
  10: 1.0,
  9:  0.38,
  8:  0.22,
  7:  0.13,
  6:  0.09,
  5:  0.07,
  4:  0.055,
  3:  0.04,
  2:  0.03,
  1:  0.02,
};

const COMPANY_COEFFS: Record<GradingCompanyId, number> = {
  PSA: 1.0,
  BGS: 0.92,
  CGC: 0.78,
  SGC: 0.65,
  AGS: 0.55,
  CSG: 0.48,
};

const COMPANY_POP_COEFF: Record<GradingCompanyId, number> = {
  PSA: 1.0,
  BGS: 0.60,
  CGC: 0.35,
  SGC: 0.25,
  AGS: 0.15,
  CSG: 0.12,
};

function seed(cardId: string): number {
  let h = 0;
  for (const c of cardId) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  return h;
}

/**
 * Génère des données de gradation réalistes pour une carte.
 * TODO : remplacer par des appels réels aux APIs PSA/BGS/CGC/SGC/AGS/CSG.
 */
export function mockGradingData(cardId: string): CardGradingData[] {
  const rng = seed(cardId);
  // Prix base aléatoire entre 30$ et 500$ pour le PSA 10
  const basePrice = 30 + (rng % 470);
  const basePop = 500 + (rng % 4500);

  return GRADING_COMPANIES.map(company => ({
    cardId,
    company: company.id,
    lastUpdated: new Date(),
    entries: [10, 9, 8, 7, 6, 5, 4, 3, 2, 1].map(grade => {
      const market = +(basePrice * GRADE_MULTIPLIERS[grade] * COMPANY_COEFFS[company.id]).toFixed(2);
      const pop = Math.round(basePop * GRADE_MULTIPLIERS[grade] * COMPANY_POP_COEFF[company.id]);
      return {
        grade,
        label: String(grade),
        marketPrice: market,
        lowPrice: +(market * 0.82).toFixed(2),
        population: pop,
      };
    }),
  }));
}
