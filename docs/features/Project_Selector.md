# Feature: Project Selector (Project Workspace Selection)

## Goal
Implement a high-fidelity workspace selection surface that aligns with the v5 Project Selector mockup. This surface allows users to initialize new environments, clone repositories, and select from recent workspaces.

## Structure
The Project Selector will be implemented as a global overlay or a distinct state in `MainWindow.xaml`.

### Components
1. **Sidebar Navigation (Lateral Nav)**:
   - Environment status (Codex version, Docker status).
   - Permissions widget (Bags Verified status).
   - Bottom settings link.
2. **Main Workspace Area**:
   - Header: "Select a Workspace".
   - Action Grid: Large cards for "Initialize Folder" and "Clone Repository".
   - Recent List: "Recent Environments" table/list with path and last active timestamp.

## Data Flow
- `MainViewModel` will track `RecentWorkspaces` collection.
- `IsProjectOpen` boolean will toggle between `ProjectSelectorSection` and the main `ShellNavigation`.
- `InitializeFolderCommand` and `CloneRepositoryCommand` will trigger system dialogs/actions.

## UI Implementation Details (WinUI 3)
- Use `studio-acrylic` style equivalent (AcrylicBrush).
- Custom `win-illuminated-border` using `Border` with specific `BorderBrush` gradients or nested borders.
- `win-press` effect using `PointerPressed` / `PointerReleased` visual states.
