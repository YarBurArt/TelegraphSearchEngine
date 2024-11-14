using System.Collections.Generic;
using System.Threading.Tasks;
//using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;

using Dapper;

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
                )
            {
                await connection.OpenAsync();

                var query = "SELECT * FROM articles WHERE CONTAINS(keywords, @keywords)";
                var articles = await connection.QueryAsync<ArticleModel>(query, new { keywords });
                List<ArticleModel> articles_aslist = articles.ToList();

                return articles_aslist;
            }
        }

        public async Task SaveArticleAsync(ArticleModel article)
        {
            using (
                var connection = new SQLiteConnection(_connectionString)
                // new SqlConnection(_connectionString) // MS SQL
                )
            {
                await connection.OpenAsync();

                var query = "INSERT INTO articles (url, keywords) VALUES (@Url, @Keywords)";
                await connection.ExecuteAsync(query, article);
            }
        }
    }

    public interface IArticleRepository
    {
        Task<List<ArticleModel>> GetArticlesByKeywordsAsync(string keywords);
        Task SaveArticleAsync(ArticleModel article);
    }
}
