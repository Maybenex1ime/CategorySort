using LogosGame.Features.Cheat.Services;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// WIN/LOSE row: ép màn hiện tại kết thúc để test luồng Result mà không phải
    /// chơi thật. Chỉ có tác dụng trong Gameplay — bấm ở lobby thì AppFlow bỏ qua
    /// (có warn trong Console).
    /// </summary>
    public sealed class CheatOutcomeSectionView : MonoBehaviour
    {
        [Inject] private readonly ICheatService _cheatService;

        [SerializeField] private Button _winButton;
        [SerializeField] private Button _loseButton;
        [Tooltip("Ép thua KẸT → RevivePopup kiểu nam châm.")]
        [SerializeField] private Button _loseReviveButton;
        [Tooltip("Ép thua HẾT NƯỚC → RevivePopup kiểu +nước.")]
        [SerializeField] private Button _loseOutOfMovesReviveButton;

        private void Start()
        {
            if (_winButton != null)
                _winButton.onClick.AddListener(OnWinClicked);

            if (_loseButton != null)
                _loseButton.onClick.AddListener(OnLoseClicked);

            if (_loseReviveButton != null)
                _loseReviveButton.onClick.AddListener(OnLoseReviveClicked);

            if (_loseOutOfMovesReviveButton != null)
                _loseOutOfMovesReviveButton.onClick.AddListener(OnLoseOutOfMovesReviveClicked);
        }

        private void OnDestroy()
        {
            if (_winButton != null)
                _winButton.onClick.RemoveListener(OnWinClicked);

            if (_loseButton != null)
                _loseButton.onClick.RemoveListener(OnLoseClicked);

            if (_loseReviveButton != null)
                _loseReviveButton.onClick.RemoveListener(OnLoseReviveClicked);

            if (_loseOutOfMovesReviveButton != null)
                _loseOutOfMovesReviveButton.onClick.RemoveListener(OnLoseOutOfMovesReviveClicked);
        }

        private void OnWinClicked() => _cheatService?.ForceWin();

        private void OnLoseClicked() => _cheatService?.ForceLose();

        private void OnLoseReviveClicked() => _cheatService?.ForceLoseStuckRevive();

        private void OnLoseOutOfMovesReviveClicked() => _cheatService?.ForceLoseOutOfMovesRevive();
    }
}
