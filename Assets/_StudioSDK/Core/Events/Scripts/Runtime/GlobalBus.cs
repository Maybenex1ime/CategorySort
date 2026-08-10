using System;
using System.Threading.Tasks;

namespace LogosSDK.Core.Events
{
    /// <summary>
    /// Static entry point for the event bus system.
    /// </summary>
    public static class Bus
    {
        /// <summary>
        /// The global event bus instance.
        /// </summary>
        public static readonly GlobalBus Global = new();
    }

    /// <summary>
    /// A global event bus that allows publishing and subscribing to events.
    /// </summary>
    public sealed class GlobalBus : IEventBus
    {
        // Explicit IEventBus implementation (struct-constrained for DI consumers)
        void IEventBus.Fire<T>(T evt) => Fire(evt);
        void IEventBus.On<T>(Action<T> handler) => On(handler);
        void IEventBus.Off<T>(Action<T> handler) => Off(handler);

        /// <summary>
        /// Publishes an event to all registered handlers.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="evt">The event instance.</param>
        public void Fire<T>(T evt)
        {
            var invoker = InvokerStore<T>.Invoker;
            
            if (ThreadDispatcher.IsMainThread)
            {
                invoker.Invoke(evt);
            }
            else
            {
                ThreadDispatcher.Enqueue(() => invoker.Invoke(evt));
            }
        }

        /// <summary>
        /// Subscribes a handler to an event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="handler">The event handler.</param>
        /// <param name="order">The execution order (lower values execute first).</param>
        /// <returns>The global bus instance for method chaining.</returns>
        public GlobalBus On<T>(Action<T> handler, int order = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            UnityEngine.Object target = null;
            
            // Try to get the target object from the handler's target
            if (handler.Target is UnityEngine.Object obj)
            {
                target = obj;
            }
            
            InvokerStore<T>.Invoker.AddSync(handler, order, target);
            return this;
        }

        /// <summary>
        /// Subscribes an asynchronous handler to an event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="handler">The asynchronous event handler.</param>
        /// <param name="order">The execution order (lower values execute first).</param>
        /// <returns>The global bus instance for method chaining.</returns>
        public GlobalBus OnAsync<T>(Func<T, ValueTask> handler, int order = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            UnityEngine.Object target = null;
            
            // Try to get the target object from the handler's target
            if (handler.Target is UnityEngine.Object obj)
            {
                target = obj;
            }
            
            InvokerStore<T>.Invoker.AddAsync(handler, order, target);
            return this;
        }

        /// <summary>
        /// Unsubscribes a handler from an event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="handler">The event handler to unsubscribe.</param>
        /// <returns>The global bus instance for method chaining.</returns>
        public GlobalBus Off<T>(Action<T> handler)
        {
            if (handler == null) return this;
            
            InvokerStore<T>.Invoker.RemoveSync(handler);
            return this;
        }

        /// <summary>
        /// Unsubscribes an asynchronous handler from an event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="handler">The asynchronous event handler to unsubscribe.</param>
        /// <returns>The global bus instance for method chaining.</returns>
        public GlobalBus OffAsync<T>(Func<T, ValueTask> handler)
        {
            if (handler == null) return this;
            
            InvokerStore<T>.Invoker.RemoveAsync(handler);
            return this;
        }

        /// <summary>
        /// Checks if there are any listeners for a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <returns>True if there are listeners; otherwise, false.</returns>
        public bool HasListeners<T>()
        {
            return InvokerStore<T>.Invoker.TotalListenerCount > 0;
        }

        /// <summary>
        /// Gets the number of listeners for a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <returns>The number of listeners.</returns>
        public int ListenerCount<T>()
        {
            return InvokerStore<T>.Invoker.TotalListenerCount;
        }
    }
} 