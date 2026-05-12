# Custom DLL Development

Custom DLL work should be developed locally and deployed as a package. Avoid
copying a single DLL unless dependencies, configuration files, and target .NET
runtime requirements are known.

Recommended package contents:

- Main custom DLL.
- Dependent DLLs.
- Configuration file changes, if required.
- Deployment manifest.
- Rollback notes.

## VGSM Notes

- The remote VGSM instance now has .NET SDK 8.0.420 installed.
- `NewPharma.InspectionRequest` targets `net8.0-windows` and `x86`.
- Build with `VGSM_EXE` pointing to `C:\Thermo\SampleManager\Server\VGSM\Exe`.
- Deploy the compiled DLL to
  `C:\Thermo\SampleManager\Server\VGSM\Exe\SolutionAssemblies`.
- Backup the existing DLL before replacement and restart VGSM services after
  deployment.
