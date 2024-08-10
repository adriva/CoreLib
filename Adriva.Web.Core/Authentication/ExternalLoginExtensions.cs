using Adriva.Common.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Adriva.Web.Core.Authentication
{
    public static class ExternalLoginExtensions
    {
        public static IApplicationBuilder UseExternalLogin(this IApplicationBuilder app, Action<ExternalLoginMiddlewareOptions> configureOptions)
        {
            ExternalLoginMiddlewareOptions options = new ExternalLoginMiddlewareOptions();

            configureOptions?.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.PathBase?.Trim())) options.PathBase = "/extacc";

            app.Map(options.PathBase, builder =>
            {
                builder.UseMiddleware<ExternalLoginMiddleware>(options);
            });

            return app;
        }


        public static AuthenticationBuilder AddAuthentication<TAuthenticationManager>(this IServiceCollection services, Action<AuthenticationOptions> configureOptions)
            where TAuthenticationManager : class, IAuthenticationManager
        {
            AuthenticationOptions options = new AuthenticationOptions();
            configureOptions.Invoke(options);

            if (options.UseGoogle || options.UseInstagram) options.UseCookies = true;

            AuthenticationBuilder builder = services.AddAuthentication(baseOptions =>
            {
                baseOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                baseOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                baseOptions.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            if (options.UseCookies)
            {
                builder.AddCookie((cookieOptions) =>
                {
                    cookieOptions.Cookie.Name = RuntimeSettings.Default.CookieName;
                    cookieOptions.Cookie.HttpOnly = true;
                    cookieOptions.Cookie.IsEssential = true;
                    cookieOptions.Cookie.Domain = RuntimeSettings.Default.CookieDomain;
                    cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(RuntimeSettings.Default.CookieExpireMinutes);
                    cookieOptions.SlidingExpiration = true;
                    options.ConfigureCookie.Invoke(cookieOptions);
                });
            }

            if (options.UseGoogle)
            {
                builder.AddGoogle((googleOptions) =>
                {
                    googleOptions.SaveTokens = true;
                    googleOptions.CallbackPath = "/extacc/callback/google";
                    options.ConfigureGoogle.Invoke(googleOptions);
                });
            }

            if (options.UseInstagram)
            {
                builder.AddInstagram((instagramOptions) =>
                {
                    instagramOptions.CallbackPath = "/extacc/callback/instagram";
                    instagramOptions.SaveTokens = true;
                    instagramOptions.Scope.Add("public_content");

                    instagramOptions.ClaimActions.MapJsonKey(ExtendedClaimTypes.ProfilePicture, "profile_picture");
                    instagramOptions.ClaimActions.MapJsonKey(ExtendedClaimTypes.Username, "username");
                    options.ConfigureInstagram.Invoke(instagramOptions);
                });
            }

            if (options.UseJwtBearer)
            {
                SymmetricSecurityKey issuerSigningKey = null;

                if (!string.IsNullOrWhiteSpace(RuntimeSettings.Default.EncryptionKey))
                {
                    byte[] keyBytes = Encoding.UTF8.GetBytes(RuntimeSettings.Default.EncryptionKey);
                    issuerSigningKey = new SymmetricSecurityKey(keyBytes);
                }

                builder.AddJwtBearer((jwtOptions) =>
                {
                    jwtOptions.TokenValidationParameters.IssuerSigningKey = issuerSigningKey;
                    jwtOptions.TokenValidationParameters.RequireSignedTokens = true;
                    jwtOptions.TokenValidationParameters.ValidateAudience = false;
                    jwtOptions.TokenValidationParameters.ValidateIssuer = false;
                    jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    jwtOptions.TokenValidationParameters.ValidateLifetime = true;
                    options.ConfigureJwtBearer?.Invoke(jwtOptions);
                });

                services.AddSingleton<IJwtTokenManager>(new DefaultJwtTokenManager(issuerSigningKey));
            }

            services.AddSingleton<IAuthenticationManager, TAuthenticationManager>();

            return builder;
        }

    }
}
