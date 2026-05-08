# Deployment Status - 2026-05-08

## VGSM prework completed

- Remote SDK prepared: portable .NET SDK 8.0.408 under `C:\Users\administrator\Desktop\codex-tools\dotnet8-sdk`.
- Custom assembly compiled successfully with zero warnings and zero errors.
- Assembly deployed to `C:\Thermo\SampleManager\Server\VGSM\Exe\SolutionAssemblies\NewPharma.InspectionRequest.dll`.
- PDB deployed beside the DLL for troubleshooting.
- New table structure appended to `C:\Thermo\SampleManager\Server\VGSM\Data\structure.txt`.
- Structure backup created under `C:\Thermo\SampleManager\Server\VGSM\Data\codex-backup`.
- `CreateEntityDefinition.exe` completed `SchemaBuildTask` successfully as `BATCH`.
- Deployment package created at `C:\Users\administrator\Desktop\codex-vgsm-export\NewPharma\NewPharma.InspectionRequest-package.zip`.

## Runtime behavior in the DLL

- The task entry point is `NewPharmaInspectionRequestExecutionTask`.
- It validates that the request is approved and has not already executed.
- It resolves the configured Login Plan by latest active version or explicit version.
- It supports root contexts `LOT_DETAILS`, `LOT`, `JOB_HEADER`, and `JOB`.
- For lot context it calls `ExtendedLoginPlan.JobCreationProcess`.
- For job context it calls `ExtendedLoginPlan.SampleCreationProcess`.
- Generated job and sample identities are serialized back onto the request record.

## Remaining in-system configuration

- Create phrase type `NPH_IR_STATUS` with the planned request lifecycle states.
- Create phrase type `NPH_IR_EXEC_STATUS` with execution states.
- Register menu/task/form/workflow bindings for `NPH_INSPECTION_REQUEST`.
- Configure the e-signature requirement per site policy before enabling production use.

## Notes

No vendor Pharma Solution or LoginPlan source code is stored in this repository. Only NewPharma custom source, manifests, and design records are tracked.
