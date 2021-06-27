namespace TeamHitori.Mulplay.shared.storage
{
    using Microsoft.Azure.Cosmos;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Polly.Retry;
    using Polly;
    using Microsoft.Extensions.Logging;
    using TeamHitori.Mulplay.shared.storage.documents;

    public class DocumentDBRepository : IDocumentDBRepository
    {
        private CosmosClient _client;
        private readonly ILogger _logger;

        public string Endpoint { get; private set; }

        public string Key { get; private set; }

        public string DatabaseId { get; private set; }

        public string CollectionId { get; private set; }
        //private object cosmosDatabase;
        //private ResourceResponse<DocumentCollection> collection;

        private readonly Random _jitterer;


        public DocumentDBRepository(
            String endpoint,
            String key,
            String databaseId,
            String collectionId,
            ILogger logger

            )
        {
            Endpoint = endpoint;
            Key = key;
            DatabaseId = databaseId;
            CollectionId = collectionId;
            _logger = logger;
            _jitterer = new Random();
            this._client = new CosmosClient(Endpoint, key);
            CreateDatabaseIfNotExistsAsync().Wait();
        }


        public async Task<T> ExecuteSproc<T>(string storedProcedureId, string partitionKey, dynamic[] args) where T : class
        {
            try
            {

                var container = _client.GetContainer(DatabaseId, CollectionId);
                var pk = new PartitionKey(partitionKey);
                var result = await Policy
                .Handle<Exception>()
               .WaitAndRetryAsync(6, (retryAttempt, timespan) =>
               {
                   return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
               }, (ex, timespan, retryCount, context) =>
               {
                   _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
               })
               .ExecuteAsync(async () => {


                   return await container.Scripts.ExecuteStoredProcedureAsync<T>(storedProcedureId, pk, args);

               });

                return result;

            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public async Task<List<T>> RawQuery<T>(string queryText = "SELECT DISTINCT c.userPrincipleId FROM c")
        {
            var container = _client.GetContainer(DatabaseId, CollectionId);

            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100 };
            options.MaxConcurrency = 0;


            FeedIterator<T> query = container.GetItemQueryIterator<T>(
                queryText,
                requestOptions: options);

            // 10 maximum parallel tasks, 10 dedicated asynchronous task to continuously make REST calls
            List<T> itemsParallel = new List<T>();

            options.MaxConcurrency = 10;
            query = container.GetItemQueryIterator<T>(
                queryText,
                requestOptions: options);

            while (query.HasMoreResults)
            {
                foreach (T item in await query.ReadNextAsync())
                {
                    itemsParallel.Add(item);
                }
            }

            return itemsParallel;
        }


        public IEnumerable<T> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var container = _client.GetContainer(DatabaseId, CollectionId);

            var res = Policy
                .Handle<Exception>()
               .WaitAndRetry(6, (retryAttempt, timespan) =>
               {
                   return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
               }, (ex, timespan, retryCount, context) =>
               {
                   _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
               })
               .Execute(() => {
                   return container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true)
                .Where(predicate);

               });

            return res;
        }


        public async Task<ItemResponse<UserDocument>> CreateItemAsync(UserDocument doc) 
        {
            var container = _client.GetContainer(DatabaseId, CollectionId);

            var res = await container.CreateItemAsync(doc, new PartitionKey(doc.userPrincipleId), new ItemRequestOptions { IfMatchEtag = doc._etag });

            //var res =  await container.CreateItemAsync(doc, new PartitionKey(doc.userPrincipleId), new ItemRequestOptions { IfMatchEtag = doc._etag }); 

            return res;
        }

        public async Task<ItemResponse<T>> UpdateItemAsync<T>(string id, T item) where T : class
        {
            return await Policy
                .Handle<Exception>()
               .WaitAndRetryAsync(6, (retryAttempt, timespan) =>
               {
                   return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
               }, (ex, timespan, retryCount, context) =>
               {
                   _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
               })
               .ExecuteAsync(async () => {
                   var container = _client.GetContainer(DatabaseId, CollectionId);
                   return await container.ReplaceItemAsync(item, id);

               });

        }

        public async Task DeleteItemAsync<T>(string id, string partitionKey)
        {
            await Policy
                .Handle<Exception>()
               .WaitAndRetryAsync(6, (retryAttempt, timespan) =>
               {
                   return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
               }, (ex, timespan, retryCount, context) =>
               {
                   _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
               })
               .ExecuteAsync(async () => {
                   var container = _client.GetContainer(DatabaseId, CollectionId);
                   return await container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));

               });
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                
                var database = await _client.CreateDatabaseIfNotExistsAsync(DatabaseId);
            }

            catch (Exception e)
            {
                throw;
            }
        }

    }
}