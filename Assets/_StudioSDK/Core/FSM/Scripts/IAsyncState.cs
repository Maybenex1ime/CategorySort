using UnityEngine;

namespace LogosSDK.Core.FSM
{
    /// <summary>
    /// Extension interface for states that need async enter/exit (scene loading, service init, etc.).
    /// StateMachine detects this and awaits OnEnterAsync/OnExitAsync automatically.
    /// Sync OnEnter/OnExit from IState are still called as fallback if IAsyncState is not implemented.
    /// </summary>
    public interface IAsyncState
    {
        Awaitable OnEnterAsync();
        Awaitable OnExitAsync();
    }
}
