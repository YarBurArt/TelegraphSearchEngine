using System.Net;
using TelegraphSearchEngine.Services;

namespace TelegraphSearchEngine.Tests.Services;

public sealed class UrlFunctionsTests
{
    [Theory]
    [InlineData("шифр", "shifr")]
    [InlineData("Привет Мир", "Privet-Mir")]
    [InlineData("ёж", "yozh")]
    [InlineData("hello world", "hello-world")]
    public void BuildSlug_TransliteratesNonEnglishNames(string articleName, string expected)
    {
        Assert.Equal(expected, UrlFunctions.BuildSlug(articleName, "ru"));
    }

    [Fact]
    public void BuildSlug_KeepsEnglishNamesAsIs()
    {
        Assert.Equal("Hello-World", UrlFunctions.BuildSlug("Hello World", "en"));
    }

    [Fact]
    public void GenerateArticleUrls_Basic_CoversEveryDayOfTheYear()
    {
        var urls = UrlFunctions.GenerateArticleUrls("shifr", includeIndexedVariants: false);

        Assert.Equal(372, urls.Count);
        Assert.Equal("https://telegra.ph/shifr-1-1", urls[0]);
        Assert.Equal("https://telegra.ph/shifr-12-31", urls[^1]);
        Assert.Equal(urls.Count, urls.Distinct().Count());
    }

    [Fact]
    public void GenerateArticleUrls_Advanced_AddsTenIndexedVariantsPerDay()
    {
        var urls = UrlFunctions.GenerateArticleUrls("shifr", includeIndexedVariants: true);

        Assert.Equal(4092, urls.Count);
        Assert.Contains("https://telegra.ph/shifr-10-12-31", urls);
        Assert.Contains("https://telegra.ph/shifr-12-31", urls);
        Assert.Equal(urls.Count, urls.Distinct().Count());
    }

    [Fact]
    public async Task CheckUrlAsync_ExtractsKeywordsFromExistingPage()
    {
        var handler = new StubHandler();
        var service = new UrlFunctions(new HttpClient(handler));

        var article = await service.CheckUrlAsync("https://telegra.ph/shifr-1-1");
        var missing = await service.CheckUrlAsync("https://telegra.ph/shifr-2-2");

        Assert.NotNull(article);
        Assert.Equal("https://telegra.ph/shifr-1-1", article.Url);
        Assert.Contains("wonderful", article.Keywords);
        Assert.Null(missing);
    }

    [Fact]
    public async Task FindArticlesAsync_GivesUpWhenEverythingFailsAndNothingIsFound()
    {
        var handler = new FailingHandler();
        var service = new UrlFunctions(new HttpClient(handler));
        var urls = Enumerable.Range(1, 100).Select(i => $"https://telegra.ph/shifr-{i}").ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FindArticlesAsync(urls));

        // stopped early: at least the threshold, never all 100 requests
        Assert.InRange(handler.Calls, 20, 60);
    }

    [Fact]
    public async Task FindArticlesAsync_CallerCancellationPropagates()
    {
        var service = new UrlFunctions(new HttpClient(new HangingHandler()));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var urls = Enumerable.Range(1, 8).Select(i => $"https://telegra.ph/shifr-{i}").ToList();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.FindArticlesAsync(urls, cancellationToken: cts.Token));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var exists = request.RequestUri!.ToString() == "https://telegra.ph/shifr-1-1";
            var response = new HttpResponseMessage(exists ? HttpStatusCode.OK : HttpStatusCode.NotFound);

            if (exists)
            {
                response.Content = new StringContent(
                    "<html><body>wonderful article about the shifr cipher and wonderful things</body></html>");
            }

            return Task.FromResult(response);
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"));
        }
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // hangs until the caller gives up, so no give-up guard can fire
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
