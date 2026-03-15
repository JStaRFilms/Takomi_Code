# Master Plan: Takomi Code v0.001 Post-Build Validation And Polish

**Session ID:** `orch-20260315-173000`  
**Created:** `2026-03-15`  
**Source:** Post-build testing and visual-parity recovery session  
**Status:** `In Progress`  
**Default Mode:** `vibe-orchestrator`

## Overview

This orchestrator session starts after the first implementation wave became runnable in Visual Studio. The purpose is no longer greenfield feature delivery. The purpose is to:

1. recover visual parity with the approved mockups
2. validate every shipped feature path step by step
3. capture regressions and missing behavior as concrete fix tasks
4. gate progress through review after every task

This session assumes the build opens and runs, but the user-facing shell, feature behavior, and workflow quality are not yet trustworthy.

## Session Rules

- The mockups remain the visual source of truth unless explicitly superseded.
- Every feature must be manually exercised and reviewed, not only compiled.
- Builder agents may implement or adjust one task at a time.
- Reviewer approval is required before the next task proceeds.
- New bugs discovered during testing must be folded into the current or next fix task, not ignored.

## Skills Registry

| Skill | Relevant | Inject Into | Reasoning |
|---|---|---|---|
| `takomi` | Yes | All tasks | Session orchestration source of truth |
| `avoid-feature-creep` | Yes | Scope checks | Keeps polish/testing from turning into a rewrite |
| `spawn-task` | Yes | Bug/fix tasks | Makes delegated tasks self-contained |
| `ui-ux-pro-max` | Yes | Visual parity work | Helps map mockups to WinUI intent |
| `webapp-testing` | Yes | Test matrix design | Useful for structured interaction coverage mindset |
| `git-worktree` | Yes | Worktree validation | Explicit workspace/worktree test paths |
| `sync-docs` | Yes | Fix and finalize tasks | Keeps behavior docs aligned |
| `code-review` | Yes | Review gates | Findings-first review discipline |
| `security-audit` | Conditional | Runtime/payment verification | Use if changes affect safety-sensitive flows |
| `context7` | No | None | Remains excluded unless explicitly approved |

## Workflows Registry

| Workflow | Relevant | Used In | Purpose |
|---|---|---|---|
| `/mode-orchestrator` | Yes | Intake and sequencing | Control the session and task gates |
| `/mode-architect` | Yes | Visual parity and final summaries | Shape recovery direction and output quality |
| `/mode-code` | Yes | UI and feature fixes | Apply implementation changes |
| `/mode-debug` | Yes | Bug investigation | Root-cause runtime and interaction issues |
| `/mode-review` | Yes | Gate reviews | Approve or reject each task |
| `/mode-ask` | Yes | UX critique | Read-only comparison to mockups and flows |
| `/vibe-continueBuild` | Yes | Iterative fix work | Resume and refine existing implementation |
| `/vibe-syncDocs` | Yes | Documentation alignment | Keep testing artifacts current |

## Dependency Graph

See [dependency_graph.md](/C:/CreativeOS/01_Projects/Code/Personal_Stuff/2026-03-10_Takomi_Code/docs/tasks/orchestrator-sessions/orch-20260315-173000/dependency_graph.md).

## Tasks

| # | Task | Status | Mode | Workflow | Skills |
|---|---|---|---|---|---|
| 00 | Intake and baseline capture | Pending | `vibe-orchestrator` | `/mode-orchestrator` | `takomi`, `avoid-feature-creep` |
| 01 | Mockup parity audit | Pending | `vibe-ask` | `/mode-ask` | `takomi`, `ui-ux-pro-max` |
| 02 | Visual shell recovery | Pending | `vibe-code` | `/vibe-continueBuild` | `takomi`, `ui-ux-pro-max`, `sync-docs` |
| 03 | Feature test matrix | Pending | `vibe-architect` | `/mode-architect` | `takomi`, `spawn-task` |
| 04 | Chat and session flow validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs` |
| 05 | Orchestrator and intervention validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs` |
| 06 | Git and worktree validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `git-worktree`, `sync-docs` |
| 07 | Billing validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs`, `security-audit` |
| 08 | Bags validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs` |
| 09 | Runtime target validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs` |
| 10 | Persistence and restart validation | Pending | `vibe-debug` | `/mode-debug` | `takomi`, `sync-docs` |
| 11 | Consolidated fix loop | Pending | `vibe-code` | `/vibe-continueBuild` | `takomi`, `spawn-task`, `sync-docs` |
| 12 | Final validation gate | Pending | `vibe-review` | `/mode-review` | `takomi`, `code-review`, `security-audit` |
| 13 | Session handoff | Pending | `vibe-architect` | `/mode-architect` | `takomi`, `sync-docs` |

## Progress Checklist

- [ ] Baseline captured from current runnable app
- [ ] Mockup delta documented explicitly
- [ ] Visual shell brought materially closer to approved mockups
- [ ] Feature test matrix written
- [ ] Core chat/session flows tested
- [ ] Orchestrator/intervention flows tested
- [ ] Git/worktree flows tested
- [ ] Billing and Bags flows tested
- [ ] Runtime routing tested
- [ ] Persistence/restart behavior tested
- [ ] Fix loop completed
- [ ] Final gate passed

## Scope Guardrails

- No fee-sharing work is added in this session.
- No broad architectural rewrite unless a blocker proves the existing approach untenable.
- Visual recovery targets shell parity and workflow clarity first, not pixel-perfect invention.
- New features are secondary to making shipped features behave correctly.
