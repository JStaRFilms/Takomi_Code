using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using TakomiCode.Application.Configuration;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TakomiCode.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly string _workspaceId;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IOrchestrationRepository _orchestrationRepository;
    private readonly TakomiCode.Application.Contracts.Services.IInterventionCommandHandler _interventionCommandHandler;
    private readonly TakomiCode.Application.Contracts.Services.IGitService _gitService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly TakomiCode.Application.Contracts.Services.IBillingService _billingService;
    private readonly TakomiCode.Application.Contracts.Services.IBagsService _bagsService;

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
    public bool IsHomeSection => SelectedShellSection == "Home";
    public bool IsSessionsSection => SelectedShellSection == "Sessions";
    public bool IsWorktreesSection => SelectedShellSection == "Worktrees";
    public bool IsBillingSection => SelectedShellSection == "Billing";
    public bool IsSettingsSection => SelectedShellSection == "Settings";
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
        TakomiCode.Application.Contracts.Services.IInterventionCommandHandler interventionCommandHandler,
        TakomiCode.Application.Contracts.Services.IGitService gitService,
        IAuditLogRepository auditLogRepository,
        TakomiCode.Application.Contracts.Services.IBillingService billingService,
        TakomiCode.Application.Contracts.Services.IBagsService bagsService)
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

        var sessions = (await _chatSessionRepository
            .GetSessionsByWorkspaceAsync(_workspaceId, cancellationToken))
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
            await CreateProjectChatAsync(cancellationToken: cancellationToken);
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
        var runs = await _orchestrationRepository.GetActiveRunsAsync(cancellationToken);
        ActiveRuns.Clear();
        foreach (var run in runs)
        {
            ActiveRuns.Add(run);
        }

        NotifyRunMetricsChanged();
    }

    public async Task CreateProjectChatAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            WorkspaceId = _workspaceId,
            Title = title ?? $"Project Chat {Sessions.Count + 1}"
        };

        await PersistNewSessionAsync(session, cancellationToken);
        SelectedShellSection = "Sessions";
        StatusMessage = $"Created chat '{session.Title}'";
    }

    public async Task CreateChildSessionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null)
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
            StatusMessage = "Select a chat before sending a message";
            return;
        }

        if (string.IsNullOrWhiteSpace(DraftMessage))
        {
            StatusMessage = "Message draft is empty";
            return;
        }

        SelectedSession.AddMessage("User", DraftMessage.Trim());
        DraftMessage = string.Empty;
        await _chatSessionRepository.SaveSessionAsync(SelectedSession.GetEntity(), cancellationToken);
        MoveSessionToTop(SelectedSession);
        StatusMessage = $"Saved transcript for '{SelectedSession.Title}'";
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
