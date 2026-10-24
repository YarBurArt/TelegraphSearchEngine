using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using TelegraphSearchEngine.Models;

namespace TelegraphSearchEngine.Data;

public sealed class ArticleRepository : IArticleRepository
{
    private const string CreateArticlesTableSql =
        """
        CREATE TABLE IF NOT EXISTS articles (
            id       INTEGER PRIMARY KEY AUTOINCREMENT,
            url      TEXT    NOT NULL UNIQUE,
            keywords TEXT    NOT NULL DEFAULT ''
        );
        """;

    private readonly string _connectionString;
    private readonly Lazy<Task> _initialization;

    public ArticleRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        _initialization = new Lazy<Task>(InitializeAsync);
    }

    public async Task<IReadOnlyList<ArticleModel>> FindArticlesAsync(string urlFragment, string keywordFragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlFragment);

        await using var connection = await OpenConnectionAsync().ConfigureAwait(false);

        // like is case-insensitive for ascii, and keywords are stored lowercase
        var urlPattern = AsPattern(urlFragment);
        var keywordPattern = AsPattern(keywordFragment);

        var rows = await connection.QueryAsync<ArticleModel>(
            """
            SELECT id, url AS Url, keywords AS Keywords
            FROM articles
            WHERE url LIKE @urlPattern
               OR keywords LIKE @keywordPattern
            ORDER BY url
            """,
            new { urlPattern, keywordPattern }).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task SaveArticlesAsync(IReadOnlyList<ArticleModel> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);
        if (articles.Count == 0)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        // a re-scan may see changed article text, so refresh keywords on conflict
        await connection.ExecuteAsync(
            """
            INSERT INTO articles (url, keywords)
            VALUES (@Url, @Keywords)
            ON CONFLICT (url) DO UPDATE SET keywords = excluded.keywords
            """,
            articles,
            transaction).ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        await _initialization.Value.ConfigureAwait(false);

        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        // foreign keys are disabled by default in sqlite and must be enabled per connection
        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;").ConfigureAwait(false);

        return connection;
    }

    private async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(CreateArticlesTableSql).ConfigureAwait(false);
    }

    // like treats % and _ as wildcards, which user input must not control
    private static string AsPattern(string fragment) =>
        "%" + fragment
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_") + "%";
}
