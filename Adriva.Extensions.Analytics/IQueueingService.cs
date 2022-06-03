using Adriva.AppInsights.Serialization.Contracts;

namespace Adriva.Extensions.Analytics
{
    public interface IQueueingService
    {
        bool IsAddingCompleted { get; }

        void Queue(Envelope envelope);

        void CompleteAdding();

        bool TryGetNext(int millisecondsTimeout, out Envelope envelope);
    }
}