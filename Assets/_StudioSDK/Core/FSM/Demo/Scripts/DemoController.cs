using System.Collections;
using LogosSDK.Core.FSM;
using UnityEngine;

namespace LogosSDK.Core.FSM.Demo
{
    public class DemoController : MonoBehaviour
    {
        private StateMachine<ICharacterState, ICharacterTrigger> _stateMachine;
        private Rigidbody _rigidbody;

        public Animator Animator { get; private set; }
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            
            _stateMachine = StateMachineFactory.Create<ICharacterState, ICharacterTrigger>(gameObject, new IdleState(this));
        }

        // Public method to trigger events on the state machine
        public bool Trigger(ICharacterTrigger triggerData)
        {
            return _stateMachine.Trigger(triggerData);
        }

        // Input handling
        private void Update()
        {
            // Example input handling
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Trigger(new JumpTrigger());
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                Trigger(new RunTrigger(5.0f));
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                Trigger(new IdleTrigger());
            }
            else if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Trigger(new AttackTrigger(1));
            }
        }

        // Physics and movement methods
        public void SetVelocity(Vector3 velocity)
        {
            _rigidbody.linearVelocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return _rigidbody.linearVelocity;
        }

        public void CheckGrounded()
        {
            IsGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f);
        }

        // Coroutine starter utility
        public new Coroutine StartCoroutine(IEnumerator routine)
        {
            return base.StartCoroutine(routine);
        }
    }
}