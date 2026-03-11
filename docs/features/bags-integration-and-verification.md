# Bags Integration and Verification

## Goal
Provide a demo-safe Bags flow that supports token linkage, service-isolated verification readiness checks, visible readiness state in the shell, and auditable verification events.

## Components
- **IBagsService**: Contract for token linkage and verification readiness checks.
- **BagsService**: Isolated integration service that updates workspace Bags metadata and emits `bags.token_linked` and `bags.verification_checked`.
- **Workspace**: Stores Bags token linkage metadata and readiness status.
- **LocalWorkspaceRepository**: Persists workspace state, including Bags metadata, to `%LocalAppData%\TakomiCode\workspaces.json`.
- **MainViewModel / MainWindow.xaml**: Expose token entry, readiness checks, and current verification state in the shell.

## Data Flow
- **Link Token**: The user enters a Bags token address -> `LinkTokenToWorkspaceAsync()` normalizes and persists the token -> readiness resets to false -> `bags.token_linked` is appended.
- **Readiness Check**: The user requests verification readiness -> `CheckVerificationReadinessAsync()` simulates a Bags API-style readiness check from the stored token -> workspace readiness is persisted -> `bags.verification_checked` is appended.
- **Shell Restore**: On app startup, the workspace record is restored before shell state is loaded, so Bags token linkage and readiness survive restarts.

## Scope Guardrail
- Fee-sharing, payouts, and marketplace revenue logic remain explicitly out of scope.
