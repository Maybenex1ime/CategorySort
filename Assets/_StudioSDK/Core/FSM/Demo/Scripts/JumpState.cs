using System.Collections;
using LogosSDK.Core.FSM;
using UnityEngine;

namespace LogosSDK.Core.FSM.Demo
{
    public class JumpState : StateBase<ICharacterState, ICharacterTrigger>, ICharacterState
    {
        private static readonly int Jump = Animator.StringToHash("Jump");
        private readonly DemoController _demo;
        private const float JumpForce = 10f;

        public JumpState(DemoController demo)
        {
            _demo = demo;

            // Define which events this state can handle
            RegisterTriggerHandler<LandTrigger>(OnLand);
        }

        public override void OnEnter()
        {
            Debug.Log("Entering Jump State");
            _demo.Animator.SetTrigger(Jump);
            _demo.SetVelocity(new Vector3(0, JumpForce, 0));
            _demo.StartCoroutine(CheckLanding());
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Jump State");
        }

        public override void OnUpdate()
        {
            // Apply gravity
            var velocity = _demo.GetVelocity();
            velocity.y += Physics.gravity.y * Time.deltaTime;
            _demo.SetVelocity(velocity);
        }

        private IEnumerator CheckLanding()
        {
            // Wait a bit before checking for landing
            yield return new WaitForSeconds(0.1f);
            
            while (!_demo.IsGrounded)
            {
                yield return null;
            }
            
            // We've landed
            _demo.Trigger(new LandTrigger());
        }

        private ICharacterState OnLand(LandTrigger evt)
        {
            return new IdleState(_demo);
        }
    }
}