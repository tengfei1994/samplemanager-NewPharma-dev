# Pharma Template Conversion Project

This project captures the design and development workspace for converting selected capabilities from the legacy Pharma Template direction into a SampleManager LIMS 21.3 environment using Global Pharma Solution 2.5, Environmental Monitoring 1.2, and targeted customization.

## Purpose

The project is not a direct technical upgrade of Pharma Template PT 3.5/3.5.1. Its purpose is to establish a new, supportable SampleManager 21.3 implementation baseline for pharmaceutical laboratory workflows, while preserving the business intent behind the older template where it remains valuable.

The target approach is:

- Use SampleManager LIMS 21.3 as the base platform.
- Use Global Pharma Solution 2.5 modules where they provide a standard fit.
- Use Environmental Monitoring 1.2 for EM-specific workflows.
- Rebuild PT-specific capabilities as configuration, workflow, VGL/API/DLL customization, or future backlog items.
- Keep design, source code, deployment scripts, and validation notes in GitHub as the source of truth.

## Source Inputs

Local project materials used for this repository update include:

- Gap analysis workbook: `SampleManager_Pharma_Template_to_Global_Pharma_Solution_Gap_Analysis.xlsx`
- Requirements baseline: `制药实验室LIMS需求基线说明书.docx`
- P1 high-level design: `P1阶段概要设计_样品主流程_容器替代方案_留样管理_V1.2.docx`
- P1 detail design: `P1详细设计_请验单_LoginPlan模板_V1.0.docx`
- Pharma Solution package inspection: `Pharma.Solution.2.5.SM.21.3.zip`
- LPN package inspection: `Laboratory Process Navigation (ENH) 2.3` package materials

## Target Instance

The remote development and validation instance is documented in `docs/samplemanager-instance.md`.

Observed instance:

- Product: SampleManager LIMS 21.3.0.0
- Instance: `VGSM`
- Root path: `C:\Thermo\SampleManager\Server\VGSM`
- Solution root: `C:\Thermo\SampleManager\Server\VGSM\Solution`
- Runtime binary path: `C:\Thermo\SampleManager\Server\VGSM\Exe`
- Log path: `C:\Thermo\SampleManager\Server\VGSM\Logfile`

## Design Documents

- `project-purpose.md` - business purpose and guiding principles
- `gap-analysis-summary.md` - PT to GPS/EM gap summary
- `p1-design-overview.md` - P1 sample lifecycle and retain sample design overview
- `inspection-request-login-plan-design.md` - Login Plan based inspection request design
- `implementation-roadmap.md` - staged implementation plan
- `artifact-index.md` - local deliverable index
