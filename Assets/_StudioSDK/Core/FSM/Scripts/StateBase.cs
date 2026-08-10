using System;
using System.Collections.Generic;

namespace LogosSDK.Core.FSM
{
    public abstract class StateBase<TState, TTrigger> : IState<TState, TTrigger>
        where TState : IState<TState, TTrigger>
        where TTrigger : ITrigger
    {
        // Dictionary mapping trigger types to handler functions
        protected readonly Dictionary<Type, Func<TTrigger, TState>> TriggerHandlers = new();

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// Handles triggers and determines the next state if applicable
        /// </summary>
        public virtual TState HandleTrigger(TTrigger data)
        {
            var eventType = data.GetType();
            
            if (TriggerHandlers.TryGetValue(eventType, out var handler))
            {
                return handler(data);
            }
            
            return default;
        }

        /// <summary>
        /// Register an trigger handler for a specific trigger type
        /// </summary>
        protected void RegisterTriggerHandler<T>(Func<T, TState> handler) where T : TTrigger
        {
            TriggerHandlers[typeof(T)] = e => handler((T)e);
        }
    }
}