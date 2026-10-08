using LogosGame.Features.UI.Popups.Args;
using LitMotion;
using LogosGame.Features.UI.Common;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class NotEnoughGoldPopup : PopupBase<NotEnoughGoldPopupArgs>
    {
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private MotionHandle _closeDrop;

        // Nút X ẩn tới khi popup mở xong rồi mới rơi xuống nảy (UIDrop).
        public override async Awaitable Show()
        {
            Vector3 scale = UIDrop.Hide(_closeButton);
            await base.Show();
            _closeDrop = UIDrop.Play(_closeButton, scale, _closeDrop);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            Dismiss();
            Args?.OnClose?.Invoke();
        }
    }
}
