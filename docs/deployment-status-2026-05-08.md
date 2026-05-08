# Deployment Status - 2026-05-08

## VGSM prework completed

- Remote SDK prepared: portable .NET SDK 8.0.408 under `C:\Users\administrator\Desktop\codex-tools\dotnet8-sdk`.
- Custom assembly compiled successfully with zero warnings and zero errors.
- Assembly deployed to `C:\Thermo\SampleManager\Server\VGSM\Exe\SolutionAssemblies\NewPharma.InspectionRequest.dll`.
- PDB deployed beside the DLL for troubleshooting.
- Standard LabTable task entry `NewPharmaInspectionRequestTask` was added and deployed for `NPH_INSPECTION_REQUEST`.
- Workflow execution task entry `NewPharmaInspectionRequestExecutionTask` remains available for approved request execution.
- New table structure appended to `C:\Thermo\SampleManager\Server\VGSM\Data\structure.txt`.
- Structure backup created under `C:\Thermo\SampleManager\Server\VGSM\Data\codex-backup`.
- `CreateEntityDefinition.exe` completed `SchemaBuildTask` successfully as `BATCH`.
- Table Loader loaded `MASTER_MENU`, `ROLE_ENTRY`, and `MENU_TOOLBAR` configuration from `C:\Users\administrator\Desktop\codex-vgsm-export\NewPharma\menu-import`.
- `SYSTEM` and `SIMPLE` menus now contain `NewPharma -> Inspection Requests...`, pointing to master menu procedure `199401`.
- Deployment package created at `C:\Users\administrator\Desktop\codex-vgsm-export\NewPharma\NewPharma.InspectionRequest-package.zip`.

## Runtime behavior in the DLL

- The task entry point is `NewPharmaInspectionRequestExecutionTask`.
- The maintenance task entry point is `NewPharmaInspectionRequestTask`.
- It validates that the request is approved and has not already executed.
- It resolves the configured Login Plan by latest active version or explicit version.
- It supports root contexts `LOT_DETAILS`, `LOT`, `JOB_HEADER`, and `JOB`.
- For lot context it calls `ExtendedLoginPlan.JobCreationProcess`.
- For job context it calls `ExtendedLoginPlan.SampleCreationProcess`.
- Generated job and sample identities are serialized back onto the request record.

## Remaining in-system configuration

- Phrase type `NPH_IR_STA` has been created in VGSM for request lifecycle states.
- Phrase type `NPH_IR_EXE` has been created in VGSM for execution states.
- SampleManager identity fields are 10 characters, so long design names such as `NPH_IR_STATUS` and `NPH_IR_EXEC_STATUS` are represented by these 10-character identifiers in runtime configuration.
- Confirm the `NewPharma -> Inspection Requests...` menu entry in the SampleManager client after refreshing or relogging.
- Configure the e-signature requirement per site policy before enabling production use.
- CSV configuration files are tracked under `samplemanager/config`. Use the SampleManager `$table_loader` VGL report for CSV loading; `EntityImportTask` expects XML and rejects table-loader CSV files.

## Notes

No vendor Pharma Solution or LoginPlan source code is stored in this repository. Only NewPharma custom source, manifests, and design records are tracked.
