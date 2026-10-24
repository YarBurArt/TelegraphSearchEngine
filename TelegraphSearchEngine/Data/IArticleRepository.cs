using System.Collections.Generic;
using System.Threading.Tasks;
using TelegraphSearchEngine.Models;

namespace TelegraphSearchEngine.Data;

public interface IArticleRepository
{
    // urlFragment is the transliterated slug, keywordFragment the raw name;
    // both are matched with sql LIKE against stored urls and keywords
    Task<IReadOnlyList<ArticleModel>> FindArticlesAsync(string urlFragment, string keywordFragment);

    // upserts by url: unknown urls are inserted, known ones get fresh keywords
    Task SaveArticlesAsync(IReadOnlyList<ArticleModel> articles);
}
