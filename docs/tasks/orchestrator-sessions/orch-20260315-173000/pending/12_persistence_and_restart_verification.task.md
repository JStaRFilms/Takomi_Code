# Task: Persistence And Restart Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-debug`  
**Dependencies:** `05_chat_sub_session_and_transcript_verification`, `06_codex_runtime_and_streaming_verification`, `07_orchestration_engine_and_task_graph_verification`, `09_git_and_worktree_verification`, `10_mode_and_workflow_loader_verification`, `11_bags_verification_flow_audit`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use the current repositories and persisted stores as the verification surface:
- chat sessions
- orchestration store
- workspace store
- runtime target state
- any visible shell restoration behavior after restart

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Persistence outcomes must be recorded precisely |

## Objective

Verify which parts of the app survive restart correctly:

1. selected workspace
2. saved sessions and transcripts
3. sub-session relationships
4. runtime target
5. Bags/workspace metadata
6. orchestration/run surfaces, if they claim persisted state

## Context

- Several old tasks introduced durable JSON stores.
- Restart behavior is the fastest way to distinguish real product state from temporary UI state.

## Required Outputs

1. restart the app and record what restores correctly
2. verify whether the correct workspace reopens or must be reselected
3. verify session and transcript restoration
4. verify persisted runtime target and workspace metadata
5. verify whether orchestration state survives meaningfully or only cosmetically

## Definition Of Done

- [ ] Workspace restoration is verified
- [ ] Session/transcript restoration is verified
- [ ] Runtime target restoration is verified
- [ ] Additional persisted metadata is verified
- [ ] Persistence gaps are documented clearly

## Constraints

- Keep this focused on restart and durable state, not fresh-run behavior already covered in earlier tasks.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/12_persistence_and_restart_verification.md`

## Recommended Next Task

After restart behavior is verified, proceed to `13_consolidated_repair_loop`.
