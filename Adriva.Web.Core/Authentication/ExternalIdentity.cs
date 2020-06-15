using System;

namespace Adriva.Web.Core.Authentication
{
    public class ExternalIdentity
    {

        public AuthenticationSchemeType Scheme { get; private set; }

        public string Id { get; set; }

        public string Username { get; set; }

        public string Name { get; set; }

        public string EMail { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string ProfilePicture { get; set; }

        public string AccessToken { get; set; }

        public DateTime ExpiresAt { get; set; }

        public ExternalIdentity(AuthenticationSchemeType scheme)
        {
            this.Scheme = scheme;
        }
    }
}
