using Adriva.Common.Core;
using Adriva.Extensions.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Optimization
{
    public class AssetPipelineManager : IAssetPipelineManager
    {
        private readonly LinkedList<IAssetProcessor> ProcessorList = new LinkedList<IAssetProcessor>();
        private readonly IMemoryCache MemoryCache;
        private readonly HttpClientWrapper HttpClient;
        private readonly IHttpContextAccessor HttpContextAccessor;
        private readonly IFileProvider LocalFileProvider;
        private readonly IServiceProvider ServiceProvider;
        private readonly ILogger Logger;

        public AssetPipelineManager(HttpClientWrapper httpClientWrapper, IHttpContextAccessor httpContextAccessor, IHostingEnvironment hostingEnvironment, ILogger<AssetPipelineManager> logger, IServiceProvider serviceProvider)
        {
            this.MemoryCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = 50_000_000 //50MB
            });

            this.HttpClient = httpClientWrapper;
            this.HttpContextAccessor = httpContextAccessor;
            this.LocalFileProvider = hostingEnvironment.WebRootFileProvider;
            this.Logger = logger;
            this.ServiceProvider = serviceProvider;
        }

        public void AddProcessor<T>() where T : class, IAssetProcessor
        {
            IAssetProcessor processor = ActivatorUtilities.CreateInstance<T>(this.ServiceProvider);
            this.ProcessorList.AddLast(processor);
        }

        private Uri GetAssetSourceUri(AssetFile asset, HttpContext httpContext, IFileProvider fileProvider)
        {
            string path = asset.Path;

            if (path.StartsWith("~/", StringComparison.Ordinal)) path = path.Substring(1);
            if (path.StartsWith("//", StringComparison.Ordinal)) path = string.Concat(httpContext.Request.Scheme, ":", path);
            if (path.StartsWith("/", StringComparison.Ordinal)) path = string.Concat(Uri.UriSchemeFile, "://", path);
            else if (!Uri.TryCreate(path, UriKind.Absolute, out Uri tempUri))
            {
                path = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}/{path}";
            }

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri targetUri))
            {
                if (!targetUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !targetUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    && !targetUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Uri scheme '{targetUri.Scheme}' not supported for asset optimization.");
                }
                else if (targetUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    var fileInfo = fileProvider.GetFileInfo(targetUri.LocalPath);
                    return new Uri(fileInfo.PhysicalPath);
                }
                else
                {
                    return targetUri;
                }
            }

            throw new ArgumentException($"Invalid path specified for asset. '{asset.Path}'.");
        }

        private async Task<string> LoadContentAsync(AssetFile asset)
        {
            Uri assetUri = this.GetAssetSourceUri(asset, this.HttpContextAccessor.HttpContext, this.LocalFileProvider);

            var crcValue = Crc64.Compute(assetUri.ToString());

            return await this.MemoryCache.GetOrCreateAsync<string>($"ac_{crcValue}", async (entry) =>
            {
                string content = null;
                bool isSuccess = false;

                if (0 == string.Compare(assetUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || 0 == string.Compare(assetUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    using (var response = await this.HttpClient.GetAsync(assetUri.ToString()))
                    {
                        content = await response.Content.ReadAsStringAsync();
                        isSuccess = true;
                    }
                }
                else if (assetUri.IsFile && assetUri.IsLoopback)
                {
                    using (var fileStream = File.OpenRead(assetUri.LocalPath))
                    using (var reader = new StreamReader(fileStream))
                    {
                        content = await reader.ReadToEndAsync();
                        isSuccess = true;
                    }
                }

                if (!isSuccess) throw new IOException($"Couldn't load asset from path '{assetUri}'.");

                entry.SlidingExpiration = TimeSpan.FromHours(12);
                entry.Priority = CacheItemPriority.Low;
                entry.Size = content.Length;

                this.Logger.LogTrace($"Content loaded for asset '{asset.Path}'.");

                return content;
            });
        }

        public async Task<IEnumerable<AssetFile>> ProcessAsync(IOptimizationContext optimizationContext, AssetFileType fileTypeToProcess)
        {
            if (null == optimizationContext) return null;

            IEnumerable<AssetFile> assets;
            IEnumerable<AssetFile> currentAssets = new List<AssetFile>();

            switch (fileTypeToProcess)
            {
                case AssetFileType.Javascript:
                    assets = optimizationContext.Options.Orderer.Order(optimizationContext.Scripts);
                    break;
                case AssetFileType.Stylesheet:
                    assets = optimizationContext.Options.Orderer.Order(optimizationContext.Stylesheets);
                    break;
                default:
                    assets = Array.Empty<AssetFile>();
                    break;
            }

            foreach (var asset in assets)
            {
                string content = await this.LoadContentAsync(asset);
                ((List<AssetFile>)currentAssets).Add(asset.WithContent(content));
            }

            LinkedListNode<IAssetProcessor> processorNode = this.ProcessorList.First;

            while (null != processorNode)
            {
                if (currentAssets.All(a => processorNode.Value.SupportedType == a.FileType))
                {
                    var assetProcessorResult = await processorNode.Value.ProcessAsync(this.Logger, optimizationContext, currentAssets);

                    if (assetProcessorResult is AggregateAssetProcessorResult aggregateAssetProcessorResult)
                    {
                        currentAssets = aggregateAssetProcessorResult.Outputs;
                    }
                    else
                    {
                        currentAssets = new AssetFile[] { assetProcessorResult.Output };
                    }
                }

                processorNode = processorNode.Next;
            }

            return currentAssets;
        }
    }
}
