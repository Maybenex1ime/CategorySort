using LogosGame.Features.Gameplay.Flow;
using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class RevivePopup : PopupBase<RevivePopupArgs>
    {
        [Header("Content theo lý do thua")]
        [Tooltip("Bật khi thua vì HẾT NƯỚC (vd \"Out of moves! +5 moves\").")]
        [SerializeField] private GameObject _outOfMovesContent;
        [Tooltip("Số nước cộng thêm, hiện dạng \"+5\".")]
        [SerializeField] private TextMeshProUGUI _extraMovesText;
        [Tooltip("Bật khi thua vì KẸT (vd \"No more moves! Use a Magnet\").")]
        [SerializeField] private GameObject _stuckContent;

        [Header("Buttons")]
        [SerializeField] private Button _coinButton;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _adButton;
        [Tooltip("Nút bỏ cuộc (X / No thanks) → sang FailedPopup.")]
        [SerializeField] private Button _giveUpButton;

        [Header("Peek board")]
        [Tooltip("Vùng NGOÀI Revive Box (vd nền tối full màn). Giữ vào đây → ẩn popup để xem bàn, thả ra → hiện lại.")]
        [SerializeField] private PressHoldArea _peekArea;
        [Tooltip("CanvasGroup bọc mọi thứ cần ẩn khi xem bàn (nền tối + Revive Box). Dùng CanvasGroup " +
                 "ở root được: animation mở/đóng chỉ chạy lúc Show/Hide, còn peek chỉ xảy ra khi popup đã hiện.")]
        [SerializeField] private CanvasGroup _peekHideGroup;

        private DisposableBag _disposables;

        protected override void Awake()
        {
            base.Awake();
            if (_coinButton != null) _coinButton.onClick.AddListener(OnCoinClicked);
            if (_adButton != null) _adButton.onClick.AddListener(OnAdClicked);
            if (_giveUpButton != null) _giveUpButton.onClick.AddListener(OnGiveUpClicked);
            if (_peekArea != null)
            {
                _peekArea.HoldStarted += OnPeekStarted;
                _peekArea.HoldEnded += OnPeekEnded;
            }
        }

        private void OnDestroy()
        {
            if (_coinButton != null) _coinButton.onClick.RemoveListener(OnCoinClicked);
            if (_adButton != null) _adButton.onClick.RemoveListener(OnAdClicked);
            if (_giveUpButton != null) _giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
            if (_peekArea != null)
            {
                _peekArea.HoldStarted -= OnPeekStarted;
                _peekArea.HoldEnded -= OnPeekEnded;
            }
            _disposables.Dispose();
        }

        protected override void Initialize(RevivePopupArgs args)
        {
            // Popup được cache và mở lại mỗi lần thua — bỏ subscription cũ.
            _disposables.Dispose();
            _disposables = new DisposableBag();
            SetPeeking(false);   // mở lại sau lần trước bị đóng giữa lúc đang giữ

            if (args == null) return;

            bool outOfMoves = args.Reason == LoseReason.OutOfMoves;
            if (_outOfMovesContent != null) _outOfMovesContent.SetActive(outOfMoves);
            if (_stuckContent != null) _stuckContent.SetActive(!outOfMoves);
            if (_extraMovesText != null) _extraMovesText.text = "+" + args.ExtraMoves;

            if (_priceText != null) _priceText.text = args.Price.ToString();

            if (_coinButton != null)
            {
                if (args.Coins != null)
                    args.Coins
                        .Subscribe(coins => _coinButton.interactable = coins >= args.Price)
                        .AddTo(ref _disposables);
                else
                    _coinButton.interactable = false;
            }
        }

        private void OnPeekStarted() => SetPeeking(true);

        private void OnPeekEnded() => SetPeeking(false);

        // Chỉ đổi alpha: GameObject phải còn bật để vùng giữ vẫn nhận PointerUp, và
        // popup vẫn chặn raycast nên người chơi chỉ NHÌN bàn, không chạm được.
        private void SetPeeking(bool peeking)
        {
            if (_peekHideGroup != null) _peekHideGroup.alpha = peeking ? 0f : 1f;
        }

        private void OnCoinClicked()
        {
            if (Args?.OnReviveWithCoins != null && Args.OnReviveWithCoins()) Dismiss();
        }

        private void OnAdClicked()
        {
            if (Args?.OnReviveWithAd != null && Args.OnReviveWithAd()) Dismiss();
        }

        private void OnGiveUpClicked()
        {
            Dismiss();
            Args?.OnGiveUp?.Invoke();
        }
    }
}
