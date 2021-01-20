using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using SqlQueryNotifications.Interfaces;
using SqlQueryNotifications.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SqlQueryNotifications
{
    public class BlobQuerySource : IQuerySource
    {
        private readonly string _azureConnectionString;
        private readonly string _sqlConnectionString;
        private readonly string _containerName;
        private readonly string _blobPrefix;

        public BlobQuerySource(string azureConnectionString, string sqlConnectionString, string senderEmail, string containerName, string blobPrefix = null)
        {
            _azureConnectionString = azureConnectionString;
            _sqlConnectionString = sqlConnectionString;
            SenderEmail = senderEmail;
            _containerName = containerName;
            _blobPrefix = blobPrefix;
        }

        public string SenderEmail { get; }

        public SqlConnection GetConnection() => new SqlConnection(_sqlConnectionString);
        
        public async Task<IEnumerable<Query>> GetQueriesAsync()
        {
            var containerClient = new BlobContainerClient(_azureConnectionString, _containerName);
            var pages = containerClient.GetBlobsAsync(prefix: _blobPrefix).AsPages();

            List<Query> results = new List<Query>();

            await foreach (var blob in pages)
            {

            }

            return results;
        }
    }
}
