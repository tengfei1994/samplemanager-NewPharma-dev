# Architecture

This repository keeps local development artifacts for SampleManager LIMS
customization work. The remote SampleManager server is treated as a validation
environment, while this repository remains the source of truth for design,
source code, scripts, and deployment notes.

## Local Responsibilities

- Source code for custom DLLs.
- VGL, SQL, form, report, and configuration artifacts.
- Deployment and rollback scripts.
- Design notes and validation records.

## Remote Responsibilities

- Run the real SampleManager LIMS instance.
- Receive packaged artifacts for validation.
- Provide logs and runtime behavior for analysis.

## Synchronization Model

Local changes are packaged and pushed to the remote server. Remote runtime logs
and selected configuration snapshots are pulled back into local working folders
for analysis.

## Inspection Request Model

Inspection Request is modeled as a lifecycle-controlled header with owned
snapshot tables. The source Login Plan remains a template/reference; it is not
edited by an Inspection Request.

The request header is `NPH_INSPECTION_REQUEST`.

Owned snapshot tables:

- `NPH_IR_LP_ENTRY`
- `NPH_IR_LP_FIELD`
- `NPH_IR_LP_TEST`
- `NPH_IR_LP_TEST_FIELD`
- `NPH_IR_PRODUCT`

The form keeps business tab names aligned with the Login Plan user experience:

- `General`
- `Data Assignment`
- `Product Spec`
- `Execution`

Lifecycle fields on the header link the request to workflow configuration and
the current workflow node. Phrase values continue to represent readable request
status and execution status.
