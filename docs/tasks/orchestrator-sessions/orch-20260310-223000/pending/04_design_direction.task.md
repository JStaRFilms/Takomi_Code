# Task: Design Direction

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-architect`  
**Dependencies:** `01_product_genesis_brief`, `02_architecture_genesis`, `03_issue_and_task_seed`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-architect.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-design.md`
- Read `.agent/workflows/mode-architect.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `ui-ux-pro-max`

### Forbidden Skills
- `context7`

## Objective

Produce the Windows-native design direction for Takomi Code, aligned to the PRD and architecture, before any mockups are generated.

## Scope

### In Scope
- desktop information architecture
- shell navigation model
- core interaction patterns for projects, sessions, task tree, worktree, billing, and Bags verification
- visual direction and UI principles

### Out of Scope
- final WinUI implementation
- HTML prototype generation

## Definition of Done

- [ ] A design direction document exists under `docs/design/`
- [ ] Navigation and panel model are explicitly defined
- [ ] The document addresses same-workspace defaults and explicit worktree switching
- [ ] The document includes design rules for background child execution and intervention visibility

## Expected Artifacts

- `docs/design/design_direction.md`

## Constraints

- Keep the design unmistakably desktop-native.
- Do not introduce fee-sharing surfaces.
- Keep HTML prototypes blocked until this task is complete.
