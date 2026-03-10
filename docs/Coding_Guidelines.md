# Coding Guidelines

## Stack Rules

- Use C# with WinUI 3 for the desktop shell.
- Use .NET 8 conventions unless an explicit constraint requires otherwise.
- Use `CommunityToolkit.Mvvm` for view-model state and command wiring.
- Keep the Codex runtime behind adapter interfaces; do not let UI layers invoke process logic directly.
- Treat workflow Markdown and mode YAML as runtime inputs, not hard-coded behavior.

## Architecture Rules

- Keep the solution layered: `App/UI`, `Application`, `Domain`, `Infrastructure`, `RuntimeAdapters`.
- UI projects may reference application-layer contracts, never infrastructure internals directly.
- All long-running operations must be cancellable and emit structured events.
- Persist user-facing state and audit trails through explicit repositories.
- Keep cloud and local runtimes behind the same interface boundary.

## Orchestration Rules

- The default mode is `vibe-orchestrator`.
- Every orchestrated task must reference:
  - its mode YAML
  - its workflow file
  - `vibe-primeAgent`
- Always inject `takomi`.
- Never inject `context7`.
- Only inject additional skills when they materially match the task scope.

## UX Rules

- The app must feel native to Windows, not like an Electron port.
- Navigation state, branch state, workspace state, and runtime state must always be visible.
- Background child runs must surface status without blocking the parent session.
- Design implementation must follow approved HTML prototypes unless explicitly revised in the design review loop.

## Data and Audit Rules

- Every run, intervention, entitlement change, verification change, and runtime error must generate an audit event.
- Transcript persistence must preserve parent-child session relationships.
- Billing and Bags integration code must be isolated from UI components.

## Quality Rules

- Prefer small, focused classes and clear interfaces.
- Name view models and services after their explicit responsibility.
- Avoid hidden global state.
- Add tests for parser, orchestration, persistence, and adapter logic where practical.
- Do not ship placeholder logic for billing or Bags success paths.

## Scope Rules

- No fee-sharing implementation in v0.001.
- No unapproved scope expansion beyond the PRD MUS features.
- Preserve same-workspace defaults while allowing explicit worktree switching.
