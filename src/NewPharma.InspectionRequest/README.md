# NewPharma.InspectionRequest

Custom extension skeleton for the P1 Inspection Request implementation.

## Purpose

This assembly is intended to provide the controlled execution wrapper around Login Plan based sample generation. It does not modify or include vendor Pharma Solution source code.

## Current State

The current version is a skeleton:

- `InspectionRequestExecutionTask` is the SampleManager task entry point.
- `InspectionRequestExecutionService` validates status, marks Executing, and records success/failure.
- The actual Login Plan execution hook is intentionally left as a guarded integration point until the VGSM object/table configuration and supported call path are confirmed.

## Build Assumptions

- Target framework: `net8.0-windows`
- Platform: `x86`
- References are resolved from the VGSM `Exe` and `Exe\SolutionAssemblies` folders using `VGSM_EXE`.

Example local build command once references are available:

```powershell
$env:VGSM_EXE = 'C:\Thermo\SampleManager\Server\VGSM\Exe'
dotnet build .\src\NewPharma.InspectionRequest\NewPharma.InspectionRequest.csproj -c Release -p:Platform=x86
```

## Next Engineering Step

Compile this skeleton against the remote VGSM assemblies, fix API signature differences, then wire `ExecuteLoginPlan` to the supported Login Plan execution path.
