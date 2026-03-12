using System.Collections.ObjectModel;
using System.Linq;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TakomiCode.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string DefaultWorkspaceId = "workspace-default";
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
    private ChatSessionViewModel? _selectedSession;

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private OrchestrationRun? _selectedActiveRun;

    public ObservableCollection<ChatSessionViewModel> Sessions { get; } = new();
    public ObservableCollection<OrchestrationRun> ActiveRuns { get; } = new();

    public string CurrentSessionTitle => SelectedSession?.Title ?? "No session selected";
    public string CurrentSessionWorktree => SelectedSession?.WorktreePath ?? "Same workspace / default worktree";

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
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
        _orchestrationRepository = orchestrationRepository;
        _interventionCommandHandler = interventionCommandHandler;
        _gitService = gitService;
        _auditLogRepository = auditLogRepository;
        _billingService = billingService;
        _bagsService = bagsService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var workspace = await EnsureWorkspaceExistsAsync(cancellationToken);

        var sessions = (await _chatSessionRepository
            .GetSessionsByWorkspaceAsync(DefaultWorkspaceId, cancellationToken))
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

        await ReloadActiveRunsAsync(cancellationToken);

        await LoadBillingStateAsync(cancellationToken);
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

        var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId);
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
    }

    public async Task CreateProjectChatAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            WorkspaceId = DefaultWorkspaceId,
            Title = title ?? $"Project Chat {Sessions.Count + 1}"
        };

        await PersistNewSessionAsync(session, cancellationToken);
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
        var entitlement = await _billingService.GetEntitlementAsync(DefaultWorkspaceId, cancellationToken);
        var pendingCheckout = await _billingService.GetPendingCheckoutAsync(DefaultWorkspaceId, cancellationToken);
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
            var url = await _billingService.CreateCheckoutSessionAsync(DefaultWorkspaceId, BillingEmail);
            var pendingCheckout = await _billingService.GetPendingCheckoutAsync(DefaultWorkspaceId);
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
            var entitlement = await _billingService.ActivateEntitlementAsync(DefaultWorkspaceId, PendingBillingReference);
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

        var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId);
        if (workspace is not null)
        {
            workspace.CurrentWorktreePath = targetWorktreePath;
            await _workspaceRepository.SaveWorkspaceAsync(workspace);
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

        var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId);
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
            WorkspaceId = DefaultWorkspaceId,
            ChatSessionId = SelectedSession.Id,
            EventType = eventType,
            Description = description
        };

        await _auditLogRepository.AppendEventAsync(audit);
    }

    private async Task<Workspace> EnsureWorkspaceExistsAsync(CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId, cancellationToken);
        if (workspace is not null)
        {
            workspace.Path = string.IsNullOrWhiteSpace(workspace.Path) ? Environment.CurrentDirectory : workspace.Path;
            workspace.Name = string.IsNullOrWhiteSpace(workspace.Name) ? "Takomi Code Workspace" : workspace.Name;
            workspace.RuntimeTarget = NormalizeRuntimeTarget(workspace.RuntimeTarget);
            workspace.IsAttached = true;
            await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);
            return workspace;
        }

        workspace = new Workspace
        {
            Id = DefaultWorkspaceId,
            Name = "Takomi Code Workspace",
            Path = Environment.CurrentDirectory,
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

    private void UpdateBagsState(Workspace? workspace)
    {
        BagsTokenAddress = workspace?.BagsTokenAddress ?? string.Empty;
        IsVerificationReady = workspace?.IsVerificationReady ?? false;
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
            await _bagsService.LinkTokenToWorkspaceAsync(DefaultWorkspaceId, trimmedTokenAddress);
            var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId);
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
            await _bagsService.CheckVerificationReadinessAsync(DefaultWorkspaceId);
            var workspace = await _workspaceRepository.GetWorkspaceAsync(DefaultWorkspaceId);
            UpdateBagsState(workspace);
            StatusMessage = "Verification readiness updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Bags check failed: {ex.Message}";
        }
    }
}
