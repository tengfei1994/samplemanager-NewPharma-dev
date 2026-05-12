param(
    [string]$ProjectPath = ".\src",
    [string]$OutDir = ".\out\publish",
    [string]$PackagePath = ".\out\samplemanager-package.zip"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

if (-not (Test-Path $ProjectPath)) {
    throw "Project path not found: $ProjectPath"
}

New-Item -ItemType Directory -Force (Split-Path $PackagePath) | Out-Null
New-Item -ItemType Directory -Force $OutDir | Out-Null

$projects = @()
if ((Get-Item $ProjectPath).PSIsContainer) {
    $projects = @(Get-ChildItem -Path $ProjectPath -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName)
} else {
    $projects = @((Resolve-Path $ProjectPath).Path)
}

if ($projects.Count -eq 0) {
    throw "No project files found under: $ProjectPath"
}

foreach ($project in $projects) {
    dotnet publish $project -c Release -o $OutDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for: $project"
    }
}

Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $PackagePath -Force

Write-Host "Package created: $PackagePath"
