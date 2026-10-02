using LogosGame.Features.Gameplay.Boosters.ViewModels;
using Reflex.Attributes;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>Nút Shuffle. Toàn bộ trạng thái nằm ở <see cref="BoosterButtonView"/>.</summary>
    public sealed class ShuffleBoosterButtonView : BoosterButtonView
    {
        [Inject] private readonly ShuffleBoosterViewModel _viewModel;

        protected override BoosterViewModelBase ViewModel => _viewModel;
    }
}
