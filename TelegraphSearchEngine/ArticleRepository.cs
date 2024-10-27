using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
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
        private readonly string _connectionString;
        private SqlConnection? connection = null;

        public ArticleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ArticleModel>> GetArticlesByKeywordsAsync(string keywords)
        {
            using (connection = new SqlConnection(_connectionString))
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
            using (connection = new SqlConnection(_connectionString))
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
