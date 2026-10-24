namespace TelegraphSearchEngine.Models;

public sealed class ArticleModel
{
    public long Id { get; set; }

    public string Url { get; set; } = string.Empty;

    // most frequent body words; shown to the user and searched over
    public string Keywords { get; set; } = string.Empty;
}
