using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using TakomiCode.Application.Configuration;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using TaskStatus = TakomiCode.Domain.Entities.TaskStatus;

namespace TakomiCode.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly string _workspaceId;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IOrchestrationRepository _orchestrationRepository;
    private readonly IInterventionCommandHandler _interventionCommandHandler;
    private readonly IGitService _gitService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IBillingService _billingService;
    private readonly IBagsService _bagsService;
    private readonly IOrchestratorExecutionEngine _orchestratorExecutionEngine;
    private readonly ICodexRuntimeAdapter _codexRuntimeAdapter;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<string, string> _runOutputFiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _runSessionIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _runErrorLines = new(StringComparer.Ordinal);
    private ChatSessionViewModel? _draftSession;

    [ObservableProperty]
    private string _statusMessage = "Welcome to Takomi Code Orchestrator";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeSection))]
    [NotifyPropertyChangedFor(nameof(IsSessionsSection))]
    [NotifyPropertyChangedFor(nameof(IsWorktreesSection))]
    [NotifyPropertyChangedFor(nameof(IsBillingSection))]
    [NotifyPropertyChangedFor(nameof(IsSettingsSection))]
    [NotifyPropertyChangedFor(nameof(ActiveSectionTitle))]
    [NotifyPropertyChangedFor(nameof(ActiveSectionSubtitle))]
    private string _selectedShellSection = "Home";

    [ObservableProperty]
    private ChatSessionViewModel? _selectedSession;

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRun))]
    [NotifyPropertyChangedFor(nameof(SelectedRunDisplayName))]
    [NotifyPropertyChangedFor(nameof(SelectedRunStatusText))]
    [NotifyPropertyChangedFor(nameof(SelectedRunTaskReference))]
    [NotifyPropertyChangedFor(nameof(SelectedRunWorkingDirectoryLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedRunDetailText))]
    [NotifyPropertyChangedFor(nameof(SelectedRunDetailLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedRunStatusBrushKey))]
    private OrchestrationRun? _selectedActiveRun;

    [ObservableProperty]
    private bool _isProjectOpen;

    public ObservableCollection<ChatSessionViewModel> Sessions { get; } = new();
    public ObservableCollection<OrchestrationRun> ActiveRuns { get; } = new();
    public ObservableCollection<WorkspaceViewModel> RecentWorkspaces { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceName))]
    private string _workspacePath = Environment.CurrentDirectory;

    [ObservableProperty]
    private string _workspaceDisplayName = "Takomi Code Workspace";

    public string CurrentSessionTitle => SelectedSession?.Title ?? "No session selected";
    public string CurrentSessionWorktree => SelectedSession?.WorktreePath ?? "Same workspace / default worktree";
    public string WorkspaceName => Path.GetFileName(WorkspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    public string RuntimeEngineLabel => IsCloudTarget ? "Cloud Runtime" : "Local Docker";
    public int SessionCount => Sessions.Count;
    public int ActiveRunCount => ActiveRuns.Count;
    public int BlockedRunCount => ActiveRuns.Count(run => run.Status == TakomiCode.Domain.Entities.TaskStatus.Blocked);
    public string BlockedRunCountLabel => $"{BlockedRunCount} Task Node(s) Blocked";
    public int HealthyRunCount => ActiveRuns.Count(run => run.Status is TakomiCode.Domain.Entities.TaskStatus.InProgress or TakomiCode.Domain.Entities.TaskStatus.Queued or TakomiCode.Domain.Entities.TaskStatus.Paused);
    public bool HasSessions => Sessions.Count > 0;
    public bool HasActiveRuns => ActiveRuns.Count > 0;
    public bool HasSelectedRun => SelectedActiveRun is not null;
    public bool IsHomeSection => SelectedShellSection == "Home";
    public bool IsSessionsSection => SelectedShellSection == "Sessions";
    public bool IsWorktreesSection => SelectedShellSection == "Worktrees";
    public bool IsBillingSection => SelectedShellSection == "Billing";
    public bool IsSettingsSection => SelectedShellSection == "Settings";
    public string SelectedRunDisplayName => SelectedActiveRun is null
        ? "No active execution"
        : $"Run {SelectedActiveRun.RunId[..Math.Min(8, SelectedActiveRun.RunId.Length)]}";
    public string SelectedRunStatusText => SelectedActiveRun?.Status switch
    {
        TaskStatus.Queued => "Queued",
        TaskStatus.InProgress => "Running",
        TaskStatus.Paused => "Paused",
        TaskStatus.Completed => "Completed",
        TaskStatus.Cancelled => "Cancelled",
        TaskStatus.Failed => "Failed",
        TaskStatus.Blocked => "Blocked",
        _ => "Idle"
    };
    public string SelectedRunTaskReference => SelectedActiveRun?.TaskId ?? "No task selected";
    public string SelectedRunWorkingDirectoryLabel => SelectedActiveRun?.WorkingDirectory ?? "No working directory";
    public string SelectedRunDetailText => SelectedActiveRun?.ErrorMessage
        ?? SelectedActiveRun?.ResultFilePath
        ?? "Waiting for Codex output.";
    public string SelectedRunDetailLabel => string.IsNullOrWhiteSpace(SelectedActiveRun?.ErrorMessage) ? "Details" : "Error";
    public string SelectedRunStatusBrushKey => SelectedActiveRun?.Status switch
    {
        TaskStatus.Completed => "SuccessBrush",
        TaskStatus.Failed or TaskStatus.Blocked or TaskStatus.Cancelled => "DangerBrush",
        _ => "AccentBrush"
    };
    public string ActiveSectionTitle => SelectedShellSection switch
    {
        "Sessions" => "Session Workspace",
        "Worktrees" => "Worktree Manager",
        "Billing" => "Billing And Verification",
        "Settings" => "Runtime Configuration",
        _ => "Orchestrator Overview"
    };
    public string ActiveSectionSubtitle => SelectedShellSection switch
    {
        "Sessions" => "Manage project chats, orchestration runs, and interventions.",
        "Worktrees" => "Control linked worktrees and branch routing for active session trees.",
        "Billing" => "Review Paystack entitlement state and Bags verification readiness.",
        "Settings" => "Adjust runtime target and review local execution defaults.",
        _ => "Workspace-first orchestration shell with quick status, recent sessions, and runtime health."
    };

    [ObservableProperty]
    private string _currentGitBranch = "unknown";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GitDirtyIndicator))]
    private bool _isGitDirty;

    public string GitDirtyIndicator => IsGitDirty ? "*" : string.Empty;

    [ObservableProperty]
    private string _targetWorktreePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCloudTarget))]
    [NotifyPropertyChangedFor(nameof(IsLocalTarget))]
    private string _runtimeTarget = "Local";

    public bool IsCloudTarget => RuntimeTarget == "Cloud";
    public bool IsLocalTarget => RuntimeTarget == "Local";

    [ObservableProperty]
    private bool _isProActive;

    [ObservableProperty]
    private string _billingStatusText = "Free Tier";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingCheckout))]
    private string _billingCheckoutUrl = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingCheckout))]
    private string _pendingBillingReference = string.Empty;

    [ObservableProperty]
    private string _billingEmail = "user@example.com";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerificationStatusText))]
    private string _bagsTokenAddress = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerificationStatusText))]
    private bool _isVerificationReady;

    public string VerificationStatusText => IsVerificationReady ? "Ready" : "Pending / Unlinked";

    public bool HasPendingCheckout =>
        !string.IsNullOrWhiteSpace(BillingCheckoutUrl) &&
        !string.IsNullOrWhiteSpace(PendingBillingReference);

    public MainViewModel(
        IChatSessionRepository chatSessionRepository,
        IWorkspaceRepository workspaceRepository,
        IOrchestrationRepository orchestrationRepository,
        IInterventionCommandHandler interventionCommandHandler,
        IGitService gitService,
        IAuditLogRepository auditLogRepository,
        IBillingService billingService,
        IBagsService bagsService,
        IOrchestratorExecutionEngine orchestratorExecutionEngine,
        ICodexRuntimeAdapter codexRuntimeAdapter)
    {
        _workspaceId = WorkspaceDefaults.ResolveWorkspaceId();
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
        _orchestrationRepository = orchestrationRepository;
        _interventionCommandHandler = interventionCommandHandler;
        _gitService = gitService;
        _auditLogRepository = auditLogRepository;
        _billingService = billingService;
        _bagsService = bagsService;
        _orchestratorExecutionEngine = orchestratorExecutionEngine;
        _codexRuntimeAdapter = codexRuntimeAdapter;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _codexRuntimeAdapter.StateChanged += OnCodexRuntimeStateChanged;
        _codexRuntimeAdapter.OutputReceived += OnCodexRuntimeOutputReceived;
    }

    public void SelectShellSection(string section)
    {
        SelectedShellSection = section;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void OpenSettings()
    {
        IsProjectOpen = true;
        SelectedShellSection = "Settings";
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task InitializeProjectAsync(WorkspaceViewModel workspaceVm)
    {
        // For now, we simulate switching. In a real app, we'd reload the context.
        WorkspacePath = workspaceVm.Path;
        WorkspaceDisplayName = workspaceVm.Name;
        IsProjectOpen = true;
        StatusMessage = $"Project {workspaceVm.Name} opened.";
        await InitializeAsync(openProjectShell: true);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void CloseProject()
    {
        IsProjectOpen = false;
        StatusMessage = "Project closed.";
    }

    public async Task InitializeAsync(bool openProjectShell = false, CancellationToken cancellationToken = default)
    {
        var workspace = await EnsureWorkspaceExistsAsync(cancellationToken);
        WorkspacePath = workspace.CurrentWorktreePath
            ?? workspace.Path
            ?? Environment.CurrentDirectory;
        WorkspaceDisplayName = workspace.Name;

        var persistedSessions = (await _chatSessionRepository
            .GetSessionsByWorkspaceAsync(_workspaceId, cancellationToken))
            .ToList();

        var emptySessions = persistedSessions
            .Where(session => session.Transcript.Count == 0)
            .ToList();

        foreach (var emptySession in emptySessions)
        {
            await _chatSessionRepository.DeleteSessionAsync(emptySession.Id, cancellationToken);
        }

        var sessions = persistedSessions
            .Where(session => session.Transcript.Count > 0)
            .Select(session => new ChatSessionViewModel(session))
            .OrderByDescending(session => session.UpdatedAt)
            .ToList();

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        if (Sessions.Count == 0)
        {
            SelectedSession = EnsureDraftSession();
            StatusMessage = "Ready to start a new session draft";
        }
        else
        {
            SelectedSession = Sessions[0];
            StatusMessage = $"Loaded {Sessions.Count} chat session(s)";
        }

        NotifySessionMetricsChanged();
        await ReloadActiveRunsAsync(cancellationToken);

        await LoadBillingStateAsync(cancellationToken);
        
        if (RecentWorkspaces.Count == 0)
        {
            RecentWorkspaces.Add(new WorkspaceViewModel { Name = "Takomi_Code", Path = @"C:\CreativeOS\01_Projects\Code\Takomi_Code", LastOpenedAt = DateTimeOffset.Now.AddMinutes(-12) });
            RecentWorkspaces.Add(new WorkspaceViewModel { Name = "Agent_Core_V2", Path = @"C:\CreativeOS\01_Projects\Code\Agent_Core_V2", LastOpenedAt = DateTimeOffset.Now.AddDays(-2) });
        }
        
        IsProjectOpen = openProjectShell;
        UpdateBagsState(workspace);
        RuntimeTarget = NormalizeRuntimeTarget(workspace.RuntimeTarget);
    }

    async partial void OnRuntimeTargetChanged(string value)
    {
        OnPropertyChanged(nameof(IsCloudTarget));
        OnPropertyChanged(nameof(IsLocalTarget));

        var normalizedValue = NormalizeRuntimeTarget(value);
        if (!string.Equals(value, normalizedValue, StringComparison.Ordinal))
        {
            RuntimeTarget = normalizedValue;
            return;
        }

        var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId);
        if (workspace != null && workspace.RuntimeTarget != value)
        {
            workspace.RuntimeTarget = normalizedValue;
            await _workspaceRepository.SaveWorkspaceAsync(workspace);
            StatusMessage = $"Runtime target switched to {normalizedValue}";
        }
    }

    public async Task ReloadActiveRunsAsync(CancellationToken cancellationToken = default)
    {
        var selectedRunId = SelectedActiveRun?.RunId;
        var runs = await _orchestrationRepository.GetActiveRunsAsync(cancellationToken);
        ActiveRuns.Clear();
        foreach (var run in runs)
        {
            ActiveRuns.Add(run);
        }

        SelectedActiveRun = runs.FirstOrDefault(run => string.Equals(run.RunId, selectedRunId, StringComparison.Ordinal))
            ?? ActiveRuns.FirstOrDefault();
        NotifyRunMetricsChanged();
    }

    public async Task CreateProjectChatAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        SelectedSession = EnsureDraftSession(title);
        DraftMessage = string.Empty;
        SelectedShellSection = "Sessions";
        StatusMessage = "Opened a blank session draft";
    }

    public async Task CreateChildSessionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null || SelectedSession.IsTransient)
        {
            StatusMessage = "Select a parent chat before creating a child session";
            return;
        }

        var child = SelectedSession.CreateChildSession(
            title: $"{SelectedSession.Title} / Child {GetChildCount(SelectedSession.Id) + 1}");

        await PersistNewSessionAsync(child, cancellationToken);
        SelectedShellSection = "Sessions";
        StatusMessage = $"Created child session under '{SelectedSession.Title}'";
    }

    public async Task SendDraftAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null)
        {
            SelectedSession = EnsureDraftSession();
        }

        if (string.IsNullOrWhiteSpace(DraftMessage))
        {
            StatusMessage = "Message draft is empty";
            return;
        }

        if (SelectedSession!.IsTransient)
        {
            if (string.IsNullOrWhiteSpace(SelectedSession.Title) || SelectedSession.Title == "New Chat")
            {
                SelectedSession.Title = $"Project Chat {GetNextSessionNumber()}";
            }

            await _chatSessionRepository.SaveSessionAsync(SelectedSession.GetEntity(), cancellationToken);
            SelectedSession.MarkPersisted();
            if (!Sessions.Contains(SelectedSession))
            {
                Sessions.Insert(0, SelectedSession);
                NotifySessionMetricsChanged();
            }

            _draftSession = null;
        }

        var prompt = DraftMessage.Trim();

        SelectedSession.AddMessage("User", prompt);
        DraftMessage = string.Empty;
        await _chatSessionRepository.SaveSessionAsync(SelectedSession.GetEntity(), cancellationToken);
        MoveSessionToTop(SelectedSession);
        await StartCodexRunForSessionAsync(SelectedSession, prompt, cancellationToken);
    }

    async partial void OnSelectedSessionChanged(ChatSessionViewModel? value)
    {
        OnPropertyChanged(nameof(CurrentSessionTitle));
        OnPropertyChanged(nameof(CurrentSessionWorktree));

        if (value is not null)
        {
            StatusMessage = $"Active session: {value.Title}";
            await RefreshGitStateAsync();
        }
    }

    partial void OnSelectedActiveRunChanged(OrchestrationRun? value)
    {
        OnPropertyChanged(nameof(HasSelectedRun));
        OnPropertyChanged(nameof(SelectedRunDisplayName));
        OnPropertyChanged(nameof(SelectedRunStatusText));
        OnPropertyChanged(nameof(SelectedRunTaskReference));
        OnPropertyChanged(nameof(SelectedRunWorkingDirectoryLabel));
        OnPropertyChanged(nameof(SelectedRunDetailText));
        OnPropertyChanged(nameof(SelectedRunDetailLabel));
        OnPropertyChanged(nameof(SelectedRunStatusBrushKey));
    }

    private async Task RefreshGitStateAsync()
    {
        if (SelectedSession is null) return;
        var path = await GetSelectedWorkspacePathAsync();
        var state = await _gitService.GetCurrentStateAsync(path);
        CurrentGitBranch = state.BranchName;
        IsGitDirty = state.IsDirty;
    }

    private async Task PersistNewSessionAsync(ChatSession session, CancellationToken cancellationToken)
    {
        await _chatSessionRepository.SaveSessionAsync(session, cancellationToken);
        var viewModel = new ChatSessionViewModel(session);
        Sessions.Insert(0, viewModel);
        SelectedSession = viewModel;
        NotifySessionMetricsChanged();
    }

    private int GetChildCount(string parentSessionId)
    {
        return Sessions.Count(session => session.ParentSessionId == parentSessionId);
    }

    private ChatSessionViewModel EnsureDraftSession(string? title = null)
    {
        if (_draftSession is not null)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                _draftSession.Title = title;
            }

            return _draftSession;
        }

        var session = new ChatSession
        {
            WorkspaceId = _workspaceId,
            Title = title ?? "New Chat"
        };

        _draftSession = new ChatSessionViewModel(session, isTransient: true);
        return _draftSession;
    }

    private int GetNextSessionNumber()
    {
        var maxNumber = Sessions
            .Select(session => session.Title)
            .Select(title =>
            {
                const string prefix = "Project Chat ";
                if (!title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                return int.TryParse(title[prefix.Length..], out var value) ? value : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return maxNumber + 1;
    }

    private void MoveSessionToTop(ChatSessionViewModel session)
    {
        var index = Sessions.IndexOf(session);
        if (index <= 0)
        {
            return;
        }

        Sessions.RemoveAt(index);
        Sessions.Insert(0, session);
        SelectedSession = session;
        NotifySessionMetricsChanged();
    }

    private async Task StartCodexRunForSessionAsync(
        ChatSessionViewModel session,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspacePath = await GetSelectedWorkspacePathAsync();
            var orchestrationSession = await _orchestratorExecutionEngine.InitiateSessionAsync(_workspaceId, prompt, cancellationToken);
            var (taskFilePath, outputFilePath) = BuildRoutedTaskPaths(orchestrationSession.SessionId, session.Id);

            await WriteRoutedTaskEnvelopeAsync(taskFilePath, session, prompt, workspacePath, cancellationToken);

            var task = await _orchestratorExecutionEngine.AddTaskAsync(
                orchestrationSession.SessionId,
                BuildTaskName(prompt),
                prompt,
                targetMode: "mode-code",
                taskFilePath: taskFilePath,
                executionCommand: BuildCodexExecutionCommand(session, prompt, outputFilePath),
                workingDirectoryOverride: workspacePath,
                cancellationToken: cancellationToken);

            var run = await _orchestratorExecutionEngine.StartTaskExecutionAsync(
                task.Id,
                chatSessionId: session.Id,
                cancellationToken: cancellationToken);

            _runOutputFiles[run.RunId] = outputFilePath;
            _runSessionIds[run.RunId] = session.Id;
            _runErrorLines[run.RunId] = new List<string>();

            session.AddMessage("Assistant", $"Routing request to Codex in {Path.GetFileName(workspacePath)}...");
            await _chatSessionRepository.SaveSessionAsync(session.GetEntity(), cancellationToken);

            await ReloadActiveRunsAsync(cancellationToken);
            SelectedActiveRun = ActiveRuns.FirstOrDefault(activeRun => string.Equals(activeRun.RunId, run.RunId, StringComparison.Ordinal))
                ?? run;

            StatusMessage = $"Codex is working on '{session.Title}'.";
        }
        catch (Exception ex)
        {
            session.AddMessage("Assistant", $"Unable to start a Codex run: {ex.Message}");
            await _chatSessionRepository.SaveSessionAsync(session.GetEntity(), cancellationToken);
            StatusMessage = $"Route failed: {ex.Message}";
        }
    }

    private void OnCodexRuntimeStateChanged(object? sender, CodexRuntimeStateEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () => await HandleCodexRuntimeStateChangedAsync(e));
    }

    private void OnCodexRuntimeOutputReceived(object? sender, CodexRuntimeOutputEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => HandleCodexRuntimeOutputReceived(e));
    }

    private void HandleCodexRuntimeOutputReceived(CodexRuntimeOutputEventArgs e)
    {
        if (!e.IsError || string.IsNullOrWhiteSpace(e.Content))
        {
            return;
        }

        if (!_runErrorLines.TryGetValue(e.RunId, out var errorLines))
        {
            errorLines = new List<string>();
            _runErrorLines[e.RunId] = errorLines;
        }

        errorLines.Add(e.Content.Trim());
        if (errorLines.Count > 12)
        {
            errorLines.RemoveAt(0);
        }
    }

    private async Task HandleCodexRuntimeStateChangedAsync(CodexRuntimeStateEventArgs e)
    {
        switch (e.State)
        {
            case CodexRuntimeState.Starting:
                StatusMessage = "Launching Codex run...";
                break;
            case CodexRuntimeState.Running:
                StatusMessage = "Codex run in progress...";
                break;
            case CodexRuntimeState.Completed:
                await FinalizeTrackedRunAsync(e.RunId, e.Details, cancellationToken: CancellationToken.None);
                break;
            case CodexRuntimeState.Cancelled:
                await FinalizeTrackedRunAsync(e.RunId, e.Details ?? "Run cancelled.", isFailure: true, cancellationToken: CancellationToken.None);
                break;
            case CodexRuntimeState.Failed:
                await FinalizeTrackedRunAsync(e.RunId, e.Details, isFailure: true, cancellationToken: CancellationToken.None);
                break;
        }

        await ReloadActiveRunsAsync(CancellationToken.None);
        if (SelectedActiveRun is null && ActiveRuns.Count > 0)
        {
            SelectedActiveRun = ActiveRuns[0];
        }
    }

    private async Task FinalizeTrackedRunAsync(
        string runId,
        string? details,
        bool isFailure = false,
        CancellationToken cancellationToken = default)
    {
        var session = await TryResolveRunSessionAsync(runId, cancellationToken);
        if (session is null)
        {
            CleanupTrackedRun(runId);
            StatusMessage = details ?? (isFailure ? "Codex run failed." : "Codex run completed.");
            return;
        }

        string finalMessage;
        if (isFailure)
        {
            finalMessage = BuildFailureMessage(runId, details);
        }
        else
        {
            finalMessage = await ReadRunOutputMessageAsync(runId, cancellationToken)
                ?? details
                ?? "Codex finished without returning a final message.";
        }

        session.AddMessage("Assistant", finalMessage);
        await _chatSessionRepository.SaveSessionAsync(session.GetEntity(), cancellationToken);
        MoveSessionToTop(session);
        CleanupTrackedRun(runId);
        StatusMessage = isFailure ? "Codex run failed." : "Codex run completed.";
    }

    private async Task<ChatSessionViewModel?> TryResolveRunSessionAsync(string runId, CancellationToken cancellationToken)
    {
        if (_runSessionIds.TryGetValue(runId, out var sessionId))
        {
            return Sessions.FirstOrDefault(session => string.Equals(session.Id, sessionId, StringComparison.Ordinal))
                ?? (SelectedSession is not null && string.Equals(SelectedSession.Id, sessionId, StringComparison.Ordinal)
                    ? SelectedSession
                    : null);
        }

        var run = await _orchestrationRepository.GetRunAsync(runId, cancellationToken);
        if (string.IsNullOrWhiteSpace(run?.ChatSessionId))
        {
            return null;
        }

        return Sessions.FirstOrDefault(session => string.Equals(session.Id, run.ChatSessionId, StringComparison.Ordinal))
            ?? (SelectedSession is not null && string.Equals(SelectedSession.Id, run.ChatSessionId, StringComparison.Ordinal)
                ? SelectedSession
                : null);
    }

    private async Task<string?> ReadRunOutputMessageAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_runOutputFiles.TryGetValue(runId, out var outputPath) || !File.Exists(outputPath))
        {
            return null;
        }

        var message = await File.ReadAllTextAsync(outputPath, cancellationToken);
        return string.IsNullOrWhiteSpace(message) ? null : message.Trim();
    }

    private string BuildFailureMessage(string runId, string? details)
    {
        if (_runErrorLines.TryGetValue(runId, out var errorLines) && errorLines.Count > 0)
        {
            var joinedErrors = string.Join(Environment.NewLine, errorLines);
            return $"Codex run failed.{Environment.NewLine}{Environment.NewLine}{joinedErrors}";
        }

        return string.IsNullOrWhiteSpace(details)
            ? "Codex run failed."
            : $"Codex run failed.{Environment.NewLine}{Environment.NewLine}{details}";
    }

    private void CleanupTrackedRun(string runId)
    {
        _runOutputFiles.Remove(runId);
        _runSessionIds.Remove(runId);
        _runErrorLines.Remove(runId);
    }

    private static string BuildTaskName(string prompt)
    {
        var singleLinePrompt = NormalizePromptForExecution(prompt);
        const int maxLength = 48;
        return singleLinePrompt.Length <= maxLength
            ? singleLinePrompt
            : $"{singleLinePrompt[..maxLength].TrimEnd()}...";
    }

    private static (string TaskFilePath, string OutputFilePath) BuildRoutedTaskPaths(string orchestrationSessionId, string chatSessionId)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode",
            "routed-tasks");

        var pendingDirectory = Path.Combine(root, "pending");
        var completedDirectory = Path.Combine(root, "completed");
        Directory.CreateDirectory(pendingDirectory);
        Directory.CreateDirectory(completedDirectory);

        var baseName = $"{orchestrationSessionId}-{chatSessionId[..Math.Min(8, chatSessionId.Length)]}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        return (
            Path.Combine(pendingDirectory, $"{baseName}.task.md"),
            Path.Combine(completedDirectory, $"{baseName}.result.md"));
    }

    private static async Task WriteRoutedTaskEnvelopeAsync(
        string taskFilePath,
        ChatSessionViewModel session,
        string prompt,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        var content = $$"""
            # Routed Session Task

            - Session: {{session.Title}}
            - ChatSessionId: {{session.Id}}
            - Workspace: {{workspacePath}}
            - RoutedAt: {{DateTimeOffset.UtcNow:O}}

            ## Prompt

            {{prompt}}
            """;

        await File.WriteAllTextAsync(taskFilePath, content, cancellationToken);
    }

    private string BuildCodexExecutionCommand(ChatSessionViewModel session, string prompt, string outputFilePath)
    {
        var transcriptContext = BuildTranscriptContext(session);
        var executionPrompt = $$"""
            You are handling a routed request from the Takomi desktop app inside the active repository workspace.
            Execute the user's latest request now.

            Rules:
            - Do not introduce yourself.
            - Do not ask what to work on.
            - Do not reply with workflow acknowledgements or generic Takomi setup text.
            - Use the repository instructions and relevant Takomi guidance only as implementation constraints.
            - If the user asked for file/code changes, make them directly in this workspace before replying.
            - If the user asked a simple conversational question, answer it briefly.
            - End with a concise summary of what you changed or why you were blocked.

            Recent conversation context:
            {{transcriptContext}}

            Latest user request:
            {{prompt}}
            """;

        return string.Join(
            " ",
            "exec",
            "--skip-git-repo-check",
            "--dangerously-bypass-approvals-and-sandbox",
            "--color",
            "never",
            "-o",
            QuoteCommandArgument(outputFilePath),
            QuoteCommandArgument(executionPrompt));
    }

    private static string BuildTranscriptContext(ChatSessionViewModel session)
    {
        var recentMessages = session.Messages
            .TakeLast(6)
            .Select(message => $"{message.Role}: {NormalizePromptForExecution(message.Content)}");

        return string.Join(Environment.NewLine, recentMessages);
    }

    private static string NormalizePromptForExecution(string prompt)
    {
        return string.Join(" ", prompt
            .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string QuoteCommandArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var builder = new StringBuilder();
        var needsQuotes = value.Any(character => char.IsWhiteSpace(character) || character == '"');
        if (!needsQuotes)
        {
            return value;
        }

        builder.Append('"');
        var backslashCount = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append(character);
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount);
                backslashCount = 0;
            }

            builder.Append(character);
        }

        if (backslashCount > 0)
        {
            builder.Append('\\', backslashCount * 2);
        }

        builder.Append('"');
        return builder.ToString();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task PauseRunAsync() => await ExecuteInterventionAsync(InterventionAction.Pause);

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ResumeRunAsync() => await ExecuteInterventionAsync(InterventionAction.Resume);

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task CancelRunAsync() => await ExecuteInterventionAsync(InterventionAction.Cancel);

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task RerouteRunAsync() => await ExecuteInterventionAsync(InterventionAction.Reroute, "Reroute requested");

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ReplaceRunAsync() => await ExecuteInterventionAsync(InterventionAction.Replace, "Replace requested");

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task MigrateRunAsync() => await ExecuteInterventionAsync(InterventionAction.Migrate, "Migrate requested");

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task InjectGuidanceAsync() => await ExecuteInterventionAsync(InterventionAction.InjectGuidance, "Guidance requested");

    private async Task ExecuteInterventionAsync(InterventionAction action, string? payload = null)
    {
        if (SelectedActiveRun is null)
        {
            StatusMessage = "Select an active run first.";
            return;
        }

        try
        {
            StatusMessage = $"Sending {action} to run {SelectedActiveRun.RunId}...";
            await _interventionCommandHandler.ExecuteInterventionAsync(SelectedActiveRun.RunId, action, payload);
            StatusMessage = $"Sent {action} successfully.";
            await ReloadActiveRunsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Action '{action}' failed: {ex.Message}";
        }
    }

    private async Task LoadBillingStateAsync(CancellationToken cancellationToken = default)
    {
        var entitlement = await _billingService.GetEntitlementAsync(_workspaceId, cancellationToken);
        var pendingCheckout = await _billingService.GetPendingCheckoutAsync(_workspaceId, cancellationToken);
        UpdateBillingState(entitlement);
        UpdatePendingCheckoutState(pendingCheckout);
    }

    private void UpdateBillingState(BillingEntitlement? entitlement)
    {
        IsProActive = entitlement?.IsActive ?? false;
        BillingStatusText = IsProActive
            ? $"{entitlement!.Provider} Pro active (since {entitlement.ActivatedAt:d})"
            : "Free Tier";
    }

    private void UpdatePendingCheckoutState(BillingCheckoutSession? checkout)
    {
        BillingCheckoutUrl = checkout?.CheckoutUrl ?? string.Empty;
        PendingBillingReference = checkout?.ReferenceId ?? string.Empty;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task InitiateCheckoutAsync()
    {
        try
        {
            StatusMessage = "Initiating Paystack checkout...";
            var url = await _billingService.CreateCheckoutSessionAsync(_workspaceId, BillingEmail);
            var pendingCheckout = await _billingService.GetPendingCheckoutAsync(_workspaceId);
            UpdatePendingCheckoutState(pendingCheckout);
            StatusMessage = $"Checkout session started. Complete Paystack checkout at: {url}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Checkout failed: {ex.Message}";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ConfirmCheckoutSuccessAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingBillingReference))
        {
            StatusMessage = "Start a Paystack checkout before confirming payment.";
            return;
        }

        try
        {
            var entitlement = await _billingService.ActivateEntitlementAsync(_workspaceId, PendingBillingReference);
            UpdateBillingState(entitlement);
            UpdatePendingCheckoutState(null);
            StatusMessage = "Paystack checkout success recorded. Pro tier activated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Activation failed: {ex.Message}";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task CreateWorktreeAsync()
    {
        if (SelectedSession is null)
        {
            StatusMessage = "Select a session before creating a worktree.";
            return;
        }

        var sourcePath = await GetSelectedWorkspacePathAsync();
        var worktreeName = $"wt-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var branchName = $"branch-{worktreeName}";

        try
        {
            var newPath = await _gitService.CreateWorktreeAsync(sourcePath, worktreeName, branchName);
            TargetWorktreePath = newPath;
            await AppendWorkspaceAuditAsync("workspace.worktree_created", $"Created worktree '{newPath}' on branch '{branchName}'.");
            await SwitchWorktreeInternallyAsync(newPath, "Create Worktree");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create worktree failed: {ex.Message}";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task SwitchWorktreeAsync()
    {
        if (SelectedSession is null)
        {
            StatusMessage = "Select a session before switching worktrees.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetWorktreePath))
        {
            StatusMessage = "Enter a target worktree path.";
            return;
        }

        try
        {
            await _gitService.EnsureWorktreeIsValidAsync(TargetWorktreePath);
            await SwitchWorktreeInternallyAsync(TargetWorktreePath, "Switch Worktree");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Switch worktree failed: {ex.Message}";
        }
    }

    private async Task SwitchWorktreeInternallyAsync(string targetWorktreePath, string reason)
    {
        if (SelectedSession is null)
        {
            return;
        }

        var oldWorktreePath = SelectedSession.WorktreePath;
        var subtreeSessions = GetSessionSubtree(SelectedSession.Id);

        foreach (var session in subtreeSessions)
        {
            if (session.Id != SelectedSession.Id
                && !string.Equals(session.WorktreePath, oldWorktreePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            session.UpdateWorktreePath(targetWorktreePath);
            await _chatSessionRepository.SaveSessionAsync(session.GetEntity());
        }

        var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId);
        if (workspace is not null)
        {
            workspace.CurrentWorktreePath = targetWorktreePath;
            await _workspaceRepository.SaveWorkspaceAsync(workspace);
            WorkspacePath = workspace.CurrentWorktreePath ?? workspace.Path;
        }

        await AppendWorkspaceAuditAsync(
            "workspace.worktree_switched",
            $"{reason}: Switched subtree rooted at '{SelectedSession.Title}' to '{targetWorktreePath}' ({subtreeSessions.Count} session(s) updated).");

        TargetWorktreePath = targetWorktreePath;
        OnPropertyChanged(nameof(CurrentSessionWorktree));
        await RefreshGitStateAsync();
        StatusMessage = $"Switched session to worktree '{targetWorktreePath}'";
    }

    private async Task<string> GetSelectedWorkspacePathAsync()
    {
        if (SelectedSession?.WorktreePath is { Length: > 0 } worktreePath)
        {
            return worktreePath;
        }

        var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId);
        return workspace?.CurrentWorktreePath
            ?? workspace?.Path
            ?? Environment.CurrentDirectory;
    }

    private List<ChatSessionViewModel> GetSessionSubtree(string rootSessionId)
    {
        var sessionsByParent = Sessions
            .Where(session => !string.IsNullOrWhiteSpace(session.ParentSessionId))
            .GroupBy(session => session.ParentSessionId!)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new List<ChatSessionViewModel>();
        var queue = new Queue<string>();
        queue.Enqueue(rootSessionId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentSession = Sessions.FirstOrDefault(session => session.Id == currentId);
            if (currentSession is null)
            {
                continue;
            }

            result.Add(currentSession);
            if (!sessionsByParent.TryGetValue(currentId, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private async Task AppendWorkspaceAuditAsync(string eventType, string description)
    {
        if (SelectedSession is null)
        {
            return;
        }

        var audit = new TakomiCode.Domain.Events.AuditEvent
        {
            WorkspaceId = _workspaceId,
            ChatSessionId = SelectedSession.Id,
            EventType = eventType,
            Description = description
        };

        await _auditLogRepository.AppendEventAsync(audit);
    }

    private async Task<Workspace> EnsureWorkspaceExistsAsync(CancellationToken cancellationToken = default)
    {
        var defaultWorkspacePath = ResolveDefaultWorkspacePath();
        var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId, cancellationToken);
        if (workspace is not null)
        {
            if (string.IsNullOrWhiteSpace(workspace.Path) || LooksLikeBuildOutputPath(workspace.Path) || !Directory.Exists(workspace.Path))
            {
                workspace.Path = defaultWorkspacePath;
            }

            if (string.IsNullOrWhiteSpace(workspace.CurrentWorktreePath) || !Directory.Exists(workspace.CurrentWorktreePath))
            {
                workspace.CurrentWorktreePath = workspace.Path;
            }

            workspace.Name = string.IsNullOrWhiteSpace(workspace.Name) ? "Takomi Code Workspace" : workspace.Name;
            workspace.RuntimeTarget = NormalizeRuntimeTarget(workspace.RuntimeTarget);
            workspace.IsAttached = true;
            await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);
            return workspace;
        }

        workspace = new Workspace
        {
            Id = _workspaceId,
            Name = "Takomi Code Workspace",
            Path = defaultWorkspacePath,
            CurrentWorktreePath = defaultWorkspacePath,
            IsAttached = true
        };

        await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);
        return workspace;
    }

    private static string NormalizeRuntimeTarget(string? runtimeTarget)
    {
        return string.Equals(runtimeTarget, "Cloud", StringComparison.OrdinalIgnoreCase)
            ? "Cloud"
            : "Local";
    }

    private static string ResolveDefaultWorkspacePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var gitPath = Path.Combine(current.FullName, ".git");
            var solutionPath = Path.Combine(current.FullName, "src", "TakomiCode.sln");
            if (Directory.Exists(gitPath) || File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static bool LooksLikeBuildOutputPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateBagsState(Workspace? workspace)
    {
        BagsTokenAddress = workspace?.BagsTokenAddress ?? string.Empty;
        IsVerificationReady = workspace?.IsVerificationReady ?? false;
    }

    partial void OnWorkspacePathChanged(string value)
    {
        OnPropertyChanged(nameof(WorkspaceName));
    }

    private void NotifySessionMetricsChanged()
    {
        OnPropertyChanged(nameof(SessionCount));
        OnPropertyChanged(nameof(HasSessions));
    }

    private void NotifyRunMetricsChanged()
    {
        OnPropertyChanged(nameof(ActiveRunCount));
        OnPropertyChanged(nameof(BlockedRunCount));
        OnPropertyChanged(nameof(HealthyRunCount));
        OnPropertyChanged(nameof(HasActiveRuns));
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task LinkBagsTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(BagsTokenAddress))
        {
            StatusMessage = "Enter a Bags token address before linking.";
            return;
        }

        StatusMessage = "Linking Bags token...";
        try
        {
            var trimmedTokenAddress = BagsTokenAddress.Trim();
            await _bagsService.LinkTokenToWorkspaceAsync(_workspaceId, trimmedTokenAddress);
            var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId);
            UpdateBagsState(workspace);
            StatusMessage = "Bags token linked successfully.";
            await CheckVerificationReadinessAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Bags token linking failed: {ex.Message}";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task CheckVerificationReadinessAsync()
    {
        StatusMessage = "Checking Bags verification readiness...";
        try
        {
            await _bagsService.CheckVerificationReadinessAsync(_workspaceId);
            var workspace = await _workspaceRepository.GetWorkspaceAsync(_workspaceId);
            UpdateBagsState(workspace);
            StatusMessage = "Verification readiness updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Bags check failed: {ex.Message}";
        }
    }
}
