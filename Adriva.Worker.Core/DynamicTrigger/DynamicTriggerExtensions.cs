using System;
using Microsoft.Azure.WebJobs;

namespace Adriva.Worker.Core.DynamicTrigger
{
    public static class DynamicTriggerExtensions
    {
        public static IWebJobsBuilder AddDynamicTrigger(this IWebJobsBuilder builder, Action<DynamicTriggerOptions> configureOptions)
        {
            builder
                .AddExtension<DynamicTriggerExtensionConfigProvider>()
                .BindOptions<DynamicTriggerOptions>()
                .ConfigureOptions<DynamicTriggerOptions>((configuration, options) =>
                {
                    configureOptions.Invoke(options);
                });
            return builder;
        }
    }
}