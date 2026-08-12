using LogosSDK.Core.FSM;

namespace LogosSDK.Core.FSM.Demo
{
    public interface ICharacterState : IState<ICharacterState, ICharacterTrigger> { }
}