# Task: Mode and Workflow Loader

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-code`  
**Dependencies:** `07_solution_scaffold`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-build.md`
- Read `.agent/workflows/mode-code.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement runtime loading for Takomi mode YAML files and workflow Markdown files, plus normalized internal definitions.

## Definition of Done

- [ ] Mode YAML discovery and parsing works
- [ ] Workflow markdown discovery and parsing works
- [ ] Internal normalized objects exist for modes and workflows
- [ ] Parse failures surface clear diagnostics

## Expected Artifacts

- parser and loader code under `src/`
- associated models and tests if feasible

## Constraints

- Keep loaded definitions data-driven.
- Do not hard-code Takomi behavior that already exists in `.agent`.
