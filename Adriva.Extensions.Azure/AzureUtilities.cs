using Adriva.Common.Core;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Extensions.Azure
{
    public static class AzureUtilities
    {
        public static string SerializeTableContinuationToken(TableContinuationToken token)
        {
            if (null == token) return null;

            string json = Utilities.SafeSerialize(token);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            return Utilities.UrlTokenEncode(buffer);
        }

        public static TableContinuationToken DeserializeTableContinuationToken(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;

            byte[] buffer = Utilities.UrlTokenDecode(data);
            string json = Encoding.UTF8.GetString(buffer);

            return Utilities.SafeDeserialize<TableContinuationToken>(json);
        }

        public static string SerializeBlobContinuationToken(BlobContinuationToken token)
        {
            if (null == token) return null;

            string json = Utilities.SafeSerialize(token);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            return Utilities.UrlTokenEncode(buffer);
        }

        internal static BlobContinuationToken DeserializeBlobContinuationToken(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;

            byte[] buffer = Utilities.UrlTokenDecode(data);
            string json = Encoding.UTF8.GetString(buffer);

            return Utilities.SafeDeserialize<BlobContinuationToken>(json);
        }

        public static async Task InitializeCoreUtilitiesAsync(string configurationContainerName = "config")
        {
            var configBlob = await BlobManager.GetAsync(configurationContainerName);

            using (var stream = await configBlob.GetStreamAsync("public_suffix_list.dat", true))
            {
                await Utilities.InitializeAsync(stream);   
            }
        }
    }
}
