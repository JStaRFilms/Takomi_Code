# Task: Git and Workspace Controls

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-code`  
**Dependencies:** `09_chat_and_session_core`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-build.md`
- Read `.agent/workflows/vibe-continueBuild.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `git-worktree`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement branch visibility, dirty-state cues, and explicit worktree creation/switching for the active session subtree.

## Definition of Done

- [ ] Current branch and dirty state are visible in the shell
- [ ] Users can create and switch worktrees from a session
- [ ] Session subtree inheritance updates correctly after switching
- [ ] Workspace changes generate audit events

## Expected Artifacts

- git state services and UI bindings
- worktree command flow

## Constraints

- Same workspace must remain the default behavior.
- Worktree switching cannot silently affect unrelated sessions.
