using System.Collections;
using LogosSDK.Core.FSM;
using UnityEngine;

namespace LogosSDK.Core.FSM.Demo
{
    public class AttackState : StateBase<ICharacterState, ICharacterTrigger>, ICharacterState
    {
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int AttackPower = Animator.StringToHash("AttackPower");
        private readonly DemoController _demo;
        private readonly int _attackPower;
        private const float AttackDuration = 1.0f;

        public AttackState(DemoController demo, int attackPower)
        {
            _demo = demo;
            _attackPower = attackPower;

            RegisterTriggerHandler<IdleTrigger>(OnIdle);
        }

        public override void OnEnter()
        {
            Debug.Log($"Entering Attack State with power: {_attackPower}");
            _demo.Animator.SetTrigger(Attack);
            _demo.Animator.SetFloat(AttackPower, _attackPower);
            _demo.SetVelocity(Vector3.zero);
            _demo.StartCoroutine(FinishAttack());
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Attack State");
        }

        private IEnumerator FinishAttack()
        {
            yield return new WaitForSeconds(AttackDuration);
            _demo.Trigger(new IdleTrigger());
        }
        
        private ICharacterState OnIdle(IdleTrigger evt)
        {
            return new IdleState(_demo);
        }
    }
}