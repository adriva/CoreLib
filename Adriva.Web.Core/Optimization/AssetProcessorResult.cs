using System;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Web.Core.Optimization
{

    public interface IAssetProcessorResult
    {
        bool IsSuccess { get; }

        AssetFile Output { get; }
    }

    public class AssetProcessorResult : IAssetProcessorResult
    {

        public bool IsSuccess { get; private set; }

        public AssetFile Output { get; private set; }

        public AssetProcessorResult(bool isSuccess, AssetFile output)
        {
            this.IsSuccess = isSuccess;
            this.Output = output ?? throw new ArgumentNullException(nameof(output));
        }
    }

    public class AggregateAssetProcessorResult : AssetProcessorResult
    {
        public IEnumerable<AssetFile> Outputs { get; private set; }

        public AggregateAssetProcessorResult(bool isSuccess, params AssetFile[] outputs)
                : base(isSuccess, outputs.FirstOrDefault())
        {
            this.Outputs = outputs;
        }
    }
}
