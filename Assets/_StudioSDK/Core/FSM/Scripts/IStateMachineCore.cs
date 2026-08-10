namespace LogosSDK.Core.FSM
{
    public interface IStateMachineCore
    {
        void OnUpdate();
        void OnFixedUpdate();
    }
}