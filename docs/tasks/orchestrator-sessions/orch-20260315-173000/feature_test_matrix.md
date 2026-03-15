# Feature Test Matrix

**Session ID:** `orch-20260315-173000`  
**Created:** `2026-03-15`  
**Purpose:** Turn the current runnable UI into a concrete functional verification checklist.

## Buckets

- `Must Work`: visible and expected to function in the current build
- `Demo-Safe`: may be acceptable if clearly read-only or explicitly scoped
- `Hide/Disable`: visible control should not remain interactive unless it is fully wired

## Execution Order

1. Project selector entry and shell opening
2. Global shell navigation sanity check
3. Chat/session flows
4. Orchestrator/intervention surfaces
5. Worktree flows
6. Billing and Bags surfaces
7. Runtime/settings behavior
8. Persistence and restart

## Matrix

| Surface | Control / Flow | Expected Behavior | Bucket | Current Evidence |
|---|---|---|---|---|
| Project Selector | `Settings` footer link | Opens shell directly to `Settings` | Must Work | Bound to `OpenSettingsCommand` |
| Project Selector | recent workspace row | Opens selected workspace and initializes shell | Must Work | Bound to `InitializeProjectCommand` |
| Project Selector | `Initialize Folder` | Either launches a real init flow or is disabled | Hide/Disable | No command/click binding |
| Project Selector | `Clone Repository` | Either launches a real clone flow or is disabled | Hide/Disable | No command/click binding |
| Project Selector | `Clear Recent History...` | Either clears stored recents or is disabled | Hide/Disable | No command/click binding |
| Shell | left nav `Home` / `Sessions` / `Worktrees` / `Billing` / `Settings` | Switches visible page correctly | Must Work | `SelectionChanged` wired in `MainWindow.xaml.cs` |
| Shell | header `New Orchestration Session` | Creates a new session and takes user into session workflow | Must Work | `CreateChatButton_Click` wired |
| Home | `Manage Runtime Settings` | Navigates to `Settings` page | Must Work | Wired via `ShellShortcutButton_Click` |
| Home | `Runtime Engine` card | Navigates to `Settings` | Must Work | Wired via `ShellShortcutButton_Click` |
| Home | `Parent Git Branch` card | Navigates to `Worktrees` | Must Work | Wired via `ShellShortcutButton_Click` |
| Home | `Resolve Now` | Navigates to `Sessions` | Must Work | Wired via `ShellShortcutButton_Click` |
| Home | `View All Sessions` | Navigates to `Sessions` | Must Work | Wired via `ShellShortcutButton_Click` |
| Home | recent sessions table rows | If rows look selectable, selection/open behavior should exist | Demo-Safe | Currently informational only |
| Sessions | session list selection | Changes active session, transcript, and status | Must Work | Bound to `SelectedSession` |
| Sessions | `New Session` | Creates a root session | Must Work | Wired to `CreateChatButton_Click` |
| Sessions | child session creation | User should be able to create a child session somewhere in flow | Must Work | View model supports it; verify whether UI exposes it |
| Sessions | transcript `Route` button | Saves/sends draft into selected session | Must Work | Wired to `SendMessageButton_Click` |
| Sessions | typing in draft input | Updates `DraftMessage` and persists on route | Must Work | Two-way binding |
| Sessions | `Suggest Fix` button in transcript | Either opens a real fix action or is disabled | Hide/Disable | No binding; decorative |
| Sessions | inspector `Fix` button | Either opens intervention/fix flow or is disabled | Hide/Disable | No binding; decorative |
| Sessions | task graph / artifacts tabs | Either switch content or are visibly static | Demo-Safe | No switching logic visible |
| Worktrees | back/home icon | Returns to `Home` | Must Work | Wired to `ShellShortcutButton_Click` |
| Worktrees | `Create Manual Link` | Creates/attaches worktree or is disabled | Hide/Disable | No binding |
| Worktrees | `Open Folder` | Opens filesystem path or is disabled | Hide/Disable | No binding |
| Worktrees | `Delete` buttons | Deletes worktree only if fully implemented; otherwise disabled | Hide/Disable | No binding |
| Billing | back/home icon | Returns to `Home` | Must Work | Wired to `ShellShortcutButton_Click` |
| Billing | Bags verification status display | Correctly reflects current stored Bags state | Must Work | Backed by `BagsTokenAddress` / `VerificationStatusText` |
| Billing | Paystack status display | Correctly reflects stored billing entitlement state | Must Work | Backed by billing service |
| Billing | `Revoke Link` | Revokes Bags link or is disabled | Hide/Disable | No binding |
| Billing | `Open Billing History` | Opens history or is disabled | Hide/Disable | No binding |
| Settings | page navigation from shell | Opens settings page reliably | Must Work | Nav wired |
| Settings | left subnav (`General`, `Runtime Config`, `Language Models`, `Tools & Packages`, `Account & Node`) | Either actually changes section or is clearly read-only | Hide/Disable | No interaction logic visible |
| Settings | runtime target controls | Should actually affect stored runtime target if presented as adjustable | Must Work | View model supports runtime target persistence; verify exposed control is wired |
| Settings | `Apply Settings` | Persists settings or is disabled | Hide/Disable | No binding |
| Settings | `ToggleSwitch`, `NumberBox`, network controls | Persist or be clearly non-editable | Hide/Disable | Static controls with no backing commands observed |
| Overlays | intervention overlay | Should appear only from a real blocked-run path | Demo-Safe | Present in XAML but no open trigger found yet |
| Overlays | task tree monitor overlay | Should appear only from a real task-tree path | Demo-Safe | Present in XAML but no open trigger found yet |
| Global | status bar labels | Should reflect real runtime/session state or be toned down | Demo-Safe | Appears mostly presentational |

## Immediate Suspect Controls

These should be treated as likely non-functional until proven otherwise:

- `Initialize Folder`
- `Clone Repository`
- `Clear Recent History...`
- `Suggest Fix`
- task-graph `Fix`
- `Create Manual Link`
- worktree `Open Folder`
- worktree `Delete`
- `Revoke Link`
- `Open Billing History`
- settings sub-navigation buttons
- `Apply Settings`
- settings `ToggleSwitch` / `NumberBox` controls

## Manual Verification Rules

- If a control looks actionable and does nothing, mark it `FAIL`.
- If a control is intentionally out of scope for `v0.001`, it should be disabled or visually de-emphasized, not left looking live.
- If a page mostly displays state and that state is real, it may pass as `Demo-Safe`.
- Where the view model already supports a flow but the UI does not expose it cleanly, log that as a UI/interaction gap rather than a backend failure.

## Ready For Task 04

Start with:

1. open a workspace from the project selector
2. verify all top-level nav items
3. create a new session
4. select a session
5. send a draft message
6. note which visible session controls are real vs decorative
