using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.JavaScript;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Optimization
{
    public class DefaultScriptMinifier : IAssetProcessor
    {

        public AssetFileType SupportedType => AssetFileType.Javascript;

        public async Task<IAssetProcessorResult> ProcessAsync(ILogger logger, IOptimizationContext optimizationContext, IEnumerable<AssetFile> filesToProcess)
        {
            CodeSettings settings = new CodeSettings()
            {
                PreserveImportantComments = false
            };

            List<AssetFile> minifiedAssets = new List<AssetFile>();

            foreach (var asset in filesToProcess)
            {
                try
                {
                    var result = Uglify.Js(asset.Content, settings);

                    if (result.HasErrors)
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogWarning($"Couldn't minify '{asset.Path}'. Will ignore error and use the default content.");
                            logger.LogWarning($"{asset.Path} has minification error : '{error.Message}' in '{error.File}', start = L{error.StartLine} C{error.StartColumn}, end = L{error.EndLine} C{error.EndColumn}");
                        }
                    }

                    string content = !result.HasErrors && result.Code.Length < asset.Content.Length ? result.Code : asset.Content;
                    minifiedAssets.Add(new AssetFile(asset.FileType, asset.Path, content));
                }
                catch (Exception error)
                {
                    logger.LogError(error, $"Error minifiying '{asset.Path}'. Will skip this asset.");
                    minifiedAssets.Add(asset);
                }
            }

            return await Task.FromResult(new AggregateAssetProcessorResult(true, minifiedAssets.ToArray()));
        }
    }
}
