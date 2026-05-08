# Topic: P1 Sample Lifecycle

## Topic Role in the Overall Conversion

The P1 sample lifecycle is the first implementation foundation for the New Pharma solution. Later areas such as stability, EM expansion, inventory, certificate generation, audit review, and advanced automation depend on a reliable sample lifecycle baseline.

## P1 Scope

P1 includes:

- Inspection request intake
- Login Plan / inspection plan resolution
- Field sampling and handover
- Sample generation
- Receipt confirmation
- Sample claim and distribution
- Test claim and assignment
- Test return and reassignment
- Testing and review status feedback
- Sample retain, return, consumption, destruction, and archive behavior

## P1 Out of Scope

P1 does not fully implement:

- Complete stability study model
- Complete EM scheduling beyond standard EM module validation
- Full reagent, standard, strain, and media inventory lifecycle
- Instrument maintenance and qualification blocking
- Complete CoA template system
- Complex reduced testing rule design

These are later work packages unless project priority changes.

## Design Principles

P1 should reuse standard SampleManager concepts first:

- Sample
- Test
- Result
- Workflow
- Status
- Explorer and work queues
- Role and security
- Location
- Audit
- Electronic Signature

A separate custom Container table is not planned for P1 by default. Native SampleManager sample/container capabilities should be used unless a later detailed gap proves otherwise.

## Main Process

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

## Workbench Candidates

- Inspection request workbench
- Sampling workbench
- Sample receipt workbench
- Sample claim and distribution workbench
- Test assignment queue
- Exception and overdue queue
- Retain sample workbench
- Approval queue

## Control Requirements

- State changes must record user, time, reason, source state, target state, and object reference.
- Sample location and responsibility must remain auditable.
- Test reassignment and return must preserve assignment history.
- GMP-sensitive operations should support electronic signature.
- Requested inspection work should not create executable samples/tests until approved and executed.