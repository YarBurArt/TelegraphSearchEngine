using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace TelegraphSearchEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// TODO: add filters and neural network, optimize, beautiful GUI
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // after pressing start, the values in the fields are processed and you show the result in MessageBox 
            pbStatus.Value = 10;
            var urlfunc = new UrlFunctions();
            var tasks = new List<Task<byte>>();

            var urls = UrlFunctions.GenerateArticleUrl(
                textBox1.Text ?? "anon", 
                textBox2.Text ?? "en", 
                checkBox1.Content.ToString() == "Checked"
                );
            pbStatus.Value = 15;
            var urls_result = new List<string>();
            // to each his own task, an asynchronous task
            foreach (var url in urls)
            {
                var task = urlfunc.GetStatusUrl(url);
                tasks.Add(task);
            }
            pbStatus.Value = 30;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Result == 1)
                    urls_result.Add(urls[i]);
            }
            pbStatus.Value = 35;
            await Task.WhenAll(tasks);
            pbStatus.Value = 100;
            // join to fit on the screen
            string message = String.Join("\n", urls_result);
            Outrext window_out = new Outrext();
            window_out.Show();
            window_out.textOutput.Text = message;

            
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
                {"а","a"}, {"б","b"}, {"в","v"}, {"г","g"}, {"д","d"}, {"е","e"},
                {"ё","yo"}, {"ж","zh"}, {"з","z"}, {"и","i"}, {"й","y"}, {"к","k"},
                {"л","l"}, {"м","m"}, {"н","n"}, {"о","o"}, {"п","p"}, {"р","r"},
                {"с","s"}, {"т","t"}, {"у","u"}, {"ф","f"}, {"х","h"}, {"ц","ts"},
                {"ч","ch"}, {"ш","sh"}, {"щ","sch"}, {"ъ","'"}, {"ы","yi"},
                {"ь",""}, {"э","e"}, {"ю","yu"}, {"я","ya"}
                };
        public string TranslitName(string source)
        {
            // replacing each character with the corresponding one in the dictionary or leave it as it is
            var result = "";
            foreach (var ch in source)
            {
                var ss = "";
                if (dictionaryChar.TryGetValue(ch.ToString(), out ss)) // get vaule by key 
                {
                    result += ss;
                }
                else result += ch;
            }
            return result;
        }
    }
}
