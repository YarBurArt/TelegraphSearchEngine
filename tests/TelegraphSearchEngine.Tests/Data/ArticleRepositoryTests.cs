using TelegraphSearchEngine.Models;
using TelegraphSearchEngine.Tests.TestSupport;

namespace TelegraphSearchEngine.Tests.Data;

public sealed class ArticleRepositoryTests
{
    [Fact]
    public async Task FindArticlesAsync_MatchesUrlsAndKeywords()
    {
        using var database = new TempDatabase();
        await database.Repository.SaveArticlesAsync(
        [
            new ArticleModel { Url = "https://telegra.ph/shifr-1-2", Keywords = "cipher, plain, text" },
            new ArticleModel { Url = "https://telegra.ph/sparrow-3-4", Keywords = "birds, nest" }
        ]);

        var byUrl = await database.Repository.FindArticlesAsync("shifr", "шифр");
        var byKeywords = await database.Repository.FindArticlesAsync("sparrow", "birds");

        Assert.Single(byUrl);
        Assert.Equal("https://telegra.ph/shifr-1-2", byUrl[0].Url);
        Assert.Single(byKeywords);
        Assert.Equal("https://telegra.ph/sparrow-3-4", byKeywords[0].Url);
    }

    [Fact]
    public async Task SaveArticlesAsync_UpsertsByUniqueUrlAndRefreshesKeywords()
    {
        using var database = new TempDatabase();
        var repository = database.Repository;

        await repository.SaveArticlesAsync(
        [
            new ArticleModel { Url = "https://telegra.ph/shifr-1-2", Keywords = "old" }
        ]);
        await repository.SaveArticlesAsync(
        [
            new ArticleModel { Url = "https://telegra.ph/shifr-1-2", Keywords = "fresh" },
            new ArticleModel { Url = "https://telegra.ph/shifr-3-4", Keywords = "new" }
        ]);

        var rows = await repository.FindArticlesAsync("shifr", "shifr");

        Assert.Equal(2, rows.Count);
        Assert.Equal(
            "fresh",
            rows.Single(row => row.Url == "https://telegra.ph/shifr-1-2").Keywords);
        Assert.Equal(2L, await database.CountRowsAsync("articles"));
    }

    [Fact]
    public async Task FindArticlesAsync_UserWildcardsStayLiteral()
    {
        using var database = new TempDatabase();
        await database.Repository.SaveArticlesAsync(
        [
            new ArticleModel { Url = "https://telegra.ph/abc-def-1-1", Keywords = "letters" }
        ]);

        Assert.Empty(await database.Repository.FindArticlesAsync("a%b", "a%b"));
        Assert.Single(await database.Repository.FindArticlesAsync("abc-def", "abc"));
    }
}
