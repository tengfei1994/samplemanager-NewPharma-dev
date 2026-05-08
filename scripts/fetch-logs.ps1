param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project",
    [string]$RemoteLogDir = "C:\Thermo\SampleManager\Server\VGSM\Logfile",
    [string]$LocalLogDir = ".\remote-logs"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force $LocalLogDir | Out-Null

$remoteScript = @"
Get-ChildItem -Path '$RemoteLogDir' -File -ErrorAction SilentlyContinue |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 10 Name,FullName,Length,LastWriteTime |
  ConvertTo-Json -Compress
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -EncodedCommand $encoded" |
  Set-Content -Path (Join-Path $LocalLogDir "latest-log-index.json") -Encoding UTF8
