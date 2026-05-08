# Topic Workplan: Login Plan Instance as Inspection Request

## Objective

Implement the P1 design where Login Plan remains the template and Inspection Request becomes the controlled runtime instance. Approved Inspection Requests execute the selected Login Plan to generate Job/Sample/Test records.

This workplan does not upload or fork vendor Pharma Solution source code. It records our implementation approach, expected configuration, automation scope, and manual actions.

## Existing Login Plan Behavior Observed

Remote source location inspected locally:

```text
C:\Thermo\SampleManager\Server\VGSM\Solution\LoginPlan
```

Key existing components:

- `LoginPlanObjectModel/ExtendedLoginPlan.cs`
  - Contains `JobCreationProcess`, `SampleCreationProcess`, and `TestCreationProcess`.
  - This is the core creation chain for Job/Sample/Test.
- `LoginPlanTasks/Nodes/LP_CreateEntityNode.cs`
  - Defines workflow node type `LP_CREATE_ENTITY`.
  - Provides Login Plan parameters and execution from Lot/Job context.
- `LoginPlanTasks/LoginPlanTask.cs`
  - UI/task logic for Login Plan maintenance.

## Design Direction

Do not modify the existing Login Plan source directly unless there is no supported extension path.

Preferred approach:

1. Add a new Inspection Request object/table/configuration.
2. Add lifecycle states and workflow actions around Inspection Request.
3. Add a server-side execution wrapper that calls the existing Login Plan execution path only when the request is Approved.
4. Record source Login Plan/version and generated Job/Sample/Test references on the request.
5. Keep all new code/configuration in our own customization package.

## Required Changes

### 1. Data Model

Create or configure an Inspection Request header with fields such as:

- Request ID
- Request status
- Source Login Plan
- Source Login Plan version
- Last active version flag
- Product / material / batch / lot context
- Requested by / requested date
- Submitted by / submitted date
- Approved by / approved date
- Execution status
- Execution started / completed date
- Generated Job ID
- Execution error message

Create child rows if request lines are needed for sample-level or test-level request parameters.

### 2. Lifecycle and Workflow

Suggested lifecycle:

```text
Draft -> Submitted -> Under Review -> Approved -> Executing -> Executed
```

Exception paths:

```text
Draft / Submitted / Under Review -> Cancelled
Under Review -> Rejected
Executing -> Execution Failed -> Approved or Cancelled
```

### 3. Execution Wrapper

Create a custom task/workflow action that:

1. Loads the Inspection Request.
2. Confirms status is Approved.
3. Confirms it has not already executed.
4. Changes status to Executing.
5. Resolves the selected Login Plan and version.
6. Calls the supported Login Plan execution mechanism.
7. Captures generated Job/Sample/Test references.
8. Changes status to Executed.
9. On error, records error text and changes status to Execution Failed.

### 4. UI / Explorer

Create operational entry points:

- Inspection Request workbench
- My Draft Requests
- Pending Approval
- Approved Pending Execution
- Execution Failed
- Executed Requests

### 5. Audit and Security

Define roles:

- Request Creator
- Request Submitter
- Request Approver
- Request Executor
- Request Viewer
- Template Maintainer

Audit requirements:

- Request creation
- Submit / approve / reject / cancel
- Execute start and completion
- Execution failure
- Generated object references
- Source Login Plan/version snapshot

## What Codex Can Automate

Codex can automate:

- Generate the implementation design documents.
- Generate object/field manifests for the new Inspection Request model.
- Generate SQL/configuration scripts if the target table/field mechanism is confirmed.
- Generate C# custom task skeletons for execution wrapper logic.
- Generate deployment and rollback scripts.
- Package generated customization files.
- Upload package to the remote VGSM server over SSH.
- Backup target folders before deployment.
- Restart selected SampleManager services when approved.
- Fetch logs after deployment and summarize errors.

## What Requires User / Admin Operation

The user or SampleManager administrator must handle or approve:

- Confirm whether new database tables are allowed or whether configuration-only objects must be used.
- Create/validate SampleManager table definitions if this must be done through admin UI/import tools.
- Confirm exact workflow/status configuration method in this VGSM instance.
- Confirm role names and security groups.
- Confirm whether electronic signature is required for submit/approve/execute/cancel.
- Confirm which Login Plan(s) are valid templates for P1 request instances.
- Confirm test data: product, batch, lot, sample, analysis, and user roles.
- Approve deployment to the VGSM instance.
- Perform UI validation inside SampleManager client if Codex cannot access the GUI.

## First Implementation Slice

The safest first slice is metadata-only plus a dry-run execution design:

1. Create Inspection Request object manifest.
2. Create lifecycle/status manifest.
3. Create execution wrapper skeleton without changing Login Plan source.
4. Deploy to a non-production folder or build output area first.
5. Review compile/deployment errors.
6. Only then wire the action into SampleManager workflow/UI.

## Open Questions

1. Should Inspection Request be a new custom table or based on an existing SampleManager request object?
2. What should the request ID format be?
3. Which user roles map to creator, approver, and executor?
4. Should execution happen automatically on approval or manually by an Executor?
5. Which Login Plan template should be used for the first test scenario?
6. Is electronic signature required for approval and execution?
