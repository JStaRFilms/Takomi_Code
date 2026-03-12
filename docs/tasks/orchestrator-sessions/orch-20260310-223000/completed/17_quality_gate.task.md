# Task: Quality Gate

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-review`  
**Dependencies:** `11_orchestrator_execution_engine`, `12_intervention_controls`, `13_git_and_workspace_controls`, `14_billing_and_entitlements`, `15_bags_integration`, `16_cloud_runtime`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-review.yaml`

### Workflow to Follow
- Read `.agent/workflows/mode-review.md`
- Read `.agent/workflows/review_code.md`
- Read `.agent/workflows/vibe-finalize.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `code-review`
- `jstar-reviewer`
- `security-audit`

### Forbidden Skills
- `context7`

## Objective

Run the code-quality, security, and readiness gate before final handoff.

## Definition of Done

- [ ] Findings are grouped by severity
- [ ] Billing, Bags, runtime, and audit flows receive explicit review attention
- [ ] The review ends with approve or reject status
- [ ] A fix list exists for blocking issues

## Expected Artifacts

- `docs/tasks/orchestrator-sessions/orch-20260310-223000/completed/17_quality_gate.result.md`
- review findings report

## Constraints

- Findings must prioritize real risks, not style noise.
- The review should explicitly confirm fee-sharing is absent.
