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
- `convert_table.exe -mode convert -tables "NPH_INSPECTION_REQUEST" -noconfirm -instance VGSM` completed successfully after VGSM services were stopped.
- Physical database table `NPH_INSPECTION_REQUEST` and its indexes were created.
- Backend verification with `SampleManagerCommand.exe -task EntityExport -entitytype NPH_INSPECTION_REQUEST` completed successfully as `BATCH`.
- Table Loader loaded `MASTER_MENU`, `ROLE_ENTRY`, and `MENU_TOOLBAR` configuration from `C:\Users\administrator\Desktop\codex-vgsm-export\NewPharma\menu-import`.
- `SYSTEM` and `SIMPLE` menus now contain `NewPharma -> Inspection Requests...`, pointing to master menu procedure `199401`.
- Procedure `199401` follows the standard SampleManager `BrowseTask` pattern: `TABLE_NAME`, `DATA_TYPE`, `ACTION_TYPE`, and `WINDOW_STYLE` are blank; `PARAMETERS` and `TASK_PARAMETERS` carry `NPH_INSPECTION_REQUEST`.
- Procedures `199402` through `199406` use the standard LabTable dispatcher `$LABTABLE_DB` / `LABTABLE_OPTION` for Add, Modify, Display, Remove, and Restore right-click actions.
- Deployment package created at `C:\Users\administrator\Desktop\codex-vgsm-export\NewPharma\NewPharma.InspectionRequest-package.zip`.

## Lifecycle/snapshot update completed

- .NET SDK 8.0.420 was installed on the remote server and verified with
  `dotnet --list-sdks`.
- `NPH_INSPECTION_REQUEST` was extended with lifecycle workflow fields:
  `LIFECYCLE_WORKFLOW_ID`, `LIFECYCLE_WORKFLOW_VERSION`,
  `LIFECYCLE_NODE_ID`, and `LIFECYCLE_EVENT`.
- Snapshot tables were added and physically created:
  `NPH_IR_LP_ENTRY`, `NPH_IR_LP_FIELD`, `NPH_IR_LP_TEST`,
  `NPH_IR_LP_TEST_FIELD`, and `NPH_IR_PRODUCT`.
- `CreateEntityDefinition.exe` completed successfully after the structure
  block was updated.
- `convert_table.exe` completed successfully for the header and each snapshot
  table. Tables were converted one at a time because comma-separated values are
  interpreted as one table name in this environment.
- `InspectionRequestSnapshotService` was added to copy Login Plan Data
  Assignment, Test Assignment, and Product Spec records into IR-owned snapshot
  tables.
- `InspectionRequestLifecycleService` was added to initialize default entity
  template/workflow metadata and maintain `WORKFLOW_LINK` for the current IR.
- `NPH_INSPECTION_REQUEST` now includes `ENTITY_TEMPLATE_ID` and
  `ENTITY_TEMPLATE_VERSION` header fields.
- `NPH_INSPECTION_REQUEST.xml` now contains `Data Assignment` and `Product Spec`
  pages. The pages use explicit Form Designer data collection components to
  avoid null grid data sources.
- The old `NPH_INSPECTION_REQUEST.binform` cache was removed after form changes
  so the client can recompile from XML.

## Runtime behavior in the DLL

- The task entry point is `NewPharmaInspectionRequestExecutionTask`.
- The maintenance task entry point is `NewPharmaInspectionRequestTask`.
- The snapshot service runs during request save for new requests or when Login
  Plan selection changes, and only creates snapshot rows when none exist yet.
- It validates that the request is approved and has not already executed.
- It resolves the configured Login Plan by latest active version or explicit version.
- It supports root contexts `LOT_DETAILS`, `LOT`, `JOB_HEADER`, and `JOB`.
- For lot context it calls `ExtendedLoginPlan.JobCreationProcess`.
- For job context it calls `ExtendedLoginPlan.SampleCreationProcess`.
- Generated job and sample identities are serialized back onto the request record.

## Runtime configuration

- Phrase type `NPH_IR_STA` has been created in VGSM for request lifecycle states.
- Phrase type `NPH_IR_EXE` has been created in VGSM for execution states.
- SampleManager identity fields are 10 characters, so long design names such as `NPH_IR_STATUS` and `NPH_IR_EXEC_STATUS` are represented by these 10-character identifiers in runtime configuration.
- Configure the e-signature requirement per site policy before enabling production use.
- CSV configuration files are tracked under `samplemanager/config`. Use the SampleManager `$table_loader` VGL report for CSV loading; `EntityImportTask` expects XML and rejects table-loader CSV files.

## User validation

- Confirm the `NewPharma -> Inspection Requests...` menu entry in the SampleManager client after refreshing or relogging.
- The previous `Invalid object name 'NPH_INSPECTION_REQUEST'` error was caused by the physical database table not yet being created; this was resolved by the `convert_table` step above.

## Notes

No vendor Pharma Solution or LoginPlan source code is stored in this repository. Only NewPharma custom source, manifests, and design records are tracked.
