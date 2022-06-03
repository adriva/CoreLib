using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Authentication
{
    public interface IAuthenticationManager
    {
        ClaimsIdentity ResolveCurrentUser(HttpContext httpContext);

        Task<ClaimsIdentity> AuthenticateAsync(HttpContext httpContext, ExternalIdentity externalIdentity);

        Task<ClaimsIdentity> AuthenticateAsGuestAsync(HttpContext httpContext);
    }
}
