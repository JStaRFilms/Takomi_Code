# Task: Feature Test Matrix

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-architect`  
**Dependencies:** `02_visual_shell_recovery`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi orchestrator/architecture workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-orchestrator.md`

### Prime Agent Context
Use the current runnable WinUI shell as the implementation baseline. Do not assume every visible control is meant to remain interactive.

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `spawn-task` | Future follow-up fix tasks must be self-contained |
| `avoid-feature-creep` | Prevent testing from turning into a redesign |

## Objective

Convert the current runnable UI into a concrete manual test matrix so the next validation tasks can distinguish:

1. features that must work now
2. demo-safe surfaces that may be acceptable if clearly scoped
3. visible controls that should be disabled or removed if they are not implemented

## Context

- Visual shell recovery is now far enough along that feature validation can proceed.
- The current WinUI implementation includes both real command-backed controls and static/decorative controls.
- The repository did not yet contain a dedicated Task `03` artifact, so this task establishes the functional verification baseline.

## Required Outputs

1. a matrix of every major visible UI surface and control
2. an execution order for manual verification
3. a bucket for each control: `must work`, `demo-safe`, or `hide/disable`
4. explicit notes on likely stubbed controls so later tasks do not waste time

## Definition Of Done

- [ ] A feature test matrix file exists for this session
- [ ] The matrix covers project selector, shell navigation, sessions, worktrees, billing, Bags, settings, and overlays
- [ ] Each major visible control is bucketed
- [ ] A practical manual execution order is defined
- [ ] Task `04` can begin directly from this artifact

## Constraints

- Do not redesign the UI in this task.
- Do not silently treat static mockup chrome as implemented functionality.
- Keep the matrix grounded in current code and currently visible UI, not intended future scope.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/feature_test_matrix.md`

## Recommended Next Task

After the matrix is accepted, proceed to `04_chat_and_session_flow_validation`.
