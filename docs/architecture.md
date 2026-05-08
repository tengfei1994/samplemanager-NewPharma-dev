# Architecture

This repository keeps local development artifacts for SampleManager LIMS
customization work. The remote SampleManager server is treated as a validation
environment, while this repository remains the source of truth for design,
source code, scripts, and deployment notes.

## Local Responsibilities

- Source code for custom DLLs.
- VGL, SQL, form, report, and configuration artifacts.
- Deployment and rollback scripts.
- Design notes and validation records.

## Remote Responsibilities

- Run the real SampleManager LIMS instance.
- Receive packaged artifacts for validation.
- Provide logs and runtime behavior for analysis.

## Synchronization Model

Local changes are packaged and pushed to the remote server. Remote runtime logs
and selected configuration snapshots are pulled back into local working folders
for analysis.
