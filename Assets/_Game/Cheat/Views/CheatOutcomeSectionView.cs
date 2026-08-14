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

        private void Start()
        {
            if (_winButton != null)
                _winButton.onClick.AddListener(OnWinClicked);

            if (_loseButton != null)
                _loseButton.onClick.AddListener(OnLoseClicked);
        }

        private void OnDestroy()
        {
            if (_winButton != null)
                _winButton.onClick.RemoveListener(OnWinClicked);

            if (_loseButton != null)
                _loseButton.onClick.RemoveListener(OnLoseClicked);
        }

        private void OnWinClicked() => _cheatService?.ForceWin();

        private void OnLoseClicked() => _cheatService?.ForceLose();
    }
}
