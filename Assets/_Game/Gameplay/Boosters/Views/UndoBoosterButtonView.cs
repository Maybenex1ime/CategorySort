using LogosGame.Features.Gameplay.Boosters.ViewModels;
using Reflex.Attributes;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>Nút Undo. Toàn bộ trạng thái nằm ở <see cref="BoosterButtonView"/>.</summary>
    public sealed class UndoBoosterButtonView : BoosterButtonView
    {
        [Inject] private readonly UndoBoosterViewModel _viewModel;

        protected override BoosterViewModelBase ViewModel => _viewModel;
    }
}
