﻿using EventBus.Abstract;
using Microsoft.Extensions.Logging;

namespace EventBus.Concrete
{
    /// <summary>
    /// Default implementation of <see cref="ISubscriptionErrorHandler"/> that log the error and re-throw it.
    /// </summary>
    /// <seealso cref="ISubscriptionErrorHandler" />
    public class DefaultSubscriptionErrorHandler : ISubscriptionErrorHandler
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        public DefaultSubscriptionErrorHandler(ILoggerFactory loggerFactory)
        {
            this._logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <inheritdoc />
        public void Handle(EventBase @event, Exception exception, ISubscription subscription)
        {
            this._logger.LogError(exception, "Error handling the event {0}", @event.GetType().Name);
            throw exception;
        }
    }
}
