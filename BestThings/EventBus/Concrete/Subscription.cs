using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Abstract;

namespace EventBus.Concrete
{
    internal class Subscription<TEventBase> : ISubscription where TEventBase : EventBase
    {
        /// <summary>
        /// Token returned to the subscriber
        /// </summary>
        public SubscriptionToken SubscriptionToken { get; }

        /// <summary>
        /// The action to invoke when a subscripted event type is published.
        /// </summary>
        private readonly Action<TEventBase> _action;

        public Subscription(Action<TEventBase> action, SubscriptionToken token)
        {
            this._action = action ?? throw new ArgumentNullException(nameof(action));
            this.SubscriptionToken = token ?? throw new ArgumentNullException(nameof(token));
        }

        public void Publish(EventBase eventItem)
        {
            if (!(eventItem is TEventBase))
                throw new ArgumentException("Event Item is not the correct type.");

            this._action.Invoke(eventItem as TEventBase);
        }
    }
}
