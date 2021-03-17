using System;

namespace Adriva.Extensions.Social
{
    [System.Serializable]
    public class SocialMediaException : Exception
    {
        public SocialMediaException() { }
        public SocialMediaException(string message) : base(message) { }
        public SocialMediaException(string message, System.Exception inner) : base(message, inner) { }
        protected SocialMediaException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}