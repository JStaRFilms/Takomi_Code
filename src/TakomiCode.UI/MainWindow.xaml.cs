using Microsoft.UI.Xaml;
using TakomiCode.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TakomiCode.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<MainViewModel>();
        this.RootGrid.DataContext = ViewModel;
    }

    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        await ViewModel.InitializeAsync();
    }

    private async void CreateChatButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateProjectChatAsync();
    }

    private async void CreateChildSessionButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateChildSessionAsync();
    }

    private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SendDraftAsync();
    }
}
