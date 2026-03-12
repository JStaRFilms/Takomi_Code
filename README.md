# Takomi Code

Takomi Code is a Windows-native WinUI 3 orchestration shell for Codex CLI. It gives builders a desktop command center for multi-session AI-assisted software work, with explicit workspace and worktree control, audit trails, intervention controls, and lifecycle-driven execution from planning through final review.

## Why This Exists

Browser-based coding agents are useful, but they are not ideal when you need durable local state, visible workspace boundaries, and direct control over long-running execution. Takomi Code is built to own orchestration, UX, auditability, and session structure while delegating file-editing execution to Codex through a runtime adapter.

## What Shipped In v0.001

- Windows-native WinUI 3 desktop shell
- Takomi mode and workflow loading from repo files
- Project and workspace management
- Multi-chat sessions with parent and child hierarchy
- Orchestrator task graph execution with background child runs
- Live intervention controls for pause, resume, reroute, replace, cancel, and migration
- Local and cloud runtime routing behind a shared contract
- Local persistence for sessions, orchestration state, and audit events
- Explicit Git branch and worktree controls
- Demo-safe billing and entitlement flow using a Paystack-style path
- Demo-safe Bags token linkage and verification readiness flow

## Product Scope

Takomi Code is currently positioned as a hackathon-ready desktop product foundation, not a production-hardened platform. Billing, Bags integration, and cloud runtime support are implemented as demo-safe flows for v0.001. Fee sharing is explicitly out of scope.

## Architecture At A Glance

- `TakomiCode.UI`: WinUI 3 desktop shell and view models
- `TakomiCode.Application`: orchestration contracts and application services
- `TakomiCode.Domain`: core entities and audit models
- `TakomiCode.Infrastructure`: local persistence and supporting services
- `TakomiCode.RuntimeAdapters`: Codex local and cloud runtime adapters

Takomi owns orchestration, state, and operator controls. Codex remains the execution worker.

## Repository Layout

```text
src/
  TakomiCode.sln
  TakomiCode.Application/
  TakomiCode.Domain/
  TakomiCode.Infrastructure/
  TakomiCode.RuntimeAdapters/
  TakomiCode.UI/
docs/
  Project_Requirements.md
  Builder_Handoff_Report.md
  architecture/
  features/
  design/
.agent/
  Takomi-Agents/
  workflows/
```

## Getting Started

### Prerequisites

- Windows
- .NET 8 SDK
- Visual Studio 2022 with WinUI 3 / Windows App SDK support
- Codex CLI installed on `PATH` for local runtime execution

### Build

```powershell
dotnet build src/TakomiCode.sln -m:1 -v:minimal
```

### Test

```powershell
dotnet test src/TakomiCode.sln -m:1 -v:minimal
```

Current state: the solution builds cleanly, but there are no runnable test projects yet, so test output is not a strong quality signal.

### Run The App

1. Open `src/TakomiCode.sln` in Visual Studio 2022.
2. Set `TakomiCode.UI` as the startup project.
3. Run `Debug | x64`.

## Review Tooling

If you want to run the J-Star review flow in this repo:

1. Copy `.env.example` to `.env.local`.
2. Set `GEMINI_API_KEY`.
3. Set `GROQ_API_KEY`.
4. Initialize J-Star locally with `jstar init`.
5. Run:

```powershell
jstar audit --json
jstar review --json
```

Note: as of March 12, 2026, `jstar review --json` is blocked until the repo-local J-Star brain is initialized successfully.

## Local Persistence

Takomi Code stores local state under `%LocalAppData%\TakomiCode\`:

- `workspaces.json`
- `chat-sessions.json`
- `orchestration-store.json`
- `audit-events.json`
- `billing-state.json`

## Current Limitations

- Billing is a demo-safe Paystack-style success path, not a live payment integration.
- Bags support is limited to token linkage, API-style verification readiness, and audit visibility.
- Cloud runtime support is a parity-oriented mock path, not a live remote executor.
- Automated test coverage is not in place yet.
- `docs/issues/` should not be treated as the source of shipped-state truth.

## Core Docs

- `docs/Project_Requirements.md`: product goals, constraints, and functional requirements
- `docs/Builder_Handoff_Report.md`: verification snapshot and delivery status
- `docs/architecture/system_architecture.md`: system architecture overview
- `docs/features/`: feature-level implementation notes
- `docs/design/`: design direction and prototype artifacts

## Next Milestone

The immediate path forward is production hardening:

- add runnable test projects
- initialize and pass the full J-Star review flow
- replace demo-safe integrations with live billing and runtime implementations
- tighten onboarding and packaging for first-run setup

## Status

As of March 12, 2026:

- `dotnet build src/TakomiCode.sln -m:1 -v:minimal` passes
- the WinUI shell and orchestration foundations are in place
- the repo is ready for continued implementation and hardening
