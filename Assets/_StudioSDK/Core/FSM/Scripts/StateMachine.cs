using System.Collections.Generic;
using UnityEngine;

namespace LogosSDK.Core.FSM
{
    public class StateMachine<TState, TTrigger> : IStateMachineCore
        where TState : IState<TState, TTrigger>
        where TTrigger : ITrigger
    {
        private TState _currentState;
        private bool _isTransitioning;
        private readonly Queue<TTrigger> _pendingTriggers = new();

        public TState CurrentState => _currentState;

        public StateMachine(GameObject gameObject)
        {
            var runner = gameObject.AddComponent<StateMachineRunner>();
            runner.Initialize(this);
        }

        /// <summary>
        /// Constructor without GameObject — for headless/test usage (no Update/FixedUpdate).
        /// </summary>
        public StateMachine() { }

        public bool Trigger(TTrigger data)
        {
            if (_currentState == null)
                return false;

            if (_isTransitioning)
            {
                _pendingTriggers.Enqueue(data);
                return false;
            }

            var nextState = _currentState.HandleTrigger(data);

            if (nextState == null || nextState.Equals(_currentState)) return false;

            TransitionToState(nextState);
            return true;
        }

        /// <summary>
        /// Async variant of Trigger — awaits IAsyncState transitions.
        /// Use for App Flow states that need async enter/exit.
        /// </summary>
        public async Awaitable<bool> TriggerAsync(TTrigger data)
        {
            if (_currentState == null)
                return false;

            if (_isTransitioning)
            {
                _pendingTriggers.Enqueue(data);
                return false;
            }

            var nextState = _currentState.HandleTrigger(data);

            if (nextState == null || nextState.Equals(_currentState)) return false;

            await TransitionToStateAsync(nextState);
            return true;
        }

        /// <summary>
        /// Sync initialize — calls OnEnter (ignores IAsyncState).
        /// Use for Gameplay Flow states.
        /// </summary>
        public void Initialize(TState initialState)
        {
            if (_currentState != null)
            {
                _currentState.OnExit();
            }

            _currentState = initialState;
            _isTransitioning = true;
            _currentState.OnEnter();
            _isTransitioning = false;

            ProcessPendingTriggers();
        }

        /// <summary>
        /// Async initialize — awaits IAsyncState.OnEnterAsync if implemented,
        /// otherwise falls back to sync OnEnter.
        /// Use for App Flow states.
        /// </summary>
        public async Awaitable InitializeAsync(TState initialState)
        {
            if (_currentState != null)
            {
                await ExitStateAsync(_currentState);
            }

            _currentState = initialState;
            _isTransitioning = true;
            await EnterStateAsync(_currentState);
            _isTransitioning = false;

            ProcessPendingTriggers();
        }

        private void TransitionToState(TState newState)
        {
            _isTransitioning = true;

            _currentState.OnExit();
            _currentState = newState;
            _currentState.OnEnter();

            _isTransitioning = false;

            ProcessPendingTriggers();
        }

        private async Awaitable TransitionToStateAsync(TState newState)
        {
            _isTransitioning = true;

            await ExitStateAsync(_currentState);
            _currentState = newState;
            await EnterStateAsync(_currentState);

            _isTransitioning = false;

            ProcessPendingTriggers();
        }

        private static async Awaitable EnterStateAsync(TState state)
        {
            if (state is IAsyncState asyncState)
            {
                await asyncState.OnEnterAsync();
            }
            else
            {
                state.OnEnter();
            }
        }

        private static async Awaitable ExitStateAsync(TState state)
        {
            if (state is IAsyncState asyncState)
            {
                await asyncState.OnExitAsync();
            }
            else
            {
                state.OnExit();
            }
        }

        private void ProcessPendingTriggers()
        {
            while (_pendingTriggers.Count > 0)
            {
                var trigger = _pendingTriggers.Dequeue();
                Trigger(trigger);
            }
        }

        public void OnUpdate()
        {
            if (_currentState != null && !_isTransitioning)
            {
                _currentState.OnUpdate();
            }
        }

        public void OnFixedUpdate()
        {
            if (_currentState != null && !_isTransitioning)
            {
                _currentState.OnFixedUpdate();
            }
        }
    }
}
