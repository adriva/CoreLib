
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Options;

namespace Adriva.Worker.Core.DynamicTrigger
{
    public class DynamicTriggerExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly INameResolver NameResolver;
        private readonly DynamicTriggerOptions Options;

        public DynamicTriggerExtensionConfigProvider(IServiceProvider serviceProvider, INameResolver nameResolver, IOptions<DynamicTriggerOptions> options)
        {
            this.ServiceProvider = serviceProvider;
            this.NameResolver = nameResolver;
            this.Options = options.Value;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingProvider = new DynamicTriggerAttributeBindingProvider(this.ServiceProvider, this.NameResolver, this.Options);
            context.AddBindingRule<DynamicTriggerAttribute>()
                    .BindToTrigger(bindingProvider);
        }
    }
}