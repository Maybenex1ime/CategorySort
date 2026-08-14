using BoosterModule;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>Booster AddQueue — inventory do BoosterManager giữ.</summary>
    public sealed class AddQueueBoosterViewModel : InstantBoosterViewModelBase
    {
        public AddQueueBoosterViewModel() : base(BoosterId.AddQueue) { }
    }
}
