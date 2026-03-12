# Task: Design Review Loop

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-ask`  
**Dependencies:** `05_mockup_prototypes`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-ask.yaml`

### Workflow to Follow
- Read `.agent/workflows/mode-ask.md`
- Read `.agent/workflows/vibe-design.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `webapp-testing`

### Forbidden Skills
- `context7`

## Objective

Review the HTML mockups as a read-only design critic and produce a refinement checklist plus an explicit build gate decision.

## Scope

### In Scope
- mockup usability review
- information hierarchy review
- desktop ergonomics review
- signoff checklist for build readiness

### Out of Scope
- editing the mockups
- implementing WinUI code

## Definition of Done

- [x] Review notes exist under `docs/design/`
- [x] A refinement checklist is recorded
- [x] The output clearly states approved or blocked
- [x] Any blockers are mapped to specific mockup files

## Expected Artifacts

- `docs/design/mockup_review.md`

## Constraints

- This is a read-only review task.
- Build work remains blocked until approval is explicit.
