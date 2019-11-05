using System;
using System.Runtime.Serialization;

namespace Wyrm.Events.Builder
{
    [Serializable]
    public class WyrmConfigurationException : Exception
    {
        public WyrmConfigurationException()
        {
        }

        public WyrmConfigurationException(string message) : base(message)
        {
        }

        public WyrmConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WyrmConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}