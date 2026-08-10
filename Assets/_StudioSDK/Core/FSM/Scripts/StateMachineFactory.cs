using UnityEngine;

namespace LogosSDK.Core.FSM
{
    public static class StateMachineFactory
    {
        /// <summary>
        /// Create and sync-initialize — for Gameplay Flow (sync states).
        /// </summary>
        public static StateMachine<TState, TTrigger> Create<TState, TTrigger>(
            GameObject gameObject,
            TState initialState)
            where TState : IState<TState, TTrigger>
            where TTrigger : ITrigger
        {
            var stateMachine = new StateMachine<TState, TTrigger>(gameObject);
            stateMachine.Initialize(initialState);
            return stateMachine;
        }

        /// <summary>
        /// Create and async-initialize — for App Flow (IAsyncState states).
        /// </summary>
        public static async Awaitable<StateMachine<TState, TTrigger>> CreateAsync<TState, TTrigger>(
            GameObject gameObject,
            TState initialState)
            where TState : IState<TState, TTrigger>
            where TTrigger : ITrigger
        {
            var stateMachine = new StateMachine<TState, TTrigger>(gameObject);
            await stateMachine.InitializeAsync(initialState);
            return stateMachine;
        }
    }
}
