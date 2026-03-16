# Task: Chat, Sub-Session, And Transcript Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-debug`  
**Dependencies:** `04_project_selector_and_workspace_lifecycle_verification`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`

### Prime Agent Context
Use these as the implementation references:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\chat-session-core.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\completed\09_chat_and_session_core.result.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Capture verified flows and remaining defects |

## Objective

Verify that the chat/session foundation really works:

1. blank draft sessions do not pollute saved state
2. a session becomes durable only when a message is actually sent
3. session selection updates the transcript correctly
4. child/sub-sessions can be created and inherit the expected workspace context
5. transcript restoration and rendering are stable

## Context

- The old Task `09` established the persisted chat/session model.
- This session already fixed several empty-session and transcript issues, but that behavior now needs a clean verification task.
- The user specifically cares about sub-sessions behaving correctly and the UI updating truthfully.

## Required Outputs

1. verify root session creation rules
2. verify child/sub-session creation rules
3. verify transcript save and restore behavior
4. verify session list ordering and naming behavior
5. capture any UI-state bugs where the selected session and visible transcript diverge

## Definition Of Done

- [ ] Empty draft sessions are tested and classified
- [ ] Root session creation is verified
- [ ] Child/sub-session creation is verified
- [ ] Transcript save/render behavior is verified
- [ ] Session-selection UI state is verified

## Constraints

- Do not broaden this task into runtime execution semantics beyond confirming that sending creates durable chat state.
- Do not redesign transcript styling in this task.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/05_chat_sub_session_and_transcript_verification.md`

## Recommended Next Task

After chat/session behavior is verified, proceed to `06_codex_runtime_and_streaming_verification`.
