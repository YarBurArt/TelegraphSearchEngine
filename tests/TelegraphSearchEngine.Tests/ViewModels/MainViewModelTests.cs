using TelegraphSearchEngine.Models;
using TelegraphSearchEngine.Tests.TestSupport;
using TelegraphSearchEngine.ViewModels;

namespace TelegraphSearchEngine.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task Search_ShowsIndexHits_ThenScansAndSavesTheScan()
    {
        var repository = new FakeArticleRepository
        {
            Stored = [Article("https://telegra.ph/Hello-World-1-1")]
        };
        var search = new FakeTelegraphSearchService
        {
            Results =
            [
                // same url as the stored hit, with refreshed keywords
                Article("https://telegra.ph/Hello-World-1-1"),
                Article("https://telegra.ph/Hello-World-2-2")
            ]
        };
        var viewModel = CreateViewModel(repository, search, "Hello World");

        await RunSearchAsync(viewModel);

        Assert.Equal(2, viewModel.Results.Count);
        Assert.Equal("https://telegra.ph/Hello-World-1-1", viewModel.Results[0].Url);
        Assert.Equal(1, search.Calls);
        Assert.Equal("Hello-World", repository.LastUrlFragment);
        Assert.Equal(2, repository.Saved!.Count);
        Assert.Contains("index updated", viewModel.StatusMessage);
        Assert.Equal(100, viewModel.StatusValue);
        Assert.False(viewModel.IsSearching);
    }

    [Fact]
    public async Task Search_WhenNothingIsFoundAnywhere_ReportsItAndSavesNothing()
    {
        var repository = new FakeArticleRepository();
        var search = new FakeTelegraphSearchService { Results = [] };
        var viewModel = CreateViewModel(repository, search, "nothing-here");

        await RunSearchAsync(viewModel);

        Assert.Empty(viewModel.Results);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Contains("No articles found", viewModel.StatusMessage);
        Assert.Equal(100, viewModel.StatusValue);
    }

    [Fact]
    public async Task Search_WhenScanFindsOnlyIndexHits_SaysNothingIsNew()
    {
        var repository = new FakeArticleRepository
        {
            Stored = [Article("https://telegra.ph/Hello-World-1-1")]
        };
        var search = new FakeTelegraphSearchService { Results = [] };
        var viewModel = CreateViewModel(repository, search, "Hello World");

        await RunSearchAsync(viewModel);

        Assert.Single(viewModel.Results);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Contains("Nothing new on telegra.ph", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Search_WhenScanFails_TheProgressBarResets()
    {
        var repository = new FakeArticleRepository();
        var search = new FakeTelegraphSearchService
        {
            Error = new InvalidOperationException("telegra.ph is down")
        };
        var viewModel = CreateViewModel(repository, search, "Hello World");

        await RunSearchAsync(viewModel);

        // regression: the bar used to freeze at its last mid-scan value
        Assert.Equal(0, viewModel.StatusValue);
        Assert.Contains("Search failed", viewModel.StatusMessage);
        Assert.Equal(0, repository.SaveCalls);
        Assert.False(viewModel.IsSearching);
    }

    [Fact]
    public async Task Search_WhenCallerCancels_ReportsCancelled()
    {
        var repository = new FakeArticleRepository();
        var search = new FakeTelegraphSearchService
        {
            Error = new OperationCanceledException()
        };
        var viewModel = CreateViewModel(repository, search, "Hello World");

        await RunSearchAsync(viewModel);

        Assert.Equal(0, viewModel.StatusValue);
        Assert.Contains("cancelled", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(viewModel.IsSearching);
    }

    [Fact]
    public async Task Search_WhenIndexIsBroken_StillScansAndSaves()
    {
        var repository = new FakeArticleRepository
        {
            FindError = new InvalidOperationException("database is gone")
        };
        var search = new FakeTelegraphSearchService
        {
            Results = [Article("https://telegra.ph/shifr-1-1")]
        };
        var viewModel = CreateViewModel(repository, search, "шифр", "ru");

        await RunSearchAsync(viewModel);

        Assert.Single(viewModel.Results);
        Assert.Equal(1, search.Calls);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Contains("Index unavailable", viewModel.StatusMessage);
    }

    private static MainViewModel CreateViewModel(
        FakeArticleRepository repository,
        FakeTelegraphSearchService search,
        string name,
        string language = "en") => new(repository, search)
    {
        NameValue = name,
        LanguageValue = language
    };

    private static Task RunSearchAsync(MainViewModel viewModel) =>
        ((RelayCommand)viewModel.SearchCommand).ExecuteAsync(null);

    private static ArticleModel Article(string url) => new()
    {
        Url = url,
        Keywords = "sample, keywords"
    };
}
