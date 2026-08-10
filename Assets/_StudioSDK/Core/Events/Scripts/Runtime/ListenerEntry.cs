using System;
using System.Threading.Tasks;

namespace LogosSDK.Core.Events
{
    /// <summary>
    /// Base class for listener entries.
    /// </summary>
    internal abstract class BaseListenerEntry
    {
        /// <summary>
        /// The execution order of this listener.
        /// </summary>
        public readonly int Order;
        
        /// <summary>
        /// Weak reference to the target object, if any.
        /// </summary>
        private readonly WeakReference<UnityEngine.Object> _target;

        /// <summary>
        /// Creates a new instance of BaseListenerEntry.
        /// </summary>
        /// <param name="order">The execution order.</param>
        /// <param name="target">The target object, if any.</param>
        protected BaseListenerEntry(int order, UnityEngine.Object target)
        {
            Order = order;
            _target = target != null ? new WeakReference<UnityEngine.Object>(target) : null;
        }

        /// <summary>
        /// Checks if the target object is still alive.
        /// </summary>
        /// <returns>True if the target is null or still alive; false if the target has been destroyed.</returns>
        protected bool IsTargetAlive()
        {
            if (_target == null) return true;
            
            if (_target.TryGetTarget(out var target))
            {
                return target != null;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Stores a synchronous event handler and its associated metadata.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    internal sealed class ListenerEntry<T> : BaseListenerEntry
    {
        /// <summary>
        /// The event handler delegate.
        /// </summary>
        internal readonly Action<T> Action;

        /// <summary>
        /// Creates a new instance of ListenerEntry.
        /// </summary>
        /// <param name="action">The event handler delegate.</param>
        /// <param name="order">The execution order.</param>
        /// <param name="target">The target object, if any.</param>
        public ListenerEntry(Action<T> action, int order, UnityEngine.Object target) : base(order, target)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Invokes the event handler if the target is still alive.
        /// </summary>
        /// <param name="evt">The event instance.</param>
        /// <returns>True if the handler was invoked; false if the target is destroyed.</returns>
        public bool Invoke(T evt)
        {
            if (!IsTargetAlive())
            {
                return false;
            }
            
            Action(evt);
            return true;
        }
    }

    /// <summary>
    /// Stores an asynchronous event handler and its associated metadata.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    internal sealed class AsyncListenerEntry<T> : BaseListenerEntry
    {
        /// <summary>
        /// The asynchronous event handler delegate.
        /// </summary>
        internal readonly Func<T, ValueTask> Action;

        /// <summary>
        /// Creates a new instance of AsyncListenerEntry.
        /// </summary>
        /// <param name="action">The asynchronous event handler delegate.</param>
        /// <param name="order">The execution order.</param>
        /// <param name="target">The target object, if any.</param>
        public AsyncListenerEntry(Func<T, ValueTask> action, int order, UnityEngine.Object target) : base(order, target)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Invokes the asynchronous event handler if the target is still alive.
        /// </summary>
        /// <param name="evt">The event instance.</param>
        /// <returns>A ValueTask representing the asynchronous operation, or a completed task if the target is destroyed.</returns>
        public ValueTask Invoke(T evt)
        {
            if (!IsTargetAlive())
            {
                return default;
            }
            
            return Action(evt);
        }
    }
} 