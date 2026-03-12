# Design Direction: Takomi Code

## 1. Visual Direction & UI Principles
- **Desktop-Native First**: The application must feel like a first-class Windows citizen, using WinUI 3 idioms. This includes leveraging native materials (Mica, Acrylic), standard Windows typography (Segoe UI Variable), and native windowing controls. It must not feel like a wrapped web app or Electron port.
- **High-Density Professional Shell**: As an orchestration tool for engineers, the UI must support high information density. Padding and margins should be compact but breathable, focusing on readability and rapid scanning of logs, trees, and code.
- **State Transparency**: System state, git state, and orchestration state must always be explicitly visible. Operations are never hidden from the user.
- **Auditability**: The design must reinforce trust. Every run, intervention, and billing action is critical path and should have a clear, unalterable visual log.

## 2. Information Architecture & Navigation Model 
The application shell is divided into structured panels to separate global context, session context, active work, and background status.

- **Primary Left Rail (Global)**: Contains high-level navigation.
    - Projects
    - Settings
    - Billing (Paystack)
    - Bags Verification
- **Secondary Left Pane (Session Explorer)**: Displays the hierarchical tree of chats and sub-chats for the active project.
- **Main Content Area (Tabbed Workspace)**: Houses active chat interfaces, orchestrator task graphs, and artifact reviews.
- **Right Pane (Inspector)**: Context-sensitive details for the currently selected node (Task details, Worktree/Git status, Artifact list, Mode/Workflow configs).
- **Bottom Status & Audit Panel**: Displays real-time background child execution status, global audit event tails, and network/runtime health.

## 3. Core Interaction Patterns

### Projects & Entry Flow
- **Project Selector First**: The initial project entry surface should prioritize recent work, explicit folder import, and immediate visibility into Codex/runtime readiness before a project is opened.
- **Project Home as Control Tower**: After opening a project, users land on a project home surface that summarizes branch state, active runtime target, recent sessions, blocked tasks, and quick actions for starting a new orchestrated session.
- **Clear Transition into Session Work**: Moving from project home into a session must preserve orientation by carrying forward the selected project, active workspace, and current branch/worktree context into the session header.

### Projects, Sessions & Task Trees
- **Hierarchical Sessions**: Multi-chat session systems are represented as an expandable tree in the Secondary Left Pane. Parent chats can expand to show child delegation sessions.
- **Task Graphs**: Within a session, the orchestrator's execution plan is visualized as a progressive task tree or node graph, clearly indicating states: `Pending`, `Running`, `Blocked`, `Completed`, `Failed`.

### Background Child Execution & Intervention Visibility
- **Non-Blocking Execution**: Child runs delegated to the background must not block the parent chat. They are represented by persistent status indicators in the Bottom Status Panel and badged in the Session Explorer.
- **Intervention Prominence**: When a task requires human intervention (e.g., requires review, fails, or is paused for manual rerouting), its corresponding node in the task tree and the session tab must visually demand attention (e.g., using a high-contrast native accent color or pulsing indicator).
- **Live Controls**: Active tasks expose live intervention controls (Pause, Resume, Reroute, Replace, Cancel, Migrate) via inline actions or context menus.

### Git & Worktree Controls
- **Same-Workspace Defaults**: By default, child sessions implicitly inherit the workspace directory of their parent session. The UI should display the active workspace path subtly in the session header.
- **Explicit Worktree Switching**: To support parallel AI execution without merge conflicts, users can explicitly switch or assign a Git worktree to a session subtree. This action is exposed via a dedicated dropdown in the Right Pane Inspector. The active worktree branch is badged prominently in the Main Area tab header to prevent context confusion.

### Billing & Bags Verification (No Fee-Sharing)
- **Billing (Paystack)**: Handled in a dedicated view accessible from the Primary Left Rail. Shows current fiat entitlements and tier limits cleanly. *Constraint strictly enforced: No fee-sharing surfaces or UI elements.*
- **Bags Verification**: Handled in a dedicated view containing connection flows for Bags token linkage and readouts of Bags API verification readiness states. Uses standard WinUI 3 success/warning iconography.

## 4. UI Design Rules for WinUI 3
- **Materials**: Use `MicaAlt` for the background of the application to provide a sense of depth and hierarchy, and `Acrylic` for transient surfaces like context menus and flyouts.
- **Controls**: Use native `NavigationView` for the Primary Rail, `TreeView` for the Session Explorer, and `TabView` for the Main Content Area.
- **Dark/Light Mode**: Full support for Windows system theme switching. High contrast mode must be fully supported for accessibility.
