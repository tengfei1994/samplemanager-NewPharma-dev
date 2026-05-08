# Implementation Roadmap

## Phase 0 - Positioning and Scope Confirmation

Target window: Week 0-2

Goals:

- Confirm the business boundary of the PT to GPS conversion.
- Confirm target SampleManager version and instance.
- Confirm GPS 2.5, EM 1.2, and LPN dependencies.
- Collect existing PT objects, fields, forms, reports, workflows, VGL, assemblies, patches, and validation package materials.

Outputs:

- Scope statement
- Current-state object inventory
- Initial gap register
- Module and license assumptions

## Phase 1 - Blueprint and Object Mapping

Target window: Week 2-6

Goals:

- Map PT functions to GPS modules, base SampleManager configuration, and custom development.
- Define the target P1 sample lifecycle model.
- Define inspection request and Login Plan design.
- Define data migration and SOP impact at a blueprint level.

Outputs:

- Target blueprint
- Object mapping matrix
- Initial URS/design baseline
- P1 lifecycle model

## Phase 2 - Standard Module PoC

Target window: Week 6-10

Goals:

- Validate GPS standard modules using real representative scenarios.
- Confirm Login Plan, Reduced Testing, EM, Media Management, Analyst Qualification, and Pharma Home behavior.
- Confirm which PT expectations cannot be met by standard modules.

Outputs:

- PoC configuration
- Scenario test records
- Fit/gap conclusion for standard modules
- Updated customization backlog

## Phase 3 - P1 Custom Design and Development

Target window: Week 10-18

Goals:

- Build or configure inspection request, sample lifecycle, test assignment, and retain sample controls.
- Implement necessary forms, workflows, rules, VGL/API/DLL logic, reports, and deployment scripts.
- Validate lifecycle, audit, and electronic signature behavior.

Outputs:

- Detailed design
- Development package
- Unit test records
- Deployment and rollback scripts
- Validation evidence

## Phase 4 - Extended Work Packages

Target window: After P1 foundation

Candidate work packages:

- Stability study model
- Retain sample enhancements
- Stock, reagent, standard, strain, and media inventory
- Audit viewer and review dashboards
- SQC
- Certificate and CoA generation
- Instrument maintenance and qualification integration
- Localization and validation package refinement

## Governance

Every phase should maintain:

- GitHub design document updates
- Issue or decision log entries
- Deployment notes
- Test evidence
- Remote log review
- Known risk register
