# Task: Mockup Prototypes

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-architect`  
**Dependencies:** `04_design_direction`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-architect.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-design.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `frontend-design`
- `ui-ux-pro-max`

### Forbidden Skills
- `context7`

## Objective

Create reviewable HTML prototypes and the supporting design-system artifacts for the core Takomi Code shell surfaces.

## Scope

### In Scope
- `docs/design/design-system.html`
- `docs/mockups/project-selector.html`
- `docs/mockups/orchestrator-home.html`
- `docs/mockups/session-workspace.html`
- `docs/mockups/task-tree-monitor.html`
- `docs/mockups/worktree-manager.html`
- `docs/mockups/intervention-controls.html`
- `docs/mockups/settings-runtime.html`
- `docs/mockups/billing-verification.html`

### Out of Scope
- production XAML
- billing integration logic
- Codex runtime logic

## Definition of Done

- [x] All required prototype pages exist
- [x] The design system file captures palette, typography, and shell components
- [x] Prototypes reflect the approved design direction
- [x] The prototypes visibly include branch, workspace, runtime, and task-state cues

## Expected Artifacts

- `docs/design/design-system.html`
- `docs/mockups/*.html`

## Constraints

- HTML is the review format, not the shipping UI technology.
- Design for WinUI implementation fidelity, not generic web layouts.
- No prototype may imply fee-sharing functionality.
