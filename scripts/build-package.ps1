param(
    [string]$ProjectPath = ".\src",
    [string]$OutDir = ".\out\publish",
    [string]$PackagePath = ".\out\samplemanager-package.zip"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ProjectPath)) {
    throw "Project path not found: $ProjectPath"
}

New-Item -ItemType Directory -Force (Split-Path $PackagePath) | Out-Null

dotnet publish $ProjectPath -c Release -o $OutDir
Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $PackagePath -Force

Write-Host "Package created: $PackagePath"
