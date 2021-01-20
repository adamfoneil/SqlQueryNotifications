using DataTables.Library;
using DataTables.Library.Abstract;
using Microsoft.Extensions.Logging;
using SqlQueryNotifications.Interfaces;
using SqlQueryNotifications.Internal;
using SqlQueryNotifications.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SqlQueryNotifications
{
    public class SqlQueryNotificationService
    {
        private readonly SmtpClient _smtpClient;
        private readonly IEnumerable<IQuerySource> _querySources;
        private readonly QueryRunner _queryRunner;
        private readonly ILogger<SqlQueryNotificationService> _logger;

        // macro that indicates where data is inserted in HTML body
        const string data = "%data%";

        public SqlQueryNotificationService(SmtpClient smtpClient, IEnumerable<IQuerySource> querySources, ILogger<SqlQueryNotificationService> logger, QueryRunner queryRunner = null)
        {
            _smtpClient = smtpClient;
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
                                            NotifyOnDataResult(querySource.SenderName, query, data);
                                            break;

                                        case TriggerSendRule.IfEmpty when (!data.AsEnumerable().Any()):
                                            NotifyOnEmptyResult(querySource.SenderName, query);
                                            break;

                                        case TriggerSendRule.IfCustom when query.CustomSendRule?.Invoke(data) ?? false:
                                            NotifyOnDataResult(querySource.SenderName, query, data);
                                            break;
                                    }

                                }
                                catch (Exception exc)
                                {
                                    _logger?.LogError($"Query failed: {query.Subject}");
                                    _logger?.LogError(exc.Message);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        _logger?.LogError($"Connection failed: {exc.Message}");
                    }
                }
            }
            catch (Exception exc)
            {
                _logger?.LogError($"Query source failed: {exc.Message}");
            }            
        }

        private void NotifyOnEmptyResult(string senderName, Query query)
        {
            try
            {
                var msg = CreateMessage(senderName, query);
                RemoveData(msg);
                _smtpClient.Send(msg);
                _logger?.LogInformation($"Sent empty result notification \"{msg.Subject}\" to {RecipientList(msg)}");
            }
            catch (Exception exc)
            {
                _logger?.LogError($"NotifyOnEmptyResult failed: {exc.Message}");
            }
        }

        private void RemoveData(MailMessage msg) => InsertData(msg, string.Empty);        

        private void NotifyOnDataResult(string senderName, Query query, DataTable data)
        {
            try
            {
                var msg = CreateMessage(senderName, query);
                InsertData(msg, DataTableToHtml(data, 50));
                msg.IsBodyHtml = true;
                _smtpClient.Send(msg);
                _logger?.LogInformation($"Sent data result notification \"{msg.Subject}\" to {RecipientList(msg)}");
            }
            catch (Exception exc)
            {
                _logger?.LogError($"NotifyOnDataResult failed: {exc.Message}");
            }
        }

        private MailMessage CreateMessage(string senderEmail, Query query)
        {
            if (!query.Recipients.Any()) throw new InvalidOperationException($"Query {query.Subject} must have at least one recipient.");
            if (string.IsNullOrWhiteSpace(query.Subject)) throw new InvalidCastException($"Query {query.Sql} must have a subject.");

            var result = new MailMessage()
            {
                From = new MailAddress(senderEmail),
                Subject = query.Subject,
                Body = $"<html><body><p>{query.BodyText}</>%data%</body></html>"
            };

            foreach (var recip in query.Recipients) result.To.Add(new MailAddress(recip));
           
            return result;
        }

        private void InsertData(MailMessage message, string content)
        {
            var sb = new StringBuilder(message.Body);
            sb.Replace(data, content);
            message.Body = sb.ToString();
        }

        private string DataTableToHtml(DataTable dataTable, int maxRows)
        {
            var rows = dataTable.AsEnumerable().Take(maxRows);
            var html = new HtmlBuilder();

            html.StartTag("table");

            html.StartTag("tr");
            foreach (DataColumn col in dataTable.Columns) html.WriteHtmlTag("th", col.ColumnName);
            html.EndTag();

            foreach (var row in rows)
            {
                html.StartTag("tr");
                foreach (DataColumn col in dataTable.Columns) html.WriteHtmlTag("td", row[col].ToString());
                html.EndTag();
            }

            html.EndTag(); // table
            return html.ToString();
        }

        private string RecipientList(MailMessage msg) => string.Join(", ", msg.To.Select(addr => addr.Address));
    }
}
