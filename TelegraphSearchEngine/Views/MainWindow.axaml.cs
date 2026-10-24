using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TelegraphSearchEngine.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void OnOpenArticle(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url } && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            // the system shell decides
            await topLevel.Launcher.LaunchUriAsync(new Uri(url));
        }
    }
}
