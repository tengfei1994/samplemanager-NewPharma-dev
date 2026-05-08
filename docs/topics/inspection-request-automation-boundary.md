# Inspection Request Automation Boundary

## Decisions Confirmed

- Create a new Inspection Request table/object.
- Request numbering follows a Lot-like controlled sequence style.
- Electronic signature is configurable, not hard-coded.
- PoC template/data selection is outside the design scope; test data will be prepared manually.
- Deployment of the custom package to VGSM is allowed after review.
- Vendor Pharma Solution source code must not be uploaded to GitHub.

## What Codex Can Automate Now

Codex can prepare and maintain:

- Table/object manifests under `samplemanager/manifests/inspection-request`.
- Lot-like numbering manifest.
- Lifecycle/status manifest.
- Security role manifest.
- Electronic signature configuration manifest.
- Deployment manifest.
- C# custom task skeleton under `src/NewPharma.InspectionRequest`.
- Build scripts once VGSM reference assemblies are available locally or through a remote build step.
- Remote package upload over SSH.
- Backup of target DLL/config files before deployment.
- Log collection after deployment.
- GitHub documentation and change tracking.

## What Codex Can Automate After First Compile

After the first compile/test cycle, Codex can likely automate:

- Fixing exact SampleManager API signatures.
- Wiring the execution wrapper to the supported Login Plan execution path.
- Capturing generated Job/Sample/Test references.
- Adding rollback scripts.
- Creating deployable ZIP packages.
- Running remote build if the server has the required SDK/tooling.

## What Requires User or SampleManager Admin Operation

The user/admin must confirm or perform:

- Import or creation of `NPH_INSPECTION_REQUEST` in SampleManager administration tools.
- Actual mapping of manifest field types to SampleManager table/property definitions.
- Lot-like numbering configuration in the VGSM administration UI if native numbering tools are required.
- Mapping logical roles to real VGSM users/groups.
- Enabling electronic signature actions according to QA decisions.
- Adding the custom task/action into workflow/UI where SampleManager requires manual admin configuration.
- Functional testing in the SampleManager client.
- Approval for service restarts.

## Immediate Next Step

The next technical step is to compile `src/NewPharma.InspectionRequest` against the VGSM assemblies. This will reveal the exact API adjustments needed before deployment.
