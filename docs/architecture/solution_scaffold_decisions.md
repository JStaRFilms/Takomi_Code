# Solution Scaffold Decisions

## Overview
This document records the architectural decisions made during the initial scaffolding of the WinUI 3 solution for Takomi Code.

## Project Structure
The solution is organized into the following layers to maintain clean architecture boundaries:
- **TakomiCode.Domain**: Contains core business entities (`Workspace`, `AuditEvent`, etc.) with no external dependencies.
- **TakomiCode.Application**: Contains our interfaces and contracts (`IAuditLogRepository`, `IWorkspaceRepository`, `ICodexRuntimeAdapter`). References the Domain layer.
- **TakomiCode.Infrastructure**: Contains local implementation details such as our persistence layer implementations (`LocalAuditLogRepository`, `LocalWorkspaceRepository`). References Application and Domain.
- **TakomiCode.RuntimeAdapters**: Isolates integration with the execution environment (`CodexCliAdapter`), separating orchestration from pure execution. References Application and Domain.
- **TakomiCode.UI**: The WinUI 3 application shell. Serves as the presentation layer and dependency injection root, wiring up MVVM components using `CommunityToolkit.Mvvm` and `Microsoft.Extensions.DependencyInjection`.

## Technical Choices
- **C# / .NET 8**: The standard modern ecosystem for building native Windows desktop applications. All libraries use `net8.0` except the UI which explicitly targets `net8.0-windows10.0.19041.0`.
- **WinUI 3**: Used via `Microsoft.WindowsAppSDK` to provide a truly native and visually intentional Windows desktop experience.
- **CommunityToolkit.Mvvm**: Adopted for view-model state and command wiring, promoting a clean MVVM separation of concerns.
- **Dependency Inversion**: Implemented using MS Extensions Dependency Injection to ensure UI projects rely on application-layer contracts, never infrastructure internals directly.

## Audit and Persistence Foundation
The scaffold lays out a basic generic persistence foundation using in-memory classes (`LocalWorkspaceRepository`, `LocalAuditLogRepository`) which implement the core application interfaces (`IWorkspaceRepository`, `IAuditLogRepository`). This foundation sets up the app for future SQLite integration as required by the Takomi data and audit rules.
