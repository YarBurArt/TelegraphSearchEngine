using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TelegraphSearchEngine
{
    class MainViewModel : INotifyPropertyChanged
    {
        // to respond to events 
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        // progress bar number 
        private int _StatusValue;
        public int StatusValue
        {
            get { return _StatusValue; }
            set
            {
                _StatusValue = value;
                OnPropertyChanged();
            }
        }
        // the name of the article in the Telegraph, possibly spaces
        private string _NameValue = "anon";
        public string NameValue
        {
            get { return _NameValue; }
            set
            {
                _NameValue = value;
                OnPropertyChanged();
            }
        }
        // language for search, for translite to work 
        private string _LangValue = "en";
        public string LangValue
        {
            get { return _LangValue; }
            set
            {
                _LangValue = value;
                OnPropertyChanged();
            }
        }
        private readonly IArticleRepository _articleRepository;

        public MainViewModel(IArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }

        private string _keywords;
        public string Keywords
        {
            get { return _keywords; }
            set
            {
                _keywords = value;
                OnPropertyChanged(nameof(Keywords));
                LoadArticles();
            }
        }

        private List<ArticleModel> _articles;
        public List<ArticleModel> Articles
        {
            get { return _articles; }
            set
            {
                _articles = value;
                OnPropertyChanged(nameof(Articles));
            }
        }

        private async void LoadArticles()
        {
            Articles = await _articleRepository.GetArticlesByKeywordsAsync(Keywords);
        }

        private ICommand _saveArticleCommand;
        public ICommand SaveArticleCommand
        {
            get
            {
                if (_saveArticleCommand == null)
                {
                    _saveArticleCommand = new RelayCommand(async (object n) =>
                    {
                        var newArticle = new ArticleModel
                        {
                            Url = "https://example.com",
                            Keywords = "example, keyword"
                        };
                        await _articleRepository.SaveArticleAsync(newArticle);
                    });
                }
                return _saveArticleCommand;
            }
        }

        public ICommand ClickStartSearch
        {
            get
            {
                return new RelayCommand((obj) =>
                {
                    Task.Factory.StartNew(async () => // async for UI updates
                    {
                        // after pressing start, the values in the fields are processed show the result in MessageBox 
                        var urlfunc = new UrlFunctions();
                        var tasks = new List<Task<byte>>();
                        StatusValue = 0;

                        var urls = UrlFunctions.GenerateArticleUrl(
                            NameValue ?? "anon",
                            LangValue ?? "en",
                            false // (checkBox1.Content.ToString() == "Checked") ?? false
                            );
                        var urls_result = new List<string>();
                        // to each his own task, an asynchronous task
                        GenerateTasks(ref tasks, urls, ref urlfunc, ref urls_result);
                        // Run each task and update StatusValue accordingly
                        for (int i = 0; i < tasks.Count; i++)
                        {
                            await tasks[i]; // Using Wait to block until the task is complete

                            // update StatusValue to reflect progress
                            StatusValue = (i + 1) * 100 / tasks.Count; // Example: 0%, 33%, 66%, 100%
                            await Task.Delay(1);
                        }

                        // after all tasks are completed, update the UI to default 
                        await Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            new DispatcherOperationCallback(delegate
                            {
                                StatusValue = 100; // set StatusValue to 100% upon completion
                                Outrext window_out = new Outrext();
                                window_out.Show();
                                window_out.textOutput.Text = string.Join("\n", urls_result);
                                StatusValue = 1; // reset StatusValue
                                return null;
                            }),
                        null);
                    });
                });
            }
        }
        private void GenerateTasks(
            ref List<Task<byte>> tasks, List<string> urls, 
            ref UrlFunctions urlfunc, ref List<string> urls_result)
        {
            // Declaring an enumeration of urls
            foreach (var url in urls)
            {
                // Trying to get the status URL corresponding to the current URL
                try
                {
                    var task = urlfunc.GetStatusUrl(url);
                    tasks.Add(task);
                }
                // Catching httpClient.GetAsync exceptions and displaying a message box
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            // Calling the Windows dispatcher to update Status in ProgressBar from a background thread
            Application.Current.Dispatcher.BeginInvoke(
               System.Windows.Threading.DispatcherPriority.Normal
               , new DispatcherOperationCallback(delegate
               {
                   StatusValue = 1;
                   return null; 
               }), null);
            for (int i = 0; i < tasks.Count; i++)
            {
                // Check if the current task was successful
                if (tasks[i].Result == 1)
                {
                    // Add the current URL to the list of successful URLs
                    urls_result.Add(urls[i]);
                }
            }
        }
    }
    public class UrlFunctions
    {
        HttpClient httpClient = new HttpClient();
        public static List<string> GenerateArticleUrl(string article_name, string request_language, bool isadvanced)
        {
            // creating a url by adding title, date and article number
            Translit translit = new Translit();
            var urls = new List<string>();
            string base_url = "https://telegra.ph/";
            string url;

            if (request_language != "en")
                article_name = translit.TranslitName(article_name);

            article_name = article_name.Replace(" ", "-");
            for (int i = 1; i < 13; i++) // month article
            {
                for (int j = 1; j < 32; j++) // day article
                {
                    string i_s = i.ToString(), j_s = j.ToString();
                    if (isadvanced) // crutch for optimizing the number of requests
                    {
                        for (int k = 1; k < 11; k++) // index article
                        {
                            url = base_url + article_name + "-" + k.ToString() + "-" + i_s + "-" + j_s;
                            urls.Add(url);
                        }
                    }
                    // any case due to the specifics of url tg operation
                    url = base_url + article_name + "-" + i_s + "-" + j_s;
                    urls.Add(url);

                }
            }
            return urls;
        }
        public async Task<byte> GetStatusUrl(string url)
        {
            // check url status, this list is then filtered out
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            if (response is { StatusCode: HttpStatusCode.OK }) return 1; // response.StatusCode
            return 0;
        }
    }
    public class Translit
    {
        // translit russian text just like the telegraph does
        // example: шифр -> shifr (translate like cipher)
        Dictionary<string, string> dictionaryChar = new Dictionary<string, string>()
        {
            {"а","a"},  {"б","b"},  {"в","v"},   {"г","g"}, {"д","d"}, {"е","e"},
            {"ё","yo"}, {"ж","zh"}, {"з","z"},   {"и","i"}, {"й","y"}, {"к","k"},
            {"л","l"},  {"м","m"},  {"н","n"},   {"о","o"}, {"п","p"}, {"р","r"},
            {"с","s"},  {"т","t"},  {"у","u"},   {"ф","f"}, {"х","h"}, {"ц","ts"},
            {"ч","ch"}, {"ш","sh"}, {"щ","sch"}, {"ъ","'"}, {"ы","yi"},
            {"ь",""},   {"э","e"},  {"ю","yu"},  {"я","ya"}
        };
        public string TranslitName(string source)
        {
            // replacing each character with the corresponding one in the dictionary or leave it as it is
            string result = "";
            // each character in the source string
            foreach (var ch in source)
            {
                string reverse_ch = "";
                // check if the reverse character for ch is in the dictionary
                if (dictionaryChar.TryGetValue(ch.ToString(), out reverse_ch!))
                {
                    result += reverse_ch;
                }
                // implies that the other characters in the target language
                else result += ch;
            }
            return result;
        }
    }
}
