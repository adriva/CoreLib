namespace Adriva.Extensions.Social.Twitter
{
    public class TwitterOptions : ISocialMediaClientOptions
    {
        public string Key { get; set; }

        public string Secret { get; set; }

        public string AccessToken { get; set; }

        public string AccessTokenSecret { get; set; }

        public bool IsEnabled { get; set; } = true;

        public TwitterOptions()
        {

        }
    }
}