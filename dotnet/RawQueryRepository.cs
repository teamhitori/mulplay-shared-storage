using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.shared.storage
{
    public class RawQueryRepository
    {
        private readonly string _endpoint;
        private readonly string _key;
        private readonly string _databaseId;
        private readonly string _collectionId;


        public RawQueryRepository(
            String endpoint = "",
            String key = "",
            String databaseId = "",
            String collectionId = ""

            )
        {
            _endpoint = endpoint;
            _key = key;
            _databaseId = databaseId;
            _collectionId = collectionId;

        }

        //public async Task<List<T>> Get<T>(string queryText = "SELECT DISTINCT c.userPrincipleId FROM c")
        //{
        //    using (CosmosClient client = new CosmosClient(_endpoint, _key))
        //    {
        //        var containerProperties = new ContainerProperties(id: _collectionId, partitionKeyPath: "/userPrincipleId");

        //        Database cosmosDatabase = await client.CreateDatabaseIfNotExistsAsync(_databaseId);
        //        Container container = await cosmosDatabase.CreateContainerIfNotExistsAsync(
        //            containerProperties: containerProperties,
        //            throughput: 400);

        //        QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100 };
        //        options.MaxConcurrency = 0;


        //        FeedIterator<T> query = container.GetItemQueryIterator<T>(
        //            queryText,
        //            requestOptions: options);

        //        // 10 maximum parallel tasks, 10 dedicated asynchronous task to continuously make REST calls
        //        List<T> itemsParallel = new List<T>();

        //        options.MaxConcurrency = 10;
        //        query = container.GetItemQueryIterator<T>(
        //            queryText,
        //            requestOptions: options);

        //        while (query.HasMoreResults)
        //        {
        //            foreach (T item in await query.ReadNextAsync())
        //            {
        //                itemsParallel.Add(item);
        //            }
        //        }

        //        return itemsParallel;

        //    }
        //}

    }
}
