using Microsoft.Data.SqlClient;
using SqlQueryNotifications.Interfaces;
using SqlQueryNotifications.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlQueryNotifications
{
    public class QuerySource : IQuerySource
    {
        private readonly string _connectionString;

        public QuerySource(string senderName, string connectionString)
        {
            SenderName = senderName;
            _connectionString = connectionString;
        }

        public string SenderName { get; }

        public SqlConnection GetConnection() => new SqlConnection(_connectionString);
        
        public Query[] Queries { get; set; }

        public async Task<IEnumerable<Query>> GetQueriesAsync() => await Task.FromResult(Queries);        
    }
}
