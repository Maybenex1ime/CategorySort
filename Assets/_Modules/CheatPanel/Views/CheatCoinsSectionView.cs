using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// COINS row: fixed-amount buttons (0 / +increment, amount decided by the
    /// game's cheat service) plus a custom-amount input + Set button.
    /// </summary>
    public sealed class CheatCoinsSectionView : MonoBehaviour
    {
        [Inject] private readonly IEconomyCheatActions _cheats;

        [SerializeField] private Button _zeroButton;
        [SerializeField] private Button _addIncrementButton;
        [SerializeField] private TMP_InputField _customInput;
        [SerializeField] private Button _customSetButton;

        private void Start()
        {
            if (_zeroButton != null) _zeroButton.onClick.AddListener(OnZeroClicked);
            if (_addIncrementButton != null) _addIncrementButton.onClick.AddListener(OnAddIncrementClicked);
            if (_customSetButton != null) _customSetButton.onClick.AddListener(OnCustomSetClicked);

            if (_customInput != null)
            {
                _customInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _customInput.onSubmit.AddListener(OnCustomInputSubmitted);
            }
        }

        private void OnDestroy()
        {
            if (_zeroButton != null) _zeroButton.onClick.RemoveListener(OnZeroClicked);
            if (_addIncrementButton != null) _addIncrementButton.onClick.RemoveListener(OnAddIncrementClicked);
            if (_customSetButton != null) _customSetButton.onClick.RemoveListener(OnCustomSetClicked);
            if (_customInput != null) _customInput.onSubmit.RemoveListener(OnCustomInputSubmitted);
        }

        private void OnZeroClicked() => _cheats?.SetCoins(0);

        private void OnAddIncrementClicked() => _cheats?.AddCoinIncrement();

        private void OnCustomSetClicked() => TrySetFromInput();

        private void OnCustomInputSubmitted(string _) => TrySetFromInput();

        private void TrySetFromInput()
        {
            if (_customInput == null) return;
            if (!int.TryParse(_customInput.text, out int amount)) return;
            if (amount < 0) amount = 0;
            _cheats?.SetCoins(amount);
        }
    }
}
