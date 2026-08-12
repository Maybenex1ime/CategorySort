using LogosSDK.Core.FSM;
using UnityEngine;

namespace LogosSDK.Core.FSM.Demo
{
    public class IdleState : StateBase<ICharacterState, ICharacterTrigger>, ICharacterState
    {
        private static readonly int Idle = Animator.StringToHash("Idle");
        private readonly DemoController _character;

        public IdleState(DemoController character)
        {
            _character = character;

            // Define which triggers this state can handle
            RegisterTriggerHandler<JumpTrigger>(OnJump);
            RegisterTriggerHandler<RunTrigger>(OnRun);
            RegisterTriggerHandler<AttackTrigger>(OnAttack);
        }

        public override void OnEnter()
        {
            Debug.Log("Entering Idle State");
            _character.Animator.SetTrigger(Idle);
            _character.SetVelocity(Vector3.zero);
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Idle State");
        }

        public override void OnUpdate()
        {
            // Idle behavior
            _character.CheckGrounded();
        }

        private ICharacterState OnJump(JumpTrigger evt)
        {
            return new JumpState(_character);
        }

        private ICharacterState OnRun(RunTrigger evt)
        {
            return new RunState(_character, evt.Speed);
        }

        private ICharacterState OnAttack(AttackTrigger evt)
        {
            return new AttackState(_character, evt.AttackPower);
        }
    }
}