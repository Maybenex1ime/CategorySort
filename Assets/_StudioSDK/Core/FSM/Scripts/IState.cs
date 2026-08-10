namespace LogosSDK.Core.FSM
{
    public interface IState<out TState, in TTrigger> where TState : IState<TState, TTrigger> where TTrigger : ITrigger
    {
        void OnEnter();
        void OnExit();
        void OnUpdate();
        void OnFixedUpdate();
        TState HandleTrigger(TTrigger data);
    }
}