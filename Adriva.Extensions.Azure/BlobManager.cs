using Adriva.Common.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Extensions.Azure
{
    public class BlobManager
    {

        private static ConcurrentDictionary<string, Task<BlobManager>> CachedBlobManagers = new ConcurrentDictionary<string, Task<BlobManager>>();

        private CloudBlobContainer blobContainer;

        public string ContainerName { get; private set; }

        public static Task<BlobManager> GetAsync(string containerName)
        {
            return BlobManager.CachedBlobManagers.GetOrAdd(containerName, key =>
            {
                return BlobManager.CreateAsync(containerName);
            });
        }

        public static async Task<BlobManager> CreateAsync(string containerName)
        {
            var blobManager = new BlobManager(containerName);
            await blobManager.InitializeAsync();
            return blobManager;
        }

        public static async Task<BlobManager> CreateAsync(string containerName, string connectionString)
        {
            var blobManager = new BlobManager(containerName);
            await blobManager.InitializeAsync(connectionString);
            return blobManager;
        }

        private BlobManager(string containerName)
        {
            this.ContainerName = containerName;
        }

        private async Task InitializeAsync()
        {
            await this.InitializeAsync(ConnectionStrings.Default.AzureBlob);
        }

        private async Task InitializeAsync(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            this.blobContainer = blobClient.GetContainerReference(this.ContainerName);

            await this.blobContainer.CreateIfNotExistsAsync();
        }

        public async Task<bool> ExistsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var blobReference = this.blobContainer.GetBlockBlobReference(name);
            return await blobReference.ExistsAsync();
        }

        public async Task<string> UpsertAsync(string name, Stream stream, int cacheDuration)
        {
            if (string.IsNullOrWhiteSpace(name) || null == stream || !stream.CanRead) return null;

            name = name.ToLowerInvariant();

            var mimeType = MimeTypes.GetMimeType(name);
            var blobReference = this.blobContainer.GetBlockBlobReference(name);
            blobReference.Properties.ContentType = mimeType;
            blobReference.Properties.CacheControl = string.Format("public, max-age={0}", cacheDuration);

            if (stream.CanSeek) stream.Position = 0;

            await blobReference.UploadFromStreamAsync(stream);
            return blobReference.Uri.ToString();
        }

        public async Task<string> UpsertAsync(string name, string data, int cacheDuration)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;

            byte[] buffer = Encoding.UTF8.GetBytes(data);
            return await this.UpsertAsync(name, buffer, cacheDuration);
        }

        public async Task<string> UpsertAsync(BlobEntity entity)
        {
            if (null == entity) return null;

            var blobReference = this.blobContainer.GetBlockBlobReference(entity.Path);
            await blobReference.UploadFromByteArrayAsync(entity.Data, 0, entity.Data.Length);

            blobReference.Metadata.Clear();
            foreach (var metadataItem in entity.Metadata)
            {
                blobReference.Metadata.Add(metadataItem.Key, metadataItem.Value);
            }

            await blobReference.SetMetadataAsync();
            return blobReference.Uri.ToString();
        }

        public async Task<string> UpsertAsync(string name, byte[] data, int cacheDuration)
        {
            if (string.IsNullOrWhiteSpace(name) || null == data || 0 == data.Length) return null;

            var mimeType = MimeTypes.GetMimeType(name);

            var blobReference = this.blobContainer.GetBlockBlobReference(name);
            blobReference.Properties.ContentType = mimeType;
            blobReference.Properties.CacheControl = string.Format("public, max-age={0}", cacheDuration);

            await blobReference.UploadFromByteArrayAsync(data, 0, data.Length);
            return blobReference.Uri.ToString();
        }

        public async Task DeleteAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var blobReference = this.blobContainer.GetBlobReference(name);
            await blobReference.DeleteIfExistsAsync();
        }

        public async Task<SegmentedResult<BlobEntity>> ListAllAsync(string token)
        {
            return await this.ListAllAsync(null, token);
        }

        public async Task<SegmentedResult<BlobEntity>> ListAllAsync(string prefix, string token, bool flattenHierarchy = false)
        {
            BlobContinuationToken blobContinuationToken = AzureUtilities.DeserializeBlobContinuationToken(token);
            var segmentedResult = await this.blobContainer.ListBlobsSegmentedAsync(prefix, flattenHierarchy, BlobListingDetails.Metadata, 250, blobContinuationToken, null, null);

            var tokenString = AzureUtilities.SerializeBlobContinuationToken(segmentedResult.ContinuationToken);
            var entities = segmentedResult.Results.OfType<CloudBlob>().Select(cloudBlob => BlobEntity.FromCloudBlob(prefix, true, cloudBlob)).ToArray();
            SegmentedResult<BlobEntity> result = new SegmentedResult<BlobEntity>(entities, tokenString, null != segmentedResult.ContinuationToken);
            return result;
        }

        public async Task<Stream> GetStreamAsync(string name)
        {
            return await this.GetStreamAsync(name, false);
        }

        public async Task<Stream> GetStreamAsync(string name, bool isTrusted)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var stream = new MemoryStream();

            if (null != this.blobContainer)
            {
                var blobReference = this.blobContainer.GetBlockBlobReference(name);

                if (null != blobReference && await blobReference.ExistsAsync())
                {
                    await blobReference.DownloadToStreamAsync(stream, null, new BlobRequestOptions() { DisableContentMD5Validation = isTrusted }, null);
                    stream.Position = 0;
                }
                else
                {
                    return null;
                }
            }
            return stream;
        }

        public async Task SaveAsync(string name, string localPath, bool overwrite = true)
        {
            using (var blobStream = await this.GetStreamAsync(name))
            using (var localStream = File.Open(localPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
            {
                await blobStream.CopyToAsync(localStream);
            }
        }

        public async Task<byte[]> GetByteArrayAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            byte[] data;

            using (MemoryStream stream = (MemoryStream)await this.GetStreamAsync(name))
            {
                data = stream.ToArray();
            }

            return data;
        }

        /// <summary>
        /// Downloads the blob item with its metadata and data stored in a byte array.
        /// </summary>
        /// <param name="name">The name of the blob item to be retrived.</param>
        /// <returns>An instance of BlobEntity class that stores the blob item.</returns>
        /// <remarks>Since this object stores the blob data in a byte array it's not recommended to use it with large data blobs.</remarks>
        public async Task<BlobEntity> GetEntityAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            BlobEntity entity = new BlobEntity(name);

            if (null != this.blobContainer)
            {
                var blobReference = this.blobContainer.GetBlockBlobReference(name);

                if (null != blobReference)
                {
                    entity.Exists = await blobReference.ExistsAsync();

                    if (entity.Exists)
                    {
                        await blobReference.FetchAttributesAsync();
                        entity.Metadata = blobReference.Metadata;
                        entity.ETag = blobReference.Properties.ETag;
                        entity.LastModified = blobReference.Properties.LastModified;
                        entity.ContentType = blobReference.Properties.ContentType;

                        if (0 < blobReference.Properties.Length)
                        {
                            byte[] data = new byte[blobReference.Properties.Length];
                            await blobReference.DownloadToByteArrayAsync(data, 0);
                            entity.Data = data;
                        }
                    }
                }
            }
            return entity;
        }

        public async Task ChangeCacheDuration(Uri uri, int cacheDuration)
        {
            var name = Path.GetFileName(uri.LocalPath);
            var blobReference = this.blobContainer.GetBlobReference(name);
            blobReference.Properties.CacheControl = string.Format("public, max-age={0}", cacheDuration);
            await blobReference.SetPropertiesAsync();
        }

        public string CreateSharedAccessSignature(string name, int expireInMinutes)
        {
            if (0 >= expireInMinutes) return null;

            var blobReference = this.blobContainer.GetBlobReference(name);

            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(expireInMinutes)
            };
            SharedAccessBlobHeaders headers = new SharedAccessBlobHeaders();

            return blobReference.Uri + blobReference.GetSharedAccessSignature(policy);
        }
    }
}
