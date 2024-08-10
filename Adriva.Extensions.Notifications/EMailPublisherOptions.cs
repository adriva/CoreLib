namespace Adriva.Extensions.Notifications
{
    public class EMailPublisherOptions
    {
        public string From { get; set; }

        public string DefaultSubject { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool UseSsl { get; set; }
    }
}
