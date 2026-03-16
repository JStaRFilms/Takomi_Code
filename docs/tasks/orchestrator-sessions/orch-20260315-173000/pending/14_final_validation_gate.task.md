# Task: Final Validation Gate

**Session ID:** `orch-20260315-173000`  
**Priority:** `P0`  
**Mode:** `vibe-review`  
**Dependencies:** `13_consolidated_repair_loop`

## Agent Setup (DO THIS FIRST)

### Workflow To Follow
Read the Takomi review workflow guidance from:
- `C:\Users\johno\.codex\skills\takomi\SKILL.md`
- `C:\Users\johno\.codex\skills\takomi\workflows\mode-review.md`

### Prime Agent Context
Review the repaired application against the validation artifacts from Tasks `04` through `13`. This is a gate, not another open-ended implementation loop.

### Required Skills

| Skill | Why |
|---|---|
| `takomi` | Session orchestration source of truth |
| `code-review` | Findings-first gate discipline |
| `security-audit` | Use if any repaired path touches sensitive runtime or payment-adjacent behavior |

## Objective

Decide whether the repaired app is ready to be treated as a trustworthy next-step baseline:

1. confirm the highest-priority product-spine flows are working
2. confirm major misleading UI surfaces are gone or truthful
3. confirm open gaps are documented rather than hidden

## Required Outputs

1. pass/fail verdict for each critical feature slice
2. explicit findings ordered by severity
3. residual risk summary
4. recommendation: proceed, partial proceed, or continue repair

## Definition Of Done

- [ ] Critical flows are rechecked
- [ ] Findings are documented clearly
- [ ] A gate verdict is produced
- [ ] Remaining risks are explicit

## Constraints

- Do not do implementation work in this task unless a tiny blocking typo must be corrected to complete the review.

## Expected Artifact

- `docs/tasks/orchestrator-sessions/orch-20260315-173000/14_final_validation_gate.md`

## Recommended Next Task

If the gate passes or reaches an acceptable partial proceed state, proceed to `15_session_handoff_and_replan_summary`.
