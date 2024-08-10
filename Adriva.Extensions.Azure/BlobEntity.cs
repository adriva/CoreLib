using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using IOPath = System.IO.Path;

namespace Adriva.Extensions.Azure
{
    public class BlobEntity
    {
        public IDictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();

        public string Name { get; private set; }

        public string Path { get; private set; }

        public byte[] Data { get; internal set; }

        public bool Exists { get; internal set; }

        public string ETag { get; internal set; }

        public string ContentType { get; internal set; }

        public DateTimeOffset? LastModified { get; internal set; }

        public BlobEntity(string name) :
            this(name, null, null)
        {
            this.Path = name;
        }

        public BlobEntity(string name, byte[] data, IDictionary<string, string> metadata)
        {
            this.Name = name;
            this.Path = name;
            this.Data = data;
            if (null != metadata) this.Metadata = metadata;
        }

        public string GetMetadataValue(string key, string defaultValue)
        {
            if (this.Metadata.ContainsKey(key))
            {
                return this.Metadata[key];
            }

            return defaultValue;
        }

        public static BlobEntity FromCloudBlob(string prefix, bool doesExist, CloudBlob blob)
        {
            BlobEntity entity = new BlobEntity(IOPath.GetFileName(blob.Name))
            {
                LastModified = blob.Properties.LastModified,
                ETag = blob.Properties.ETag,
                Metadata = blob.Metadata,
                Path = blob.Name,
                Exists = doesExist
            };
            return entity;
        }

    }
}
