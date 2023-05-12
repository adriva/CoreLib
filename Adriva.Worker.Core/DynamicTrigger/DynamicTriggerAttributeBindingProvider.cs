using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Adriva.Worker.Core.DynamicTrigger
{
    public class DynamicTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly INameResolver NameResolver;
        private readonly DynamicTriggerOptions Options;

        public DynamicTriggerAttributeBindingProvider(IServiceProvider serviceProvider, INameResolver nameResolver, DynamicTriggerOptions options)
        {
            this.ServiceProvider = serviceProvider;
            this.NameResolver = nameResolver;
            this.Options = options;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            ParameterInfo parameter = context.Parameter;
            DynamicTriggerAttribute attribute = parameter.GetCustomAttribute<DynamicTriggerAttribute>(inherit: false);

            if (null == attribute) return Task.FromResult<ITriggerBinding>(null);

            return Task.FromResult<ITriggerBinding>(new DynamicTriggerBinding(this.ServiceProvider, parameter, this.Options));
        }
    }
}