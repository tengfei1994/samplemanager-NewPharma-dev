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

## Loading table CSV configuration

SampleManager table-loader CSV files are loaded through the built-in VGL report
`$table_loader`. `EntityImportTask` is for XML entity imports and does not load
these CSV files.

Example:

```powershell
cd C:\Thermo\SampleManager\Server\VGSM\Exe
.\SampleManagerCommand.exe -instance VGSM -username BATCH -task VGL -report '$table_loader' -prompts "(C:\path\to\file.csv,overwrite_table)"
```

Current NewPharma menu configuration files:

- `samplemanager/config/master_menu.csv`
- `samplemanager/config/role_entry.csv`
- `samplemanager/config/menu_toolbar.csv`
