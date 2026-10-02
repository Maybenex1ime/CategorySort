using LogosGame.Features.Gameplay.Flow;
using R3;
using Reflex.Attributes;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Views
{
    public sealed class GameplayUiRoot : MonoBehaviour
    {
        [Inject] private readonly IGameplayFlowController _viewModel;

        [SerializeField] private GameplayHudView _hudView;
        [SerializeField] private GameplayReadyOverlayView _readyOverlayView;
        [SerializeField] private GameplayBlockInputOverlayView _blockInputOverlayView;

        private readonly CompositeDisposable _subscriptions = new();

        private void Start()
        {
            if (_viewModel == null) return;

            _subscriptions.Add(_viewModel.LevelTitle.Subscribe(ApplyLevelTitle));
            _subscriptions.Add(_viewModel.CanOpenSettings.Subscribe(ApplySettingsEnabled));
            _subscriptions.Add(_viewModel.ShowSettingsButton.Subscribe(ApplySettingsVisible));
            _subscriptions.Add(_viewModel.ShowCoinBox.Subscribe(ApplyCoinBoxVisible));
            _subscriptions.Add(_viewModel.CurrentPhase.Subscribe(ApplyPhase));
            _subscriptions.Add(_viewModel.IsInputBlocked.Subscribe(ApplyInputBlock));
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void ApplyInputBlock(bool isBlocked)
        {
            GameplayPhase phase = GameplayPhase.None;
            if (_viewModel != null)
                phase = _viewModel.CurrentPhase.CurrentValue;

            bool shouldShowBlocker = isBlocked && phase != GameplayPhase.Ready && phase != GameplayPhase.Paused;
            if (_blockInputOverlayView != null)
                _blockInputOverlayView.SetVisible(shouldShowBlocker);
        }

        private void ApplyPhase(GameplayPhase phase)
        {
            if (_readyOverlayView != null)
                _readyOverlayView.SetVisible(phase == GameplayPhase.Ready);
        }

        private void ApplyLevelTitle(string value)
        {
            if (_hudView != null)
                _hudView.SetLevelTitle(value);
        }

        private void ApplySettingsEnabled(bool value)
        {
            if (_hudView != null)
                _hudView.SetSettingsEnabled(value);
        }

        private void ApplySettingsVisible(bool value)
        {
            if (_hudView != null)
                _hudView.SetSettingsVisible(value);
        }

        private void ApplyCoinBoxVisible(bool value)
        {
            if (_hudView != null)
                _hudView.SetCoinBoxVisible(value);
        }
    }
}
