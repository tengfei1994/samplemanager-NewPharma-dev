# samplemanager-NewPharma-dev

Development workspace for NewPharma customizations on Thermo Scientific
SampleManager LIMS 21.3, validated against the remote VGSM instance.

## Purpose

This repository is the source of truth for the NewPharma development stream. It
keeps the design notes, SampleManager configuration files, form definitions,
structure changes, deployment scripts, and custom .NET code needed to build the
Inspection Request workflow.

The current focus is converting Inspection Request from a simple lab-table
record into a lifecycle-controlled business entity that can:

- Create inspection requests from a Login Plan.
- Snapshot Login Plan Data Assignment rows into IR-owned tables.
- Snapshot Product Spec rows into IR-owned tables.
- Allow request-level adjustment without modifying the source Login Plan.
- Support lifecycle/workflow status progression.
- Support Lot-context and Job-context request creation.

Vendor Pharma Solution and Login Plan source code is not stored in this
repository.

## Remote Instance

- Host alias: `conversion-project`
- Product: SampleManager LIMS 21.3
- Instance root: `C:\Thermo\SampleManager\Server\VGSM`
- Solution root: `C:\Thermo\SampleManager\Server\VGSM\Solution`
- Runtime binaries: `C:\Thermo\SampleManager\Server\VGSM\Exe`
- Logs: `C:\Thermo\SampleManager\Server\VGSM\Logfile`

## Workflow

1. Design and code locally.
2. Build and package local changes.
3. Deploy to the remote SampleManager instance over SSH.
4. Fetch remote logs for review.
5. Commit code, scripts, and design notes to GitHub.

## Key Areas

- `samplemanager/structure`: SampleManager structure fragments.
- `samplemanager/config`: Table-loader CSV configuration.
- `samplemanager/forms`: Form Designer XML definitions.
- `src/NewPharma.InspectionRequest`: Custom .NET task assembly.
- `docs`: Architecture, deployment, lifecycle, and validation notes.
