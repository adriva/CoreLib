using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Web.Core.Optimization
{
    public class AssetMemoryStore : AssetStore
    {
        private readonly IMemoryCache MemoryCache;
        private readonly ILogger Logger;

        public AssetMemoryStore(IAssetPipelineManager pipelineManager, IOptions<OptimizationOptions> optionsAccessor, IMemoryCache memoryCache, ILogger<AssetMemoryStore> logger)
            : base(pipelineManager, optionsAccessor)
        {
            this.MemoryCache = memoryCache;
            this.Logger = logger;
        }

        public override async Task RespondAsync(string path, HttpContext httpContext)
        {
            if (!this.MemoryCache.TryGetValue(path, out AssetFile assetFile))
            {
                httpContext.Response.StatusCode = 404;
                return;
            }

            if (AssetFileType.Javascript == assetFile.FileType)
            {
                httpContext.Response.Headers.Add("content-type", "application/javascript");
            }
            else if (AssetFileType.Stylesheet == assetFile.FileType)
            {
                httpContext.Response.Headers.Add("content-type", "text/css");
            }

            httpContext.Response.Headers.Add("cache-control", "public,max-age=86400");

            await httpContext.Response.WriteAsync(assetFile.Content);
        }

        protected override bool EnsureStored(string path, IEnumerable<AssetFile> assets)
        {
            string cacheKey = path;

            this.MemoryCache.GetOrCreate<AssetFile>(cacheKey, (entry) =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                this.Logger.LogTrace($"Cache entry is constructed for '{path}'.");
                return assets.First();
            });

            return true;
        }
    }

}
