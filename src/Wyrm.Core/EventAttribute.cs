using System;

namespace Wyrm.Events
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class EventAttribute: Attribute
    {
        public EventAttribute(string eventName) { EventName = eventName; }
        public string EventName { get; set; }

        /// <summary>
        /// Specifies wether other workers should receive the same messages
        /// (true = Pub/Sub with multiple consumers, false for worker task queue).
        /// Default is false
        /// </summary>
        public bool AllowMultipleConsumers { get; set; } = false;
        public Direction Direction { get; set; } = Direction.In;
    }
}