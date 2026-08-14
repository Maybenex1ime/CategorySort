using BoosterModule;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>Booster Hammer — inventory do BoosterManager giữ.</summary>
    public sealed class HammerBoosterViewModel : ArmableBoosterViewModelBase
    {
        public HammerBoosterViewModel() : base(BoosterId.Hammer) { }
    }
}
