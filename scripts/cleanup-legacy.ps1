<#
  cleanup-legacy.ps1
  Nettoyage des vestiges du projet resto précédent + consolidation des .blend.
  À lancer depuis la RACINE du repo :  ./scripts/cleanup-legacy.ps1
  (les suppressions backend nécessitent ENSUITE une migration EF, voir la fin.)
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot   # racine du repo (parent de /scripts)
Set-Location $root
Write-Host "Racine repo : $root" -ForegroundColor Cyan

function Remove-IfExists([string]$path) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
        Write-Host "  supprimé : $path" -ForegroundColor Yellow
    } else {
        Write-Host "  (absent)  : $path" -ForegroundColor DarkGray
    }
}

# ── 1. Front : assets resto morts (non référencés) ────────────────────────────
Write-Host "`n[1] Front — assets floor-plans (resto)" -ForegroundColor Green
Remove-IfExists "src/web/poke-web/src/assets/floor-plans"

# ── 2. Backend : entités + configs resto (FloorPlan / RestaurantTable / …) ────
#     Non référencées par le code métier ET absentes du modèle EF actif
#     (aucune table en base, absentes du snapshot) → suppression sans migration.
Write-Host "`n[2] Backend — entités resto (aucune migration nécessaire)" -ForegroundColor Green
Remove-IfExists "src/backend/Pokekool.Domain/Entities/Tables"
Remove-IfExists "src/backend/Pokekool.Infrastructure/Persistence/Configurations/Tables"

# ── 3. Consolidation des .blend dans assets/blender-source/ ───────────────────
Write-Host "`n[3] Consolidation des .blend -> assets/blender-source/" -ForegroundColor Green
$dest = "assets/blender-source"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$roomTemplate = "src/web/poke-web/public/assets/3d/storage/room_template.blend"
if (Test-Path $roomTemplate) {
    Move-Item -Force $roomTemplate $dest
    Write-Host "  déplacé : room_template.blend -> $dest/" -ForegroundColor Yellow
} else {
    Write-Host "  (absent) : $roomTemplate" -ForegroundColor DarkGray
}
Write-Host "  .blend regroupés :" -ForegroundColor Cyan
Get-ChildItem -Path $dest -Filter *.blend | ForEach-Object { Write-Host "    - $($_.Name)" }

# ── 4. Fin ────────────────────────────────────────────────────────────────────
#     Aucune migration : les entités resto n'étaient pas dans le modèle EF actif
#     (pas de table en base, absentes du snapshot). Un simple rebuild suffit.
Write-Host "`nTerminé. Aucune migration nécessaire — rebuild/restart de l'API si besoin." -ForegroundColor Green
