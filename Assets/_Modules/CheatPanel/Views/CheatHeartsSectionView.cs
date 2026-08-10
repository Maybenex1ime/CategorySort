using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// HEARTS row: two buttons — "0" sets the count to zero and restarts the
    /// regen timer; "Refill full heart" sets to MAX.
    /// </summary>
    public sealed class CheatHeartsSectionView : MonoBehaviour
    {
        [Inject] private readonly IEconomyCheatActions _cheats;

        [SerializeField] private Button _zeroButton;
        [SerializeField] private Button _refillFullButton;

        private void Start()
        {
            if (_zeroButton != null)
                _zeroButton.onClick.AddListener(OnZeroClicked);
            if (_refillFullButton != null)
                _refillFullButton.onClick.AddListener(OnRefillClicked);
        }

        private void OnDestroy()
        {
            if (_zeroButton != null)
                _zeroButton.onClick.RemoveListener(OnZeroClicked);
            if (_refillFullButton != null)
                _refillFullButton.onClick.RemoveListener(OnRefillClicked);
        }

        private void OnZeroClicked() => _cheats?.SetHearts(0);
        private void OnRefillClicked() => _cheats?.RefillHeartsToMax();
    }
}
