using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Adriva.Web.Controls
{
    internal class ResourceLoaderMiddleware
    {
        private readonly RequestDelegate Next;
        private readonly ResourceLoader Loader;
        private readonly WebControlsOptions Options;
        private readonly FileExtensionContentTypeProvider ContentTypeProvider;

        public ResourceLoaderMiddleware(RequestDelegate next, IOptions<WebControlsOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
            this.Next = next;
            this.Loader = new ResourceLoader();
            this.ContentTypeProvider = new FileExtensionContentTypeProvider();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            PathString path = context.Request.Path;

            if (!path.StartsWithSegments(this.Options.ResourceBasePath) && !path.StartsWithSegments("/wc/er"))
            {
                await this.Next(context);
                return;
            }

            string resourceName = Path.GetFileName(path);
            string extension = Path.GetExtension(path);

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                await this.Next(context);
                return;
            }

            string contentType;
            string content = null;
            Stream contentStream = null;

            if (!this.ContentTypeProvider.TryGetContentType(resourceName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (0 == string.Compare(".js", extension, StringComparison.OrdinalIgnoreCase)
                || 0 == string.Compare(".css", extension, StringComparison.OrdinalIgnoreCase)
                || 0 == string.Compare(".txt", extension, StringComparison.OrdinalIgnoreCase)
                || 0 == string.Compare(".htm", extension, StringComparison.OrdinalIgnoreCase)
                || 0 == string.Compare(".html", extension, StringComparison.OrdinalIgnoreCase))
            {
                content = await this.Loader.LoadAsync(resourceName);
            }
            else
            {
                contentStream = this.Loader.LoadBinary(resourceName);
            }

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 200;
                context.Response.Headers.Add("content-type", contentType);
                context.Response.Headers.Add("cache-control", "public, max-age=15552000");

                if (null != content)
                {
                    await context.Response.WriteAsync(content, Encoding.UTF8);
                }
                else if (null != contentStream)
                {
                    await contentStream.CopyToAsync(context.Response.Body);
                }

            }

        }
    }
}