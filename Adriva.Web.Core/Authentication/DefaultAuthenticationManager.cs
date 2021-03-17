using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Authentication
{
    public class DefaultAuthenticationManager : IAuthenticationManager
    {
        public virtual ClaimsIdentity ResolveCurrentUser(HttpContext httpContext)
        {
            if (null == httpContext?.User) throw new ArgumentNullException("HttpContext or User");

            return (httpContext.User.Identity as ClaimsIdentity) ?? throw new ArgumentException("Invalid ClaimsIdentity");
        }

        public Task<ClaimsIdentity> AuthenticateAsGuestAsync(HttpContext httpContext)
        {
            ClaimsIdentity guestIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            guestIdentity.AddClaims(new[] {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N")),
                new Claim(ClaimTypes.Name, $"Guest{Guid.NewGuid().ToString("N")}"),
                new Claim(ClaimTypes.Anonymous, "1")
            });

            return Task.FromResult(guestIdentity);
        }

        public virtual Task<ClaimsIdentity> AuthenticateAsync(HttpContext httpContext, ExternalIdentity externalIdentity)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            claimsIdentity.AddClaims(new[] {
                new Claim(ClaimTypes.NameIdentifier, externalIdentity.Id),
                new Claim(ClaimTypes.Name, externalIdentity.Name)
            });

            return Task.FromResult(claimsIdentity);
        }
    }
}
