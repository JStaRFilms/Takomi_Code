# Task: Intervention Controls

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-code`  
**Dependencies:** `11_orchestrator_execution_engine`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`
- Read `.agent/Takomi-Agents/vibe-debug.yaml`

### Workflow to Follow
- Read `.agent/workflows/mode-orchestrator.md`
- Read `.agent/workflows/mode-debug.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement the live intervention model for active runs and expose it in the shell.

## Definition of Done

- [ ] Inject guidance, pause, resume, cancel, reroute, replace, and migrate actions exist
- [ ] Actions are available for active runs in the UI
- [ ] Interventions emit structured audit events
- [ ] Unsupported actions fail with clear UI feedback

## Expected Artifacts

- intervention command models and handlers
- UI bindings for intervention actions

## Constraints

- Do not treat interventions as transient UI state only.
- Preserve event history for every intervention.
