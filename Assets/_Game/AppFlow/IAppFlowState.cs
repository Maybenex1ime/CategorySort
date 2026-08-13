using LogosSDK.Core.FSM;

namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// Đóng vòng generic của IState. StateMachine cần TState : IState&lt;TState, TTrigger&gt;,
    /// nên phải có một interface cụ thể trỏ về chính nó.
    /// </summary>
    public interface IAppFlowState : IState<IAppFlowState, IAppFlowTrigger>
    {
    }
}
