namespace Adriva.Common.Core
{
    public class ConnectionStrings
    {
        public static ConnectionStrings Default { get; internal set; }

        public string AzureTable { get; set; }

        public string AzureQueue { get; set; }

        public string AzureBlob { get; set; }

        public string DefaultSqlServer { get; set; }

    }
}
