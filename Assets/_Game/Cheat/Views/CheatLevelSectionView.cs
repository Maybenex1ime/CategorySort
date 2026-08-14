using LogosGame.Features.Cheat.Services;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// LEVEL row: input + Set button. Works from both lobby (starts gameplay at
    /// that level) and gameplay (in-memory jump).
    /// </summary>
    public sealed class CheatLevelSectionView : MonoBehaviour
    {
        [Inject] private readonly ICheatService _cheatService;

        [SerializeField] private TMP_InputField _levelInput;
        [SerializeField] private Button _setButton;

        private void Start()
        {
            if (_setButton != null)
                _setButton.onClick.AddListener(OnSetClicked);

            if (_levelInput != null)
            {
                _levelInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _levelInput.onSubmit.AddListener(OnInputSubmitted);
            }
        }

        private void OnDestroy()
        {
            if (_setButton != null)
                _setButton.onClick.RemoveListener(OnSetClicked);
            if (_levelInput != null)
                _levelInput.onSubmit.RemoveListener(OnInputSubmitted);
        }

        private void OnSetClicked() => TryJumpFromInput();

        private void OnInputSubmitted(string _) => TryJumpFromInput();

        private void TryJumpFromInput()
        {
            if (_levelInput == null) return;
            if (!int.TryParse(_levelInput.text, out int oneBased)) return;
            _cheatService?.JumpToLevel(oneBased);
        }
    }
}
