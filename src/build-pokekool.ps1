# Pokekool backend build script
$sln = Join-Path $PSScriptRoot "backend\pokekool.sln"
Write-Host "Building Pokekool backend..." -ForegroundColor Cyan
dotnet build $sln --no-incremental -v minimal 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCEEDED" -ForegroundColor Green
} else {
    Write-Host "`nBUILD FAILED" -ForegroundColor Red
}
