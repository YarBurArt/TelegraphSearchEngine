using System.Windows;

namespace TelegraphSearchEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var connectionString = "Data Source=./SQLEXPRESS;Initial Catalog=articledb;Integrated Security=True";
            var articleRepository = new ArticleRepository(connectionString);

            var mainViewModel = new MainViewModel(articleRepository);
            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }

}
