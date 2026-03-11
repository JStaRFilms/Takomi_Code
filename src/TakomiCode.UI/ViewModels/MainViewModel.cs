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

    public MainViewModel(
        IChatSessionRepository chatSessionRepository,
        IWorkspaceRepository workspaceRepository,
        IOrchestrationRepository orchestrationRepository,
        TakomiCode.Application.Contracts.Services.IInterventionCommandHandler interventionCommandHandler,
        TakomiCode.Application.Contracts.Services.IGitService gitService,
        IAuditLogRepository auditLogRepository)
    {
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
        _orchestrationRepository = orchestrationRepository;
        _interventionCommandHandler = interventionCommandHandler;
        _gitService = gitService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var workspace = new Workspace
        {
            Id = DefaultWorkspaceId,
            Name = "Takomi Code Workspace",
            Path = Environment.CurrentDirectory,
            IsAttached = true
        };

        await _workspaceRepository.SaveWorkspaceAsync(workspace, cancellationToken);

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
            SessionId = SelectedSession.Id,
            WorkspaceId = DefaultWorkspaceId,
            EventType = eventType,
            Description = description
        };

        await _auditLogRepository.AppendEventAsync(audit);
    }
}
