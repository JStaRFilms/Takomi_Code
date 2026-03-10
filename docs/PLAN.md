# Takomi Code Orchestrator Session Plan v0.001

## Summary
- Revise the product scope to remove all `fee sharing` work. Bags scope is now: Bags token, Bags API usage, project verification readiness, and related auditability. Paystack stays for fiat billing.
- Use the Takomi skill as the orchestration source of truth, with `vibe-genesis` as the first execution phase and the full Takomi lifecycle preserved: Genesis -> Design -> Build -> Continue Build -> Review -> Finalize.
- The orchestrator session must generate the full planning and delivery system, not just app code: PRD, architecture docs, issue/task breakdown, design artifacts, mockups, build tasks, review tasks, recovery paths, and final handoff artifacts.

## Orchestrator Rules
- Default mode is `vibe-orchestrator`. `vibe-visionary` is optional only for vague future ideation, not required for this session.
- Every sub-agent must read:
  - the assigned mode YAML from `.agent/Takomi-Agents/`
  - the assigned workflow Markdown from `.agent/workflows/`
  - `vibe-primeAgent` before doing work
- Mandatory skill injection policy:
  - Always inject `takomi`
  - Never inject `context7`
  - Only inject additional skills that directly match the task
- Global task constraints:
  - preserve the Windows-native WinUI 3 + C# architecture
  - preserve the Codex adapter-first runtime strategy
  - preserve same-workspace-by-default chat behavior, with optional explicit worktree switching
  - preserve local + cloud runtime scope for v0.001
  - preserve audit logging for runs, interventions, billing, and verification events

## Session Breakdown
### Wave 0: Orchestrator Intake
- Task `00_orchestrator_intake`
- Mode: `vibe-orchestrator`
- Read:
  - `mode-orchestrator.md`
  - `vibe-genesis.md`
  - `vibe-design.md`
  - `vibe-build.md`
  - `vibe-continueBuild.md`
  - `vibe-finalize.md`
  - all Takomi mode YAMLs
- Skills:
  - `takomi`
  - `avoid-feature-creep`
- Output:
  - skills registry excluding `context7`
  - workflows registry
  - session `master_plan`
  - dependency graph for all downstream tasks

### Wave 1: Genesis
- Task `01_product_genesis_brief`
- Mode: `vibe-architect`
- Read:
  - `vibe-genesis.md`
  - `mode-architect.md`
  - `vibe-primeAgent.md`
- Skills:
  - `takomi`
  - `spawn-task`
  - `avoid-feature-creep`
- Output:
  - complete `docs/Project_Requirements.md`
  - explicit MUS / Future feature set
  - updated product constraints with fee sharing removed
  - exact acceptance gates for hackathon compliance

- Task `02_architecture_genesis`
- Mode: `vibe-architect`
- Read:
  - `vibe-genesis.md`
  - `mode-architect.md`
- Skills:
  - `takomi`
  - `spawn-task`
- Output:
  - system architecture doc for WinUI shell, local/cloud runtime adapter, persistence, Git/worktree model, payments, Bags integration, and audit log

- Task `03_issue_and_task_seed`
- Mode: `vibe-architect`
- Read:
  - `vibe-genesis.md`
  - `vibe-spawnTask.md`
- Skills:
  - `takomi`
  - `spawn-task`
  - `github-ops` only if GitHub issue export is explicitly prepared
- Output:
  - one issue per FR
  - orchestrator-ready task files for all core build streams
  - clear DoD and dependencies for each task

### Wave 2: Design
- Task `04_design_direction`
- Mode: `vibe-architect`
- Read:
  - `vibe-design.md`
  - `mode-architect.md`
- Skills:
  - `takomi`
  - `ui-ux-pro-max`
- Output:
  - Windows-native design brief
  - navigation model
  - visual language
  - desktop-specific interaction patterns for multi-project and multi-chat workflows

- Task `05_mockup_prototypes`
- Mode: `vibe-architect`
- Read:
  - `vibe-design.md`
  - `vibe-primeAgent.md`
- Skills:
  - `takomi`
  - `frontend-design`
  - `ui-ux-pro-max`
- Output:
  - HTML prototypes for key screens before build:
    - project/workspace picker
    - orchestrator home
    - multi-chat workspace
    - task tree / child-run monitor
    - branch/worktree switcher
    - intervention controls
    - settings / runtime configuration
    - billing / verification screens
  - `docs/design/design-system.html`
  - `docs/mockups/*.html`

- Task `06_design_review_loop`
- Mode: `vibe-ask`
- Read:
  - `mode-ask.md`
  - `vibe-design.md`
- Skills:
  - `takomi`
  - `webapp-testing`
- Output:
  - review notes on the HTML mockups
  - refinement checklist before build starts
  - explicit signoff gate: no Build wave starts until mockups are approved

### Wave 3: Build Foundation
- Task `07_solution_scaffold`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
  - `vibe-primeAgent.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - WinUI 3 solution scaffold
  - project layering
  - MVVM base
  - runtime adapter contracts
  - local persistence setup
  - audit/event pipeline foundation

- Task `08_mode_and_workflow_loader`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - runtime loader for `.agent/Takomi-Agents/*.yaml`
  - runtime loader for `.agent/workflows/*.md`
  - internal normalized definitions for modes, workflows, stages, and prompts

- Task `09_chat_and_session_core`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `vibe-continueBuild.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - project tabs
  - chat tabs
  - sub-chat hierarchy
  - transcript persistence
  - session restore
  - same-workspace inheritance and explicit worktree switching

- Task `10_codex_adapter_runtime`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
  - `mode-debug.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - background Codex process adapter
  - local execution path
  - Windows fallback strategy if WSL mediation is required
  - run lifecycle events and failure handling

### Wave 4: Orchestration Features
- Task `11_orchestrator_execution_engine`
- Mode: `vibe-code`
- Read:
  - `mode-orchestrator.md`
  - `vibe-spawnTask.md`
  - `vibe-continueBuild.md`
- Skills:
  - `takomi`
  - `spawn-task`
  - `sync-docs`
- Output:
  - parent/child run tree
  - dependency tracking
  - background child execution
  - nested orchestration
  - artifact/result recording

- Task `12_intervention_controls`
- Mode: `vibe-code`
- Read:
  - `mode-orchestrator.md`
  - `mode-debug.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - inject guidance
  - pause
  - resume
  - cancel
  - reroute
  - replace
  - migrate context

- Task `13_git_and_workspace_controls`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `vibe-continueBuild.md`
- Skills:
  - `takomi`
  - `git-worktree`
  - `sync-docs`
- Output:
  - current branch display
  - dirty-state indicators
  - create/switch worktree flow
  - session inheritance after workspace switch

### Wave 5: Payments, Bags, Cloud
- Task `14_billing_and_entitlements`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - Paystack billing flow
  - entitlement checks
  - billing event logging

- Task `15_bags_integration`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - Bags token linkage
  - Bags API integration
  - verification-ready project records
  - Bags-related event logging
  - explicitly no fee-sharing implementation

- Task `16_cloud_runtime`
- Mode: `vibe-code`
- Read:
  - `vibe-build.md`
  - `mode-code.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - cloud execution service matching the local adapter contract
  - runtime target switching between local and cloud
  - shared event and artifact shapes

### Wave 6: Review, Debug, Finalize
- Task `17_quality_gate`
- Mode: `vibe-review`
- Read:
  - `mode-review.md`
  - `review_code.md`
  - `vibe-finalize.md`
- Skills:
  - `takomi`
  - `code-review`
  - `jstar-reviewer`
  - `security-audit`
- Output:
  - code review findings
  - security findings
  - fix list grouped by severity
  - explicit approval or rejection gate

- Task `18_fix_loop`
- Mode: `vibe-debug` then `vibe-code`
- Read:
  - `mode-debug.md`
  - `vibe-continueBuild.md`
  - `vibe-build.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - root-cause-based fixes for all blocking review findings
  - rerun verification until clean

- Task `19_final_handoff`
- Mode: `vibe-architect`
- Read:
  - `vibe-finalize.md`
  - `vibe-syncDocs.md`
- Skills:
  - `takomi`
  - `sync-docs`
- Output:
  - final builder/orchestrator handoff report
  - scope compliance report
  - hackathon-readiness report
  - deployment/runbook notes
  - known issues and next-step list

## Required Design Artifacts
- The Design phase must produce reviewable HTML prototypes before WinUI implementation starts.
- The minimum required prototype set is:
  - project selector and onboarding
  - orchestrator dashboard
  - session/chat/sub-chat workspace
  - task tree timeline
  - child-run inspector
  - worktree/branch manager
  - settings/runtime panel
  - billing/Bags verification pages
- Build tasks must treat these mockups as the UI source of truth unless the design review loop explicitly revises them.

## Verification and Acceptance
- Genesis is complete only when PRD, architecture, FR issue set, and task breakdown all exist and align with the fee-share-removed scope.
- Design is complete only when HTML mockups are reviewable, refined, and approved.
- Build is complete only when the WinUI shell, orchestration engine, Codex adapter, Git/worktree controls, payments, Bags API/token support, local/cloud runtime switching, and audit logs are all functional.
- Finalize is complete only when:
  - the review gate passes
  - blocking issues are fixed
  - final docs are synced
  - the app demonstrates a full Takomi flow from Genesis through orchestrated execution
  - the Bags scope is token + API + verification ready, with no fee-sharing dependency

## Assumptions and Defaults
- The orchestrator session will be generated from this plan, not improvised task-by-task.
- `context7` is excluded from all sub-agent prompts.
- Mockups are HTML-first even though the shipped app is WinUI 3.
- Local and cloud runtime scope stays in v0.001.
- The Codex source may be imported as reference material, but runtime execution remains adapter-first through the CLI/process boundary.
- Hackathon compliance now depends on Bags token/API integration and verification readiness, not fee sharing.
