using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Worker.Core
{
    public class JobActivatorBase : IJobActivator
    {
        private readonly IServiceProvider ServiceProvider;

        public JobActivatorBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        protected virtual T CreateInstance<T>(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider);

        }

        public T CreateInstance<T>() => this.CreateInstance<T>(this.ServiceProvider);
    }
}
