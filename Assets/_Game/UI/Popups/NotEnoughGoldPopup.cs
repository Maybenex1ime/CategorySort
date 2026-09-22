using LogosGame.Features.UI.Popups.Args;
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
