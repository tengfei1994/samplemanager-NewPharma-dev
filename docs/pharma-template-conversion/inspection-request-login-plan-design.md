# Inspection Request and Login Plan Design

## Design Conclusion

P1 treats Login Plan as the Inspection Request Template. The runtime Inspection Request is an instance of a Login Plan template and carries its own request number, status, approval history, execution result, and source Login Plan/version snapshot.

This avoids creating a separate template object when Login Plan already provides the structured hierarchy needed to generate jobs, samples, and tests.

## Key Concepts

- Login Plan: maintained as the reusable inspection request template.
- Inspection Request: runtime business request created from a Login Plan.
- Template Mode: UI mode for maintaining the Login Plan structure.
- Request Mode: UI mode for filling request-specific values and running lifecycle actions.
- Execution: approved request triggers Login Plan execution to create Job/Sample/Test records.

## Core Rules

- Approval must happen before Job/Sample/Test generation.
- Execution is only allowed from Approved state.
- Execution must be idempotent and prevent repeated sample generation.
- Execution should move the request into Executing before creating downstream objects.
- Successful execution records generated Job, Sample, and Test references.
- Failed execution records the error and enters an Execution Failed state for review and retry.
- The source Login Plan and version snapshot must remain traceable.

## Suggested Lifecycle

```text
Draft
  -> Submitted
  -> Under Review
  -> Approved
  -> Executing
  -> Executed
```

Exception paths:

```text
Draft / Submitted / Under Review -> Cancelled
Under Review -> Rejected
Executing -> Execution Failed -> Approved or Cancelled
```

## Permissions

Minimum role split:

- Template Maintainer
- Request Creator
- Request Submitter
- Approver
- Executor
- Viewer

## Data Requirements

The Inspection Request should store:

- Request ID
- Source Login Plan ID
- Source Login Plan version
- Request status
- Request creator and creation time
- Product, batch, lot, material, site, or other business context
- Requested sampling or testing context
- Approval history
- Execution status
- Generated Job ID
- Generated Sample/Test references
- Execution error message, if any

## UI Requirements

The same tree-style interface should support two modes:

- Template Mode for structure maintenance
- Request Mode for request data entry, review, approval, and execution

The execution button should only be visible or enabled when:

- Request is Approved
- Request has not already been Executed
- User has Executor permission
- Required request fields are complete

## Audit and Validation

The following must be auditable:

- Template version used by the request
- Request creation and submission
- Approval and rejection decisions
- Execution start and completion
- Generated downstream object references
- Execution failure and retry
- Any manual correction to request data after submission

## Development Implications

This design likely requires a combination of:

- SampleManager configuration for status, roles, Explorer entry points, and lifecycle actions
- Form or UI customization for Template Mode and Request Mode behavior
- Server-side logic for execution orchestration and duplicate prevention
- Audit/journal writing for lifecycle and execution events
- Deployment packaging into the VGSM instance for validation
