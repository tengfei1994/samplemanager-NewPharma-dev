param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project",
    [string]$PackagePath = ".\out\samplemanager-package.zip",
    [string]$RemoteDrop = "C:\Users\administrator\Desktop\samplemanager-deploy",
    [string]$RemoteTarget = "C:\Thermo\SampleManager\Server\VGSM\Exe"
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
New-Item -ItemType Directory -Force `$backup | Out-Null
New-Item -ItemType Directory -Force '$RemoteTarget' | Out-Null
Expand-Archive -Path (Join-Path '$RemoteDrop' '$packageName') -DestinationPath (Join-Path '$RemoteDrop' 'expanded') -Force
Get-ChildItem -Path (Join-Path '$RemoteDrop' 'expanded') -File | ForEach-Object {
    `$targetFile = Join-Path '$RemoteTarget' `$_.Name
    if (Test-Path `$targetFile) {
        Copy-Item `$targetFile `$backup -Force
    }
    Copy-Item `$_.FullName '$RemoteTarget' -Force
}
Write-Host "Deployed package to $RemoteTarget"
Write-Host "Backup: `$backup"
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -EncodedCommand $encoded"

