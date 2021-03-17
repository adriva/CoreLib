using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adriva.Web.Controls
{
    public static class Extensions
    {

        public static IServiceCollection AddWebControls(this IServiceCollection services, Action<WebControlsOptions> configure)
        {
            services.Configure(configure);
            return services;
        }

        public static IApplicationBuilder UseWebControls(this IApplicationBuilder app)
        {
            app.UseMiddleware<ResourceLoaderMiddleware>();
            return app;
        }

        public static string WebControlResource(this IUrlHelper url, string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentNullException(nameof(resourceName));

            var optionsAccessor = url.ActionContext.HttpContext.RequestServices.GetService<IOptions<WebControlsOptions>>();
            var request = url.ActionContext.HttpContext.Request;

            UriBuilder rootUriBuilder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, request.Path);

            Uri resourceUri = new Uri(rootUriBuilder.Uri, url.Content($"~{optionsAccessor.Value.ResourceBasePath}/{resourceName}"));

            return rootUriBuilder.Uri.MakeRelativeUri(resourceUri).ToString();
        }
    }
}