using System;
using Microsoft.Azure.WebJobs.Description;

namespace Adriva.Worker.Core.DynamicTrigger
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding()]
    public sealed class DynamicTriggerAttribute : System.Attribute
    {
        public string Identifier { get; private set; }

        public DynamicTriggerAttribute(string identifier)
        {
            this.Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        }
    }
}