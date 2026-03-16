# Task: Bags Verification Flow Audit

**Session ID:** `orch-20260315-173000`  
**Priority:** `P2`  
**Mode:** `vibe-debug`  
**Dependencies:** `04_project_selector_and_workspace_lifecycle_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use these as the implementation references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\bags-integration-and-verification.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\15_bags_integration.result.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Record demo-safe boundaries clearly |

## Objective

Verify the Bags flow truthfully without over-prioritizing it:

1. token-link state persists correctly
2. readiness check updates UI state correctly
3. the feature is clearly framed as demo-safe if it is not a live integration

## Context

- Billing is deferred, but Bags still appears in product scope and old-session docs.
- This task is intentionally lower priority than the core runtime/orchestration spine.

## Required Outputs

1. verify token link behavior
2. verify readiness-check behavior
3. verify persistence across basic navigation or restart if feasible
4. classify the flow as `working demo`, `broken demo`, or `misleading`

## Definition Of Done

- [ ] Token link flow is tested or directly classified
- [ ] Readiness behavior is tested or directly classified
- [ ] Demo-safe framing is recorded

## Constraints

- Do not spend major time here if it blocks higher-value core features.
- Do not reintroduce billing scope through this task.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/11_bags_verification_flow_audit.md`

## Recommended Next Task

After this lower-priority audit, proceed to `12_persistence_and_restart_verification`.
