using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Adriva.Extensions.Reports
{
    public abstract class RendererOptionsProvider
    {
        internal IDictionary<string, IConfigurationSection> RendererOptionsConfiguration { get; set; }

        public TOptions GetRendererOptions<TOptions>(string rendererName) where TOptions : class
        {
            if (null == this.RendererOptionsConfiguration) return null;
            string normalizedKey = rendererName;
            foreach (string key in this.RendererOptionsConfiguration.Keys)
            {
                if (0 == string.Compare(key, rendererName, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedKey = key;
                    break;
                }
            }
            if (!this.RendererOptionsConfiguration.ContainsKey(normalizedKey)) return null;
            return this.RendererOptionsConfiguration[normalizedKey].Get<TOptions>();
        }
    }
}