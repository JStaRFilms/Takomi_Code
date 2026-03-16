# Task: Consolidated Repair Loop

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-code`  
**Dependencies:** `04_project_selector_and_workspace_lifecycle_verification`, `05_chat_sub_session_and_transcript_verification`, `06_codex_runtime_and_streaming_verification`, `07_orchestration_engine_and_task_graph_verification`, `08_intervention_controls_verification`, `09_git_and_worktree_verification`, `10_mode_and_workflow_loader_verification`, `11_bags_verification_flow_audit`, `12_persistence_and_restart_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi continue-build workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\vibe-continueBuild.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-code.md`

### Prime Agent Context
Use all accepted verification artifacts from Tasks `04` through `12` as the repair backlog. Only fix issues that were actually observed and recorded.

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `spawn-task` | Any overflow work should be split cleanly, not stuffed into this loop |
| `sync-docs` | Keep docs aligned with repaired behavior |

## Objective

Fix the verified issues from the feature-validation wave in a controlled pass:

1. implement the highest-signal repairs first
2. disable or remove misleading controls if full implementation is not justified
3. keep the UI truthful to the actual feature set
4. leave the app in a state ready for the final validation gate

## Required Outputs

1. a prioritized fix list derived from completed validation tasks
2. code changes for the approved repairs
3. updated docs/artifacts reflecting what changed
4. a short build and smoke-test summary

## Definition Of Done

- [ ] Verified defects are triaged by severity and product importance
- [ ] High-priority fixes are implemented
- [ ] Misleading controls are disabled or made truthful where implementation is deferred
- [ ] Build verification is run
- [ ] Follow-up items are clearly separated from completed repairs

## Constraints

- Do not introduce broad new features here.
- Keep this loop grounded in verified defects, not speculative improvements.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/13_consolidated_repair_loop.md`

## Recommended Next Task

After repairs are complete, proceed to `14_final_validation_gate`.
