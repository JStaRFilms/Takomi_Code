using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TakomiCode.Domain.Entities;

namespace TakomiCode.UI.ViewModels;

public partial class ChatMessageViewModel : ObservableObject
{
    private readonly ChatMessage _entity;

    public ChatMessageViewModel(ChatMessage entity)
    {
        _entity = entity;
    }

    public string Id => _entity.Id;
    public string Role => _entity.Role;
    
    public string Content 
    {
        get => _entity.Content;
        set 
        {
            if (_entity.Content != value)
            {
                _entity.Content = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    
    public DateTimeOffset Timestamp => _entity.Timestamp;
}
