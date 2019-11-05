using System;
using System.Runtime.Serialization;

namespace Microsoft.Extensions.DependencyInjection
{
    [Serializable]
    internal class DeserializeMessageException : Exception
    {
        public DeserializeMessageException()
        {
        }

        public DeserializeMessageException(string message) : base(message)
        {
        }

        public DeserializeMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeserializeMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}