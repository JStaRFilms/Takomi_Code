# Builder Prompt

## Product Context

Takomi Code is a WinUI 3 desktop application that wraps Codex CLI with the Takomi orchestration model. The desktop shell owns state, task orchestration, auditability, project/workspace management, Git/worktree switching, and local/cloud runtime routing.

## Stack-Specific Instructions

- Build the shell in C# + WinUI 3.
- Use MVVM and keep UI logic out of XAML code-behind except for unavoidable view glue.
- Use HTML prototypes in `docs/mockups/` as the design source of truth for layout and interaction intent.
- Preserve same-workspace chat inheritance by default.
- Treat worktree switching as a deliberate user action that updates the active workspace context for the current session subtree.
- Keep Codex file-editing execution behind a runtime adapter instead of pulling edit logic into the UI shell.

## MUS Priority Order

1. FR-010: WinUI shell and navigation
2. FR-001: Project and workspace management
3. FR-002: Multi-chat session system
4. FR-003: Sub-chat hierarchy
5. FR-004: Takomi mode loader
6. FR-005: Workflow loader
7. FR-006: Orchestrator session engine
8. FR-007: Background child execution
9. FR-008: Live intervention controls
10. FR-009: Codex adapter runtime
11. FR-011: Transcript and artifact persistence
12. FR-012: Git and worktree controls
13. FR-013: Audit event log
14. FR-014: HTML-first design review flow
15. FR-015: Paystack billing and entitlements
16. FR-016: Bags token and API integration
17. FR-017: Local and cloud runtime parity
18. FR-018: Recovery workflows

## Mandatory Mockup-Driven Implementation

The `/docs/mockups` folder is the source of truth for front-end behavior and information architecture during v0.001.

- Do not improvise a different shell layout once mockups are approved.
- If implementation pressure reveals a mismatch, update the design docs first, then build.
- Keep the final WinUI shell faithful to the approved structure, interaction model, and visual hierarchy.

## Special Considerations

- Fee sharing is removed from scope.
- Bags implementation is token + API + verification readiness only.
- The local and cloud runtime contracts must stay shape-compatible.
