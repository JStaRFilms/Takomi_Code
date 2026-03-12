# Billing and Entitlements

## Goal
Provide a Paystack-oriented billing success path that is concrete enough for demo use: checkout initiation, pending reference visibility, entitlement activation, and auditability.

## Components
- **IBillingService**: Billing contract for checkout creation, pending checkout lookup, entitlement activation, and entitlement lookup.
- **PaystackMockBillingService**: Local Paystack-style adapter that persists pending checkout sessions and active entitlements in `%LocalAppData%\TakomiCode\billing-state.json`.
- **MainViewModel**:
  - Loads entitlement and pending checkout state during shell initialization.
  - Exposes billing status, checkout URL, pending Paystack reference, and customer email.
  - Provides explicit commands to start checkout and confirm the success path.
- **MainWindow.xaml**: Surfaces current billing tier, billing email entry, checkout start/confirm actions, and the pending Paystack reference directly in the shell.

## Data Flow
- **Checkout Start**: The user enters an email and starts checkout -> `CreateCheckoutSessionAsync()` creates a Paystack-style reference and checkout URL -> the pending checkout is persisted and `billing.checkout_started` is appended to the audit log.
- **Success Path**: The user confirms the Paystack success path -> `ActivateEntitlementAsync()` verifies the pending reference, creates a Pro entitlement, clears the pending checkout, and appends `billing.entitlement_activated`.
- **Shell Restore**: On app startup, the shell restores both the current entitlement and any unfinished pending checkout so billing state remains visible after restart.

## Audit Trail
- `billing.checkout_started`
- `billing.entitlement_activated`

Billing audit events are workspace-scoped. The billing service no longer writes a synthetic `"system"` session id, which keeps billing records queryable alongside orchestration/runtime events without overloading session identity.
