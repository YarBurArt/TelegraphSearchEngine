using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TelegraphSearchEngine.Data;
using TelegraphSearchEngine.Models;
using TelegraphSearchEngine.Services;

namespace TelegraphSearchEngine.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IArticleRepository _repository;
    private readonly ITelegraphSearchService _searchService;
    private readonly RelayCommand _searchCommand;

    private string _nameValue = "anon";
    private string _languageValue = "en";
    private bool _advancedMode;
    private bool _isSearching;
    private int _statusValue;
    private string _statusMessage = "Enter an article name and press “Start search”.";

    public MainViewModel(IArticleRepository repository, ITelegraphSearchService searchService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _searchCommand = new RelayCommand(_ => SearchAsync(), _ => !IsSearching);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string NameValue
    {
        get => _nameValue;
        set => SetField(ref _nameValue, value);
    }

    public string LanguageValue
    {
        get => _languageValue;
        set => SetField(ref _languageValue, value);
    }

    public bool AdvancedMode
    {
        get => _advancedMode;
        set => SetField(ref _advancedMode, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (SetField(ref _isSearching, value))
            {
                _searchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int StatusValue
    {
        get => _statusValue;
        private set => SetField(ref _statusValue, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<ArticleModel> Results { get; } = [];

    public ICommand SearchCommand => _searchCommand;

    private async Task SearchAsync()
    {
        var name = string.IsNullOrWhiteSpace(NameValue) ? "anon" : NameValue.Trim();
        var language = string.IsNullOrWhiteSpace(LanguageValue) ? "en" : LanguageValue.Trim().ToLowerInvariant();
        var slug = UrlFunctions.BuildSlug(name, language);

        IsSearching = true;
        StatusValue = 0;
        Results.Clear();

        string? dbWarning = null;

        try
        {
            // brute-force telegra.ph; a url that answers becomes an article row
            var urls = UrlFunctions.GenerateArticleUrls(slug, AdvancedMode);
            var progress = new Progress<int>(value => StatusValue = value);
            var found = await _searchService.FindArticlesAsync(urls, progress);

            found.Sort((left, right) => string.CompareOrdinal(left.Url, right.Url));
            StatusValue = 100;
            AddResults(found);

            if (found.Count == 0)
            {
                StatusMessage = $"No articles found for “{slug}”. Check the article name and language.";
                return;
            }

            try
            {
                await _repository.SaveArticlesAsync(found);
            }
            catch (Exception exception)
            {
                dbWarning = exception.Message;
            }

            StatusMessage = dbWarning is null
                ? $"Found {found.Count} article(s) for “{slug}”, saved to the database."
                : $"Found {found.Count} article(s) for “{slug}”. Database unavailable: {dbWarning}";
        }
        catch (OperationCanceledException)
        {
            // the progress bar must not keep a mid-scan value on early exit
            StatusValue = 0;
            StatusMessage = "Search cancelled.";
        }
        catch (Exception exception)
        {
            StatusValue = 0;
            StatusMessage = $"Search failed: {exception.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private void AddResults(IEnumerable<ArticleModel> articles)
    {
        foreach (var article in articles)
        {
            Results.Add(article);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
