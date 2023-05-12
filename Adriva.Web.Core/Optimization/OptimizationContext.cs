using Adriva.Common.Core;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Adriva.Web.Core.Optimization
{
    public class OptimizationContext : IOptimizationContext
    {
        private readonly IList<AssetFile> ScriptAssets = new List<AssetFile>();
        private readonly IList<AssetFile> StylesheetAssets = new List<AssetFile>();
        private readonly IHttpContextAccessor HttpContextAccessor;
        private readonly IOptions<OptimizationOptions> OptionsAccessor;
        private readonly IDictionary<string, IOptimizationContext> SubContexts = new Dictionary<string, IOptimizationContext>();

        private bool ShouldUseAbsoluteUrls = false;

        public string Name { get; private set; } = "Default";

        public Uri ScriptUri { get; private set; }

        public Uri StylesheetUri { get; private set; }

        public OptimizationOptions Options => this.OptionsAccessor.Value;

        public IEnumerable<AssetFile> Scripts => this.ScriptAssets;

        public IEnumerable<AssetFile> Stylesheets => this.StylesheetAssets;

        public bool HasAssets => this.ScriptAssets.Any() || this.StylesheetAssets.Any();

        public ReadOnlyDictionary<string, IOptimizationContext> ChildContexts => new ReadOnlyDictionary<string, IOptimizationContext>(this.SubContexts);

        public string ScriptPath
        {
            get
            {
                if (null == this.ScriptUri)
                {
                    this.GenerateFileName(AssetFileType.Javascript);
                }

                return this.ShouldUseAbsoluteUrls ? this.ScriptUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped) : this.ScriptUri.LocalPath;
            }
        }

        public string StylesheetPath
        {
            get
            {
                if (null == this.StylesheetUri)
                {
                    this.GenerateFileName(AssetFileType.Stylesheet);
                }

                return this.ShouldUseAbsoluteUrls ? this.StylesheetUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped) : this.StylesheetUri.LocalPath;
            }
        }

        public OptimizationContext(IOptions<OptimizationOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor)
        {
            this.OptionsAccessor = optionsAccessor;
            this.HttpContextAccessor = httpContextAccessor;
        }

        public IOptimizationContext GetOrCreate(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            if (this.SubContexts.TryGetValue(name, out IOptimizationContext optimizationContext)) return optimizationContext;

            var childContext = new OptimizationContext(this.OptionsAccessor, this.HttpContextAccessor);
            this.SubContexts.Add(name, childContext);
            childContext.Name = name;
            return childContext;
        }

        private void GenerateFileName(AssetFileType fileType)
        {
            IEnumerable<AssetFile> assets = Array.Empty<AssetFile>();
            if (AssetFileType.Javascript == fileType) assets = this.ScriptAssets;
            else if (AssetFileType.Stylesheet == fileType) assets = this.StylesheetAssets;

            string fileName = null;

            if (null != assets && assets.Any())
            {

                var httpContext = this.HttpContextAccessor.HttpContext;

                StringBuilder fileNameBuffer = new StringBuilder();

                if (null == this.Options.AssetNameGenerator)
                {
                    fileNameBuffer.AppendLine(this.Name);
                    foreach (var asset in assets.OrderBy(x => x.Path))
                    {
                        fileNameBuffer.AppendLine(asset.Path);
                    }
                }
                else
                {
                    this.Options.AssetNameGenerator.Invoke(fileType, assets, httpContext, fileNameBuffer);
                }

                ulong crc64 = Crc64.Compute(fileNameBuffer.ToString());
                fileName = Utilities.GetBaseString(crc64, Utilities.Base63Alphabet, 0);
            }
            else
            {
                fileName = string.Empty;
            }

            string urlRoot = "http://temp.uri";

            if (Uri.TryCreate(this.Options.UrlRoot, UriKind.Absolute, out Uri rootUri))
            {
                urlRoot = this.Options.UrlRoot;
                if (urlRoot.EndsWith("/", StringComparison.Ordinal))
                {
                    urlRoot = urlRoot.Substring(0, urlRoot.Length - 1);
                }
                this.ShouldUseAbsoluteUrls = true;
            }

            switch (fileType)
            {
                case AssetFileType.Stylesheet:
                    this.StylesheetUri = new Uri($"{urlRoot}{this.Options.PathPrefix}/c{fileName}.css?v={this.Options.Version}", UriKind.Absolute);
                    return;
                case AssetFileType.Javascript:
                    this.ScriptUri = new Uri($"{urlRoot}{this.Options.PathPrefix}/j{fileName}.js?v={this.Options.Version}", UriKind.Absolute);
                    return;
                default:
                    throw new ArgumentException($"Invalid asset type specified. '{fileType}'");
            }

        }

        private string ResolveUrl(string pathOrUrl)
        {
            if (pathOrUrl.StartsWith("~/"))
            {
                var request = this.HttpContextAccessor.HttpContext.Request;
                pathOrUrl = pathOrUrl.Substring(1);
                return $"{request.PathBase}{pathOrUrl}";
            }

            return pathOrUrl;
        }

        private void AddAsset(AssetFileType fileType, string pathOrUrl, IList<AssetFile> list)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) throw new ArgumentNullException(nameof(pathOrUrl));

            pathOrUrl = this.ResolveUrl(pathOrUrl);

            if (list.Any(a => 0 == string.Compare(a.Path, pathOrUrl, StringComparison.OrdinalIgnoreCase))) return;

            list.Add(new AssetFile(fileType, pathOrUrl));
        }

        public void AddScript(params string[] pathOrUrls)
        {
            if (null == pathOrUrls) return;

            foreach (string pathOrUrl in pathOrUrls)
            {
                this.AddAsset(AssetFileType.Javascript, pathOrUrl, this.ScriptAssets);
            }
        }

        public void AddStylesheet(params string[] pathOrUrls)
        {
            if (null == pathOrUrls) return;

            foreach (string pathOrUrl in pathOrUrls)
            {
                this.AddAsset(AssetFileType.Stylesheet, pathOrUrl, this.StylesheetAssets);
            }
        }

        public HtmlString RenderScript(object htmlAttributes = null)
        {

            if (0 == this.ScriptAssets.Count) return HtmlString.Empty;

            if (this.Options.OptimizeJavascript)
            {
                StringBuilder buffer = new StringBuilder("<script");

                htmlAttributes = htmlAttributes ?? new { };

                var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

                attributes["src"] = $"{this.ScriptPath}{this.ScriptUri?.Query}";

                foreach (var attribute in attributes)
                {
                    buffer.AppendFormat(" {0}=\"{1}\"", attribute.Key, Convert.ToString(attribute.Value));
                }

                buffer.Append("></script>");

                return new HtmlString(buffer.ToString());
            }
            else
            {
                StringBuilder buffer = new StringBuilder();
                IEnumerable<AssetFile> orderedAssets = this.ScriptAssets.ApplyOrderer(this.Options?.Orderer);

                foreach (var scriptAsset in orderedAssets)
                {
                    buffer.AppendFormat("<script src=\"{0}\"></script>", scriptAsset.Path);
                }

                return new HtmlString(buffer.ToString());
            }
        }

        public HtmlString RenderStyesheet(object htmlAttributes = null)
        {

            if (0 == this.StylesheetAssets.Count) return HtmlString.Empty;

            if (this.Options.OptimizeStylesheet)
            {
                StringBuilder buffer = new StringBuilder("<link rel=\"stylesheet\"");

                htmlAttributes = htmlAttributes ?? new { };

                var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

                attributes["href"] = $"{this.StylesheetPath}{this.StylesheetUri?.Query}";

                foreach (var attribute in attributes)
                {
                    buffer.AppendFormat(" {0}=\"{1}\"", attribute.Key, Convert.ToString(attribute.Value));
                }

                buffer.Append("/>");

                return new HtmlString(buffer.ToString());
            }
            else
            {
                StringBuilder buffer = new StringBuilder();

                IEnumerable<AssetFile> orderedAssets = this.StylesheetAssets.ApplyOrderer(this.Options?.Orderer);

                foreach (var styleAsset in orderedAssets)
                {
                    buffer.AppendFormat("<link rel=\"stylesheet\" href=\"{0}\" />", styleAsset.Path);
                }

                return new HtmlString(buffer.ToString());
            }
        }
    }
}
