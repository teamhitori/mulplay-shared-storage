
using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Azure.Storage.Blobs;
using Azure.Storage;
using System.Collections.Generic;

namespace TeamHitori.Mulplay.shared.storage
{
    public class Storage
    {
        public BlobContainerClient BlobContainerClient { get; private set; }

        public BlobServiceClient BlobServiceClient { get; private set; }

        public IDocumentDBRepository Repository { get; private set; }

        public StorageSharedKeyCredential StorageSharedKeyCredential { get; private set; }

        public string UserId { get; private set; }
        public ILogger Logger { get; private set; }
        public IDatabase Cache { get; private set; }

        public Storage(
            IDocumentDBRepository repository,
            BlobServiceClient blobServiceClient,
            StorageSharedKeyCredential storageSharedKeyCredential,
            String userId, 
            ILogger logger,
            IDatabase cache = null)
        {

            BlobServiceClient = blobServiceClient;
            BlobContainerClient = blobServiceClient.GetBlobContainerClient(userId);
            StorageSharedKeyCredential = storageSharedKeyCredential;

            Repository = repository;
            UserId = userId;
            Logger = logger;
            Cache = cache;
        }
    }

}
