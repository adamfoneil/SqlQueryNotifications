using Azure.Storage.Blobs.Models;
using System;

namespace BlobQueryAlerts.Exceptions
{
    public class BlobQueryParseException : Exception
    {
        public BlobQueryParseException(BlobItem blobItem, Exception innerException) : base(innerException.Message, innerException)
        {
            BlobItem = blobItem;
        }

        public BlobItem BlobItem { get; }
    }
}
