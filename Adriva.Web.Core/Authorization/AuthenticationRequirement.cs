using Microsoft.AspNetCore.Authorization;

namespace Adriva.Web.Core.Authorization
{
    public class AuthenticationRequirement : IAuthorizationRequirement
    {
        public bool AuthenticateAnonymous { get; private set; }

        public AuthenticationRequirement(bool authenticateAnonymous)
        {
            this.AuthenticateAnonymous = authenticateAnonymous;
        }
    }
}
