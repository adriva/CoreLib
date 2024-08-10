
using System;

namespace Adriva.Worker.Core.DynamicTrigger
{
    public class DynamicTriggerOptions
    {
        internal Type ListenerType { get; private set; }

        public void UseListener<T>() where T : IDynamicTriggerListener
        {
            this.ListenerType = typeof(T);
        }
    }
}