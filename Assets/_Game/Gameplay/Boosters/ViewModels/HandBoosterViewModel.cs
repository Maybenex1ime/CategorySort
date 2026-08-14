using BoosterModule;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>Booster Hand — inventory do BoosterManager giữ.</summary>
    public sealed class HandBoosterViewModel : ArmableBoosterViewModelBase
    {
        public HandBoosterViewModel() : base(BoosterId.Hand) { }
    }
}
