using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using TeamHitori.Mulplay.shared.storage.documents;

namespace TeamHitori.Mulplay.shared.storage
{
    public interface IDocumentDBRepository
    {
        String Endpoint { get; }
        String Key { get; }
        String DatabaseId { get; }
        String CollectionId { get; }
        Task<List<T>> RawQuery<T>(string queryText);
        Task<ItemResponse<UserDocument>> CreateItemAsync(UserDocument doc);
        Task DeleteItemAsync<T>(string id, string partitionKey);
        IEnumerable<T> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<ItemResponse<T>> UpdateItemAsync<T>(string id, T item) where T : class;
        Task<T> ExecuteSproc<T>(string storedProcedureId, string partitionKey, dynamic[] args) where T : class;
    }
}