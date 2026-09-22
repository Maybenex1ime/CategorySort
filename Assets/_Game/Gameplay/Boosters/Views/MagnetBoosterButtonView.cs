using LogosGame.Features.Gameplay.Boosters.ViewModels;
using Reflex.Attributes;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>Nút Nam châm. Toàn bộ trạng thái nằm ở <see cref="BoosterButtonView"/>.</summary>
    public sealed class MagnetBoosterButtonView : BoosterButtonView
    {
        [Inject] private readonly MagnetBoosterViewModel _viewModel;

        protected override BoosterViewModelBase ViewModel => _viewModel;
    }
}
