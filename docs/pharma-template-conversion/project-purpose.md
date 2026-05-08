# Project Purpose

## Business Goal

The Pharma Template Conversion project defines how a pharmaceutical laboratory implementation should move from the older Pharma Template PT 3.5/3.5.1 concept toward a SampleManager LIMS 21.3 based solution using Global Pharma Solution 2.5, Environmental Monitoring 1.2, and controlled customization.

The goal is to deliver a practical and validated laboratory workflow baseline rather than mechanically reproducing a previous template. The new design should support GMP evidence, controlled sample lifecycle management, reusable master data, auditability, and phased delivery.

## Why This Project Exists

The legacy Pharma Template contains useful industry patterns, especially around sample lifecycle, retain sample management, stability, inventory, certificates, audit viewing, and local pharmaceutical practices. Global Pharma Solution 2.5 provides a newer modular foundation, but it is not a full one-to-one replacement for PT 3.5/3.5.1.

This project therefore creates a conversion path:

- Identify which PT capabilities are covered by GPS 2.5 or EM 1.2.
- Identify which capabilities should be implemented using standard SampleManager configuration.
- Identify which capabilities need custom development.
- Define a P1 scope that can be implemented and validated first.
- Keep technical design, deployment scripts, and validation evidence under version control.

## Guiding Principles

- Business scenario first: design from QC, QA, microbiology, stability, EM, inventory, and retain sample workflows.
- Use standard SampleManager concepts first: Sample, Test, Result, Status, Workflow, Explorer, Location, Role, Audit, and Electronic Signature.
- Do not create custom tables when a native SampleManager object can carry the responsibility cleanly.
- Treat Global Pharma Solution modules as accelerators, not as a complete replacement for all PT capabilities.
- Separate standard fit, configuration migration, and customization work packages.
- Keep GMP traceability and validation impact visible in every design decision.
- Deliver in phases, starting with the core sample lifecycle and retain sample controls.

## Initial P1 Focus

P1 focuses on the foundation needed for later pharmaceutical workflows:

- Inspection request / sample request intake
- Login Plan based request template and execution
- Sample login and sample generation
- Sampling and receipt
- Sample claim, distribution, and test assignment
- Test claim, return, and reassignment
- Sample disposal and retain conversion
- Retain sample creation, receipt, storage, observation, use, return, and destruction
- Audit, status, role, location, and electronic signature baseline

## Out of P1 Scope

The following areas are important but should be handled as later phases unless project priority changes:

- Full stability study model
- Full environmental monitoring scheduling beyond standard EM module use
- Full reagent, standard, strain, and media inventory lifecycle
- Instrument maintenance and qualification blocking
- Complex reduced testing rule design
- Complete CoA template system
- Full audit viewer replacement
