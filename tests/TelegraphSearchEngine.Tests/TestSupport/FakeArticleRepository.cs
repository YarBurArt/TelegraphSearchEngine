using TelegraphSearchEngine.Data;
using TelegraphSearchEngine.Models;

namespace TelegraphSearchEngine.Tests.TestSupport;

internal sealed class FakeArticleRepository : IArticleRepository
{
    // what FindArticlesAsync hands back, and what SaveArticlesAsync received
    public IReadOnlyList<ArticleModel> Stored { get; set; } = [];

    public Exception? FindError { get; set; }

    public Exception? SaveError { get; set; }

    public IReadOnlyList<ArticleModel> Saved { get; private set; } = [];

    public string? LastUrlFragment { get; private set; }

    public string? LastKeywordFragment { get; private set; }

    public int FindCalls { get; private set; }

    public int SaveCalls { get; private set; }

    public Task<IReadOnlyList<ArticleModel>> FindArticlesAsync(string urlFragment, string keywordFragment)
    {
        FindCalls++;
        LastUrlFragment = urlFragment;
        LastKeywordFragment = keywordFragment;

        return FindError is null
            ? Task.FromResult(Stored)
            : Task.FromException<IReadOnlyList<ArticleModel>>(FindError);
    }

    public Task SaveArticlesAsync(IReadOnlyList<ArticleModel> articles)
    {
        SaveCalls++;

        if (SaveError is not null)
        {
            return Task.FromException(SaveError);
        }

        Saved = articles;
        return Task.CompletedTask;
    }
}
