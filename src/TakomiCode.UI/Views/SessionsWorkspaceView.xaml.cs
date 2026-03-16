using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TakomiCode.UI.ViewModels;

namespace TakomiCode.UI.Views;

public sealed partial class SessionsWorkspaceView : UserControl
{
    public MainViewModel? ViewModel => DataContext as MainViewModel;

    public SessionsWorkspaceView()
    {
        InitializeComponent();
        Loaded += SessionsWorkspaceView_Loaded;
    }

    private void SessionsWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is null)
        {
            DataContext = App.Host?.Services.GetRequiredService<MainViewModel>();
        }
    }

    private async void CreateChatButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.CreateProjectChatAsync();
        }
    }

    private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.SendDraftAsync();
        }
    }
}
