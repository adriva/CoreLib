using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adriva.Extensions.Analytics.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Analytics
{
    internal sealed class SqlServerAnalyticsStore
    {
        private readonly IServiceProvider ServiceProvider;

        public SqlServerAnalyticsStore(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public async Task StoreAsync(IList<AnalyticsItem> items)
        {
            if (null == items || !items.Any()) return;

            using (var serviceScope = this.ServiceProvider.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
                var cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
                    //string sql = dbContext.GetSql();
                    await dbContext.AddRangeAsync(items);
                    await dbContext.SaveChangesAsync(cancellationTokenSource.Token);
                }
                catch
                {
                }
                finally
                {
                    cancellationTokenSource.Dispose();
                }
            }
        }
    }
}