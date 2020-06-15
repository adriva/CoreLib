using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUglify;
using NUglify.Css;
using NUglify.JavaScript;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Optimization
{
    public class DefaultStylesheetMinifier : IAssetProcessor
    {
        private readonly OptimizationOptions Options;
        public AssetFileType SupportedType => AssetFileType.Stylesheet;

        public DefaultStylesheetMinifier(IOptions<OptimizationOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
        }

        public async Task<IAssetProcessorResult> ProcessAsync(ILogger logger, IOptimizationContext optimizationContext, IEnumerable<AssetFile> filesToProcess)
        {
            CssSettings cssSettings = new CssSettings()
            {
                CommentMode = CssComment.None
            };

            if (0 < this.Options.StylesheetReplaceTokens.Count)
            {
                cssSettings.ReplacementTokensApplyDefaults(this.Options.StylesheetReplaceTokens);
            }

            CodeSettings codeSettings = new CodeSettings()
            {
                PreserveImportantComments = false
            };

            List<AssetFile> minifiedAssets = new List<AssetFile>();

            foreach (var asset in filesToProcess)
            {
                var result = Uglify.Css(asset.Content, cssSettings, codeSettings);
                string content = !result.HasErrors && result.Code.Length < asset.Content.Length ? result.Code : asset.Content;
                minifiedAssets.Add(new AssetFile(asset.FileType, asset.Path, content));
            }

            return await Task.FromResult(new AggregateAssetProcessorResult(true, minifiedAssets.ToArray()));
        }
    }
}
