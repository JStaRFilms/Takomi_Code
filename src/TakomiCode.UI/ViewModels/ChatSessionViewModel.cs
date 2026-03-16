using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TakomiCode.Domain.Entities;

namespace TakomiCode.UI.ViewModels;

public partial class ChatSessionViewModel : ObservableObject
{
    private readonly ChatSession _entity;
    private bool _isTransient;
    
    public ChatSessionViewModel(ChatSession entity, bool isTransient = false)
    {
        _entity = entity;
        _isTransient = isTransient;
        Messages = new ObservableCollection<ChatMessageViewModel>(
            _entity.Transcript.Select(m => new ChatMessageViewModel(m))
        );
    }
    
    public string Id => _entity.Id;
    public string WorkspaceId => _entity.WorkspaceId;
    public string? ParentSessionId => _entity.ParentSessionId;
    public bool IsChildSession => !string.IsNullOrWhiteSpace(_entity.ParentSessionId);
    
    public string Title
    {
        get => _entity.Title;
        set
        {
            if (_entity.Title != value)
            {
                _entity.Title = value;
                _entity.UpdatedAt = DateTimeOffset.UtcNow;
                OnPropertyChanged();
            }
        }
    }
    
    public string? ModeSlug => _entity.ModeSlug;
    public string? WorktreePath => _entity.WorktreePath;
    public DateTimeOffset UpdatedAt => _entity.UpdatedAt;
    public DateTimeOffset CreatedAt => _entity.CreatedAt;
    public bool IsTransient => _isTransient;
    public bool HasMessages => _entity.Transcript.Count > 0;

    public string LastActiveFormatted => UpdatedAt.LocalDateTime.ToString("g");

    public ObservableCollection<ChatMessageViewModel> Messages { get; }
    
    public ChatMessageViewModel AddMessage(string role, string content)
    {
        var msg = new ChatMessage
        {
            Role = role,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };
        _entity.Transcript.Add(msg);
        _entity.UpdatedAt = DateTimeOffset.UtcNow;
        var viewModel = new ChatMessageViewModel(msg);
        Messages.Add(viewModel);
        OnPropertyChanged(nameof(UpdatedAt));
        return viewModel;
    }

    public void UpdateMessage(ChatMessageViewModel message, string content)
    {
        if (!Messages.Contains(message))
        {
            return;
        }

        message.Content = content;
        _entity.UpdatedAt = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(UpdatedAt));
    }

    public void AppendToMessage(ChatMessageViewModel message, string content, bool insertBlankLine = true)
    {
        if (!Messages.Contains(message) || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var trimmed = content.Trim();
        message.Content = string.IsNullOrWhiteSpace(message.Content)
            ? trimmed
            : insertBlankLine
                ? $"{message.Content}{Environment.NewLine}{Environment.NewLine}{trimmed}"
                : $"{message.Content}{trimmed}";

        _entity.UpdatedAt = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(UpdatedAt));
    }

    // Helper to spawn a child session in the same workspace (inheritance preserved)
    public ChatSession CreateChildSession(string title, string? modeSlug = null, string? overrideWorktreePath = null)
    {
        var child = new ChatSession
        {
            WorkspaceId = this.WorkspaceId,
            ParentSessionId = this.Id,
            Title = title,
            ModeSlug = modeSlug ?? this.ModeSlug,
            WorktreePath = overrideWorktreePath ?? this.WorktreePath
        };
        return child;
    }

    public void UpdateWorktreePath(string? worktreePath)
    {
        if (_entity.WorktreePath == worktreePath)
        {
            return;
        }

        _entity.WorktreePath = worktreePath;
        _entity.UpdatedAt = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(WorktreePath));
        OnPropertyChanged(nameof(UpdatedAt));
    }

    public void MarkPersisted()
    {
        if (!_isTransient)
        {
            return;
        }

        _isTransient = false;
        OnPropertyChanged(nameof(IsTransient));
    }
    
    public ChatSession GetEntity() => _entity;
}
