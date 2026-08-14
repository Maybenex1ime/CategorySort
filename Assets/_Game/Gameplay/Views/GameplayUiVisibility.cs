using LogosSDK.Core.Events;
using UnityEngine;
using WordStack.Meta.AppFlow;

namespace LogosGame.Features.Gameplay.Views
{
    /// <summary>
    /// Ẩn/hiện UI gameplay theo phase của app.
    ///
    /// Aquapark không cần thứ này: HUD của nó nằm trong GameplayScene, load additive
    /// lúc vào chơi và unload khi về menu — không tồn tại thì khỏi phải ẩn. WordStack
    /// để mọi thứ trong Main.unity nên phải tự tắt, nếu không HUD sẽ đè lên MainMenu.
    ///
    /// Tách khỏi GameplayUiRoot để file đó giữ nguyên bản aquapark, chép qua chép lại
    /// không phải merge tay.
    /// </summary>
    public sealed class GameplayUiVisibility : MonoBehaviour
    {
        [Tooltip("Các nhánh chỉ hiện khi đang chơi. KHÔNG gán object đang giữ script này — " +
                 "tắt nó là mất luôn listener.")]
        [SerializeField] private GameObject[] _gameplayOnlyRoots;

        private void Awake()
        {
            Bus.Global.On<AppFlowPhaseChangedEvent>(OnPhaseChanged);

            // Awake chạy trước khi AppFlow vào phase đầu tiên → mặc định ẩn.
            SetVisible(false);
        }

        private void OnDestroy()
        {
            Bus.Global.Off<AppFlowPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnPhaseChanged(AppFlowPhaseChangedEvent evt)
        {
            // Giữ hiện ở Result: popup kết quả đè lên, vẫn thấy coin/level phía sau.
            SetVisible(evt.Phase == AppFlowPhase.Gameplay || evt.Phase == AppFlowPhase.Result);
        }

        private void SetVisible(bool visible)
        {
            if (_gameplayOnlyRoots == null) return;

            for (int i = 0; i < _gameplayOnlyRoots.Length; i++)
            {
                if (_gameplayOnlyRoots[i] != null)
                    _gameplayOnlyRoots[i].SetActive(visible);
            }
        }
    }
}
