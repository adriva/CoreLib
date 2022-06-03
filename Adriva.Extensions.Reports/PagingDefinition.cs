namespace Adriva.Extensions.Reports
{
    public sealed class PagingDefinition
    {
        public int PageSize { get; set; } = 20;

        public string PageIndexParameter { get; set; } = "pageIndex";

        public string PageSizeParameter { get; set; } = "pageSize";

        public string RowCountFieldName { get; set; } = "TotalCount";

    }
}