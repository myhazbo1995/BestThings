using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Abstract
{
    public abstract class EventBase
    {
        public Guid CorrelationId { get; }

        public EventBase()
        {
            // Assigns an unique id to the event.
            this.CorrelationId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return $"{this.CorrelationId.ToString()} - {this.GetType().Name}";
        }

        public string EventName { get { return this.GetType().Name.ToLowerInvariant(); } }
    }
}
