#!/usr/bin/env node
/**
 * Export-maui.js
 *
 * Génère src/mobile/Pokekool.Mobile/Resources/Styles/Tokens.xaml à partir de tokens.json.
 * Zéro dépendance NPM.
 *
 * Convention de nommage XAML : PascalCase, préfixe sémantique
 *   ex : color.brand.primary      → ColorBrandPrimary
 *        spacing.4                 → Space4
 *        typography.font-size.md   → FontSizeMd
 *
 * Types XAML produits :
 *   - Color  : pour les couleurs (conversion hex / rgba → #AARRGGBB)
 *   - x:Double : pour les tailles (extraction des unités px / ms)
 *   - x:String : pour les font-families et autres
 *   - TimeSpan : pour les durations
 *   - x:Int32  : pour les z-index et font-weights
 *
 * Usage :
 *   node design-system/export-maui.js
 *   ou
 *   npm run tokens:maui
 */

'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..');
const TOKENS_PATH = path.join(__dirname, 'tokens.json');
const OUTPUT_PATH = path.join(ROOT, 'src/mobile/Pokekool.Mobile/Resources/Styles/Tokens.xaml');

// =====================================================
// Mapping des préfixes XAML par section (PascalCase)
// =====================================================
const PREFIX_MAP = {
  color: 'Color',
  typography: '', // les sous-clés sont self-explanatory
  spacing: 'Space',
  radius: 'Radius',
  shadow: 'Shadow',
  motion: '',
  'z-index': 'Z',
  breakpoint: 'Breakpoint',
};

// =====================================================
// Helpers
// =====================================================
function loadTokens() {
  const raw = fs.readFileSync(TOKENS_PATH, 'utf8');
  const json = JSON.parse(raw);
  return Object.fromEntries(
    Object.entries(json).filter(([k]) => !k.startsWith('$'))
  );
}

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

function pascalCase(s) {
  return String(s)
    .replace(/[-_]+/g, ' ')
    .replace(/(?:^|\s)(\w)/g, (_, c) => c.toUpperCase())
    .replace(/\s+/g, '');
}

function buildKeyName(category, pathSegments) {
  const mapped = PREFIX_MAP[category];
  const parts = mapped === undefined
    ? [category, ...pathSegments]
    : mapped === ''
      ? pathSegments
      : [mapped, ...pathSegments];
  return parts.map(pascalCase).join('');
}

// =====================================================
// Convertit une couleur (#RGB, #RRGGBB, rgba(...)) en #AARRGGBB
// =====================================================
function normalizeColor(value) {
  if (typeof value !== 'string') return null;
  const v = value.trim().toLowerCase();

  // rgba(r, g, b, a)
  const rgba = v.match(/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+)\s*)?\)$/);
  if (rgba) {
    const r = parseInt(rgba[1], 10);
    const g = parseInt(rgba[2], 10);
    const b = parseInt(rgba[3], 10);
    const a = rgba[4] !== undefined ? Math.round(parseFloat(rgba[4]) * 255) : 255;
    return `#${[a, r, g, b].map(n => n.toString(16).padStart(2, '0').toUpperCase()).join('')}`;
  }

  // #RGB
  let m = v.match(/^#([0-9a-f])([0-9a-f])([0-9a-f])$/);
  if (m) {
    return `#FF${m[1]}${m[1]}${m[2]}${m[2]}${m[3]}${m[3]}`.toUpperCase();
  }
  // #RRGGBB
  m = v.match(/^#([0-9a-f]{6})$/);
  if (m) {
    return `#FF${m[1]}`.toUpperCase();
  }
  // #RRGGBBAA → #AARRGGBB (XAML order)
  m = v.match(/^#([0-9a-f]{6})([0-9a-f]{2})$/);
  if (m) {
    return `#${m[2]}${m[1]}`.toUpperCase();
  }
  // #AARRGGBB déjà au bon format
  m = v.match(/^#[0-9a-f]{8}$/);
  if (m) {
    return v.toUpperCase();
  }
  return null;
}

// Extrait un nombre depuis "16px", "250ms", "0.04em", ou "1.5".
function extractNumber(value) {
  if (typeof value === 'number') return value;
  const m = String(value).trim().match(/^(-?\d+(?:\.\d+)?)(px|ms|em|rem|%)?$/);
  return m ? parseFloat(m[1]) : null;
}

// Catégories qui sont des nombres "purs" exposés en x:Double
const NUMERIC_CATEGORIES = new Set(['spacing', 'radius', 'breakpoint', 'z-index']);
const NUMERIC_SUBPATHS = new Set([
  'typography.font-size',
  'typography.line-height',
  'typography.letter-spacing',
]);

// Catégories spécifiques
function isNumericToken(category, segments) {
  if (NUMERIC_CATEGORIES.has(category)) return true;
  const joined = [category, ...segments].join('.');
  for (const prefix of NUMERIC_SUBPATHS) {
    if (joined.startsWith(prefix + '.') || joined === prefix) return true;
  }
  return false;
}

// =====================================================
// Génération du XAML
// =====================================================
function generateXaml(tokens) {
  const lines = [];
  lines.push('<?xml version="1.0" encoding="UTF-8"?>');
  lines.push('<!--');
  lines.push('  GÉNÉRÉ AUTOMATIQUEMENT - NE PAS MODIFIER À LA MAIN');
  lines.push('');
  lines.push('  Source : design-system/tokens.json');
  lines.push('  Script : design-system/export-maui.js');
  lines.push('  Pour régénérer : npm run tokens:build');
  lines.push(`  Date    : ${new Date().toISOString()}`);
  lines.push('-->');
  lines.push('<ResourceDictionary');
  lines.push('    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"');
  lines.push('    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"');
  lines.push('    xmlns:sys="clr-namespace:System;assembly=netstandard">');

  const skipped = [];

  for (const [category, value] of Object.entries(tokens)) {
    lines.push('');
    lines.push(`    <!-- ===== ${category.toUpperCase()} ===== -->`);
    const flat = flatten(value);

    for (const [segments, raw] of flat) {
      const key = buildKeyName(category, segments);

      // Couleurs
      if (category === 'color') {
        const norm = normalizeColor(raw);
        if (norm) {
          lines.push(`    <Color x:Key="${key}">${norm}</Color>`);
        } else {
          skipped.push(`${key} (couleur non convertible : ${raw})`);
          lines.push(`    <!-- ${key} skipped : couleur non convertible (${raw}) -->`);
        }
        continue;
      }

      // Durations → TimeSpan
      if (category === 'motion' && segments[0] === 'duration') {
        const ms = extractNumber(raw);
        if (ms !== null) {
          // Format TimeSpan : HH:MM:SS.fffffff
          const totalSeconds = ms / 1000;
          const hours = Math.floor(totalSeconds / 3600);
          const minutes = Math.floor((totalSeconds % 3600) / 60);
          const seconds = totalSeconds - hours * 3600 - minutes * 60;
          const ts = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${seconds.toFixed(7).padStart(9, '0')}`;
          lines.push(`    <sys:TimeSpan x:Key="${key}">${ts}</sys:TimeSpan>`);
        } else {
          skipped.push(`${key} (durée non convertible : ${raw})`);
        }
        continue;
      }

      // Easings : MAUI utilise des Easing nommés, pas de cubic-bezier portable
      if (category === 'motion' && segments[0] === 'easing') {
        lines.push(`    <!-- ${key} : easings MAUI à mapper manuellement (Easing.CubicInOut...) -->`);
        skipped.push(`${key} (easing non portable)`);
        continue;
      }

      // Font weights : x:Int32
      if (category === 'typography' && segments[0] === 'font-weight') {
        const n = extractNumber(raw);
        if (n !== null) {
          lines.push(`    <x:Int32 x:Key="${key}">${n}</x:Int32>`);
        }
        continue;
      }

      // Font families : x:String
      if (category === 'typography' && segments[0] === 'font-family') {
        // En MAUI on garde uniquement le premier nom de la stack (les fallbacks ne s'appliquent pas).
        const first = String(raw).split(',')[0].trim().replace(/^['"]|['"]$/g, '');
        lines.push(`    <x:String x:Key="${key}">${first}</x:String>`);
        continue;
      }

      // Shadows : non portables tel quel, on garde une chaîne descriptive en commentaire
      if (category === 'shadow') {
        lines.push(`    <!-- ${key} : utiliser <Shadow Brush="..." Offset="..." Radius="..."/> à la main -->`);
        skipped.push(`${key} (shadow CSS non portable)`);
        continue;
      }

      // Numériques (spacing, radius, font-size, line-height, breakpoint, z-index)
      if (isNumericToken(category, segments)) {
        const n = extractNumber(raw);
        if (n !== null) {
          if (category === 'z-index') {
            lines.push(`    <x:Int32 x:Key="${key}">${n}</x:Int32>`);
          } else {
            lines.push(`    <x:Double x:Key="${key}">${n}</x:Double>`);
          }
        } else {
          skipped.push(`${key} (numérique non extractible : ${raw})`);
        }
        continue;
      }

      // Fallback : x:String
      lines.push(`    <x:String x:Key="${key}">${raw}</x:String>`);
    }
  }

  lines.push('');
  lines.push('</ResourceDictionary>');
  lines.push('');

  return { xaml: lines.join('\n'), skipped };
}

// =====================================================
// Entry point
// =====================================================
function main() {
  try {
    const tokens = loadTokens();
    const { xaml, skipped } = generateXaml(tokens);

    const outDir = path.dirname(OUTPUT_PATH);
    if (!fs.existsSync(outDir)) {
      fs.mkdirSync(outDir, { recursive: true });
    }
    fs.writeFileSync(OUTPUT_PATH, xaml, 'utf8');

    const exported = xaml.split('\n').filter(l => /<(Color|x:Double|x:String|x:Int32|sys:TimeSpan)/.test(l)).length;
    console.log(`[export-maui] OK - ${exported} tokens écrits dans ${path.relative(ROOT, OUTPUT_PATH)}`);
    if (skipped.length > 0) {
      console.log(`[export-maui] ${skipped.length} tokens non portés (commentés en XAML) :`);
      for (const s of skipped) console.log(`              - ${s}`);
    }
  } catch (err) {
    console.error('[export-maui] ECHEC :', err.message);
    process.exitCode = 1;
  }
}

main();
