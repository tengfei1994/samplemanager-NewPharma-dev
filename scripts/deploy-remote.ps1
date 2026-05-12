param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project",
    [string]$PackagePath = ".\out\samplemanager-package.zip",
    [string]$RemoteDrop = "C:\Users\administrator\Desktop\samplemanager-deploy",
    [string]$RemoteTarget = "C:\Thermo\SampleManager\Server\VGSM\Exe",
    [int]$KeepBackups = 1
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PackagePath)) {
    throw "Package not found: $PackagePath"
}

$packageName = Split-Path $PackagePath -Leaf

& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -Command `"New-Item -ItemType Directory -Force '$RemoteDrop' | Out-Null`""
& C:\Windows\System32\OpenSSH\scp.exe -F $SshConfig $PackagePath "${HostAlias}:$RemoteDrop/$packageName"

$remoteScript = @"
`$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
`$backup = Join-Path '$RemoteDrop' "backup-`$stamp"
`$expanded = Join-Path '$RemoteDrop' 'expanded'
New-Item -ItemType Directory -Force `$backup | Out-Null
New-Item -ItemType Directory -Force '$RemoteTarget' | Out-Null
if (Test-Path `$expanded) {
    Remove-Item -LiteralPath `$expanded -Recurse -Force
}
Expand-Archive -Path (Join-Path '$RemoteDrop' '$packageName') -DestinationPath `$expanded -Force
Get-ChildItem -Path `$expanded -File | ForEach-Object {
    `$targetFile = Join-Path '$RemoteTarget' `$_.Name
    if (Test-Path `$targetFile) {
        Copy-Item `$targetFile `$backup -Force
    }
    Copy-Item `$_.FullName '$RemoteTarget' -Force

    if (`$_.Extension -in @('.nphir', '.xml')) {
        `$remoteTargetPath = '$RemoteTarget'
        if ((Split-Path `$remoteTargetPath -Leaf) -ieq 'Forms') {
            `$formsBin = Join-Path (Split-Path `$remoteTargetPath -Parent) 'FormsBin'
        } else {
            `$formsBin = Join-Path `$remoteTargetPath 'FormsBin'
        }

        if (Test-Path `$formsBin) {
            `$cacheName = [System.IO.Path]::GetFileNameWithoutExtension(`$_.Name) + '.binform*'
            Get-ChildItem `$formsBin -File -Filter `$cacheName -ErrorAction SilentlyContinue |
                Remove-Item -Force
        }
    }
}
Remove-Item -LiteralPath `$expanded -Recurse -Force
Remove-Item -LiteralPath (Join-Path '$RemoteDrop' '$packageName') -Force
Get-ChildItem '$RemoteDrop' -Directory -Filter 'backup-*' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip $KeepBackups |
    Remove-Item -Recurse -Force
Write-Host "Deployed package to $RemoteTarget"
Write-Host "Backup: `$backup"
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -EncodedCommand $encoded"
