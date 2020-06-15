using System;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Extensions.Reports
{
    internal sealed class ReportRendererFactory : IReportRendererFactory
    {

        private readonly IEnumerable<IReportRenderer> Renderers;

        public ReportRendererFactory(IEnumerable<IReportRenderer> renderers)
        {
            this.Renderers = renderers;
        }

        public TRendererInterface GetRenderer<TRendererInterface>() where TRendererInterface : IReportRenderer
        {
            Type typeOfRenderer = typeof(TRendererInterface);

            var renderer = this.Renderers.FirstOrDefault(r => typeOfRenderer.IsAssignableFrom(r.GetType()));

            if (null == renderer) throw new ArgumentException($"Report renderer '{typeOfRenderer.FullName}' could not be found.");

            return ((TRendererInterface)renderer);
        }
    }

}