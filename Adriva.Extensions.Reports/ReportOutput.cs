using System.Collections.ObjectModel;
using System.Linq;

namespace Adriva.Extensions.Reports
{
    public class ReportOutput
    {
        public string Name { get; private set; }

        public string Title => this.Definition.Title;

        public ReadOnlyCollection<ReportColumnDefinition> ColumnDefinitons => new ReadOnlyCollection<ReportColumnDefinition>(this.Definition.Output.ColumnDefinitions);

        public IDataSet Data { get; private set; }

        public int TotalCount
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.Definition.Output.Paging?.RowCountFieldName))
                {
                    var firstItem = this.Data.Items.FirstOrDefault();
                    if (null == firstItem) return 0;

                    return (int)firstItem.GetValue(this.Definition.Output.Paging?.RowCountFieldName);
                }
                return this.Data.Items.Count();
            }
        }

        public ReportDefinition Definition { get; private set; }

        public FilterValues FilterValues { get; private set; }

        public ReportOutput(string name, ReportDefinition reportDefinition, IDataSet data, FilterValues filterValues)
        {
            this.Name = name;
            this.Definition = reportDefinition;
            this.Data = data;
            this.FilterValues = filterValues;
        }
    }
}