# Task: Codex Runtime And Streaming Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-debug`  
**Dependencies:** `05_chat_sub_session_and_transcript_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use these as the runtime references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\codex-runtime-adapter.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\10_codex_adapter_runtime.result.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\src\TakomiCode.RuntimeAdapters\Codex\CodexSdkAdapter.cs`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\src\TakomiCode.RuntimeAdapters\Codex\WorkspaceAwareCodexRuntimeAdapter.cs`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Record runtime truth instead of old assumptions |

## Objective

Verify the real runtime path from the session UI through Codex execution:

1. `Route` starts a real run
2. run state becomes visible in the UI
3. streaming/progress updates appear during execution
4. final assistant output is written back into the same session
5. follow-up turns retain enough context to behave like the same working thread

## Context

- The old session implemented a CLI adapter; the current app has evolved to an SDK-backed adapter.
- This task is about actual observable runtime behavior, not just contract presence.
- The user already confirmed partial success here, but we need a formal verification record.

## Required Outputs

1. verify a successful run from prompt to final response
2. verify progress/status streaming behavior during the run
3. verify failure behavior when runtime execution fails
4. verify that a second turn in the same session behaves as a continuation, not a cold reset
5. record any remaining UX truthfulness issues in the transcript or inspector

## Definition Of Done

- [ ] Route-to-run behavior is verified
- [ ] Streaming/progress behavior is verified
- [ ] Final output persistence is verified
- [ ] Multi-turn continuation is tested
- [ ] Runtime error handling is at least smoke-tested or clearly flagged as unverified

## Constraints

- Do not redesign the transcript in this task unless a bug prevents verification.
- Do not broaden into orchestration task-graph behavior yet; stay on direct runtime execution.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/06_codex_runtime_and_streaming_verification.md`

## Recommended Next Task

After direct runtime behavior is verified, proceed to `07_orchestration_engine_and_task_graph_verification`.
