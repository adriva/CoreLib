using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Web.Core.Optimization
{

    public interface IAssetProcessor
    {
        AssetFileType SupportedType { get; }

        Task<IAssetProcessorResult> ProcessAsync(ILogger logger, IOptimizationContext optimizationContext, IEnumerable<AssetFile> filesToProcess);
    }

}
