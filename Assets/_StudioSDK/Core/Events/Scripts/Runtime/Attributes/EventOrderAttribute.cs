using System;

namespace LogosSDK.Core.Events
{
    /// <summary>
    /// Attribute to specify the execution order of event handlers.
    /// Lower values are executed first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventOrderAttribute : Attribute
    {
        /// <summary>
        /// The order value. Lower values are executed first.
        /// </summary>
        public readonly int Order;

        /// <summary>
        /// Creates a new instance of EventOrderAttribute with the specified order.
        /// </summary>
        /// <param name="order">The order value. Lower values are executed first.</param>
        public EventOrderAttribute(int order) => Order = order;
    }
} 