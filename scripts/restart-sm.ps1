param(
    [string]$SshConfig = "..\.codex-ssh\conversion_project.ssh_config",
    [string]$HostAlias = "conversion-project"
)

$ErrorActionPreference = "Stop"

$remoteScript = @"
`$services = @(
  'smptqvgsm',
  'smpSTATvgsm',
  'smpvgsm',
  'smplockvgsm',
  'SMDaemonvgsm'
)

foreach (`$svc in `$services) {
  if (Get-Service `$svc -ErrorAction SilentlyContinue) {
    Restart-Service `$svc -Force
  }
}

Get-Service `$services | Select-Object Name,Status | Format-Table -AutoSize
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
& C:\Windows\System32\OpenSSH\ssh.exe -F $SshConfig $HostAlias "powershell -NoProfile -EncodedCommand $encoded"

