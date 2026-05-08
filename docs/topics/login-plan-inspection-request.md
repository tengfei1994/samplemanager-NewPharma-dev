# Topic: Login Plan Based Inspection Request

## Topic Role in the Overall Conversion

Login Plan conversion is one专题 inside the wider Pharma Template Conversion project. It supports the P1 sample lifecycle foundation by turning a business inspection request into controlled Job/Sample/Test generation.

It is not the whole project. It is one design stream that connects request intake, approval, sample generation, and downstream test execution.

## Design Conclusion

P1 treats **Login Plan as the Inspection Request Template**.

The runtime **Inspection Request** is an instance created from a Login Plan template and carries its own:

- Request number
- Lifecycle status
- Approval history
- Execution status
- Source Login Plan and version snapshot
- Generated Job/Sample/Test references

## Why This Direction

Login Plan already provides the structured hierarchy needed to generate jobs, samples, and tests. Creating a separate custom template object would duplicate structure and create synchronization risk.

The design therefore separates:

- Template definition: Login Plan
- Runtime business request: Inspection Request
- Laboratory execution objects: Job, Sample, Test, Result

## Core Rules

- Approval must happen before Job/Sample/Test generation.
- Execution is only allowed from Approved state.
- Execution must be idempotent and prevent duplicate sample generation.
- Execution should move the request into Executing before creating downstream objects.
- Successful execution records generated Job/Sample/Test references.
- Failed execution records the error and enters Execution Failed state.
- Source Login Plan and version snapshot must remain traceable.

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

## Development Implications

Likely implementation areas:

- Status and workflow configuration
- Explorer/workbench entry points
- Template Mode and Request Mode UI behavior
- Server-side execution orchestration
- Duplicate execution prevention
- Audit or journal writing
- Generated object traceability
- Deployment to VGSM for validation