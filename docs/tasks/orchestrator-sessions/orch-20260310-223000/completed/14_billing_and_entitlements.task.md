# Task: Billing and Entitlements

**Session ID:** orch-20260310-223000  
**Priority:** P0  
**Mode:** `vibe-code`  
**Dependencies:** `07_solution_scaffold`

## Agent Setup (Do This First)

### Mode Definition
- Read `.agent/Takomi-Agents/vibe-code.yaml`

### Workflow to Follow
- Read `.agent/workflows/vibe-build.md`
- Read `.agent/workflows/mode-code.md`
- Run `.agent/workflows/vibe-primeAgent.md`

### Required Skills
- `takomi`
- `sync-docs`

### Forbidden Skills
- `context7`

## Objective

Implement Paystack billing success-path handling and entitlement activation with auditable state changes.

## Definition of Done

- [ ] Billing configuration and success-path flow exist
- [ ] Entitlement records can be created and checked
- [ ] Billing state is visible from the app
- [ ] Billing events are written to the audit log

## Expected Artifacts

- billing services and entitlement models
- billing UI and persistence updates

## Constraints

- Success path must be real enough for hackathon demonstration.
- Do not add fee-sharing concepts.
