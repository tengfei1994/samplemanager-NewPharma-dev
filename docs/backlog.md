# Backlog

## P0 - Scope and Blueprint

- Confirm target module list for GPS 2.5, EM 1.2, and LPN/ENH dependencies.
- Build PT object inventory for fields, forms, workflows, reports, VGL, assemblies, patches, and validation documents.
- Create PT to target object mapping matrix.
- Confirm whether legacy stability, retain, and inventory capabilities are mandatory for first release.
- Confirm GMP criticality and validation category for each P1 workflow.

## P1 - Core Lifecycle Implementation

- Define Inspection Request data model.
- Define Login Plan version snapshot behavior.
- Configure request lifecycle and approval workflow.
- Build execution logic for approved request to Job/Sample/Test generation.
- Add duplicate execution prevention.
- Define sample receipt and location model.
- Define test assignment, return, and reassignment behavior.
- Define retain sample creation and lifecycle actions.
- Define audit and electronic signature points.

## P2 - Extended Pharma Capabilities

- Stability study design.
- Inventory and stock lifecycle design.
- Certificate and CoA generation.
- Audit viewer and review dashboard.
- SQC scenarios.
- Instrument maintenance and qualification integration.
- Localization and validation package enhancement.

## Technical Tasks

- Create local .NET solution structure under `src` when the first DLL scope is confirmed.
- Add deployment manifest format under `samplemanager`.
- Extend `scripts/deploy-remote.ps1` to support targeted deployment by artifact type.
- Add rollback script once target DLL/config locations are confirmed.
- Add log collection script for specific SampleManager services.