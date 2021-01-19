using DataTables.Library;
using DataTables.Library.Abstract;
using Microsoft.Extensions.Logging;
using SqlQueryNotifications.Interfaces;
using SqlQueryNotifications.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SqlQueryNotifications
{
    public class SqlQueryNotificationService
    {
        private readonly SmtpClient _emailClient;
        private readonly IEnumerable<IQuerySource> _querySources;
        private readonly QueryRunner _queryRunner;
        private readonly ILogger<SqlQueryNotificationService> _logger;

        public SqlQueryNotificationService(SmtpClient smtpClient, IEnumerable<IQuerySource> querySources, ILogger<SqlQueryNotificationService> logger, QueryRunner queryRunner = null)
        {
            _emailClient = smtpClient;
            _querySources = querySources;
            _queryRunner = queryRunner ?? new SqlServerQueryRunner();
            _logger = logger;
        }

        public SqlQueryNotificationService(SmtpClient smtpClient, IQuerySource querySource, ILogger<SqlQueryNotificationService> logger, QueryRunner queryRunner = null) : this(smtpClient, new IQuerySource[] { querySource }, logger, queryRunner)
        {
        }

        public async Task ExecuteAsync()
        {
            try
            {
                foreach (var querySource in _querySources)
                {
                    var queries = await querySource.GetQueriesAsync();

                    try
                    {
                        using (var cn = querySource.GetConnection())
                        {
                            foreach (var query in queries)
                            {
                                try
                                {
                                    var data = await _queryRunner.QueryTableAsync(cn, query.Sql);

                                    switch (query.SendRule)
                                    {
                                        case TriggerSendRule.IfAny when (data.AsEnumerable().Any()):
                                            break;

                                        case TriggerSendRule.IfEmpty when (!data.AsEnumerable().Any()):
                                            break;

                                        case TriggerSendRule.IfCustom when query.CustomSendRule?.Invoke(data) ?? false:
                                            break;
                                    }

                                }
                                catch (Exception exc)
                                {
                                    _logger.LogError($"Query failed: {query.Subject}");
                                    _logger.LogError(exc.Message);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError($"Connection failed: {exc.Message}");
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError($"Query source failed: {exc.Message}");
            }            
        }
    }
}
