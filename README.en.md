# samplemanager-NewPharma-dev

Language / 语言: [简体中文](README.md) | English

## Project Positioning

`samplemanager-NewPharma-dev` is the main engineering workspace for NewPharma
customization, design capture, and delivery validation on Thermo Scientific
SampleManager LIMS. It is not a temporary folder for one isolated feature; it is
intended to host multiple NewPharma business themes over time.

## Background And Intent

The project started from adapting existing Pharma Solution and Login Plan
capabilities into maintainable NewPharma-specific workflows. The repository is
the source of truth for local code, configuration, structure files, and design
notes, which are then deployed to a SampleManager instance for validation.

It stores NewPharma-owned work, design decisions, configuration deltas, and
deployment helpers. It does not store vendor template source code, full customer
system exports, or sensitive connection details.

## Current Theme: Inspection Request

The current development focus is Inspection Request (IR). IR is the first
business theme in the wider NewPharma roadmap, not the whole scope of this
repository. Its purpose is to turn inspection request handling from simple table
maintenance into a configurable, lifecycle-driven, executable business entity.

The current IR objectives are:

- Create IR records through a dedicated workflow login.
- Manage IR header properties through an IR-owned entity template.
- Load Data Assignment from the selected Login Plan and store it as an
  IR-owned snapshot.
- Load Product Selection / Product Spec information and store it as an
  IR-owned snapshot.
- Allow request-level adjustments without changing the source Login Plan.
- Support IR creation from Lot or Job context.
- Use the IR snapshot to generate downstream Job, Sample, Test, and related
  objects during execution.

## Future Themes

After IR, this repository can host additional NewPharma business themes. Each
new theme should keep its design notes, configuration deltas, schema changes,
forms, and code boundaries as independent as practical, instead of growing one
large mixed feature.

## Information Boundaries

The public README should describe project purpose, business scope, repository
layout, and development principles. Server addresses, SSH configuration,
instance paths, accounts, deployment keys, runtime logs, and temporary exports
should not be placed here. Keep them in the local environment, private notes, or
directories excluded by `.gitignore`.

## Repository Layout

```text
docs/
  project/                         Project-level architecture and decisions
  topics/
    inspection-request/             IR-specific design and lifecycle notes
  operations/                       Build, deploy, validation, and DLL notes
  private/                          Local-only instance notes, ignored by git

samplemanager/
  structure/                        SampleManager table/structure fragments
  config/                           Table-loader CSV configuration
  forms/                            Form Designer XML definitions
  sql/                              SQL setup and repair scripts

src/
  NewPharma.InspectionRequest/      IR task assembly and form/runtime logic
  NewPharma.InspectionRequest.Workflow/
                                    IR workflow node assembly

scripts/                            Local packaging, deployment, and log tools
out/                                Generated packages, ignored by git
tmp-*/                              Temporary analysis folders, ignored by git
```

## Working Principles

- The local repository is the source of truth for design and code.
- The SampleManager instance is used for runtime validation, not as the source
  repository.
- Each business theme should capture its purpose, design, data model, UI,
  workflow, and deployment notes.
- Vendor template source and sensitive connection details should stay out of
  the public repository.
- Changes should be committed in small, understandable steps with a clear
  business or technical purpose.
