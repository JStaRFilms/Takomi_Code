using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TakomiCode.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

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
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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
        if (sender is FrameworkElement element && element.Tag is string section)
        {
            SelectSection(section);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedShellSection))
        {
            SyncNavigationSelection(ViewModel.SelectedShellSection);
        }
    }

    private void SelectSection(string section)
    {
        ViewModel.SelectShellSection(section);
        SyncNavigationSelection(section);
    }

    private void SyncNavigationSelection(string section)
    {
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
