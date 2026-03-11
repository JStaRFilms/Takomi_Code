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

    public MainViewModel(
        IChatSessionRepository chatSessionRepository,
        IWorkspaceRepository workspaceRepository,
        IOrchestrationRepository orchestrationRepository,
        TakomiCode.Application.Contracts.Services.IInterventionCommandHandler interventionCommandHandler)
    {
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
        _orchestrationRepository = orchestrationRepository;
        _interventionCommandHandler = interventionCommandHandler;
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

    partial void OnSelectedSessionChanged(ChatSessionViewModel? value)
    {
        OnPropertyChanged(nameof(CurrentSessionTitle));
        OnPropertyChanged(nameof(CurrentSessionWorktree));

        if (value is not null)
        {
            StatusMessage = $"Active session: {value.Title}";
        }
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
}
