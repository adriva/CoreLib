using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Adriva.Web.Core.Authorization
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationWithPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options => {
                options.AddPolicy(WellKnownAuthorizationPolicies.AuthenticateAnonymous, policy => {
                    policy.Requirements.Add(new AuthenticationRequirement(true));
                });
            });

            services.AddSingleton<IAuthorizationHandler, AuthenticateAnonymousHandler>();

            return services;
        }
    }
}
