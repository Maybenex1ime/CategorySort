using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LogosSDK.Core.Events
{
    /// <summary>
    /// Handles dispatching of events across threads, ensuring that event handlers
    /// are always executed on the main Unity thread.
    /// </summary>
    internal static class ThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> ActionQueue = new();
        private static int _mainThreadId;
        private static bool _initialized;

        /// <summary>
        /// Gets whether the current code is executing on Unity's main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _initialized = true;
            
#if UNITY_EDITOR
            EditorApplication.update += ProcessQueue;
#else
            Application.onBeforeRender += ProcessQueue;
#endif
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            
            ActionQueue.Enqueue(action);
            
#if UNITY_EDITOR
            if (EditorApplication.isPlaying && IsMainThread)
            {
                ProcessQueue();
            }
#endif
        }

        /// <summary>
        /// Awaits a ValueTask on the main thread.
        /// </summary>
        /// <param name="task">The task to await.</param>
        public static async void AwaitOnMainThread(ValueTask task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void ProcessQueue()
        {
            // Process a maximum of 100 actions per frame to prevent freezing
            var processCount = 0;
            const int maxActionsPerFrame = 100;
            
            while (ActionQueue.TryDequeue(out var action) && processCount < maxActionsPerFrame)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                
                processCount++;
            }
        }
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            Initialize();
        }
#endif
    }
} 