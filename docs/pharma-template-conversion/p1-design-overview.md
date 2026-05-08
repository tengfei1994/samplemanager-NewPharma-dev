# P1 Design Overview

## Scope

P1 establishes the core sample lifecycle foundation for the new SampleManager pharmaceutical solution. It focuses on the workflows that must be stable before later modules such as stability, inventory, EM expansion, reporting, and advanced automation can be safely layered on top.

P1 includes:

- Inspection request intake
- Login Plan / inspection plan driven sample and test creation
- Field sampling and handover
- Sample generation and receipt
- Sample claim and distribution
- Test claim, assignment, return, and reassignment
- Task tracking and result status feedback
- Sample return, consumption, disposal, destruction, and retain conversion
- Retain sample creation, receipt, observation, use request, approval, issue, return, destruction, and expiry reminders

P1 does not include full design for stability studies, environmental monitoring scheduling, full inventory lifecycle, instrument maintenance, complete CoA templates, or complex reduced testing rules.

## Business Value

P1 lets the laboratory operate a controlled sample lifecycle with clear status, ownership, location, and audit evidence. It creates the foundation for later GMP workflows by making sample movement, test assignment, retain sample behavior, and disposal decisions traceable.

## Design Principles

P1 should reuse standard SampleManager concepts wherever possible:

- `Sample` as the main business object
- `Test` and `Result` for laboratory task and result control
- `Workflow` and `Status` for lifecycle control
- `Explorer`, folders, and work queues for operational entry points
- `Role`, security, and electronic signature for controlled actions
- `Audit` and `Location` for traceability

A separate custom Container table is not planned for P1. SampleManager native sample/container capabilities should carry the container and physical sample responsibilities unless later detailed design proves a gap.

## Main Process

The P1 business flow is:

```text
Inspection request / source confirmation
  -> Login Plan or inspection plan resolution
  -> Field sampling
  -> Sample generation
  -> Receipt confirmation
  -> Claim and distribution
  -> Test claim and assignment
  -> Testing and review
  -> Retain / return / consume / destroy
  -> Archive and review
```

## Core Object View

- `Sample`: physical and logical sample unit, including retain-related behavior where applicable.
- `Test`: analytical task that can be assigned, returned, or reassigned.
- `Result`: result entry and review state.
- `Login Plan`: structured template used to generate jobs, samples, and tests.
- `Inspection Request`: runtime request instance that captures business request data before sample/test generation.
- `Location`: controlled physical or logical location such as warehouse, refrigerator, cabinet, shelf, box, or lab area.
- `Retain Test Plan`: retain sample plan or rule set for retain sample creation and later observation/use/destruction.

## Workbench and Queue Design

Expected operational entry points include:

- Inspection request workbench
- Sampling workbench
- Sample receipt workbench
- Sample claim and distribution workbench
- Test assignment queue
- Retain sample workbench
- Approval queue
- Exception and overdue queue

## Control Requirements

- Critical state changes must record user, time, reason, source state, target state, and related object.
- Destruction, retain issue, over-claim, and other GMP-sensitive operations should support electronic signature.
- Test reassignment and return must preserve source relationship and assignment history.
- Sample location and responsibility must remain auditable.
- Requested or inspection-request state should not create executable samples/tests until approved and executed.
