using Microsoft.Data.Sqlite;
using TelegraphSearchEngine.Data;

namespace TelegraphSearchEngine.Tests.TestSupport;

internal sealed class TempDatabase : IDisposable
{
    private readonly string _directory;

    public TempDatabase()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "TelegraphSearchEngineTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_directory);
        DatabasePath = Path.Combine(_directory, "articles.db");
        Repository = new ArticleRepository(DatabasePath);
    }

    public string DatabasePath { get; }

    public ArticleRepository Repository { get; }

    public async Task<long> CountRowsAsync(string table)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public void Dispose()
    {
        // pooling is process-wide; clear it so the temp files can be deleted right away
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // best effort: a locked file must not fail the test run
        }
    }
}
