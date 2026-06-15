#!/usr/bin/env node
/**
 * Watch.js
 *
 * Surveille tokens.json et relance les deux exports à chaque modification.
 * Usage : npm run tokens:watch
 */

'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');

const TOKENS_PATH = path.join(__dirname, 'tokens.json');
let isBuilding = false;
let pendingBuild = false;

function build() {
  if (isBuilding) {
    pendingBuild = true;
    return;
  }
  isBuilding = true;
  console.log(`[watch] tokens.json modifié - rebuild...`);

  const angular = spawn('node', [path.join(__dirname, 'export-angular.js')], { stdio: 'inherit' });
  angular.on('exit', () => {
    const maui = spawn('node', [path.join(__dirname, 'export-maui.js')], { stdio: 'inherit' });
    maui.on('exit', () => {
      isBuilding = false;
      if (pendingBuild) {
        pendingBuild = false;
        build();
      }
    });
  });
}

console.log(`[watch] Surveillance de ${path.relative(process.cwd(), TOKENS_PATH)}`);
console.log('[watch] Ctrl+C pour arrêter.');

build();

let debounce;
fs.watch(TOKENS_PATH, () => {
  clearTimeout(debounce);
  debounce = setTimeout(build, 200);
});
