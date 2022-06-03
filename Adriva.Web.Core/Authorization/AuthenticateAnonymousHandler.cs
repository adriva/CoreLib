using Adriva.Web.Core.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Authorization
{

    public class AuthenticateAnonymousHandler : AuthorizationHandler<AuthenticationRequirement>
    {
        private readonly IAuthenticationManager AuthenticationManager;
        private readonly IHttpContextAccessor HttpContextAccessor;

        public AuthenticateAnonymousHandler(IAuthenticationManager authenticationManager, IHttpContextAccessor httpContextAccessor)
        {
            this.AuthenticationManager = authenticationManager;
            this.HttpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthenticationRequirement requirement)
        {
            if (requirement.AuthenticateAnonymous)
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    var httpContext = this.HttpContextAccessor.HttpContext;

                    var identity = await this.AuthenticationManager.AuthenticateAsGuestAsync(httpContext);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    await httpContext.SignInAsync(principal, new AuthenticationProperties() { AllowRefresh = true, IsPersistent = true  });
                    httpContext.User = principal;
                }
                context.Succeed(requirement);
            }
        }
    }
}
