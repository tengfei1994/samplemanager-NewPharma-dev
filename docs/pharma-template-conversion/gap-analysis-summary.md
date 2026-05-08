# Gap Analysis Summary

## Version Scope

The gap analysis compares:

- Source baseline: Pharma Template PT 3.5 / PT 3.5.1 on SampleManager 21.1
- Target baseline: Global Pharma Solution 2.5 plus Environmental Monitoring 1.2 on SampleManager 21.3

## Overall Conclusion

Global Pharma Solution 2.5 is a modular pharmaceutical accelerator. It should not be treated as a complete equivalent upgrade package for Pharma Template PT 3.5/3.5.1.

The conversion should use GPS standard modules where they fit, then rebuild PT-specific functions through configuration, workflow design, custom code, reporting, and validation work packages.

## Main Fit Areas

The following areas show useful alignment with the target solution direction:

- Pharma Home as the target navigation shell
- Login Plan for structured sample and test generation
- Reduced Testing for reduced test scenarios
- Environmental Monitoring 1.2 for EM workflows
- Media Management for media-related laboratory control
- Analyst Qualification for training and qualification workflows

## Main Gap Areas

The following PT-style capabilities require early scope decisions because they are not simple GPS standard replacements:

- End-to-end sample lifecycle controls beyond sample login
- Stability study management
- Retain sample lifecycle
- Full stock, reagent, standard, strain, and inventory management
- Certificate and CoA design
- Audit display and review experience
- SQC and statistical quality control scenarios
- Instrument maintenance and qualification blocking
- Localized workflow, forms, labels, and validation package expectations

## Priority Interpretation

High priority gaps should be handled during project scope definition and blueprint design. These include stability, retain sample, sample lifecycle, stock/inventory, and validation boundary decisions.

Medium priority gaps should be proven through PoC. These include Login Plan, Reduced Testing, EM, Media Management, Analyst Qualification, certificate/audit behavior, and selected workflows.

Lower priority gaps can be held in backlog when they do not block the P1 lifecycle foundation.

## Recommended Conversion Strategy

1. Confirm the target SampleManager 21.3 environment, GPS 2.5 licensing, EM 1.2 use, and LPN dependency.
2. Split source PT capabilities into standard GPS fit, base SampleManager configuration, and custom development.
3. Use a P1 foundation to establish sample lifecycle, inspection request, Login Plan execution, retain sample controls, status model, and audit behavior.
4. Run PoC scenarios using representative products, batches, sample plans, Login Plans, retain samples, and approval flows.
5. Move large PT-specific areas such as stability, inventory, audit viewer, SQC, and certificate design into separate work packages.

## Key Design Warning

Do not assume a PT function is available out of the box in GPS 2.5 only because both are pharmaceutical solution assets. The target design must explicitly classify every capability as one of:

- GPS/EM standard module
- SampleManager base configuration
- Migrated configuration or master data
- Custom workflow/form/report/code
- Deferred backlog
