# Task: Intervention Controls Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P1`  
**Mode:** `vibe-debug`  
**Dependencies:** `07_orchestration_engine_and_task_graph_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use these as the implementation references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\intervention-controls.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\12_intervention_controls.result.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Capture which interventions are real versus unsupported |

## Objective

Verify the truthfulness of intervention controls for active runs:

1. determine which intervention actions are truly supported
2. verify that supported actions actually affect run state
3. verify that unsupported actions fail clearly and are not presented as fully working
4. verify that intervention audit/state changes are durable if exposed in the UI

## Context

- The old task modeled a broad intervention surface, but even its own feature doc admits most actions were unsupported at runtime.
- This task decides whether these controls should remain active, become disabled, or require real implementation.

## Required Outputs

1. test cancel behavior against a real active run if feasible
2. test pause/resume/reroute/guidance/replace/migrate truthfulness
3. classify each visible intervention control as `working`, `unsupported but clear`, or `misleading`
4. record what the UI should hide or disable if support is incomplete

## Definition Of Done

- [ ] Visible intervention controls are inventoried
- [ ] At least one real intervention path is tested
- [ ] Unsupported actions are identified explicitly
- [ ] Truthfulness of shell feedback is recorded

## Constraints

- Do not force-enable unsupported intervention paths.
- Do not treat modeled commands as shipped behavior unless runtime proof exists.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/08_intervention_controls_verification.md`

## Recommended Next Task

After intervention truthfulness is established, proceed to `09_git_and_worktree_verification`.
