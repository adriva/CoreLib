using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Adriva.Web.Core.Optimization
{
    public abstract class AssetStore
    {
        private readonly IAssetPipelineManager PipelineManager;

        protected OptimizationOptions Options { get; private set; }

        protected AssetStore(IAssetPipelineManager pipelineManager, IOptions<OptimizationOptions> optionsAccessor)
        {
            this.PipelineManager = pipelineManager;
            this.Options = optionsAccessor.Value;
        }

        public abstract Task RespondAsync(string path, HttpContext httpContext);

        protected virtual bool EnsureStored(string path, IEnumerable<AssetFile> assets)
        {
            return false;
        }

        protected virtual async Task<bool> EnsureStoredAsync(string path, IEnumerable<AssetFile> assets)
        {
            await Task.CompletedTask;
            if (null == assets || !assets.Any()) return false;
            return this.EnsureStored(path, assets);
        }

        public async Task EnsureStoredAsync(IOptimizationContext optimizationContext, AssetFileType assetFileType)
        {
            if (null == optimizationContext || !optimizationContext.HasAssets) return;

            IEnumerable<AssetFile> assets = Array.Empty<AssetFile>();
            Uri uri = null;

            switch (assetFileType)
            {
                case AssetFileType.Javascript:
                    assets = optimizationContext.Scripts;
                    uri = optimizationContext.ScriptUri;
                    break;
                case AssetFileType.Stylesheet:
                    assets = optimizationContext.Stylesheets;
                    uri = optimizationContext.StylesheetUri;
                    break;
                default: break;
            }

            if (null == assets || !assets.Any()) return;

            var output = await this.PipelineManager.ProcessAsync(optimizationContext, assetFileType);
            if (await this.EnsureStoredAsync(uri.LocalPath, output))
            {
                this.Options.AssetWrittenCallback?.Invoke(uri.LocalPath, uri.PathAndQuery);
            }
        }
    }
}
