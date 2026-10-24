using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TelegraphSearchEngine.Models;

namespace TelegraphSearchEngine.Services;

// telegra.ph slugs look like article-name-M-D, and article-name-N-M-D when
// several articles share a day; N is an index, not a date part.
public sealed partial class UrlFunctions : ITelegraphSearchService
{
    private const string BaseUrl = "https://telegra.ph/";

    // brute-forcing 372 urls sequentially is too slow, but hundreds of open
    // sockets at once is rude to the site
    private const int MaxConcurrentRequests = 8;

    // when this many requests failed and nothing was found, the site is
    // probably unreachable; stop instead of waiting out every timeout
    private const int MaxFailedRequestsBeforeGivingUp = 20;

    private static readonly char[] WordSeparators =
    [
        ' ', '\t', '\n', '\r', '\u00A0', ',', '.', ';', ':', '!', '?', '"', '\'', '«', '»',
        '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '+', '=', '—', '–', '_'
    ];

    private readonly HttpClient _httpClient;

    public UrlFunctions() : this(CreateHttpClient()) { }

    // tests inject a client with a fake handler instead of calling the real site
    public UrlFunctions(HttpClient httpClient) => _httpClient = httpClient;

    public static string BuildSlug(string articleName, string language)
    {
        var name = articleName.Trim();

        if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
        {
            name = Translit.Transliterate(name);
        }

        return name.Replace(' ', '-').Replace('/', '-');
    }

    public static List<string> GenerateArticleUrls(string slug, bool includeIndexedVariants)
    {
        var urls = new List<string>(includeIndexedVariants ? 12 * 31 * 11 : 12 * 31);

        for (var month = 1; month <= 12; month++)
        {
            for (var day = 1; day <= 31; day++)
            {
                if (includeIndexedVariants)
                {
                    for (var index = 1; index <= 10; index++)
                    {
                        urls.Add($"{BaseUrl}{slug}-{index}-{month}-{day}");
                    }
                }

                urls.Add($"{BaseUrl}{slug}-{month}-{day}");
            }
        }

        return urls;
    }

    public async Task<List<ArticleModel>> FindArticlesAsync(
        IReadOnlyList<string> urls,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var found = new List<ArticleModel>();
        var lockObject = new object();
        var completed = 0;
        var failed = 0;
        var foundCount = 0;

        using var giveUp = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentRequests,
            CancellationToken = giveUp.Token
        };

        try
        {
            await Parallel.ForEachAsync(urls, options, async (url, token) =>
            {
                try
                {
                    var article = await CheckUrlAsync(url, token).ConfigureAwait(false);

                    if (article is not null)
                    {
                        lock (lockObject)
                        {
                            found.Add(article);
                        }

                        Interlocked.Increment(ref foundCount);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    if (giveUp.IsCancellationRequested)
                    {
                        throw;
                    }

                    // one request timed out; treat it as "not found" and keep scanning
                    Interlocked.Increment(ref failed);
                }
                catch (HttpRequestException)
                {
                    Interlocked.Increment(ref failed);
                }

                var done = Interlocked.Increment(ref completed);
                if (done == urls.Count || done % 5 == 0)
                {
                    progress?.Report(done * 100 / urls.Count);
                }

                if (Volatile.Read(ref failed) >= MaxFailedRequestsBeforeGivingUp
                    && Volatile.Read(ref foundCount) == 0)
                {
                    giveUp.Cancel();
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // the give-up guard fired, not the caller; handled below
        }

        if (urls.Count > 0
            && Volatile.Read(ref failed) >= MaxFailedRequestsBeforeGivingUp
            && found.Count == 0)
        {
            throw new InvalidOperationException(
                "Telegra.ph did not respond to any request. Check your internet connection.");
        }

        return found;
    }

    public async Task<ArticleModel?> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        return new ArticleModel
        {
            Url = url,
            Keywords = string.Join(", ", GetTopFrequentWords(ExtractPlainText(html), 62))
        };
    }

    private static string ExtractPlainText(string html)
    {
        // script and style bodies are not article text; drop them before removing tags
        var withoutNoise = NoiseBlockRegex().Replace(html, " ");
        var withoutTags = TagRegex().Replace(withoutNoise, " ");
        return WebUtility.HtmlDecode(withoutTags);
    }

    private static List<string> GetTopFrequentWords(string text, int topN)
    {
        return text
            .ToLowerInvariant()
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 5 && word.Any(char.IsLetter))
            .GroupBy(word => word, StringComparer.Ordinal)
            .Select(group => new { Word = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Word, StringComparer.Ordinal)
            .Take(topN)
            .Select(item => item.Word)
            .ToList();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TelegraphSearchEngine/1.0");
        return client;
    }

    [GeneratedRegex("<(script|style|noscript)[^>]*>.*?</\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NoiseBlockRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();
}
