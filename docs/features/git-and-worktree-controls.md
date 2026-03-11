# Git and Worktree Controls

## Goal
Integrate Git actions directly into the session core shell so that users can clearly see the state of their current worktree (branch and dirty status) and manage parallel work effortlessly through explicit worktree creation and switching commands.

## Components
- **IGitService**: Contract defining Git interactions (e.g. `GetCurrentStateAsync`, `CreateWorktreeAsync`, `EnsureWorktreeIsValidAsync`).
- **GitService**: Implementation using `System.Diagnostics.Process` to run specific Git commands (`rev-parse`, `status`, `branch`, `worktree add`) and fail clearly when Git rejects a command.
- **MainViewModel**: 
  - Subscribes to session changes to automatically refresh the `CurrentGitBranch` and `GitDirtyIndicator`.
  - Provides `CreateWorktreeCommand` and `SwitchWorktreeCommand`.
  - Updates the selected session subtree when a worktree switch is explicit and only for descendants still inheriting the prior worktree path.
  - Triggers structured audit logging events whenever a worktree is created or a subtree is switched.
- **MainWindow.xaml**: The core shell UI binding to these properties to render Git feedback right next to the current session's workspace path.

## Data Flow
- **Initialization/Selection**: When the user opens the app or selects a session, `MainViewModel.RefreshGitStateAsync()` fires off, parsing `git rev-parse --abbrev-ref HEAD` and `git status --porcelain`.
- **Worktree Switching**: User inputs a path -> `SwitchWorktreeAsync()` validates the target path through `IGitService` -> the selected session subtree is updated and persisted -> `IAuditLogRepository.AppendEventAsync` logs the switch. Unrelated sessions are left untouched.
- **Worktree Creation**: User requests a new worktree -> `GitService` creates and validates the worktree -> `MainViewModel` switches the selected subtree to it and logs both creation and switch events.

## Audit Trail
- `workspace.worktree_created`
- `workspace.worktree_switched`

Audit events are stored durably in `%LocalAppData%\TakomiCode\audit-events.json`.
