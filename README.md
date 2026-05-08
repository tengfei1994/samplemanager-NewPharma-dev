# samplemanager-NewPharma-dev

This repository is the working home for the **Pharma Template Conversion / New Pharma** project.

The project purpose is to design and implement a new SampleManager LIMS 21.3 pharmaceutical laboratory solution by converting selected business capabilities from the legacy Pharma Template direction into a modern Global Pharma Solution 2.5 based architecture, with targeted configuration and custom development where the standard solution does not provide a direct fit.

This repo is the source of truth for:

- Project purpose and conversion scope
- Fit/gap conclusions
- Overall implementation roadmap
- Topic designs such as Login Plan based Inspection Request
- SampleManager instance notes
- Local-to-remote development and deployment scripts
- Future .NET DLL, VGL, SQL, form, report, and configuration artifacts

## Project Positioning

This is **not** a direct technical upgrade from Pharma Template PT 3.5/3.5.1 to Global Pharma Solution 2.5.

The target approach is:

- Use **SampleManager LIMS 21.3** as the base platform.
- Use **Global Pharma Solution 2.5** modules where they provide a standard fit.
- Use **Environmental Monitoring 1.2** for EM-specific workflows.
- Use **Laboratory Process Navigation / ENH** where required by the target solution stack.
- Rebuild PT-specific business capabilities through SampleManager configuration, workflow, VGL/API/.NET customization, reports, or later work packages.

## Business Goal

The business goal is to create a supportable pharmaceutical laboratory LIMS baseline that can cover GMP-critical workflows while avoiding unnecessary recreation of legacy template internals.

The design is anchored on real laboratory scenarios:

- QC batch release testing
- Sample request, sampling, receipt, claim, distribution, and test assignment
- Retain sample lifecycle
- Stability, EM, media, inventory, analyst qualification, and reduced testing as phased capabilities
- Audit, electronic signature, status, location, role, and validation evidence

## Current P1 Focus

P1 focuses on the foundation that later pharma modules depend on:

- Inspection Request as the business intake object
- Login Plan as the Inspection Request Template
- Approved request execution to generate Job/Sample/Test
- Sample lifecycle control from request to receipt, testing, retain, disposal, and archive
- Test claim, assignment, return, and reassignment
- Retain sample creation, storage, observation, issue, return, and destruction
- GMP traceability through workflow, audit, location, roles, and electronic signature

## Repository Structure

```text
docs/
  conversion-plan.md                 Overall PT to New Pharma conversion plan
  design-decisions.md                Key architecture and scope decisions
  backlog.md                         Delivery backlog and future work packages
  artifact-index.md                  Local source material and deliverable index
  samplemanager-instance.md          Remote VGSM instance information
  architecture.md                    Technical working architecture
  dev-workflow.md                    Local/GitHub/remote development workflow
  deployment.md                      Deployment notes
  custom-dll.md                      .NET DLL development notes
  topics/
    login-plan-inspection-request.md Login Plan / Inspection Request专题设计
    p1-sample-lifecycle.md           P1样品主流程专题设计
    retain-sample.md                 留样管理专题设计
scripts/
  pull-remote.ps1                    Pull selected remote instance metadata
  build-package.ps1                  Build local .NET package
  deploy-remote.ps1                  Upload and deploy package to VGSM
  fetch-logs.ps1                     Fetch remote log index
  restart-sm.ps1                     Restart SampleManager services
src/                                 Local source code, including future DLL projects
samplemanager/                       VGL, SQL, forms, reports, config templates, manifests
out/                                 Local build output placeholder
```

## Remote Development Instance

Observed validation instance:

- Product: SampleManager LIMS 21.3.0.0
- Instance: `VGSM`
- Root path: `C:\Thermo\SampleManager\Server\VGSM`
- Solution path: `C:\Thermo\SampleManager\Server\VGSM\Solution`
- Runtime binary path: `C:\Thermo\SampleManager\Server\VGSM\Exe`
- Log path: `C:\Thermo\SampleManager\Server\VGSM\Logfile`

See `docs/samplemanager-instance.md` for details.

## Design Entry Points

Start with these documents:

1. `docs/conversion-plan.md`
2. `docs/topics/login-plan-inspection-request.md`
3. `docs/topics/p1-sample-lifecycle.md`
4. `docs/topics/retain-sample.md`
5. `docs/design-decisions.md`
6. `docs/backlog.md`

## Development Model

The intended working model is:

```text
Local Codex workspace
  -> GitHub repository as design/source control
  -> SSH deployment to remote SampleManager VGSM instance
  -> Remote validation and log collection
  -> Findings folded back into GitHub
```

Large vendor packages, generated rendered pages, and controlled customer documents should not be committed by default. They are indexed in `docs/artifact-index.md` and can be added later only after repository size, license, and confidentiality rules are confirmed.