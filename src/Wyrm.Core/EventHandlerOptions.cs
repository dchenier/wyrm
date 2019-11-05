using System;
using System.Threading;
using System.Threading.Tasks;
using Wyrm.Events.Builder;

namespace Wyrm.Events
{
    public class EventHandlerOptions
    {
        private readonly IEventBuilder _builder;
        internal EventHandlerOptions(IEventBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }
        

        public void Use(Func<EventContext, Func<Task>, Task> middleware)
        {
            _builder.Use(middleware);
        }

        private int _instanceCount = 1;
        public int InstanceCount 
        { 
            get => _instanceCount; 
            set 
            {
                if (value <= 0)
                    throw new ArgumentException("InstanceCount cannot be less than or equal to zero", nameof(value));
                _instanceCount = value;
            } 
        }        
    }


}