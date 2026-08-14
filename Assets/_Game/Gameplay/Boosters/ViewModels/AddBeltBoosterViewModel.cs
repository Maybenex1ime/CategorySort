using BoosterModule;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>Booster AddBelt — inventory do BoosterManager giữ.</summary>
    public sealed class AddBeltBoosterViewModel : InstantBoosterViewModelBase
    {
        public AddBeltBoosterViewModel() : base(BoosterId.AddBelt) { }
    }
}
