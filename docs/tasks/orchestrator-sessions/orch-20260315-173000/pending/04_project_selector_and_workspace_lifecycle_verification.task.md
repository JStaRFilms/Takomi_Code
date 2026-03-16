# Task: Project Selector And Workspace Lifecycle Verification

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-debug`  
**Dependencies:** `02_visual_shell_recovery`, `03_feature_test_matrix`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi debug/orchestrator workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-debug.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-orchestrator.md`

### Prime Agent Context
Use the current WinUI app as the implementation baseline and compare it against:
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\features\Project_Selector.md`
- `C:\CreativeOS\01_Projects\Code\Personal_Stuff\2026-03-10_Takomi_Code\docs\tasks\orchestrator-sessions\orch-20260310-223000\master_plan.md`

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `sync-docs` | Record verified behavior and defects cleanly |
| `avoid-feature-creep` | Prevent selector validation from turning into a redesign |

## Objective

Verify that the project selector and workspace-open lifecycle behave truthfully end to end:

1. recent workspace selection opens the correct project
2. workspace metadata shown in the shell matches the selected project
3. selector state transitions cleanly into the main shell
4. non-implemented actions are clearly disabled or explicitly deferred

## Context

- The selector surface has changed shape several times during this session.
- The old feature definition describes initialize-folder and clone flows, but those are not trusted as implemented yet.
- Later tasks depend on workspace selection being stable before session/runtime/orchestration behavior is judged.

## Required Outputs

1. verify recent workspace open behavior
2. verify what happens when the app launches with no explicitly opened project
3. classify `Initialize Folder` and `Clone Repository` as working, disabled, or misleading
4. confirm that workspace path, runtime target, and project label update correctly after project open
5. record any mismatch between selector UI state and the actual selected workspace object

## Definition Of Done

- [ ] Opening a recent workspace is tested and result recorded
- [ ] Selector-to-shell transition is tested and result recorded
- [ ] Workspace metadata rendering is validated against actual state
- [ ] Stubbed selector actions are clearly classified
- [ ] Follow-up defects are written down for the repair loop

## Constraints

- Do not redesign the selector in this task.
- Do not silently assume disabled actions are acceptable unless the UI makes that clear.
- Keep this task focused on workspace selection and shell entry, not chat behavior.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/04_project_selector_and_workspace_lifecycle_verification.md`

## Recommended Next Task

After selector behavior is verified, proceed to `05_chat_sub_session_and_transcript_verification`.
