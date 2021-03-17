namespace Adriva.Extensions.Reports.Excel
{
    public sealed class ExcelTemplateRendererContext : ExcelRendererContext
    {
        public string TemplateFilePath { get; private set; }

        public string WorksheetName { get; private set; } // target worksheet's name

        public bool KeepCellStyle { get; set; }

        public ProcessingMode Mode { get; set; } = ProcessingMode.Default;

        public ExcelTemplateRendererContext(string templateFilePath, string worksheetName, string startingCell)
             : base(startingCell)
        {
            this.TemplateFilePath = templateFilePath;
            this.WorksheetName = worksheetName;
        }
    }
}
