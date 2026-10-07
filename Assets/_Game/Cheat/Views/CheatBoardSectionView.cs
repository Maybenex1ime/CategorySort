using UnityEngine;
using UnityEngine.UI;
using WordStack.Contracts;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// BOARD section: bật/tắt tô màu thẻ cùng nhóm trong hộp (cặp / bộ ba / đủ bộ).
    /// Không nhớ qua lần mở game — cheat, mỗi phiên bắt đầu ở mặc định (bật).
    /// </summary>
    public sealed class CheatBoardSectionView : MonoBehaviour
    {
        [Tooltip("Bật: thẻ cùng nhóm trong hộp đổi màu nền. Tắt: mọi thẻ nền trơn.")]
        [SerializeField] private Toggle _matchColorsToggle;

        private void Start()
        {
            if (_matchColorsToggle == null) return;
            _matchColorsToggle.SetIsOnWithoutNotify(LevelCommands.MatchColors);
            _matchColorsToggle.onValueChanged.AddListener(LevelCommands.SetMatchColors);
        }

        private void OnDestroy()
        {
            if (_matchColorsToggle != null)
                _matchColorsToggle.onValueChanged.RemoveListener(LevelCommands.SetMatchColors);
        }
    }
}
