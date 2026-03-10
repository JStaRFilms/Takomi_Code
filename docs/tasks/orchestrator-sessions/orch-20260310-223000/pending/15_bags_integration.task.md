# Task: Bags Integration

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
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement Bags token linkage, Bags API interactions, and verification-readiness tracking for projects.

## Definition of Done

- [ ] A project can store Bags token linkage metadata
- [ ] Planned Bags API calls are implemented behind a service boundary
- [ ] Verification readiness is visible in the UI
- [ ] Bags-related state changes are logged

## Expected Artifacts

- Bags integration services and models
- verification-ready UI and persistence updates

## Constraints

- Explicitly exclude fee-sharing implementation.
- Keep Bags logic isolated from general shell UI code.
