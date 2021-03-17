using System;
using System.Runtime.Serialization;

namespace Adriva.Extensions.RuleEngine
{
    [Serializable]
    public class RuleEngineException : Exception
    {
        public bool IsWarning { get; private set; }

        public RuleEngineException() { }

        public RuleEngineException(bool isWarning)
        {
            this.IsWarning = isWarning;
        }

        public RuleEngineException(string message) : base(message) { }

        public RuleEngineException(string message, System.Exception inner) : base(message, inner) { }

        protected RuleEngineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.IsWarning = info.GetBoolean("isWarning");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("isWarning", this.IsWarning);
            base.GetObjectData(info, context);
        }
    }
}
