# Project Requirements Document

## Project Overview

**Name:** Takomi Code  
**Mission:** Build a Windows-native agentic coding desktop app that wraps Codex CLI with the Takomi orchestration model and delivers a polished, multi-session workflow for planning, designing, building, reviewing, and shipping software.  
**Platform:** Windows desktop, built with C# + WinUI 3.  
**Primary Runtime Model:** Adapter-first. Takomi owns orchestration, state, UX, auditability, and workspace management; Codex CLI owns file-editing execution.  
**Hackathon Scope:** The app must support Bags token linkage, Bags API usage, project verification readiness, and Paystack-powered fiat billing. Fee sharing is explicitly out of scope for v0.001.

## Goals

- Deliver a distinctive WinUI 3 app that feels native, fast, and visually intentional.
- Preserve the Takomi lifecycle: Genesis -> Design -> Build -> Continue Build -> Review -> Finalize.
- Support multiple projects, multiple chats per project, and hierarchical sub-chats.
- Keep same-workspace behavior by default while allowing explicit worktree switching.
- Support both local and cloud execution targets behind the same runtime contract.
- Maintain a complete audit trail for runs, interventions, billing, and verification state.

## Target Users

- Solo builders using Takomi workflows to ship fast.
- Hackathon teams coordinating parallel AI-assisted execution.
- Power users who want a desktop-native orchestration shell instead of browser-based tools.

## Functional Requirements

| FR ID | Description | User Story | Status |
| :--- | :--- | :--- | :--- |
| FR-001 | Project and workspace management | As a builder, I want to open and manage multiple project directories so that I can work across products without losing context. | MUS |
| FR-002 | Multi-chat session system | As a builder, I want multiple chats per project so that I can separate strategy, build, review, and debugging work. | MUS |
| FR-003 | Sub-chat hierarchy | As a builder, I want child sessions under a parent chat so that orchestration chains stay organized. | MUS |
| FR-004 | Takomi mode loader | As a builder, I want the app to read mode YAML files so that orchestrator behavior stays aligned with the Takomi contracts. | MUS |
| FR-005 | Workflow loader | As a builder, I want the app to read workflow Markdown files so that execution stages follow the VibeCode protocol. | MUS |
| FR-006 | Orchestrator session engine | As a builder, I want the orchestrator to create task graphs and delegate work so that large efforts can be coordinated safely. | MUS |
| FR-007 | Background child execution | As a builder, I want child runs to continue in the background so that I can keep working in the parent session. | MUS |
| FR-008 | Live intervention controls | As a builder, I want to inject guidance, pause, resume, reroute, replace, cancel, and migrate context so that I stay in control of AI execution. | MUS |
| FR-009 | Codex adapter runtime | As a builder, I want Takomi to run Codex CLI through a controlled adapter so that code editing remains delegated while the app owns orchestration. | MUS |
| FR-010 | WinUI shell and navigation | As a builder, I want a beautiful native shell with project navigation, session tabs, and inspectors so that the workflow feels clear and fast. | MUS |
| FR-011 | Transcript and artifact persistence | As a builder, I want chats, events, and generated artifacts saved locally so that sessions can be resumed reliably. | MUS |
| FR-012 | Git and worktree controls | As a builder, I want branch visibility and explicit worktree switching so that parallel work stays manageable. | MUS |
| FR-013 | Audit event log | As a builder, I want every run, intervention, billing event, and verification change logged so that execution is reviewable and trustworthy. | MUS |
| FR-014 | HTML-first design review flow | As a builder, I want reviewable HTML prototypes before WinUI implementation so that the UX can be refined before engineering hardens it. | MUS |
| FR-015 | Paystack billing and entitlements | As a builder, I want fiat billing and entitlements so that paid access can be enforced. | MUS |
| FR-016 | Bags token and API integration | As a builder, I want Bags token linkage and Bags API-backed verification readiness so that the app meets hackathon requirements. | MUS |
| FR-017 | Local and cloud runtime parity | As a builder, I want to switch between local and cloud execution so that work can continue under different runtime conditions. | MUS |
| FR-018 | Recovery workflows | As a builder, I want continue-build, migration, reset, and escalation flows so that long-running projects remain recoverable. | MUS |
| FR-019 | Packaging and onboarding polish | As a builder, I want a stronger onboarding flow, first-run setup, and packaging improvements so that setup friction is minimized. | Future |
| FR-020 | Team collaboration primitives | As a builder, I want shared sessions and team-level visibility so that multiple humans can coordinate in one app. | Future |
| FR-021 | Usage analytics dashboard | As a builder, I want app and run analytics so that I can understand throughput, costs, and agent effectiveness. | Future |
| FR-022 | Skill marketplace and remote installs | As a builder, I want to browse and install skill bundles from the app so that extending the system is easier. | Future |

## Non-Negotiable Constraints

- WinUI 3 + C# is the application stack.
- Same workspace is the default inheritance model for chats and sub-chats.
- Worktree switching must be explicit and visible in the UI.
- `context7` must not be injected into orchestrated sub-agent tasks for this session.
- Fee sharing must not appear in requirements, tasks, payments, or Bags integration scope.
- Local and cloud runtime support remain in v0.001.

## Acceptance Gates

- The app demonstrates the full Takomi lifecycle from Genesis through Finalize.
- Mode and workflow behavior are loaded from `.agent/Takomi-Agents/` and `.agent/workflows/`.
- Orchestrator task graphs, background child execution, and intervention controls are functional.
- Bags scope is limited to token linkage, Bags API usage, verification readiness, and event logging.
- Paystack billing and entitlement checks work on the success path.
- Every critical run and payment action is recorded in the audit log.
