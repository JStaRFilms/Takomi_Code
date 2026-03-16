# Task: Orchestration Engine And Task-Graph Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-debug`  
**Dependencies:** `05_chat_sub_session_and_transcript_verification`, `06_codex_runtime_and_streaming_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug and orchestrator workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-orchestrator.md`

### Prime Agent Context
Use these as the implementation references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\orchestrator-execution-engine.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\11_orchestrator_execution_engine.result.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Record which orchestration behaviors are real |
| `avoid-feature-creep` | Keep this on verification, not speculative feature design |

## Objective

Verify whether the app's orchestration layer behaves like a real engine or only like surface chrome:

1. orchestration sessions can be created or inferred from user actions
2. runs/tasks are persisted in a coherent way
3. task graph and active-run surfaces reflect real state changes
4. artifacts/result-linkage behavior is truthful
5. parent/child run relationships are visible or at least durably stored

## Context

- The old Task `11` implemented an orchestration repository and engine.
- This is one of the most important product-spine tasks because it decides whether Takomi is only chat-plus-runtime or a real orchestration shell.
- UI truth matters here: fake task graph chrome should not be mistaken for actual orchestration state.

## Required Outputs

1. verify how orchestration sessions are created today
2. verify whether task/run state is persisted and surfaced accurately
3. verify whether task graph UI corresponds to real engine data
4. verify whether artifact/result paths are attached truthfully
5. classify the orchestration surface as working, partial, or largely stubbed

## Definition Of Done

- [ ] Orchestration session creation path is documented
- [ ] Run/task persistence is tested or directly inspected
- [ ] Task graph truthfulness is classified
- [ ] Artifact/result linkage is checked
- [ ] Major gaps are written down in a way that can drive repair work

## Constraints

- Do not redesign the orchestration surface in this task.
- Do not assume the presence of a panel means the engine is wired to it.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/07_orchestration_engine_and_task_graph_verification.md`

## Recommended Next Task

After orchestration behavior is classified, proceed to `08_intervention_controls_verification`.
