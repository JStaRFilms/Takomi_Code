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

    [ObservableProperty]
    private string _statusMessage = "Welcome to Takomi Code Orchestrator";

    [ObservableProperty]
    private ChatSessionViewModel? _selectedSession;

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    public ObservableCollection<ChatSessionViewModel> Sessions { get; } = new();
    public string CurrentSessionTitle => SelectedSession?.Title ?? "No session selected";
    public string CurrentSessionWorktree => SelectedSession?.WorktreePath ?? "Same workspace / default worktree";

    public MainViewModel(
        IChatSessionRepository chatSessionRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _chatSessionRepository = chatSessionRepository;
        _workspaceRepository = workspaceRepository;
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
}
