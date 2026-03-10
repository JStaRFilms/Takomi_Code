# Master Plan: Takomi Code v0.001

**Session ID:** orch-20260310-223000  
**Created:** 2026-03-10  
**Source:** User-approved Takomi Code Orchestrator Session Plan v0.001  
**Status:** In Progress  
**Default Mode:** `vibe-orchestrator`

## Overview

This orchestrator session turns the Takomi Code v0.001 plan into a delegated execution graph. The Genesis layer is already seeded in this session: the PRD, coding guidelines, builder prompt, architecture source of truth, design brief, visual sitemap, and FR issue set now exist. The remaining work is delegated across design, build foundation, orchestration engine, integrations, review, and finalization tasks.

## Skills Registry

| Skill | Relevant | Inject Into | Reasoning |
|---|---|---|---|
| `takomi` | Yes | All tasks | Required orchestration source of truth |
| `avoid-feature-creep` | Yes | Intake, genesis, scope reviews | Keeps hackathon scope tight |
| `spawn-task` | Yes | Genesis, orchestration engine | Used to shape task payloads |
| `ui-ux-pro-max` | Yes | Design direction, prototypes | Strong desktop UX guidance |
| `frontend-design` | Yes | HTML prototype generation | High-quality design artifacts |
| `webapp-testing` | Yes | Design review loop | Reviewable mockup behavior |
| `sync-docs` | Yes | Build and finalize tasks | Keeps docs aligned with implementation |
| `git-worktree` | Yes | Git/worktree task | Explicit worktree UX and logic |
| `code-review` | Yes | Quality gate | Manual and automated review flow support |
| `jstar-reviewer` | Yes | Quality gate | Review tooling alignment |
| `security-audit` | Yes | Quality gate | Security review for billing/runtime flows |
| `context7` | No | None | Explicitly excluded from this session |

## Workflows Registry

| Workflow | Relevant | Used In | Purpose |
|---|---|---|---|
| `/mode-orchestrator` | Yes | Intake, orchestration engine | Task graph, delegation, monitoring |
| `/mode-architect` | Yes | Genesis, design, final handoff | Architecture and delivery planning |
| `/mode-code` | Yes | Build and integration tasks | Implementation work |
| `/mode-debug` | Yes | Fix loop, runtime edge cases | Root-cause investigation |
| `/mode-review` | Yes | Quality gate | Review and validation |
| `/mode-ask` | Yes | Design review loop | Read-only critique and analysis |
| `/vibe-primeAgent` | Yes | All delegated tasks | Context priming requirement |
| `/vibe-genesis` | Yes | Completed Genesis wave | PRD and issue generation |
| `/vibe-design` | Yes | Design wave | Design system and prototypes |
| `/vibe-build` | Yes | Build and integration waves | Scaffold and implementation |
| `/vibe-continueBuild` | Yes | Recovery, orchestration, fix loop | Resume and iterate safely |
| `/vibe-finalize` | Yes | Review and handoff | Final verification and delivery |
| `/vibe-spawnTask` | Yes | Task-generation-aware work | Self-contained task framing |
| `/vibe-syncDocs` | Yes | Build and finalize tasks | Documentation alignment |

## Dependency Graph

See [dependency_graph.md](/C:/CreativeOS/01_Projects/Code/Personal_Stuff/2026-03-10_Takomi_Code/docs/tasks/orchestrator-sessions/orch-20260310-223000/dependency_graph.md).

## Tasks

| # | Task | Status | Mode | Workflow | Skills |
|---|---|---|---|---|---|
| 00 | Orchestrator intake | Completed | `vibe-orchestrator` | `/mode-orchestrator` | `takomi`, `avoid-feature-creep` |
| 01 | Product genesis brief | Completed | `vibe-architect` | `/vibe-genesis` | `takomi`, `spawn-task`, `avoid-feature-creep` |
| 02 | Architecture genesis | Completed | `vibe-architect` | `/vibe-genesis` | `takomi`, `spawn-task` |
| 03 | Issue and task seed | Completed | `vibe-architect` | `/vibe-spawnTask` | `takomi`, `spawn-task` |
| 04 | Design direction | Completed | `vibe-architect` | `/vibe-design` | `takomi`, `ui-ux-pro-max` |
| 05 | Mockup prototypes | Completed | `vibe-architect` | `/vibe-design` | `takomi`, `frontend-design`, `ui-ux-pro-max` |
| 06 | Design review loop | Pending | `vibe-ask` | `/mode-ask` | `takomi`, `webapp-testing` |
| 07 | Solution scaffold | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 08 | Mode and workflow loader | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 09 | Chat and session core | Pending | `vibe-code` | `/vibe-continueBuild` | `takomi`, `sync-docs` |
| 10 | Codex adapter runtime | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 11 | Orchestrator execution engine | Pending | `vibe-code` | `/mode-orchestrator` | `takomi`, `spawn-task`, `sync-docs` |
| 12 | Intervention controls | Pending | `vibe-code` | `/mode-orchestrator` | `takomi`, `sync-docs` |
| 13 | Git and workspace controls | Pending | `vibe-code` | `/vibe-continueBuild` | `takomi`, `git-worktree`, `sync-docs` |
| 14 | Billing and entitlements | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 15 | Bags integration | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 16 | Cloud runtime | Pending | `vibe-code` | `/vibe-build` | `takomi`, `sync-docs` |
| 17 | Quality gate | Pending | `vibe-review` | `/mode-review` | `takomi`, `code-review`, `jstar-reviewer`, `security-audit` |
| 18 | Fix loop | Pending | `vibe-debug` -> `vibe-code` | `/vibe-continueBuild` | `takomi`, `sync-docs` |
| 19 | Final handoff | Pending | `vibe-architect` | `/vibe-finalize` | `takomi`, `sync-docs` |

## Progress Checklist

- [x] Intake and session framing complete
- [x] Genesis docs complete
- [x] FR issue set complete
- [x] Design direction approved
- [ ] HTML prototypes approved
- [ ] Build foundation complete
- [ ] Orchestration features complete
- [ ] Payments and Bags integrations complete
- [ ] Review and fix loop complete
- [ ] Final handoff complete

## Scope Guardrails

- No fee-sharing logic anywhere in v0.001.
- No `context7` in any task prompt.
- Same workspace is the default for sessions and sub-sessions.
- Worktree switching is explicit and visible.
- Local and cloud runtime parity remains in scope.
