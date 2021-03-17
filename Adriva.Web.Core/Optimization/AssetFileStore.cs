using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Web.Core.Optimization
{
    public class AssetFileStoreOptions
    {
        public IFileProvider FileProvider { get; set; }
    }

    public class AssetFileStore : AssetStore
    {
        private readonly IHostingEnvironment HostingEnvironment;
        private readonly ILogger Logger;
        private readonly AssetFileStoreOptions FileStoreOptions;

        public AssetFileStore(IAssetPipelineManager pipelineManager,
                        IOptions<OptimizationOptions> optimizationOptionsAccessor,
                        IOptions<AssetFileStoreOptions> FileStoreOptionsAccessor,
                        IHostingEnvironment hostingEnvironment,
                        ILogger<AssetFileStore> logger)
            : base(pipelineManager, optimizationOptionsAccessor)
        {
            this.FileStoreOptions = FileStoreOptionsAccessor.Value;
            this.HostingEnvironment = hostingEnvironment;
            this.FileStoreOptions.FileProvider = this.FileStoreOptions.FileProvider ?? this.HostingEnvironment.WebRootFileProvider;
            this.Logger = logger;
        }

        public override async Task RespondAsync(string path, HttpContext httpContext)
        {
            //this is a very interesting case that we have seen on chrome
            //chrome may send the request once the script tag is received before the HTML processing is over
            //in other terms tries to get a response before the EnsureStoredAsync call is finised
            //that's why we spin wait 3 secs. to give it a chance

            IFileInfo fileInfo = null;
            if (!System.Threading.SpinWait.SpinUntil(() =>
            {
                fileInfo = this.FileStoreOptions.FileProvider.GetFileInfo(path);
                return fileInfo.Exists && 0 < fileInfo.Length;
            }, 3000))
            {
                httpContext.Response.StatusCode = 404;
            }
            else
            {
                bool hasError = true;
                int retryCount = 10;
                while (hasError && 0 < retryCount--)
                {
                    try
                    {
                        using (var stream = fileInfo.CreateReadStream()) { }
                        hasError = false;
                    }
                    catch
                    {
                        await Task.Delay(50);
                    }
                }
                httpContext.Response.Redirect(httpContext.Request.Path + httpContext.Request.QueryString);
            }
        }

        protected override async Task<bool> EnsureStoredAsync(string path, IEnumerable<AssetFile> assets)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            var fileInfo = this.FileStoreOptions.FileProvider.GetFileInfo(path);
            string physicalPath = fileInfo.PhysicalPath;
            string directoryPath = Path.GetDirectoryName(physicalPath);

            if (!Directory.Exists(directoryPath))
            {
                this.Logger.LogInformation($"Asset directory '{directoryPath}' doesn't exist. Creating now...");
                Directory.CreateDirectory(directoryPath);
            }

            Stream stream = null;
            try
            {
                stream = File.Open(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, false))
                {
                    await writer.WriteAsync(assets.First().Content);
                    await writer.FlushAsync();
                    this.Logger.LogTrace($"Asset file '{path}' written to '{physicalPath}'.");
                    return true;
                }
            }
            catch (Exception error)
            {
                this.Logger.LogError(error, $"Error writing asset '{path}' to '{physicalPath}.'");
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}
