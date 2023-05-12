using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Adriva.Web.Core.Authorization
{
    public sealed class AuthorizeAllAttribute : AuthorizeAttribute
    {
        public AuthorizeAllAttribute(bool authenticateAnonymous)
            : this(authenticateAnonymous, CookieAuthenticationDefaults.AuthenticationScheme)
        {
        }

        public AuthorizeAllAttribute(bool authenticateAnonymous, string authenticationSchemes)
        {
            if (authenticateAnonymous) base.Policy = WellKnownAuthorizationPolicies.AuthenticateAnonymous;
            base.AuthenticationSchemes = authenticationSchemes;

        }
    }
}
