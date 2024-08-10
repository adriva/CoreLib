using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Optimization
{
    public interface IAssetPipelineManager
    {
        void AddProcessor<T>() where T : class, IAssetProcessor;

        Task<IEnumerable<AssetFile>> ProcessAsync(IOptimizationContext optimizationContext, AssetFileType fileTypeToProcess);
    }
}
