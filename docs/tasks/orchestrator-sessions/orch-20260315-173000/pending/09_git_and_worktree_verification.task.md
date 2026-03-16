# Task: Git And Worktree Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P1`  
**Mode:** `vibe-debug`  
**Dependencies:** `05_chat_sub_session_and_transcript_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use these as the implementation references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\git-and-worktree-controls.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\13_git_and_workspace_controls.result.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `git-worktree` | Ground worktree validation in real Git behavior |
| `sync-docs` | Capture actual shell-vs-git behavior |

## Objective

Verify the Git/worktree feature set end to end:

1. current branch and dirty-state indicators are truthful
2. worktree creation either works or is clearly disabled
3. worktree switching updates the selected session subtree correctly
4. unrelated sessions are not silently rewritten

## Context

- The old task implemented the service layer and claimed shell bindings.
- This area has not yet been verified live in a trustworthy way.
- The user specifically wants meaningful orchestration/workspace behavior, so fake worktree chrome is not acceptable.

## Required Outputs

1. verify branch and dirty-state rendering
2. verify create-worktree behavior
3. verify switch-worktree behavior
4. verify session-subtree propagation rules
5. classify any remaining worktree buttons as working, partial, or misleading

## Definition Of Done

- [ ] Branch visibility is tested against actual Git state
- [ ] Dirty-state visibility is tested against actual Git state
- [ ] Create-worktree flow is tested or clearly marked blocked
- [ ] Switch-worktree flow is tested or clearly marked blocked
- [ ] Session propagation behavior is verified

## Constraints

- Do not create destructive Git changes.
- If a real worktree operation would materially disrupt the user’s current workspace, stop and document the blocker rather than improvising.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/09_git_and_worktree_verification.md`

## Recommended Next Task

After Git/worktree behavior is classified, proceed to `10_mode_and_workflow_loader_verification`.
