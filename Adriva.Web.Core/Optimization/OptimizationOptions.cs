using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Adriva.Web.Core.Optimization
{
    public class OptimizationOptions
    {

        internal Type StoreType { get; private set; } = typeof(AssetMemoryStore);

        public string PathPrefix { get; set; } = "/assets";

        public string UrlRoot { get; set; } = null;

        public string Version { get; internal set; }

        public string[] LocalPaths { get; set; }

        public bool OptimizeJavascript { get; set; }

        public bool OptimizeStylesheet { get; set; }

        public bool OptimizeHtml { get; set; }

        public Action<AssetFileType, IEnumerable<AssetFile>, HttpContext, StringBuilder> AssetNameGenerator { get; set; }

        public Func<string, string, Task> AssetWrittenCallback { get; set; }

        public IAssetOrderer Orderer { get; set; }

        public TimeSpan? AbsoluteExpiration { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public IDictionary<string, string> StylesheetReplaceTokens { get; private set; } = new Dictionary<string, string>();

        public void UseStore<T>() where T : AssetStore
        {
            this.StoreType = typeof(T);
        }

    }
}
