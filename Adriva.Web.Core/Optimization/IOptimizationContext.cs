using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Adriva.Web.Core.Optimization
{
    public interface IOptimizationContext
    {
        ReadOnlyDictionary<string, IOptimizationContext> ChildContexts { get; }

        IEnumerable<AssetFile> Scripts { get; }

        IEnumerable<AssetFile> Stylesheets { get; }

        bool HasAssets { get; }

        Uri ScriptUri { get; }

        Uri StylesheetUri { get; }

        string ScriptPath { get; }

        string StylesheetPath { get; }

        OptimizationOptions Options { get; }

        void AddScript(params string[] pathOrUrl);

        void AddStylesheet(params string[] pathOrUrl);

        HtmlString RenderScript(object htmlAttributes = null);

        HtmlString RenderStyesheet(object htmlAttributes = null);

        IOptimizationContext GetOrCreate(string name);
    }
}
