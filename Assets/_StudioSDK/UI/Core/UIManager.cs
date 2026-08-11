using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Reflex.Attributes;
using Reflex.Core;
using Reflex.Injectors;
using ILogger = LogosSDK.Core.Logging.ILogger;
using LogosSDK.Core.Logging;

namespace LogosSDK.UI.Core
{
    public sealed class UIManager : MonoBehaviour
    {
        #region Types
        private static readonly ILogger _logger = LogManager.GetLogger<UIManager>();

        [Inject] private readonly Container _container;

        private struct CachedUI
        {
            public GameObject Instance;
            public AsyncOperationHandle Handle;
        }

        private struct PopupRequest
        {
            public Type PopupType;
            public object Args;
            public int Priority;
        }

        private struct ScreenOperationRequest
        {
            public Func<Awaitable> Operation;
            public AwaitableCompletionSource CompletionSource;
        }
        #endregion

        #region Fields
        private readonly Stack<IScreen> _screenStack = new Stack<IScreen>();
        private readonly List<PopupRequest> _popupQueue = new List<PopupRequest>();
        private readonly Queue<ScreenOperationRequest> _screenOperationQueue = new Queue<ScreenOperationRequest>();

        private readonly Dictionary<Type, CachedUI> _type2CachedScreen = new Dictionary<Type, CachedUI>();
        private readonly Dictionary<Type, CachedUI> _type2CachedPopup = new Dictionary<Type, CachedUI>();

        private readonly Dictionary<Type, Awaitable<IScreen>> _type2PendingScreen = new Dictionary<Type, Awaitable<IScreen>>();
        private readonly Dictionary<Type, Awaitable<IPopup>> _type2PendingPopup = new Dictionary<Type, Awaitable<IPopup>>();

        [SerializeField] private Transform _screenRoot;
        [SerializeField] private Transform _popupRoot;

        private IPopup _currentPopup;
        private bool _isPopupProcessing;
        private bool _isScreenQueueProcessing;
        #endregion

        #region Query
        /// Returns the current screen at the top of the stack.
        public IScreen CurrentScreen => _screenStack.TryPeek(out IScreen screen) ? screen : null;

        /// Returns whether a popup is currently visible or being shown.
        public bool IsPopupActive => _currentPopup != null || _isPopupProcessing;

        /// Returns whether the current screen stack has a screen above the root.
        public bool CanPopScreen => _screenStack.Count > 1;
        #endregion

        #region Screen Flow
        /// Loads and caches a screen without showing it, so the first push is instant.
        public async Awaitable PreWarmScreen<T>() where T : IScreen
        {
            await LoadOrGetScreen(typeof(T));
        }

        /// Pushes a new screen on top of the stack and hides the previous top screen.
        public Awaitable PushScreen<T>(object args = null) where T : IScreen
        {
            return EnqueueScreenOperation(() => PushScreenInternal<T>(args));
        }

        /// Pops the current top screen and re-activates the screen below it.
        public Awaitable PopScreen()
        {
            return EnqueueScreenOperation(PopScreenInternal);
        }

        /// Replaces the current top screen with a new screen.
        public Awaitable ReplaceScreen<T>(object args = null) where T : IScreen
        {
            return EnqueueScreenOperation(() => ReplaceScreenInternal<T>(args));
        }

        /// Pushes a new screen on top of the stack and hides the previous top screen.
        private async Awaitable PushScreenInternal<T>(object args) where T : IScreen
        {
            T screen = (T)await LoadOrGetScreen(typeof(T));

            if (_screenStack.TryPeek(out IScreen current))
            {
                current.OnLostTop();
                SetCachedInstanceActive(_type2CachedScreen, current.GetType(), false);
            }

            _screenStack.Push(screen);
            await screen.Show(args);
        }

        /// Pops the current top screen and re-activates the screen below it.
        private async Awaitable PopScreenInternal()
        {
            if (!CanPopScreen) return;

            IScreen top = _screenStack.Pop();
            await top.Hide();

            if (_screenStack.TryPeek(out IScreen below))
            {
                SetCachedInstanceActive(_type2CachedScreen, below.GetType(), true);
                below.OnBecameTop();
            }
        }

        /// Replaces the current top screen with a new screen.
        private async Awaitable ReplaceScreenInternal<T>(object args) where T : IScreen
        {
            if (_screenStack.TryPop(out IScreen old))
            {
                old.OnLostTop();
                await old.Hide();
            }

            T screen = (T)await LoadOrGetScreen(typeof(T));
            _screenStack.Push(screen);
            await screen.Show(args);
        }

        /// Enqueues a screen operation so fast repeated inputs are executed in order.
        private Awaitable EnqueueScreenOperation(Func<Awaitable> operation)
        {
            AwaitableCompletionSource completionSource = new AwaitableCompletionSource();
            _screenOperationQueue.Enqueue(new ScreenOperationRequest
            {
                Operation = operation,
                CompletionSource = completionSource
            });

            if (!_isScreenQueueProcessing)
            {
                ProcessScreenQueueInBackground();
            }

            return completionSource.Awaitable;
        }

        /// Drains queued screen operations one by one and waits for each animation to finish.
        private async Awaitable ProcessScreenQueue()
        {
            if (_isScreenQueueProcessing)
            {
                return;
            }

            _isScreenQueueProcessing = true;

            try
            {
                while (_screenOperationQueue.Count > 0)
                {
                    ScreenOperationRequest request = _screenOperationQueue.Dequeue();

                    try
                    {
                        await request.Operation();
                        request.CompletionSource.SetResult();
                    }
                    catch (Exception ex)
                    {
                        request.CompletionSource.SetException(ex);
                        _logger.Error(ex, "[UIManager] Screen operation failed.");
                    }
                }
            }
            finally
            {
                _isScreenQueueProcessing = false;
            }
        }
        #endregion

        #region Popup Flow
        /// Enqueues a popup by priority and shows it when no popup is active.
        public void EnqueuePopup<TPopup, TArgs>(TArgs args, int priority = 0) where TPopup : IPopup<TArgs>
        {
            var request = new PopupRequest { PopupType = typeof(TPopup), Args = args, Priority = priority };

            int insertIndex = -1;
            for (int i = _popupQueue.Count - 1; i >= 0; i--)
            {
                if (_popupQueue[i].Priority >= priority)
                {
                    insertIndex = i;
                    break;
                }
            }
            _popupQueue.Insert(insertIndex + 1, request);
            
            TryShowNextPopup();
        }

        /// Dismisses the current popup first, then shows the requested popup immediately.
        public async Awaitable ShowPopupImmediate<TPopup, TArgs>(TArgs args) where TPopup : IPopup<TArgs>
        {
            try
            {
                await ShowPopupImmediateInternal(typeof(TPopup), args);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UIManager] Show popup immediate failed.");
                throw;
            }
        }

        public Awaitable RemoveCurrentScreen()
        {
            return EnqueueScreenOperation(RemoveCurrentScreenInternal);
        }

        private async Awaitable ShowPopupImmediateInternal(Type popupType, object args)
        {
            if (IsPopupActive)
            {
                await DismissCurrentPopupAsync();
            }

            await ExecuteShowPopup(popupType, args);
        }

        private async Awaitable RemoveCurrentScreenInternal()
        {
            if (!_screenStack.TryPop(out IScreen currentScreen))
            {
                return;
            }

            currentScreen.OnLostTop();
            await currentScreen.Hide();

            if (_screenStack.TryPeek(out IScreen nextScreen))
            {
                SetCachedInstanceActive(_type2CachedScreen, nextScreen.GetType(), true);
                nextScreen.OnBecameTop();
            }
        }

        private async Awaitable ExecuteShowPopup(Type popupType, object args)
        {
            _isPopupProcessing = true;

            try
            {
                await ShowPopupInternal(popupType, args);
            }
            finally
            {
                _isPopupProcessing = false;
                if (_currentPopup == null) 
                {
                    TryShowNextPopup();
                }
            }
        }

        /// Loads a popup, binds its dismissal callback, and shows it with the provided args.
        private async Awaitable ShowPopupInternal(Type popupType, object args)
        {
            IPopup popup = await LoadOrGetPopup(popupType);
            _currentPopup = popup;
            _currentPopup.OnDismissed += DismissCurrentPopup;

            try
            {
                _currentPopup.SetArgs(args);
                await _currentPopup.Show();
            }
            catch
            {
                ResetCurrentPopup();
                throw;
            }
        }

        /// Dismisses the currently active popup and continues the popup queue.
        public void DismissCurrentPopup()
        {
            DismissCurrentPopupInBackground();
        }

        private async Awaitable DismissCurrentPopupAsync()
        {
            if (_currentPopup != null)
            {
                var dismissedPopup = _currentPopup;
                ResetCurrentPopup();
                await dismissedPopup.Hide();
            }

            TryShowNextPopup();
        }

        /// Clears the current popup reference and detaches dismissal callbacks.
        private void ResetCurrentPopup()
        {
            if (_currentPopup == null)
            {
                return;
            }

            _currentPopup.OnDismissed -= DismissCurrentPopup;
            _currentPopup = null;
        }

        private void TryShowNextPopup()
        {
            if (IsPopupActive || _popupQueue.Count == 0) return;

            PopupRequest request = _popupQueue[0];
            _popupQueue.RemoveAt(0);

            ShowQueuedPopupInBackground(request);
        }
        #endregion

        #region Loading
        private async Awaitable<IScreen> LoadOrGetScreen(Type type)
        {
            if (_type2CachedScreen.TryGetValue(type, out CachedUI cached))
            {
                cached.Instance.TryGetComponent(out IScreen comp);
                return comp;
            }

            if (_type2PendingScreen.TryGetValue(type, out Awaitable<IScreen> pendingLoad))
            {
                return await pendingLoad;
            }

            var asyncLoad = LoadScreenAsync(type);
            _type2PendingScreen[type] = asyncLoad;
            return await asyncLoad;
        }

        private async Awaitable<IScreen> LoadScreenAsync(Type type)
        {
            try
            {
                Transform root = _screenRoot != null ? _screenRoot : transform;
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(type.Name, root);
                GameObject instance = await handle.Task;
                instance.SetActive(false);
                InjectRecursiveIfAvailable(instance);

                if (!instance.TryGetComponent(out IScreen screen) || screen == null)
                {
                    if (handle.IsValid())
                    {
                        Addressables.ReleaseInstance(instance);
                    }

                    throw new InvalidOperationException($"[UIManager] Loaded screen '{type.Name}' does not implement IScreen.");
                }

                _type2CachedScreen[type] = new CachedUI { Instance = instance, Handle = handle };
                return screen;
            }
            finally
            {
                _type2PendingScreen.Remove(type);
            }
        }

        private async Awaitable<IPopup> LoadOrGetPopup(Type type)
        {
            if (_type2CachedPopup.TryGetValue(type, out CachedUI cached))
            {
                SetCachedInstanceActive(_type2CachedPopup, type, true);
                cached.Instance.TryGetComponent(out IPopup comp);
                return comp;
            }

            if (_type2PendingPopup.TryGetValue(type, out Awaitable<IPopup> pendingLoad))
            {
                return await pendingLoad;
            }

            var asyncLoad = LoadPopupAsync(type);
            _type2PendingPopup[type] = asyncLoad;
            return await asyncLoad;
        }

        private async Awaitable<IPopup> LoadPopupAsync(Type type)
        {
            try
            {
                Transform root = _popupRoot != null ? _popupRoot : transform;
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(type.Name, root);
                GameObject instance = await handle.Task;
                InjectRecursiveIfAvailable(instance);

                if (!instance.TryGetComponent(out IPopup popup) || popup == null)
                {
                    if (handle.IsValid())
                    {
                        Addressables.ReleaseInstance(instance);
                    }

                    throw new InvalidOperationException($"[UIManager] Loaded popup '{type.Name}' does not implement IPopup.");
                }

                _type2CachedPopup[type] = new CachedUI { Instance = instance, Handle = handle };
                return popup;
            }
            finally
            {
                _type2PendingPopup.Remove(type);
            }
        }

        private async void ProcessScreenQueueInBackground()
        {
            await ProcessScreenQueue();
        }
        #endregion

        private async void DismissCurrentPopupInBackground()
        {
            try
            {
                await DismissCurrentPopupAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UIManager] Dismiss current popup failed.");
            }
        }

        private async void ShowQueuedPopupInBackground(PopupRequest request)
        {
            try
            {
                await ExecuteShowPopup(request.PopupType, request.Args);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UIManager] Show queued popup failed.");
            }
        }

        #region Helpers
        private static void SetCachedInstanceActive(Dictionary<Type, CachedUI> cache, Type type, bool isActive)
        {
            if (cache.TryGetValue(type, out CachedUI cached))
            {
                cached.Instance.SetActive(isActive);
            }
        }

        private void InjectRecursiveIfAvailable(GameObject instance)
        {
            if (_container != null)
            {
                GameObjectInjector.InjectRecursive(instance, _container);
                return;
            }

            if (_logger.IsDebugEnabled)
            {
                _logger.Warn("Reflex Container is null in UIManager. Dependency injection for UI instance skipped.");
            }
        }

        private static void ReleaseCachedInstances(Dictionary<Type, CachedUI> cache)
        {
            foreach (CachedUI cached in cache.Values)
            {
                if (cached.Handle.IsValid())
                {
                    Addressables.ReleaseInstance(cached.Instance);
                }
            }

            cache.Clear();
        }

        private void OnDestroy()
        {
            ReleaseCachedInstances(_type2CachedScreen);
            ReleaseCachedInstances(_type2CachedPopup);
        }
        #endregion
    }
}
