namespace Adriva.Common.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Unless it is a platform update do not add app specific settings to this class. Instead inherit from this one.</remarks>
    public class RuntimeSettings
    {
        public static RuntimeSettings Default { get; internal set; }

        public string RootDomain { get; set; }

        public string DefaultProfilePicture { get; set; }

        public string PublicUrlPattern { get; set; }

        public string CookieDomain { get; set; }

        public string CookieName { get; set; }

        public int CookieExpireMinutes { get; set; }

        public string EncryptionKey { get; set; }

        public string EncryptionIV { get; set; }

    }
}
