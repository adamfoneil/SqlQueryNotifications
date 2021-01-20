using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlQueryNotifications;
using SqlQueryNotifications.Models;
using SqlServer.LocalDb;
using SqlServer.LocalDb.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace Testing
{
    [TestClass]
    public class QueryNotifications
    {
        const string DbName = "QueryNotifications";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            LocalDb.TryDropDatabaseIfExists(DbName, out _);

            using (var cn = LocalDb.GetConnection(DbName, GetInitializeStatements()))
            {
                for (int i = 0; i < 10; i++)
                {
                    cn.Execute("INSERT INTO [dbo].[SomeTable] ([Quantity]) VALUES (@value)", new { value = i * 5 });
                }
            }
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            LocalDb.TryDropDatabaseIfExists(DbName, out _);
        }

        private static IEnumerable<InitializeStatement> GetInitializeStatements() => new InitializeStatement[]
        {
            new InitializeStatement("dbo.SomeTable", @"CREATE TABLE [dbo].[SomeTable] (
                [Id] int identity(1, 1) PRIMARY KEY,
                [Quantity] int NOT NULL
            )")
        };

        [TestMethod]
        public void WithDataResult()
        {
            var config = ConfigHelper.GetConfig();

            var smtp = new SmtpClient(
                config["SendGrid:Server"], 
                int.Parse(config["SendGrid:Port"]));

            smtp.Credentials = new NetworkCredential("apikey", config["SendGrid:ApiKey"]);

            var querySource = new QuerySource("noreply@aosoftware.net", LocalDb.GetConnectionString(DbName))
            {
                Queries = new Query[]
                {
                    new Query()
                    {
                        Subject = "Sample query notification",
                        Recipients = new string[] { "adamosoftware@gmail.com" },
                        BodyText = "This is a sample query notification. There should not be any rows in SomeTable WHERE Quantity is greater than 10.",
                        Sql = "SELECT * FROM [dbo].[SomeTable] WHERE [Quantity]>10",
                        SendRule = TriggerSendRule.IfAny
                    }
                }
            };

            var service = new SqlQueryNotificationService(smtp, querySource, null);
            service.ExecuteAsync().Wait();
        }
    }
}
