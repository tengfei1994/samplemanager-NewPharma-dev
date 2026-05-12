param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project",
    [int]$KeepLatestBackup = 1
)

$ErrorActionPreference = "Stop"

$remoteScript = @"
`$ErrorActionPreference = 'Stop'
`$desktop = 'C:\Users\administrator\Desktop'
`$desktopPath = (Resolve-Path `$desktop).Path

`$directoryPatterns = @(
    'nphir-build-*',
    'newpharma-ir-build-*',
    'newpharma-ir-deploy-*',
    'newpharma-ir-sql-*',
    'newpharma-ir-runtimefix-build',
    'codex-ir-workflow-login',
    'samplemanager-deploy',
    'out'
)

`$filePatterns = @(
    'nphir-*.sql',
    'nphir-*.zip',
    'newpharma-ir-*.sql',
    'newpharma-ir-*.zip',
    'inspection_request_workflow_login.sql',
    'NPH_INSPECTION_REQUEST.xml'
)

`$remove = New-Object System.Collections.Generic.List[System.IO.FileSystemInfo]
foreach (`$pattern in `$directoryPatterns) {
    Get-ChildItem `$desktop -Directory -Filter `$pattern -ErrorAction SilentlyContinue |
        ForEach-Object { `$remove.Add(`$_) }
}

foreach (`$pattern in `$filePatterns) {
    Get-ChildItem `$desktop -File -Filter `$pattern -ErrorAction SilentlyContinue |
        ForEach-Object { `$remove.Add(`$_) }
}

Get-ChildItem `$desktop -Directory -Filter 'nphir-deploy-backup-*' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip $KeepLatestBackup |
    ForEach-Object { `$remove.Add(`$_) }

`$deleted = @()
foreach (`$item in `$remove | Sort-Object FullName -Unique) {
    `$resolved = (Resolve-Path -LiteralPath `$item.FullName).Path
    if (-not `$resolved.StartsWith(`$desktopPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove outside Desktop: `$resolved"
    }

    Remove-Item -LiteralPath `$resolved -Recurse -Force
    `$deleted += `$resolved
}

Write-Host "Deleted items: `$(`$deleted.Count)"
`$deleted | ForEach-Object { Write-Host `$_ }
Write-Host "Kept latest deployment backups:"
Get-ChildItem `$desktop -Directory -Filter 'nphir-deploy-backup-*' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object Name,LastWriteTime |
    Format-Table -AutoSize
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded"
