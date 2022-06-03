using System.Threading.Tasks;

namespace Adriva.Extensions.Reports
{
    public abstract class ReportRenderer<TOutputContext> : IReportRenderer where TOutputContext : class
    {
        public string Name { get; private set; }

        protected ReportRenderer(string name)
        {
            this.Name = name;
        }

        public virtual Task RenderOutputAsync(TOutputContext context, ReportOutput output)
        {
            this.RenderOutput(context, output);
            return Task.CompletedTask;
        }

        public virtual void RenderOutput(TOutputContext context, ReportOutput output)
        {

        }

        public Task RenderOutputAsync(object context, ReportOutput output) => this.RenderOutputAsync((TOutputContext)context, output);
    }

}