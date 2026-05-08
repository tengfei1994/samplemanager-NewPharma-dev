# Topic: Retain Sample Management

## Topic Role in the Overall Conversion

Retain sample management is a P1 foundation topic because many pharmaceutical workflows need controlled retained material, storage location, observation, use request, approval, issue, return, and destruction records.

The design should preserve the business intent of legacy PT retain capabilities while using SampleManager-native objects wherever practical.

## P1 Retain Scope

P1 retain sample scope includes:

- Retain creation from sample lifecycle
- Retain receipt
- Retain storage location
- Retain observation
- Retain use request
- Retain approval
- Retain issue
- Retain return
- Retain destruction
- Expiry or near-expiry reminder

## Object Direction

Retain samples should be modeled through SampleManager sample-related capabilities first. A separate retain object or custom table should only be introduced when native Sample/Location/Workflow/Audit behavior cannot satisfy the requirements.

## Control Requirements

- Retain status must be controlled through lifecycle actions.
- Retain location must be auditable.
- Retain issue and destruction should support reason capture and electronic signature.
- Retain usage should keep links to the original sample, batch, product, request, and related tests where applicable.
- Retain quantity changes should be traceable.

## Later Enhancements

Possible later work packages:

- Stability retain integration
- Inventory integration
- Barcode and label design
- Automated expiry reminder service
- Retain dashboard and KPI reporting
- Retain destruction approval campaign