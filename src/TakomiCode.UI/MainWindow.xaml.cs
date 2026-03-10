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

    private void myButton_Click(object sender, RoutedEventArgs e)
    {
        myButton.Content = "Clicked";
    }
}
