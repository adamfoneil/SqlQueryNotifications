using Azure.Storage.Blobs;
using BlobQueryAlerts.Exceptions;
using Microsoft.Data.SqlClient;
using SqlQueryNotifications.Interfaces;
using SqlQueryNotifications.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SqlQueryNotifications
{
    public class BlobQuerySource : IQuerySource
    {
        private readonly string _azureConnectionString;
        private readonly string _sqlConnectionString;
        private readonly string _containerName;
        private readonly string _blobPrefix;

        private List<BlobQueryParseException> _loadExceptions = new List<BlobQueryParseException>();

        public BlobQuerySource(string azureConnectionString, string sqlConnectionString, string senderEmail, string containerName, string blobPrefix = null)
        {
            _azureConnectionString = azureConnectionString;
            _sqlConnectionString = sqlConnectionString;
            SenderEmail = senderEmail;
            _containerName = containerName;
            _blobPrefix = blobPrefix;            
        }

        public string SenderEmail { get; }

        public IEnumerable<BlobQueryParseException> LoadExceptions => _loadExceptions;

        public SqlConnection GetConnection() => new SqlConnection(_sqlConnectionString);
        
        public async Task<IEnumerable<Query>> GetQueriesAsync()
        {
            var containerClient = new BlobContainerClient(_azureConnectionString, _containerName);
            var pages = containerClient.GetBlobsAsync(prefix: _blobPrefix).AsPages();

            List<Query> results = new List<Query>();

            _loadExceptions = new List<BlobQueryParseException>();

            await foreach (var page in pages)
            {
                var blobs = page.Values;

                foreach (var blob in blobs)
                {
                    var blobClient = new BlobClient(_azureConnectionString, _containerName, blob.Name);                    
                    using (var input = await blobClient.OpenReadAsync())
                    {
                        using (var reader = new StreamReader(input))
                        {
                            var json = await reader.ReadToEndAsync();
                            try
                            {
                                var query = JsonSerializer.Deserialize<Query>(json);
                                results.Add(query);
                            }
                            catch (Exception exc)
                            {
                                _loadExceptions.Add(new BlobQueryParseException(blob, exc));
                                continue;
                            }
                        }
                    }
                }             
            }

            return results;
        }
    }
}
