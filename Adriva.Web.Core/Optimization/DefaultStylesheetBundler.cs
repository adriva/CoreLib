using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Web.Core.Optimization
{
    public class DefaultStylesheetBundler : IAssetProcessor
    {

        public AssetFileType SupportedType => AssetFileType.Stylesheet;

        public async Task<IAssetProcessorResult> ProcessAsync(ILogger logger, IOptimizationContext optimizationContext, IEnumerable<AssetFile> filesToProcess)
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var asset in filesToProcess)
            {

                buffer.AppendFormat("{0}{1}", asset.Content, Environment.NewLine);
            }

            var output = new AssetFile(AssetFileType.Stylesheet, null, buffer.ToString());

            return await Task.FromResult(new AssetProcessorResult(true, output));
        }
    }
}
