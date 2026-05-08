# Conversion Plan

## Scope

This plan defines the overall conversion from Pharma Template PT 3.5/3.5.1 concepts into a new SampleManager LIMS 21.3 pharmaceutical implementation using Global Pharma Solution 2.5, Environmental Monitoring 1.2, and targeted customization.

## Version Baseline

Source baseline:

- Pharma Template PT 3.5 / PT 3.5.1
- SampleManager 21.1

Target baseline:

- SampleManager LIMS 21.3
- Global Pharma Solution 2.5
- Environmental Monitoring 1.2
- LPN / ENH dependency where required

## Overall Conclusion

Global Pharma Solution 2.5 is a modular pharmaceutical accelerator. It is not a complete equivalent replacement for Pharma Template PT 3.5/3.5.1.

The conversion must classify each legacy capability into one of these categories:

- Standard GPS or EM module fit
- SampleManager base configuration
- Migrated configuration or master data
- Custom workflow, form, report, VGL, API, or .NET DLL development
- Deferred backlog item

## Main Fit Areas

- Pharma Home for navigation and landing pages
- Login Plan for structured Job/Sample/Test generation
- Reduced Testing for reduced test scenarios
- Environmental Monitoring 1.2 for EM-specific workflows
- Media Management for controlled media processes
- Analyst Qualification for training and qualification workflows

## Main Gap Areas

- End-to-end sample lifecycle controls beyond standard login
- Stability study management
- Retain sample lifecycle
- Full stock, reagent, standard, strain, and inventory management
- Certificate and CoA generation
- Audit display and review experience
- SQC scenarios
- Instrument maintenance and qualification blocking
- Localized validation, forms, labels, and workflow expectations

## Phased Roadmap

### Phase 0 - Positioning and Scope Confirmation

Target window: Week 0-2

Confirm target version, module scope, license dependencies, legacy object inventory, and validation boundaries.

Outputs:

- Scope statement
- Current-state object inventory
- Initial gap register
- Module and license assumptions

### Phase 1 - Blueprint and Object Mapping

Target window: Week 2-6

Map legacy capabilities to GPS modules, base SampleManager configuration, and custom work packages.

Outputs:

- Target blueprint
- Object mapping matrix
- P1 lifecycle model
- Initial URS/design baseline

### Phase 2 - Standard Module PoC

Target window: Week 6-10

Validate GPS standard modules using representative product, batch, sample, Login Plan, EM, media, reduced testing, and AQ scenarios.

Outputs:

- PoC configuration
- Scenario test records
- Standard module fit/gap conclusion
- Updated customization backlog

### Phase 3 - P1 Custom Design and Development

Target window: Week 10-18

Build or configure the P1 foundation: inspection request, sample lifecycle, test assignment, retain sample controls, workflow, audit, and deployment scripts.

Outputs:

- Detailed design
- Development package
- Unit test records
- Deployment and rollback scripts
- Validation evidence

### Phase 4 - Extended Pharma Work Packages

Candidate work packages:

- Stability study model
- Inventory and stock lifecycle
- Certificate and CoA generation
- Audit viewer and review dashboards
- SQC
- Instrument maintenance and qualification integration
- Localization and validation package refinement

## Governance

Every phase should maintain:

- GitHub design updates
- Decision log updates
- Backlog updates
- Deployment notes
- Test evidence
- Remote log review