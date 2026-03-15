using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TakomiCode.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TakomiCode.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        Title = "Takomi Code";
        ViewModel = App.Host!.Services.GetRequiredService<MainViewModel>();
        this.RootGrid.DataContext = ViewModel;
        SelectSection("Home");
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

    private void ShellNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string section)
        {
            SelectSection(section);
        }
    }

    private void ShellShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string section)
        {
            SelectSection(section);
        }
    }

    private void SelectSection(string section)
    {
        ViewModel.SelectShellSection(section);
        HomeSection.Visibility = section == "Home" ? Visibility.Visible : Visibility.Collapsed;
        SessionsSection.Visibility = section == "Sessions" ? Visibility.Visible : Visibility.Collapsed;
        WorktreesSection.Visibility = section == "Worktrees" ? Visibility.Visible : Visibility.Collapsed;
        BillingSection.Visibility = section == "Billing" ? Visibility.Visible : Visibility.Collapsed;
        SettingsSection.Visibility = section == "Settings" ? Visibility.Visible : Visibility.Collapsed;

        var targetItem = section switch
        {
            "Sessions" => SessionsNavItem,
            "Worktrees" => WorktreesNavItem,
            "Billing" => BillingNavItem,
            "Settings" => SettingsNavItem,
            _ => HomeNavItem
        };

        if (!ReferenceEquals(ShellNavigation.SelectedItem, targetItem))
        {
            ShellNavigation.SelectedItem = targetItem;
        }
    }
}
