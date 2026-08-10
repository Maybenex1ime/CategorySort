using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LogosSDK.Core.Events
{
    /// <summary>
    /// Static store for invokers, one per event type.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    public static class InvokerStore<T>
    {
        /// <summary>
        /// The invoker instance for this event type.
        /// </summary>
        public static readonly Invoker<T> Invoker = new();
    }

    /// <summary>
    /// Manages and invokes event handlers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    public sealed class Invoker<T>
    {
        private readonly List<ListenerEntry<T>> _sync = new();
        private readonly List<AsyncListenerEntry<T>> _async = new();
        private int _dirty;
        private readonly object _syncLock = new();
        private readonly object _asyncLock = new();

        /// <summary>
        /// Gets the number of synchronous listeners.
        /// </summary>
        public int SyncListenerCount => _sync.Count;

        /// <summary>
        /// Gets the number of asynchronous listeners.
        /// </summary>
        public int AsyncListenerCount => _async.Count;

        /// <summary>
        /// Gets the total number of listeners.
        /// </summary>
        public int TotalListenerCount => _sync.Count + _async.Count;

        /// <summary>
        /// Invokes all registered handlers for an event.
        /// </summary>
        /// <param name="evt">The event instance.</param>
        internal void Invoke(T evt)
        {
            if (_dirty != 0) Sort();

            // Create a copy of the lists to avoid issues if handlers are added/removed during invocation
            List<ListenerEntry<T>> syncCopy;
            List<AsyncListenerEntry<T>> asyncCopy;

            lock (_syncLock)
            {
                syncCopy = new List<ListenerEntry<T>>(_sync);
            }

            lock (_asyncLock)
            {
                asyncCopy = new List<AsyncListenerEntry<T>>(_async);
            }

            // Invoke synchronous handlers
            foreach (var entry in syncCopy)
            {
                try
                {
                    if (entry.Invoke(evt)) continue;
                    // Target was destroyed, remove from the original list
                    lock (_syncLock)
                    {
                        _sync.Remove(entry);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            // Invoke asynchronous handlers
            if (asyncCopy.Count <= 0) return;
            var task = InvokeAsync(evt, asyncCopy);
            if (!task.IsCompletedSuccessfully)
            {
                ThreadDispatcher.AwaitOnMainThread(task);
            }
        }

        /// <summary>
        /// Adds a synchronous event handler.
        /// </summary>
        /// <param name="handler">The event handler delegate.</param>
        /// <param name="order">The execution order.</param>
        /// <param name="target">The target object, if any.</param>
        internal void AddSync(Action<T> handler, int order, UnityEngine.Object target)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_syncLock)
            {
                // Check if handler already exists
                if (_sync.Any(t => Equals(t.Action, handler)))
                {
                    return; // Already registered
                }

                _sync.Add(new ListenerEntry<T>(handler, order, target));
                Interlocked.Exchange(ref _dirty, 1);
            }
        }

        /// <summary>
        /// Adds an asynchronous event handler.
        /// </summary>
        /// <param name="handler">The asynchronous event handler delegate.</param>
        /// <param name="order">The execution order.</param>
        /// <param name="target">The target object, if any.</param>
        internal void AddAsync(Func<T, ValueTask> handler, int order, UnityEngine.Object target)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_asyncLock)
            {
                // Check if handler already exists
                if (_async.Any(t => Equals(t.Action, handler)))
                {
                    return; // Already registered
                }

                _async.Add(new AsyncListenerEntry<T>(handler, order, target));
                Interlocked.Exchange(ref _dirty, 1);
            }
        }

        /// <summary>
        /// Removes a synchronous event handler.
        /// </summary>
        /// <param name="handler">The event handler delegate to remove.</param>
        internal void RemoveSync(Action<T> handler)
        {
            if (handler == null) return;

            lock (_syncLock)
            {
                for (var i = _sync.Count - 1; i >= 0; i--)
                {
                    // Compare delegate targets and methods
                    if (!Equals(_sync[i].Action, handler)) continue;
                    _sync.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Removes an asynchronous event handler.
        /// </summary>
        /// <param name="handler">The asynchronous event handler delegate to remove.</param>
        internal void RemoveAsync(Func<T, ValueTask> handler)
        {
            if (handler == null) return;

            lock (_asyncLock)
            {
                for (var i = _async.Count - 1; i >= 0; i--)
                {
                    // Compare delegate targets and methods
                    if (!Equals(_async[i].Action, handler)) continue;
                    _async.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Removes all listeners (sync and async). Intended for test cleanup.
        /// </summary>
        internal void Clear()
        {
            lock (_syncLock) _sync.Clear();
            lock (_asyncLock) _async.Clear();
            Interlocked.Exchange(ref _dirty, 0);
        }

        /// <summary>
        /// Sorts the listener lists by order.
        /// </summary>
        private void Sort()
        {
            lock (_syncLock)
            {
                _sync.Sort((a, b) => a.Order.CompareTo(b.Order));
            }

            lock (_asyncLock)
            {
                _async.Sort((a, b) => a.Order.CompareTo(b.Order));
            }

            Interlocked.Exchange(ref _dirty, 0);
        }

        /// <summary>
        /// Invokes all asynchronous handlers for an event.
        /// </summary>
        /// <param name="evt">The event instance.</param>
        /// <param name="handlers">The list of handlers to invoke.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async ValueTask InvokeAsync(T evt, List<AsyncListenerEntry<T>> handlers)
        {
            List<AsyncListenerEntry<T>> toRemove = null;

            foreach (var t in handlers)
            {
                try
                {
                    var entry = t;
                    var task = entry.Invoke(evt);

                    if (task.Equals(default))
                    {
                        // Target was destroyed
                        toRemove ??= new List<AsyncListenerEntry<T>>();
                        toRemove.Add(entry);
                        continue;
                    }

                    await task;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            // Remove destroyed targets
            if (toRemove is { Count: > 0 })
            {
                lock (_asyncLock)
                {
                    foreach (var entry in toRemove)
                    {
                        _async.Remove(entry);
                    }
                }
            }
        }
    }
} 