# NewPharma.InspectionRequest

Custom SampleManager task assembly for the NewPharma Inspection Request
implementation.

## Purpose

This assembly provides the controlled wrapper around Login Plan based inspection
request creation and execution. It does not modify or include vendor Pharma
Solution source code.

## Current State

Current task/service responsibilities:

- `InspectionRequestLabTableTask` sets initial request defaults and triggers
  snapshot creation during save.
- `InspectionRequestSnapshotService` copies Login Plan Data Assignment, Test
  Assignment, and Product Spec data into IR-owned snapshot tables.
- `InspectionRequestExecutionTask` is the execution task entry point.
- `InspectionRequestExecutionService` validates status, marks execution
  progress, calls the Login Plan execution path, and records generated objects
  or execution errors.
- `ConfigureInspectionRequestPhrasesTask` creates the phrase types used by the
  request and execution status fields.

## Build Assumptions

- Target framework: `net8.0-windows`
- Platform: `x86`
- References are resolved from the VGSM `Exe` and `Exe\SolutionAssemblies` folders using `VGSM_EXE`.

Example remote build command once references are available:

```powershell
$env:VGSM_EXE = 'C:\Thermo\SampleManager\Server\VGSM\Exe'
dotnet build C:\Thermo\SampleManager\Server\VGSM\Solution\NewPharma\InspectionRequest\NewPharma.InspectionRequest.csproj -c Release -p:Platform=x86
```

## Next Engineering Step

Add workflow action/menu configuration for submit, review, approve, reject, and
execute transitions.
