using Adriva.Common.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Optimization
{
    public class OptimizationMiddleware : MiddlewareBase
    {
        private readonly IMemoryCache MemoryCache;
        private readonly OptimizationOptions Options;
        private readonly IAssetPipelineManager PipelineManager;
        private readonly AssetStore Store;
        private readonly IServiceProvider ServiceProvider;
        private readonly string VersionString;

        public OptimizationMiddleware(IOptions<OptimizationOptions> optionsAccessor, IMemoryCache memoryCache, AssetStore store,
                                    IAssetPipelineManager assetPipelineManager, RequestDelegate next,
                                    IServiceProvider serviceProvider, ILogger<OptimizationMiddleware> logger) : base(next, logger)
        {
            this.Options = optionsAccessor.Value;
            this.MemoryCache = memoryCache;
            this.Store = store;
            this.PipelineManager = assetPipelineManager;
            this.ServiceProvider = serviceProvider;

            if (this.Options.OptimizeJavascript)
            {
                this.PipelineManager.AddProcessor<DefaultScriptMinifier>();
                this.PipelineManager.AddProcessor<DefaultScriptBundler>();
            }

            if (this.Options.OptimizeStylesheet)
            {
                this.PipelineManager.AddProcessor<DefaultStylesheetMinifier>();
                this.PipelineManager.AddProcessor<DefaultStylesheetBundler>();
            }

            if (null != this.Options.LocalPaths)
            {
                IHostingEnvironment hostingEnvironment = this.ServiceProvider.GetService<IHostingEnvironment>();

                foreach (string localPath in this.Options.LocalPaths)
                {
                    var files = Utilities.EnumerateFiles(hostingEnvironment.WebRootFileProvider, localPath).OrderBy(f => f.Name);
                    var hashBytes = Utilities.CalculateMultiFileHash(files);
                    this.VersionString = Utilities.GetBaseString(hashBytes, Utilities.Base63Alphabet, 0);
                }
            }
        }

        public async Task InvokeAsync(HttpContext httpContext, IOptimizationContext optimizationContext)
        {
            PathString prefix = new PathString(httpContext.Request.PathBase).Add(this.Options.PathPrefix);
            PathString path = httpContext.Request.PathBase.Add(httpContext.Request.Path);

            if (path.StartsWithSegments(prefix))
            {
                await this.Store.RespondAsync(path.Value, httpContext);
            }
            else
            {
                if (null != optimizationContext)
                {
                    this.Options.Version = this.VersionString;
                }

                httpContext.Response.OnStarting(async () =>
                {
                    if (optimizationContext.HasAssets)
                    {
                        string cacheKey = $"{optimizationContext.ScriptPath}{optimizationContext.StylesheetPath}";
                        _ = await this.MemoryCache.GetOrCreateAsync<byte>(cacheKey, async entry =>
                        {
                            if (this.Options.AbsoluteExpiration.HasValue)
                            {
                                entry.SetAbsoluteExpiration(this.Options.AbsoluteExpiration.Value);
                            }

                            if (this.Options.SlidingExpiration.HasValue)
                            {
                                entry.SetSlidingExpiration(this.Options.SlidingExpiration.Value);
                            }

                            await this.Store.EnsureStoredAsync(optimizationContext, AssetFileType.Javascript);
                            await this.Store.EnsureStoredAsync(optimizationContext, AssetFileType.Stylesheet);
                            return 0x1;
                        });
                    }

                    foreach (var childContextEntry in optimizationContext.ChildContexts)
                    {
                        IOptimizationContext childContext = childContextEntry.Value;

                        if (childContext.HasAssets)
                        {
                            string cacheKey = $"{childContext.ScriptPath}{childContext.StylesheetPath}";
                            _ = await this.MemoryCache.GetOrCreateAsync<byte>(cacheKey, async entry =>
                            {

                                if (this.Options.AbsoluteExpiration.HasValue)
                                {
                                    entry.SetAbsoluteExpiration(this.Options.AbsoluteExpiration.Value);
                                }

                                if (this.Options.SlidingExpiration.HasValue)
                                {
                                    entry.SetSlidingExpiration(this.Options.SlidingExpiration.Value);
                                }

                                await this.Store.EnsureStoredAsync(childContext, AssetFileType.Javascript);
                                await this.Store.EnsureStoredAsync(childContext, AssetFileType.Stylesheet);
                                return 0x1;
                            });
                        }
                    }
                });

                await this.Next.Invoke(httpContext);
            }
        }
    }
}
