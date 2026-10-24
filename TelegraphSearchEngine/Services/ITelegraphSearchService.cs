using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegraphSearchEngine.Models;

namespace TelegraphSearchEngine.Services;

public interface ITelegraphSearchService
{
    // checks every url in parallel and returns the ones that exist, with
    // extracted keywords; throws when telegra.ph is unreachable and nothing
    // was found, caller cancellation propagates as operationcanceledexception
    Task<List<ArticleModel>> FindArticlesAsync(
        IReadOnlyList<string> urls,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
