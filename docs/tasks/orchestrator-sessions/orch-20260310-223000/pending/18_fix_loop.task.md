# Task: Fix Loop

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Primary Mode:** `vibe-debug`  
**Follow-On Mode:** `vibe-code`  
**Dependencies:** `17_quality_gate`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-debug.yaml`
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/mode-debug.md`
- Read `.agent/workflows/vibe-continueBuild.md`
- Read `.agent/workflows/vibe-build.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Investigate every blocking finding from the quality gate, fix root causes, and rerun verification until the build is clean.

## Definition of Done

- [ ] Every blocking finding has a diagnosis or fix
- [ ] Fixes are implemented with root-cause reasoning
- [ ] Verification is rerun after the fixes
- [ ] Remaining known issues are documented explicitly

## Expected Artifacts

- updated source and docs
- fix summary attached to the completed result

## Constraints

- Do not patch symptoms without diagnosis.
- Preserve auditability and scope boundaries while fixing.
