using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Worker.Core.DynamicTrigger
{
    internal class DynamicTriggerBinding : ITriggerBinding
    {
        private class ValueProvider : IValueProvider
        {
            private readonly object Value;

            public ValueProvider(object value)
            {
                Value = value;
            }

            public Type Type
            {
                get { return typeof(DynamicTriggerData); }
            }

            public Task<object> GetValueAsync()
            {
                return Task.FromResult(this.Value);
            }

            public string ToInvokeString()
            {
                return Convert.ToString(this.Value);
            }
        }

        private readonly IServiceProvider ServiceProvider;
        private readonly DynamicTriggerAttribute Attribute;
        private readonly BindingDataProvider BindingDataProvider;
        private readonly ParameterInfo Parameter;
        private readonly DynamicTriggerOptions Options;

        public Type TriggerValueType => typeof(DynamicTriggerData);

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; private set; }

        public DynamicTriggerBinding(IServiceProvider serviceProvider, ParameterInfo parameter, DynamicTriggerOptions options)
        {
            this.ServiceProvider = serviceProvider;
            this.Parameter = parameter;
            this.Attribute = parameter.GetCustomAttribute<DynamicTriggerAttribute>(inherit: false);
            this.BindingDataProvider = BindingDataProvider.FromTemplate(this.Attribute.Identifier);
            this.BindingDataContract = this.CreateBindingContract();
            this.Options = options;
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            await Task.CompletedTask;

            DynamicTriggerData dynamicTriggerData = value as DynamicTriggerData;

            Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            bindingData.Add("DynamicTrigger", dynamicTriggerData);

            return new TriggerData(new ValueProvider(dynamicTriggerData), bindingData);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            IDynamicTriggerListener innerListener = (IDynamicTriggerListener)ActivatorUtilities.CreateInstance(this.ServiceProvider, this.Options.ListenerType);
            innerListener.Identifier = this.Attribute.Identifier;
            return Task.FromResult<IListener>(new DynamicTriggerListener(context.Executor, innerListener));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor()
            {
                Name = this.Parameter.Name,
                DisplayHints = new ParameterDisplayHints()
                {
                    Description = "Dynamic Triggger Data"
                }
            };
        }

        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            contract.Add("DynamicTrigger", typeof(DynamicTriggerData));

            if (null != this.BindingDataProvider.Contract)
            {
                foreach (KeyValuePair<string, Type> item in this.BindingDataProvider.Contract)
                {
                    // In case of conflict, binding data from the value type overrides the built-in binding data above.
                    contract[item.Key] = item.Value;
                }
            }

            return contract;
        }
    }
}