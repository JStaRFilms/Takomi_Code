# Task: Cloud Runtime

**Session ID:** orch-20260310-223000  
**Priority:** P1  
**Mode:** `vibe-code`  
**Dependencies:** `10_codex_adapter_runtime`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-build.md`
- Read `.agent/workflows/mode-code.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement the cloud execution runtime so the shell can switch between local and cloud without changing orchestration contracts.

## Definition of Done

- [x] Shared runtime contracts are enforced across local and cloud paths
- [x] Cloud runtime requests and event handling exist
- [x] Runtime target switching is visible and persisted
- [x] Artifact and event shapes match the local runtime

## Expected Artifacts

- cloud runtime service code
- runtime target settings and persistence

## Constraints

- Match local runtime semantics as closely as possible.
- Avoid embedding cloud-specific assumptions into the shell core.
