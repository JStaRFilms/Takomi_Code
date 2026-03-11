# Task: Chat and Session Core

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-code`  
**Dependencies:** `07_solution_scaffold`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-build.md`
- Read `.agent/workflows/vibe-continueBuild.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement the core project, chat, and sub-chat session model with transcript persistence and restore behavior.

## Definition of Done

- [x] Project-scoped chats can be created
- [x] Child sessions can be linked to parent sessions
- [x] Transcript state persists and restores
- [x] Same-workspace inheritance is preserved by default

## Expected Artifacts

- session models, persistence logic, and UI state bindings under `src/`

## Constraints

- Workspace switching must remain explicit, not automatic.
- Parent-child relationships must be queryable for orchestration features.
