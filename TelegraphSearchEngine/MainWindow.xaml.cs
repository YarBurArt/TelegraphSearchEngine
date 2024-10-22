using System.Windows;

namespace TelegraphSearchEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// TODO: rewrite outrext to list
    /// TODO: add filters 
    /// TODO: add neural network for word context
    /// TODO: optimize 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var connectionString = "Data Source=./SQLEXPRESS;Initial Catalog=articledb;Integrated Security=True";
            var articleRepository = new ArticleRepository(connectionString);
            DataContext = new MainViewModel(articleRepository);
        }

    }
}
