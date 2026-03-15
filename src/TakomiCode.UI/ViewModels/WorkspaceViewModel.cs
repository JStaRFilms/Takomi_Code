using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TakomiCode.UI.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _lastOpenedAt;

    public string LastOpenedFormatted => LastOpenedAt.LocalDateTime.ToString("g");
}
