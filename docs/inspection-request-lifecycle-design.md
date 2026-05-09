# Inspection Request Lifecycle Design

## Purpose

Inspection Request is a lifecycle-controlled business entity for reviewing and executing Login Plan based inspection creation. It is not a generic lab table maintenance object.

## Scope

The Inspection Request form should support:

- Selecting a Login Plan filtered by context.
- Loading Login Plan Data Assignment into an editable Inspection Request snapshot.
- Loading Product Selection / Product Spec from `MLP_HEADER` into an editable Inspection Request snapshot.
- Driving request status through SampleManager workflow/lifecycle configuration.
- Creating a request from a Lot right-click action, with Lot context pre-filled.
- Creating a request without Lot context, using Job context.

## Data Model

`NPH_INSPECTION_REQUEST` remains the request header.

Lifecycle fields:

- `LIFECYCLE_WORKFLOW_ID`
- `LIFECYCLE_WORKFLOW_VERSION`
- `LIFECYCLE_NODE_ID`
- `LIFECYCLE_EVENT`

Login Plan Data Assignment snapshot:

- `NPH_IR_LP_ENTRY`
- `NPH_IR_LP_FIELD`
- `NPH_IR_LP_TEST`
- `NPH_IR_LP_TEST_FIELD`

Product Spec snapshot:

- `NPH_IR_PRODUCT`

The snapshot model deliberately copies Login Plan content into IR-owned rows. Execution should read the IR snapshot, not the source Login Plan, so later Login Plan version changes do not alter an already created request.

## Form Design

Recommended tabs:

- `General`
  - Request number
  - Status
  - Root context table/id
  - Login Plan selector
  - Lifecycle workflow/current node

- `Data Assignment`
  - Data Assignment tree/grid from `NPH_IR_LP_ENTRY`
  - Entry fields from `NPH_IR_LP_FIELD`
  - Test Assignment from `NPH_IR_LP_TEST`
  - Test fields from `NPH_IR_LP_TEST_FIELD`

- `Product Spec`
  - Product rows from `NPH_IR_PRODUCT`

- `Execution`
  - Execution status
  - Generated job/sample summary
  - Execution error

## Context Rules

Lot right-click create:

- Set `ROOT_CONTEXT_TABLE = LOT_DETAILS`.
- Set `ROOT_CONTEXT_ID` from the selected Lot.
- Filter Login Plan where `ROOT_TABLE_NAME = LOT_DETAILS`.

Main menu / non-Lot create:

- Default `ROOT_CONTEXT_TABLE = JOB_HEADER`.
- Filter Login Plan where `ROOT_TABLE_NAME = JOB_HEADER`.
- User can select or provide Job context before execution.

## Workflow Rules

IR status should be driven by workflow actions, not direct field edits.

Initial states:

- `DRAFT`
- `SUBMITTED`
- `UNDER_REVI`
- `APPROVED`
- `EXECUTING`
- `EXECUTED`
- `EXECUTION_`

The current phrase IDs remain valid, but the transition authority moves into workflow configuration and workflow nodes.

## Current Implementation Notes

- The first implemented form pass uses `datagridcontrol` pages with explicit
  `dataqueryentitycollection` components:
  - `DataAssignmentEntries`
  - `DataAssignmentTests`
  - `ProductSpecRows`
- `InspectionRequestSnapshotService` currently creates the initial snapshot on
  save when no snapshot rows exist. It deliberately does not overwrite existing
  IR snapshot rows, so user adjustments are preserved.
- `InspectionRequestLifecycleService` initializes the IR header with a default
  entity template and workflow, creates the default workflow configuration when
  the IR task opens or saves, and writes the current request to `WORKFLOW_LINK`.
- Default lifecycle configuration:
  - Entity Template: `NPH_IR`
  - Workflow: `19940000-0000-0000-0000-000000000001`
  - Nodes: Draft, Submitted, Under Review, Approved, Executed
- The next implementation step is exposing submit, review, approve, reject, and
  execute actions as RMB/menu items and replacing the placeholder tree with
  Task Code populated nodes.
