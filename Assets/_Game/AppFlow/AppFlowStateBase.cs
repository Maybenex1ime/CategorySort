using LogosSDK.Core.FSM;
using UnityEngine;

namespace WordStack.Meta.AppFlow
{
    internal abstract class AppFlowStateBase
        : StateBase<IAppFlowState, IAppFlowTrigger>, IAppFlowState, IAsyncState
    {
        protected AppFlowStateBase(AppFlowContext context)
        {
            Context = context;
        }

        protected AppFlowContext Context { get; }

        public virtual Awaitable OnEnterAsync() => AwaitableUtility.Completed();

        public virtual Awaitable OnExitAsync() => AwaitableUtility.Completed();
    }
}
