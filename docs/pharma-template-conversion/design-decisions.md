# Design Decisions

## DD-001: Treat GPS 2.5 as a modular target foundation, not a direct PT replacement

Decision: The project will not assume Global Pharma Solution 2.5 is a direct technical upgrade of Pharma Template PT 3.5/3.5.1.

Rationale: The gap analysis shows strong module fit in areas such as Login Plan, Reduced Testing, EM, Media Management, Analyst Qualification, and Pharma Home, but also high-impact gaps in stability, retain sample, inventory, certificate, audit, SQC, and instrument maintenance areas.

Impact: Every PT capability must be classified as standard module, base configuration, migration/configuration work, customization, or backlog.

## DD-002: Use SampleManager native objects before introducing custom tables

Decision: P1 design should reuse SampleManager native Sample, Test, Result, Workflow, Status, Explorer, Role, Audit, Location, and Electronic Signature concepts before creating custom tables.

Rationale: Native objects reduce validation and maintenance risk and improve fit with SampleManager behavior.

Impact: P1 does not introduce an independent Container table by default. Container-like behavior is carried through native sample/container capabilities unless a later gap proves otherwise.

## DD-003: Login Plan is the Inspection Request Template

Decision: Login Plan will serve as the inspection request template. Runtime Inspection Requests are instances derived from Login Plan templates.

Rationale: Login Plan already models structured generation of Job/Sample/Test records. Creating another template object would duplicate structure and increase synchronization risk.

Impact: Request instances must store their own lifecycle, approval, source Login Plan/version snapshot, and execution result.

## DD-004: Approved request execution is the boundary for sample/test generation

Decision: Job/Sample/Test records are not generated until the Inspection Request is approved and executed.

Rationale: This keeps requested work separate from executable laboratory work and prevents premature sample creation.

Impact: Execution logic must be idempotent, status-controlled, auditable, and able to record generated downstream object references.

## DD-005: P1 focuses on sample lifecycle and retain foundation

Decision: P1 will prioritize inspection request, sample lifecycle, test assignment, retain sample controls, status, location, role, audit, and electronic signature baseline.

Rationale: These workflows are prerequisites for later stability, inventory, certificate, audit, and automation enhancements.

Impact: Stability, full inventory, complete CoA, instrument maintenance, and advanced reduced testing remain later work packages unless reprioritized.
