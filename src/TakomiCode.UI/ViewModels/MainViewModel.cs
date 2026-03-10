using CommunityToolkit.Mvvm.ComponentModel;

namespace TakomiCode.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Welcome to Takomi Code Orchestrator";

    public MainViewModel()
    {
    }
}
