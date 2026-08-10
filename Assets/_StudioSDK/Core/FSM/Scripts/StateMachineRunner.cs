using UnityEngine;

namespace LogosSDK.Core.FSM
{
    public class StateMachineRunner : MonoBehaviour
    {
        private IStateMachineCore _stateMachineCore;

        public void Initialize(IStateMachineCore stateMachineCore)
        {
            _stateMachineCore = stateMachineCore;
        }

        private void Update()
        {
            _stateMachineCore?.OnUpdate();
        }

        private void FixedUpdate()
        {
            _stateMachineCore?.OnFixedUpdate();
        }
    }
}