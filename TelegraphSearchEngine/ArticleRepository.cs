using System.Collections.Generic;
using System.Threading.Tasks;
//using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;

using Dapper;
using System;

namespace TelegraphSearchEngine
{
    public class ArticleModel
    {
        public int Id { get; set; }
        public required string Url { get; set; }
        public required string Keywords { get; set; }
    }

    public class ArticleRepository : IArticleRepository
    {
        private readonly string _connectionString = @"Data Source=mydatabase.db";
        //private SqlConnection? connection = null;
        private readonly SQLiteConnection _connection;

        public ArticleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        // TODO: special circumstances exceptions
        public async Task<List<ArticleModel>> GetArticlesByKeywordsAsync(string keywords)
        {
            using (
                var connection = new SQLiteConnection(_connectionString) 
                // new SqlConnection(_connectionString) // MS SQL
                ) {
                await connection.OpenAsync();
                try {
                    var query = "SELECT * FROM articles WHERE keywords LIKE '%' + @keywords + '%'";
                    var articles = await connection.QueryAsync<ArticleModel>(query, new { keywords });
                    List<ArticleModel> articles_aslist = articles.ToList();
                    return articles_aslist;
                }
                catch (Exception ex) {
                    // Check if the exception is due to the table not existing
                    if (ex is SQLiteException sqliteException && sqliteException.ErrorCode == 1 
                        && sqliteException.Message.Contains("no such table")) {

                        await CreateArticlesTableAsync(connection);
                        // Retry the query
                        var query = "SELECT * FROM articles WHERE keywords LIKE '%@keywords%'";
                        var articles = await connection.QueryAsync<ArticleModel>(query, new { keywords });
                        List<ArticleModel> articles_aslist = articles.ToList();
                        return articles_aslist;
                    } else { throw; }
                }
            }
        }

        private async Task CreateArticlesTableAsync(SQLiteConnection connection) {
            try {
                var createTableQuery = "CREATE TABLE articles (id INTEGER PRIMARY KEY AUTOINCREMENT, url TEXT, keywords TEXT)";
                await connection.ExecuteAsync(createTableQuery);
            }
            catch (SQLiteException ex) {
                // if call for a second time 
                if (ex.ErrorCode == 1 && ex.Message.Contains("table articles already exists")) return;
                else throw;
            }
        }

        public async Task SaveArticleAsync(ArticleModel article)
        {
            using (var connection = new SQLiteConnection(_connectionString)) {
                await connection.OpenAsync();
                var query = "INSERT INTO articles (url, keywords) VALUES (@Url, @Keywords)";
                try {
                    await connection.ExecuteAsync(query, article);
                }
                catch (Exception ex) {
                    // Check if the exception is due to the table not existing on start
                    if (ex is SQLiteException sqliteException && sqliteException.ErrorCode == 1 
                        && sqliteException.Message.Contains("no such table"))
                    {
                        await CreateArticlesTableAsync(connection); // Create the table
                        await connection.ExecuteAsync(query, article); // Retry the insert
                    } else { throw; }
                }
            }
        }
    }

    public interface IArticleRepository
    {
        Task<List<ArticleModel>> GetArticlesByKeywordsAsync(string keywords);
        Task SaveArticleAsync(ArticleModel article);
    }
}
