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
