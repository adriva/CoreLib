using Adriva.Common.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Adriva.Extensions.Azure
{
    public sealed class TableManager<T> where T : TableEntity, new()
    {

        private static ConcurrentDictionary<string, Task<TableManager<T>>> CachedTableManagers = new ConcurrentDictionary<string, Task<TableManager<T>>>();

        private CloudTable cloudTable;

        public string TableName { get; private set; }

        public static Task<TableManager<T>> GetAsync(string tableName)
        {
            return TableManager<T>.CachedTableManagers.GetOrAdd(tableName, key =>
            {
                return TableManager<T>.CreateAsync(tableName);
            });
        }

        // always creates a new copy
        public static async Task<TableManager<T>> CreateAsync(string tableName)
        {
            var tableManager = new TableManager<T>(tableName);
            await tableManager.InitializeAsync();
            return tableManager;
        }

        // always creates a new copy
        public static async Task<TableManager<T>> CreateAsync(string tableName, string connectionString)
        {
            var tableManager = new TableManager<T>(tableName);
            await tableManager.InitializeAsync(connectionString);
            return tableManager;
        }

        private TableManager(string tableName)
        {
            this.TableName = tableName;
        }

        private async Task InitializeAsync(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.cloudTable = tableClient.GetTableReference(this.TableName);

            await this.cloudTable.CreateIfNotExistsAsync();
        }

        private async Task InitializeAsync()
        {
            await this.InitializeAsync(ConnectionStrings.Default.AzureTable);
        }

        public async Task<bool> TruncateAsync()
        {
            if (await this.cloudTable.DeleteIfExistsAsync())
            {
                bool shouldThrow = false;
                int retryCount = 100;

                while (true)
                {
                    try
                    {
                        return await this.cloudTable.CreateIfNotExistsAsync();
                    }
                    catch (StorageException sex)
                    {
                        if (shouldThrow) throw sex;

                        if (/*conflict*/ 409 == sex.RequestInformation.HttpStatusCode)
                        {
                            await Task.Delay(3001);
                            if (0 == --retryCount)
                            {
                                shouldThrow = true;
                            }
                        }
                    }
                    catch (Exception) { throw; }
                }
            }

            return true;
        }

        public async Task<bool> ExistsAsync(T entity)
        {
            var selectOperation = TableOperation.Retrieve(entity.PartitionKey, entity.RowKey, new List<string>() { "PartitionKey", "RowKey" });
            var tableReesult = await this.cloudTable.ExecuteAsync(selectOperation);
            return null != tableReesult.Result;
        }

        public async Task<bool> ExistsAsync(string partitionKey, string rowKey)
        {
            var selectOperation = TableOperation.Retrieve(partitionKey, rowKey, new List<string>() { "PartitionKey", "RowKey" });
            var tableReesult = await this.cloudTable.ExecuteAsync(selectOperation);
            return null != tableReesult.Result;
        }

        public async Task<long> GetCountAsync(string partitionKey)
        {
            long count = 0;
            string filterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            TableQuery<T> query = new TableQuery<T>().Where(filterString).Select(new List<string> { "PartitionKey" });
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await this.cloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                count += result.LongCount();

            } while (null != continuationToken);

            return count;
        }

        public async Task<T> GetFilteredAsync(T entity, params string[] fieldNames)
        {
            var fieldNamesList = fieldNames.ToList();
            entity.Normalize();
            var tableOperation = TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey, fieldNamesList);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
            return (T)tableResult.Result;
        }

        public async Task<T> GetAsync(T entity)
        {
            entity.Normalize();
            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return null;
            return await this.GetAsync(entity.PartitionKey, entity.RowKey);
        }

        public async Task<T> GetByRowKeyAsync(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey)) return null;

            var query = new TableQuery<T>()
                        .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey))
                        .Take(1);

            var segmentedResult = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, null);
            return segmentedResult.FirstOrDefault();
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var tableOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
            return (T)tableResult.Result;
        }

        public async Task<IEnumerable<T>> GetMultipleAsync(string partitionKey, params string[] rowKeys)
        {
            if (50 < rowKeys.Length) rowKeys = rowKeys.Take(50).ToArray();
            else if (0 == rowKeys.Length) return new T[0];

            List<T> results = new List<T>();

            foreach (var rowKey in rowKeys)
            {
                T item = await this.GetAsync(partitionKey, rowKey);
                if (null != item) results.Add(item);
            }

            return results;
        }

        public async Task<SegmentedResult<T>> SelectOlderAsync(DateTimeOffset olderThanTimestamp, string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, olderThanTimestamp)).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> SelectNewerAsync(DateTimeOffset newerThanTimestamp, string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, newerThanTimestamp)).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> SelectAsync(string partitionKey, string whereClause, string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            string filterString = null;
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                filterString = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey), TableOperators.And, whereClause);
            }
            else
            {
                filterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            }

            TableQuery<T> query = new TableQuery<T>().Where(filterString).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> SelectAsync(string whereClause, string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            TableQuery<T> query = new TableQuery<T>().Where(whereClause).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> SelectAsync(string partitionKey, string whereClause, string continuationToken, int rowCount, params string[] columnNames)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            string filterString = null;

            if (!string.IsNullOrWhiteSpace(partitionKey))
            {
                filterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            }

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                if (string.IsNullOrWhiteSpace(filterString))
                {
                    filterString = whereClause;
                }
                else
                {
                    filterString = TableQuery.CombineFilters(filterString, TableOperators.And, whereClause);
                }
            }
            else
            {
                filterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            }

            TableQuery<T> query = new TableQuery<T>().Where(filterString).Take(rowCount);

            if (null != columnNames)
            {
                query.SelectColumns = columnNames.ToList();
            }

            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> GetAllAsync(string partitionKey, string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey)).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        // ACHTUNG !!! This is a dangerous method  since it will try to return everything in that partition.
        // It may create unnecessary network traffic as well as increased memory usage.
        // Only use where you are certain that there's relatively small amount of data and/or the problems mentioned above are no subject.
        public async Task<IEnumerable<T>> GetAllAsync(string partitionKey)
        {
            List<T> items = new List<T>();
            SegmentedResult<T> segmentedResult = null;
            string token = null;

            do
            {
                segmentedResult = await this.GetAllAsync(partitionKey, token, 1000);
                token = segmentedResult.ContinuationToken;
                items.AddRange(segmentedResult.Items);
            } while (segmentedResult.HasMore);

            return items;
        }

        // ACHTUNG !!! This is a dangerous method  since it will try to return everything in that table (all partitions).
        // It may create unnecessary network traffic as well as increased memory usage.
        // Only use where you are certain that there's relatively small amount of data and/or the problems mentioned above are no subject.
        public async Task<SegmentedResult<T>> GetAllAsync(string continuationToken, int rowCount)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);
            TableQuery<T> query = new TableQuery<T>().Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<SegmentedResult<T>> GetAllFilteredAsync(string continuationToken, int rowCount, params string[] fieldNames)
        {
            rowCount = Math.Max(1, rowCount);
            rowCount = Math.Min(1000, rowCount);

            if (null == fieldNames || 0 == fieldNames.Length)
            {
                fieldNames = new[] { "PartitionKey", "RowKey" };
            }

            TableQuery<T> query = new TableQuery<T>().Select(fieldNames).Take(rowCount);
            TableContinuationToken token = AzureUtilities.DeserializeTableContinuationToken(continuationToken);

            var querySegment = await this.cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);

            var newContinuationToken = AzureUtilities.SerializeTableContinuationToken(querySegment.ContinuationToken);

            SegmentedResult<T> result = new SegmentedResult<T>(querySegment.ToArray(), newContinuationToken, null != querySegment.ContinuationToken);
            return result;
        }

        public async Task<IEnumerable<T>> SelectSortedAsync<TKey>(string partitionKey, int count, int batchCount, Func<T, TKey> valueExtractor) where TKey : IComparable
        {
            SegmentedResult<T> result;
            string token = null;

            IList<T> selectedItems = new List<T>();
            bool needsSorting = true;
            TKey currentMinimum = default(TKey);

            do
            {
                result = await this.GetAllAsync(partitionKey, token, batchCount);
                token = result.ContinuationToken;

                var orderedItems = result.Items.OrderByDescending(valueExtractor);
                var enumerator = orderedItems.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (selectedItems.Count < count)
                    {
                        selectedItems.Add(enumerator.Current);
                    }
                    else
                    {
                        if (needsSorting)
                        {
                            selectedItems = selectedItems.OrderByDescending(valueExtractor).ToList();
                            currentMinimum = valueExtractor(selectedItems[selectedItems.Count - 1]);
                            needsSorting = false;
                        }

                        TKey currentValue = valueExtractor(enumerator.Current);

                        if (-1 == currentMinimum.CompareTo(currentValue))
                        {
                            var closestItem = selectedItems.FirstOrDefault(si => -1 == valueExtractor(si).CompareTo(currentValue));

                            if (null != closestItem)
                            {
                                selectedItems.RemoveAt(selectedItems.Count - 1);
                                selectedItems.Add(enumerator.Current);
                                needsSorting = true;
                            }
                        }
                    }
                }
            } while (result.HasMore);

            return selectedItems;
        }

        public async Task UpsertAsync(T entity)
        {
            entity.Normalize();

            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return;

            var tableOperation = TableOperation.InsertOrReplace(entity);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
        }

        public async Task InsertAsync(T entity)
        {
            entity.Normalize();

            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return;

            var tableOperation = TableOperation.Insert(entity);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
        }

        public async Task<bool> InsertIfNotExistsAsync(T entity)
        {
            try
            {
                await this.InsertAsync(entity);
                return true;
            }
            catch (StorageException storageException)
            {
                if ((int)HttpStatusCode.Conflict == storageException.RequestInformation.HttpStatusCode)
                {
                    return false;
                }
                else throw;
            }
        }

        public async Task BatchUpsertAsync(IEnumerable<T> entities)
        {
            TableBatchOperation batchUpsert = new TableBatchOperation();

            int pageCount = 0;
            IEnumerable<T> pagedEntities = entities.Skip(pageCount * 100).Take(100);

            while (null != pagedEntities.FirstOrDefault())
            {
                foreach (var entity in pagedEntities)
                {

                    if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey))
                    {
                        entity.Normalize();
                        if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) continue;
                    }
                    else
                    {
                        entity.Normalize();
                    }

                    batchUpsert.InsertOrReplace(entity);
                }

                pageCount++;

                if (0 < batchUpsert.Count)
                {
                    await this.cloudTable.ExecuteBatchAsync(batchUpsert);
                    batchUpsert.Clear();
                }
                pagedEntities = entities.Skip(pageCount * 100).Take(100);
            }
        }

        public async Task BatchDeleteAsync(IEnumerable<T> entities)
        {
            if (null == entities) return;

            var groupedEntities = entities.GroupBy(e => e.PartitionKey);

            foreach (var group in groupedEntities)
            {
                await this.BatchDeleteItemsAsync(group);
            }
        }

        private async Task BatchDeleteItemsAsync(IEnumerable<T> entities)
        {
            TableBatchOperation batchDelete = new TableBatchOperation();

            int pageCount = 0;
            IEnumerable<T> pagedEntities = entities.Skip(pageCount * 100).Take(100);

            while (null != pagedEntities.FirstOrDefault())
            {
                foreach (var entity in pagedEntities)
                {
                    if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) continue;

                    batchDelete.Delete(entity);
                }
                pageCount++;

                if (0 < batchDelete.Count)
                {
                    await this.cloudTable.ExecuteBatchAsync(batchDelete);
                    batchDelete.Clear();
                }
                pagedEntities = entities.Skip(pageCount * 100).Take(100);
            }
        }

        public async Task UpdateAsync(T entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return;

            entity.Normalize();
            var tableOperation = TableOperation.Replace(entity);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
        }

        /// <summary>
        /// Updates a single property of the given entity without checking the ETag.
        /// </summary>
        /// <param name="entity">The entity that owns the property.</param>
        /// <param name="propertyName">The name of the property that will be updated. This value is case sensitive.</param>
        /// <returns>A task that represents the async update operation.</returns>
        public async Task UpdatePropertyAsync(T entity, string propertyName)
        {
            entity.Normalize();

            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return;

            var existingProperties = entity.WriteEntity(new OperationContext());
            DynamicTableEntity dynamicEntity = new DynamicTableEntity(entity.PartitionKey, entity.RowKey)
            {
                ETag = "*"
            };
            dynamicEntity.Properties.Add(propertyName, existingProperties[propertyName]);
            var tableOperation = TableOperation.Merge(dynamicEntity);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            return await this.DeleteAsync(entity, false);
        }

        public async Task<bool> DeleteAsync(T entity, bool ignoreETag)
        {
            if (string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey)) return false;

            if (ignoreETag) entity.ETag = "*";

            var tableOperation = TableOperation.Delete(entity);
            var tableResult = await this.cloudTable.ExecuteAsync(tableOperation);
            return null != tableResult && Utilities.IsHttpSuccess(tableResult.HttpStatusCode);
        }
    }

}
