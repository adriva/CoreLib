using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Web.Core.Optimization
{
    public class DefaultScriptBundler : IAssetProcessor
    {
        public AssetFileType SupportedType => AssetFileType.Javascript;

        public async Task<IAssetProcessorResult> ProcessAsync(ILogger logger, IOptimizationContext optimizationContext, IEnumerable<AssetFile> filesToProcess)
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var asset in filesToProcess)
            {

                buffer.AppendFormat("{0}{1};", Environment.NewLine, asset.Content);
            }

            var output = new AssetFile(AssetFileType.Javascript, null, buffer.ToString());
            return await Task.FromResult(new AssetProcessorResult(true, output));
        }
    }
}
