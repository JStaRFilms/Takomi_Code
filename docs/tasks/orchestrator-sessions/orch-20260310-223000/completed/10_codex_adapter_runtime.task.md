# Task: Codex Adapter Runtime

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
- Read `.agent/workflows/mode-debug.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement the local Codex adapter runtime and isolate Windows-specific execution and fallback behavior behind the runtime boundary.

## Definition of Done

- [x] A local runtime adapter can start and monitor Codex-backed runs ✅ Completed
- [x] Runtime state transitions are emitted as structured events ✅ Completed
- [x] Failure handling is implemented for missing CLI, auth issues, and execution failures ✅ Completed
- [x] Windows-specific compatibility handling is adapter-local ✅ Completed

## Expected Artifacts

- runtime adapter interfaces and local runtime implementation
- runtime event models

## Constraints

- The UI must not own process orchestration details.
- Keep the door open for WSL mediation without changing shell contracts.
