using Microsoft.Data.SqlClient;
using SqlQueryNotifications.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlQueryNotifications.Interfaces
{
    /// <summary>
    /// defines a source of where queries can be defined to use with the notification service
    /// </summary>
    public interface IQuerySource
    {
        Task<IEnumerable<Query>> GetQueriesAsync();
        SqlConnection GetConnection();
    }
}
