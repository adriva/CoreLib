using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adriva.Web.Core.Optimization
{

    public interface IAssetOrderer
    {
        IEnumerable<AssetFile> Order(IEnumerable<AssetFile> assets);

    }
    public class DefaultAssetOrderer : IAssetOrderer
    {
        protected virtual int ResolveFileOrder(AssetFile asset)
        {
            if (null == asset) throw new ArgumentNullException(nameof(asset));

            string fileName = Path.GetFileName(asset.Path);

            if (fileName.StartsWith("jquery", StringComparison.OrdinalIgnoreCase)) return 1;
            else if (fileName.StartsWith("bootstrap", StringComparison.OrdinalIgnoreCase)) return 10;
            else if (fileName.StartsWith("site", StringComparison.OrdinalIgnoreCase)) return 100;
            return 1000;
        }

        public IEnumerable<AssetFile> Order(IEnumerable<AssetFile> assets)
        {
            if (!assets.Any() || 1 == assets.Count()) return assets;

            return assets.OrderBy(a => this.ResolveFileOrder(a));
        }
    }
}
