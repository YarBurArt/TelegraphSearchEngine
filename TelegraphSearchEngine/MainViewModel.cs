using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TelegraphSearchEngine
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

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

        public MainViewModel()
        {
            Task.Factory.StartNew(() =>
            {
                while (StatusValue <= 35)
                {
                    Task.Delay(1000).Wait();
                    StatusValue++;
                }
            });
        }


        public ICommand ClickStartSearch
        {
            get
            {
                return new RelayCommand((obj) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        // after pressing start, the values in the fields are processed and you show the result in MessageBox 
                        var urlfunc = new UrlFunctions();
                        var tasks = new List<Task<byte>>();

                        var urls = UrlFunctions.GenerateArticleUrl(
                            NameValue ?? "anon",
                            LangValue ?? "en",
                            false // old (checkBox1.Content.ToString() == "Checked") ?? false
                            );
                        var urls_result = new List<string>();
                        // to each his own task, an asynchronous task
                        GenerateTasks(ref tasks, urls, ref urlfunc, ref urls_result);

                        // run test url for 200 response 
                        Task.WhenAll(tasks);
                        
                        // join to fit on the screen
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal, 
                            new DispatcherOperationCallback(delegate
                            {
                                   StatusValue = 100;
                                   Outrext window_out = new Outrext();
                                   window_out.Show();
                                   window_out.textOutput.Text = String.Join("\n", urls_result);
                                   StatusValue = 1;
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
            foreach (var url in urls)
            {
                try
                {
                    var task = urlfunc.GetStatusUrl(url);
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            Application.Current.Dispatcher.BeginInvoke(
               System.Windows.Threading.DispatcherPriority.Normal
               , new DispatcherOperationCallback(delegate
               {
                   StatusValue = 50;
                   return null;
               }), null);
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Result == 1)
                    urls_result.Add(urls[i]);
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
            foreach (var ch in source)
            {
                string reverse_ch = "";
                if (dictionaryChar.TryGetValue(ch.ToString(), out reverse_ch!)) // get vaule by key 
                {
                    result += reverse_ch;
                }
                else result += ch;
            }
            return result;
        }
    }
}
