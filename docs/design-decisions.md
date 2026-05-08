# Design Decisions

## DD-001: The repository represents the whole conversion project

Decision: The root `README.md` carries the project purpose and overall positioning. Project-level purpose should not live only inside a topic document folder.

Impact: Topic documents live under `docs/topics`, while cross-project planning lives under `docs`.

## DD-002: GPS 2.5 is a modular target foundation, not a direct PT replacement

Decision: Global Pharma Solution 2.5 is not treated as a direct technical upgrade of Pharma Template PT 3.5/3.5.1.

Impact: Every PT capability must be classified as standard module, base configuration, migration/configuration, customization, or backlog.

## DD-003: Use SampleManager native objects before custom tables

Decision: P1 should reuse SampleManager native Sample, Test, Result, Workflow, Status, Explorer, Role, Audit, Location, and Electronic Signature concepts before introducing custom tables.

Impact: P1 does not introduce an independent Container table by default.

## DD-004: Login Plan conversion is a topic under the overall plan

Decision: Login Plan based Inspection Request is a专题 under the overall conversion plan, not the overall project itself.

Impact: Login Plan design lives under `docs/topics/login-plan-inspection-request.md` and is linked from the root README and conversion plan.

## DD-005: Login Plan is the Inspection Request Template

Decision: Login Plan will serve as the inspection request template. Runtime Inspection Requests are instances derived from Login Plan templates.

Impact: Request instances must store their own lifecycle, approval, source Login Plan/version snapshot, and execution result.

## DD-006: Approved request execution is the boundary for sample/test generation

Decision: Job/Sample/Test records are not generated until the Inspection Request is approved and executed.

Impact: Execution logic must be idempotent, status-controlled, auditable, and able to record generated downstream object references.

## DD-007: P1 focuses on sample lifecycle and retain foundation

Decision: P1 prioritizes inspection request, sample lifecycle, test assignment, retain sample controls, status, location, role, audit, and electronic signature baseline.

Impact: Stability, full inventory, complete CoA, instrument maintenance, and advanced reduced testing remain later work packages unless reprioritized.