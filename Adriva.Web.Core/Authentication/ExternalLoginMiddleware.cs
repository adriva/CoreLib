using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Extensions;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Adriva.Web.Core.Authentication
{
    public class ExternalLoginMiddleware : MiddlewareBase
    {
        private readonly ExternalLoginMiddlewareOptions Options;
        private readonly IAuthenticationManager AuthenticationManager;

        public ExternalLoginMiddleware(RequestDelegate next, IAuthenticationManager authenticationManager, ExternalLoginMiddlewareOptions options, ILogger<ExternalLoginMiddleware> logger)
            : base(next, logger)
        {
            this.Options = options;
            this.AuthenticationManager = authenticationManager;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var request = httpContext.Request;

            if (request.Path.HasValue)
            {
                if (0 == string.Compare(request.Path.Value, this.Options.LoginPath))
                {
                    this.Logger.LogTrace("External login request received.");

                    if (httpContext.Request.Query.ContainsKey("p"))
                    {
                        this.Logger.LogTrace("External login provider is '{0}'.", httpContext.Request.Query["p"]);
                        UriBuilder builder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, request.PathBase + this.Options.CallbackPath);

                        AuthenticationProperties authenticationProperties = new AuthenticationProperties()
                        {
                            RedirectUri = builder.Uri.ToString()
                        };

                        string schemeName = httpContext.Request.Query["p"];

                        if (request.Query.ContainsKey(this.Options.ReturnUrlParameter))
                        {
                            authenticationProperties.Items.Add(this.Options.ReturnUrlParameter, request.Query[this.Options.ReturnUrlParameter]);
                        }

                        await httpContext.ChallengeAsync(schemeName, authenticationProperties);
                        return;
                    }
                }
                else if (0 == string.Compare(request.Path.Value, this.Options.CallbackPath))
                {
                    this.Logger.LogTrace("External login callback request received.");
                    var authenticationResult = await httpContext.AuthenticateAsync();

                    if (authenticationResult.Succeeded)
                    {
                        this.Logger.LogTrace("External login authentication succeeded for '{0}'.", authenticationResult.Ticket.AuthenticationScheme);

                        var remoteIdentity = authenticationResult.Principal.Identities.First();

                        this.Logger.LogTrace($"Seeking ticket parser for type '{remoteIdentity.AuthenticationType}'.");

                        var parser = this.Options.GetParser(remoteIdentity.AuthenticationType);

                        if (null != parser)
                        {
                            var externalIdentity = parser.Parse(authenticationResult.Ticket);
                            var identity = await this.AuthenticationManager.AuthenticateAsync(httpContext, externalIdentity);

                            if (null != identity)
                            {
                                ClaimsPrincipal principal = new ClaimsPrincipal(new[] { identity });
                                await httpContext.SignInAsync(principal, new AuthenticationProperties() { IsPersistent = true });

                                if (authenticationResult.Properties.Items.ContainsKey(this.Options.ReturnUrlParameter))
                                {
                                    string returnUrl = authenticationResult.Properties.Items[this.Options.ReturnUrlParameter];
                                    if (!string.IsNullOrWhiteSpace(this.Options.SuccessPath))
                                    {
                                        returnUrl = $"{this.Options.SuccessPath}?{this.Options.ReturnUrlParameter}={returnUrl}";
                                    }
                                    httpContext.Response.Redirect(returnUrl, false);
                                }
                                return;
                            }
                        }
                        else
                        {
                            throw new Exception($"AuthenticationTicketParser for scheme '{remoteIdentity.AuthenticationType}' could not be found.");
                        }
                    }
                    else
                    {
                        this.Logger.LogError(authenticationResult.Failure, "Authentication failed");
                        if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = 500;
                        await httpContext.Response.WriteAsync("Error logging in");
                    }
                }
            }

            await this.Next.Invoke(httpContext);
        }
    }
}
