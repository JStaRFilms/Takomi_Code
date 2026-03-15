# Task: Intake And Baseline Capture

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-orchestrator`  
**Dependencies:** none

## Objective

Freeze the current runnable state of the application so the rest of the testing/polish session works from a shared baseline instead of impressions.

## Context

- The WinUI app now opens successfully in Visual Studio 2022.
- The startup project was initially mis-set to `TakomiCode.UI.Package` and corrected to `TakomiCode.UI`.
- `TakomiCode.UI.csproj` was adjusted to use self-contained Windows App SDK packaging so the app can find its WinUI runtime dependencies.
- The application is now runnable, but the visual shell does not resemble the approved mockups closely enough.
- A screenshot from the live app is now the current baseline artifact for comparison and planning.

## Required Outputs

1. confirm the runnable baseline and active startup configuration
2. record the screenshot-backed visual mismatch against the mockups
3. identify any repo-local changes introduced by Visual Studio or local runs
4. document the exact build/test state available at the beginning of this session
5. explicitly approve or reject moving into mockup-parity recovery

## Definition Of Done

- [ ] Startup project and active platform/runtime assumptions are documented
- [ ] Current build/test signal is recorded
- [ ] Visual mismatch against mockups is acknowledged explicitly
- [ ] Repo-local noise is separated from real product changes
- [ ] Next task recommendation is explicit

## Constraints

- Do not fix visual issues in this intake task.
- Do not broaden scope into feature redesign yet.
- Treat the screenshot and current runnable shell as evidence, not as the new source of truth.

## Recommended Next Task

If intake is complete, proceed to `01_mockup_parity_audit`.
