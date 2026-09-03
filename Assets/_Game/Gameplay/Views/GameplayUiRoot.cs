using System;
using BoosterModule;
using LogosGame.Features.Gameplay.Content;
using LogosGame.Features.Gameplay.Flow;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Gameplay.Views
{
    public sealed class GameplayUiRoot : MonoBehaviour
    {
        [Serializable]
        private struct BoosterSlot
        {
            public BoosterId BoosterId;
            public Image BackgroundImage;
            public Image IconImage;
            public GameObject CountObject;
            public TextMeshProUGUI LevelText;
            public Button Button;
        }

        [Inject] private readonly IGameplayFlowController _viewModel;

        [SerializeField] private GameplayHudView _hudView;
        [SerializeField] private GameplayReadyOverlayView _readyOverlayView;
        [SerializeField] private GameplayBlockInputOverlayView _blockInputOverlayView;

        [Header("Booster Slots")]
        [SerializeField] private SO_UnlockSchedule _unlockSchedule;
        [SerializeField] private Sprite _lockedBgSprite;
        [SerializeField] private Sprite _lockedIconSprite;
        [SerializeField] private BoosterSlot[] _boosterSlots;

        private Sprite[] _originalBgSprites;
        private Sprite[] _originalIconSprites;
        private CanvasGroup[] _slotGroups;
        private readonly CompositeDisposable _subscriptions = new();

        private void Start()
        {
            CacheBgSprites();

            if (_viewModel == null) return;

            _subscriptions.Add(_viewModel.LevelTitle.Subscribe(ApplyLevelTitle));
            _subscriptions.Add(_viewModel.CanOpenSettings.Subscribe(ApplySettingsEnabled));
            _subscriptions.Add(_viewModel.ShowSettingsButton.Subscribe(ApplySettingsVisible));
            _subscriptions.Add(_viewModel.ShowCoinBox.Subscribe(ApplyCoinBoxVisible));
            _subscriptions.Add(_viewModel.CurrentPhase.Subscribe(ApplyPhase));
            _subscriptions.Add(_viewModel.IsInputBlocked.Subscribe(ApplyInputBlock));
            _subscriptions.Add(_viewModel.LevelTitle.Subscribe(OnLevelTitleChanged));
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void CacheBgSprites()
        {
            _originalBgSprites = new Sprite[_boosterSlots.Length];
            _originalIconSprites = new Sprite[_boosterSlots.Length];
            _slotGroups = new CanvasGroup[_boosterSlots.Length];
            for (int i = 0; i < _boosterSlots.Length; i++)
            {
                if (_boosterSlots[i].BackgroundImage != null)
                    _originalBgSprites[i] = _boosterSlots[i].BackgroundImage.sprite;
                if (_boosterSlots[i].IconImage != null)
                    _originalIconSprites[i] = _boosterSlots[i].IconImage.sprite;

                // Khoá qua CanvasGroup chứ không ghi Button.interactable: các
                // *BoosterButtonView cũng ghi field đó (xám khi bàn không dùng được)
                // và sẽ bật lại nút đang khoá mỗi lần count đổi. CanvasGroup
                // interactable = false thắng mọi Button.interactable bên dưới nó.
                Button button = _boosterSlots[i].Button;
                if (button != null)
                {
                    CanvasGroup group = button.GetComponent<CanvasGroup>();
                    if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                    _slotGroups[i] = group;
                }
            }
        }

        private void OnLevelTitleChanged(string levelTitle)
        {
            int currentLevel = ParseLevelNumber(levelTitle);
            RefreshBoosterSlots(currentLevel);
        }

        private void RefreshBoosterSlots(int currentLevel)
        {
            if (_unlockSchedule == null) return;

            for (int i = 0; i < _boosterSlots.Length; i++)
            {
                BoosterSlot slot = _boosterSlots[i];
                bool unlocked = _unlockSchedule.TryGetBoosterIcon(slot.BoosterId, currentLevel, out Sprite icon);

                if (slot.BackgroundImage != null)
                    slot.BackgroundImage.sprite = unlocked ? _originalBgSprites[i] : _lockedBgSprite;

                if (slot.IconImage != null)
                {
                    // Lịch không có icon riêng thì giữ icon sẵn trong prefab — gán null
                    // là ra ô trắng. Khoá mà không có sprite khoá thì ẩn hẳn icon, vì
                    // nền Booster Lock đã in sẵn ổ khoá.
                    Sprite shown = unlocked
                        ? (icon != null ? icon : _originalIconSprites[i])
                        : _lockedIconSprite;
                    slot.IconImage.sprite = shown;
                    slot.IconImage.gameObject.SetActive(shown != null);
                }

                if (slot.CountObject != null)
                    slot.CountObject.SetActive(unlocked);

                if (slot.LevelText != null)
                {
                    slot.LevelText.gameObject.SetActive(!unlocked);
                    if (!unlocked && _unlockSchedule.TryGetBoosterUnlockLevel(slot.BoosterId, out int unlockLevel))
                        slot.LevelText.text = "Lv." + unlockLevel;
                }

                if (_slotGroups[i] != null)
                    _slotGroups[i].interactable = unlocked;
            }
        }

        private static int ParseLevelNumber(string levelTitle)
        {
            if (string.IsNullOrEmpty(levelTitle)) return 0;
            string[] parts = levelTitle.Split(' ');
            return parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int n) ? n : 0;
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
