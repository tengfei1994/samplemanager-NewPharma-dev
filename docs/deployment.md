# Deployment

Deployments are performed over the project SSH connection:

```powershell
C:\Windows\System32\OpenSSH\ssh.exe -F ..\.codex-ssh\conversion_project.ssh_config conversion-project
```

The default remote instance root is:

```text
C:\Thermo\SampleManager\Server\VGSM
```

Deployment scripts should always backup replaced files before copying new
artifacts into the instance.
