namespace Adriva.Common.Core
{
    public interface ISegmentedResult
    {
        bool HasMore { get; }
        string ContinuationToken { get; }
    }
}
