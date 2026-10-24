using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TelegraphSearchEngine.Data;
using TelegraphSearchEngine.Services;
using TelegraphSearchEngine.ViewModels;
using TelegraphSearchEngine.Views;

namespace TelegraphSearchEngine;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var repository = new ArticleRepository(AppPaths.DatabasePath);
            var viewModel = new MainViewModel(repository, new UrlFunctions());

            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
