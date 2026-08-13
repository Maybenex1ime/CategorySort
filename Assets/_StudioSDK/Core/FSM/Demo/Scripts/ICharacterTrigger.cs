using LogosSDK.Core.FSM;

namespace LogosSDK.Core.FSM.Demo
{
    // Define Events (Triggers)
    public interface ICharacterTrigger : ITrigger { }

    public class JumpTrigger : ICharacterTrigger { }

    public class LandTrigger : ICharacterTrigger { }

    public class RunTrigger : ICharacterTrigger 
    {
        public float Speed { get; private set; }

        public RunTrigger(float speed)
        {
            Speed = speed;
        }
    }

    public class IdleTrigger : ICharacterTrigger { }

    public class AttackTrigger : ICharacterTrigger 
    {
        public int AttackPower { get; private set; }

        public AttackTrigger(int power)
        {
            AttackPower = power;
        }
    }
}
