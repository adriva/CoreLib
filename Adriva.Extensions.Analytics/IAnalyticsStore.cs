using System.Collections.Generic;
using System.Threading.Tasks;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    public interface IAnalyticsStore
    {
        Task StoreAsync(IList<AnalyticsItem> items);
    }
}