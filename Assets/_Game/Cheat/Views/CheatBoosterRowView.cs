using LogosGame.Features.Cheat.Services;
using BoosterModule;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// One row in the BOOSTERS section: [0] [+1] [NAME]. Bound at runtime by
    /// <see cref="CheatBoostersSectionView"/> when it instantiates the row from
    /// the SO-driven booster list.
    /// </summary>
    public sealed class CheatBoosterRowView : MonoBehaviour
    {
        [SerializeField] private Button _zeroButton;
        [SerializeField] private Button _plusOneButton;
        [SerializeField] private TMP_Text _nameLabel;

        private ICheatService _cheatService;
        private BoosterId _boosterId;
        private bool _wired;

        public void Bind(ICheatService cheatService, BoosterId boosterId, string displayName)
        {
            _cheatService = cheatService;
            _boosterId = boosterId;

            if (_nameLabel != null)
                _nameLabel.text = string.IsNullOrEmpty(displayName) ? boosterId.ToString() : displayName;

            if (_wired) return;

            if (_zeroButton != null) _zeroButton.onClick.AddListener(OnZeroClicked);
            if (_plusOneButton != null) _plusOneButton.onClick.AddListener(OnPlusOneClicked);
            _wired = true;
        }

        private void OnDestroy()
        {
            if (!_wired) return;
            if (_zeroButton != null) _zeroButton.onClick.RemoveListener(OnZeroClicked);
            if (_plusOneButton != null) _plusOneButton.onClick.RemoveListener(OnPlusOneClicked);
        }

        private void OnZeroClicked() => _cheatService?.SetBoosterCount(_boosterId, 0);
        private void OnPlusOneClicked() => _cheatService?.AddBoosterCount(_boosterId, 1);
    }
}
