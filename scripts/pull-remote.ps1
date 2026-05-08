param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project",
    [string]$RemoteRoot = "C:\Thermo\SampleManager\Server\VGSM",
    [string]$LocalOut = ".\remote-snapshot"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force $LocalOut | Out-Null

$remoteScript = @"
Get-ChildItem -Path '$RemoteRoot\Solution' -Directory -ErrorAction SilentlyContinue |
  Select-Object Name,FullName,LastWriteTime |
  ConvertTo-Json -Compress
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -EncodedCommand $encoded" |
  Set-Content -Path (Join-Path $LocalOut "solution-folders.json") -Encoding UTF8
