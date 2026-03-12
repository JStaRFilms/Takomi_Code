# Cloud Runtime

## Goal
Allow the shell and orchestration engine to switch between local and cloud Codex execution without changing the shared runtime contract or task/run artifact shapes.

## Components
- **Workspace.RuntimeTarget**: Persists the selected runtime target as `Local` or `Cloud`.
- **CodexCloudAdapter**: Provides a demo-safe cloud execution path with structured runtime events and audit logging.
- **WorkspaceAwareCodexRuntimeAdapter**: Routes each run to the correct local or cloud adapter and preserves per-run runtime ownership for cancellations and interventions.
- **MainViewModel / MainWindow.xaml**: Expose runtime target switching in the shell and persist the selected target through the workspace repository.

## Data Flow
- **Target Selection**: The shell updates `Workspace.RuntimeTarget` when the user switches between local and cloud execution.
- **Run Start**: `WorkspaceAwareCodexRuntimeAdapter` resolves the correct adapter for the workspace and records the runtime target used for that run.
- **Run Control**: Cancellation and interventions are routed back to the runtime that actually owns the run, even if the workspace target is changed while the run is active.
- **Shape Parity**: Both local and cloud paths emit shared `ICodexRuntimeAdapter` events and `CodexRunResult` shapes so orchestration code remains unchanged.
