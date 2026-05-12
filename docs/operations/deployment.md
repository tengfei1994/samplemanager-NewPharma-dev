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

## Applying structure changes to the database

`CreateEntityDefinition.exe` generates `structure.sec` and the entity-definition
assembly, but it does not by itself create the physical database table. After a
new table has been added to `Data\structure.txt` and entity definitions have
been generated, run `convert_table` for the new table.

Stop the SampleManager VGSM services before converting the table, then restart
them after the command completes.

Example for the original inspection-request table:

```powershell
cd C:\Thermo\SampleManager\Server\VGSM\Exe
.\convert_table.exe -mode convert -tables "NPH_INSPECTION_REQUEST" -noconfirm -instance VGSM
```

Expected success output includes:

```text
Checking Table NPH_INSPECTION_REQUEST
Table not in database
Conversion Method - Create Table
Creating Table NPH_INSPECTION_REQUEST
Creating all indexes on table NPH_INSPECTION_REQUEST
Table converted successfully
```

For the lifecycle/snapshot Inspection Request model, convert the header and
snapshot tables individually. `convert_table.exe` treats a comma-separated
string as a single table name in this VGSM environment.

```powershell
cd C:\Thermo\SampleManager\Server\VGSM\Exe
.\convert_table.exe -mode convert -tables NPH_INSPECTION_REQUEST -noconfirm -instance VGSM
.\convert_table.exe -mode convert -tables NPH_IR_LP_ENTRY -noconfirm -instance VGSM
.\convert_table.exe -mode convert -tables NPH_IR_LP_FIELD -noconfirm -instance VGSM
.\convert_table.exe -mode convert -tables NPH_IR_LP_TEST -noconfirm -instance VGSM
.\convert_table.exe -mode convert -tables NPH_IR_LP_TEST_FIELD -noconfirm -instance VGSM
.\convert_table.exe -mode convert -tables NPH_IR_PRODUCT -noconfirm -instance VGSM
```

After changing form XML, remove the cached form binary before restarting
services:

```powershell
Remove-Item C:\Thermo\SampleManager\Server\VGSM\Exe\FormsBin\NPH_INSPECTION_REQUEST.binform -Force
```
