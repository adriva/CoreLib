namespace Adriva.Extensions.Reports
{
    public interface IDataItem
    {
        object GetValue(string fieldName);
    }
}