using TelegraphSearchEngine.Models;
using TelegraphSearchEngine.Services;

namespace TelegraphSearchEngine.Tests.TestSupport;

internal sealed class FakeTelegraphSearchService : ITelegraphSearchService
{
    public List<ArticleModel> Results { get; set; } = [];

    public Exception? Error { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<string>? LastUrls { get; private set; }

    public Task<List<ArticleModel>> FindArticlesAsync(
        IReadOnlyList<string> urls,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastUrls = urls;

        if (Error is not null)
        {
            return Task.FromException<List<ArticleModel>>(Error);
        }

        progress?.Report(100);
        return Task.FromResult(new List<ArticleModel>(Results));
    }
}
