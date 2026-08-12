using LogosSDK.Core.FSM;
using UnityEngine;

namespace LogosSDK.Core.FSM.Demo
{
    public class RunState : StateBase<ICharacterState, ICharacterTrigger>, ICharacterState
    {
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int RunSpeed = Animator.StringToHash("RunSpeed");
        private readonly DemoController _demo;
        private readonly float _speed;

        public RunState(DemoController demo, float speed)
        {
            _demo = demo;
            _speed = speed;

            // Define which events this state can handle
            RegisterTriggerHandler<JumpTrigger>(OnJump);
            RegisterTriggerHandler<IdleTrigger>(OnIdle);
            RegisterTriggerHandler<AttackTrigger>(OnAttack);
        }

        public override void OnEnter()
        {
            Debug.Log($"Entering Run State with speed: {_speed}");
            _demo.Animator.SetTrigger(Run);
            _demo.Animator.SetFloat(RunSpeed, _speed);
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Run State");
        }

        public override void OnUpdate()
        {
            // Run behavior
            _demo.CheckGrounded();
            _demo.SetVelocity(_demo.transform.forward * _speed);
        }

        private ICharacterState OnJump(JumpTrigger evt)
        {
            return new JumpState(_demo);
        }

        private ICharacterState OnIdle(IdleTrigger evt)
        {
            return new IdleState(_demo);
        }

        private ICharacterState OnAttack(AttackTrigger evt)
        {
            return new AttackState(_demo, evt.AttackPower);
        }
    }
}