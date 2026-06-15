#!/usr/bin/env node
/**
 * Export-angular.js
 *
 * Génère src/web/pokekool-web/src/styles/tokens.css à partir de tokens.json.
 * Zéro dépendance NPM — utilise uniquement les modules natifs Node.
 *
 * Convention de nommage CSS : --{category}-{path...}
 *   ex : color.brand.primary       → --color-brand-primary
 *        spacing.4                  → --space-4
 *        typography.font-size.md    → --font-size-md
 *
 * Usage :
 *   node design-system/export-angular.js
 *   ou
 *   npm run tokens:angular
 */

'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..');
const TOKENS_PATH = path.join(__dirname, 'tokens.json');
const OUTPUT_PATH = path.join(ROOT, 'src/web/pokekool-web/src/styles/tokens.css');

// =====================================================
// Mapping des préfixes CSS par section
// =====================================================
// Garde des noms compacts et idiomatiques (Tailwind-like).
const PREFIX_MAP = {
  color: 'color',
  typography: '', // les sous-clés sont déjà self-explanatory (font-size, font-weight…)
  spacing: 'space',
  radius: 'radius',
  shadow: 'shadow',
  motion: '', // duration-*, easing-*
  'z-index': 'z',
  breakpoint: 'breakpoint',
};

// =====================================================
// Lecture
// =====================================================
function loadTokens() {
  const raw = fs.readFileSync(TOKENS_PATH, 'utf8');
  const json = JSON.parse(raw);
  // Filtre les méta-clés ($schema, $meta, ...).
  return Object.fromEntries(
    Object.entries(json).filter(([k]) => !k.startsWith('$'))
  );
}

// =====================================================
// Aplatissement récursif d'un objet en paires (chemin, valeur)
// =====================================================
function flatten(obj, prefix = []) {
  const out = [];
  for (const [key, value] of Object.entries(obj)) {
    const next = [...prefix, key];
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      out.push(...flatten(value, next));
    } else {
      out.push([next, value]);
    }
  }
  return out;
}

// =====================================================
// Construction du nom de variable CSS
// =====================================================
function buildVarName(category, pathSegments) {
  const mapped = PREFIX_MAP[category];
  const parts = mapped === undefined
    ? [category, ...pathSegments]
    : mapped === ''
      ? pathSegments
      : [mapped, ...pathSegments];
  // toLowerCase + safe pour CSS (chiffres OK en suffixe d'identifiant)
  return '--' + parts.join('-').toLowerCase();
}

// =====================================================
// Génération du CSS
// =====================================================
function generateCss(tokens) {
  const lines = [];
  lines.push('/*');
  lines.push(' * GÉNÉRÉ AUTOMATIQUEMENT - NE PAS MODIFIER À LA MAIN');
  lines.push(' *');
  lines.push(' * Source : design-system/tokens.json');
  lines.push(' * Script : design-system/export-angular.js');
  lines.push(' * Pour régénérer : npm run tokens:build');
  lines.push(` * Date    : ${new Date().toISOString()}`);
  lines.push(' */');
  lines.push('');
  lines.push(':root {');

  for (const [category, value] of Object.entries(tokens)) {
    lines.push('');
    lines.push(`  /* ===== ${category.toUpperCase()} ===== */`);
    const flat = flatten(value);
    for (const [segments, val] of flat) {
      const name = buildVarName(category, segments);
      lines.push(`  ${name}: ${val};`);
    }
  }

  lines.push('}');
  lines.push('');
  return lines.join('\n');
}

// =====================================================
// Écriture
// =====================================================
function writeOutput(css) {
  const outDir = path.dirname(OUTPUT_PATH);
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true });
  }
  fs.writeFileSync(OUTPUT_PATH, css, 'utf8');
}

// =====================================================
// Entry point
// =====================================================
function main() {
  try {
    const tokens = loadTokens();
    const css = generateCss(tokens);
    writeOutput(css);
    const tokenCount = css.split('\n').filter(l => l.trim().startsWith('--')).length;
    console.log(`[export-angular] OK - ${tokenCount} tokens écrits dans ${path.relative(ROOT, OUTPUT_PATH)}`);
  } catch (err) {
    console.error('[export-angular] ECHEC :', err.message);
    process.exitCode = 1;
  }
}

main();
