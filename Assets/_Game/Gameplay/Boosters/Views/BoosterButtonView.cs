using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.Gameplay.Boosters.ViewModels;
using LogosGame.Features.Gameplay.Content;
using LogosGame.Features.Gameplay.Flow;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using R3;
using Reflex.Attributes;
using Reflex.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WordStack.Meta.AppFlow;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>
    /// Nút booster trên HUD:
    ///   Locked   — chưa tới level mở khoá: đổi nền + icon sang sprite khoá, hiện "Lv.N",
    ///              không bấm được.
    ///   HasStock — còn lượt: hiện số lượt, bấm là dùng booster.
    ///   Buyable  — hết lượt, đủ coin: hiện giá, bấm là trừ coin mua 1 lượt → HasStock
    ///              (chưa dùng, bấm lần nữa mới dùng).
    ///   WatchAd  — hết lượt, thiếu coin: hiện icon reward ad, bấm là xin AppFlow cho xem
    ///              ad để nhận booster.
    /// Riêng khi bàn không dùng được booster: phần nút + icon mờ còn 50% và không bấm
    /// được (nhãn giá / icon ad nằm ngoài phần mờ nên giữ nguyên).
    ///
    /// Mỗi loại booster là một lớp con chỉ để inject đúng ViewModel.
    /// </summary>
    public abstract class BoosterButtonView : MonoBehaviour
    {
        private enum State { Locked, HasStock, Buyable, WatchAd }

        [SerializeField] private Button _button;
        [Tooltip("Để trống thì tự lấy/thêm trên chính GameObject này.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("CanvasGroup của riêng phần nút (nền + icon + số lượt), KHÔNG chứa nhãn giá / icon ad. " +
                 "Để trống thì tự lấy/thêm trên GameObject của Button.")]
        [SerializeField] private CanvasGroup _buttonFadeGroup;

        [Header("Locked")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Sprite _lockedBgSprite;
        [Tooltip("Để trống thì ẩn icon lúc khoá (nền khoá đã in sẵn ổ khoá).")]
        [SerializeField] private Sprite _lockedIconSprite;
        [SerializeField] private TextMeshProUGUI _unlockLevelText;

        [Header("Has Stock")]
        [SerializeField] private GameObject _countRoot;
        [SerializeField] private TextMeshProUGUI _countLabel;

        [Header("Buyable")]
        [SerializeField] private GameObject _priceRoot;
        [SerializeField] private TextMeshProUGUI _priceLabel;

        [Header("Watch Ad")]
        [SerializeField] private GameObject _adRoot;

        [Header("Unusable")]
        [Tooltip("Opacity phần nút khi bàn không dùng được booster.")]
        [FormerlySerializedAs("_unavailableAlpha")]
        [SerializeField, Range(0f, 1f)] private float _unusableAlpha = 0.5f;

        [Inject] private readonly Container _container;
        [Inject] private readonly ICurrencyService _currencyService;
        [Inject] private readonly IGameplayFlowController _flow;

        // Hai service dưới có thể vắng (installer chưa gán asset) nên resolve tay.
        private IUnlockSchedule _unlockSchedule;
        private int _price;
        private bool _hasPrice;

        private Sprite _unlockedBgSprite;
        private Sprite _unlockedIconSprite;
        private int _currentLevel;
        private State _state;
        private DisposableBag _disposables;

        protected abstract BoosterViewModelBase ViewModel { get; }

        private void Start()
        {
            if (ViewModel == null) return;

            if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (_buttonFadeGroup == null && _button != null
                && !_button.TryGetComponent(out _buttonFadeGroup))
                _buttonFadeGroup = _button.gameObject.AddComponent<CanvasGroup>();

            ResolveOptionalServices();
            CacheUnlockedSprites();

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            ViewModel.Count.Subscribe(_ => Refresh()).AddTo(ref _disposables);
            ViewModel.IsUsable.Subscribe(_ => Refresh()).AddTo(ref _disposables);
            _currencyService?.Coins.Subscribe(_ => Refresh()).AddTo(ref _disposables);
            _flow?.LevelTitle.Subscribe(title =>
            {
                _currentLevel = ParseLevelNumber(title);
                Refresh();
            }).AddTo(ref _disposables);

            Refresh();
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnButtonClicked);
            _disposables.Dispose();
        }

        private void ResolveOptionalServices()
        {
            if (_container == null) return;

            if (_container.TryGetResolver<IUnlockSchedule>(out _))
                _unlockSchedule = _container.Resolve<IUnlockSchedule>();

            if (_container.TryGetResolver<IPurchaseService>(out _)
                && _container.Resolve<IPurchaseService>().TryGetTransaction(
                    TransactionIds.ForBooster(ViewModel.BoosterId), out TransactionDefinition entry))
            {
                _price = entry.Price;
                _hasPrice = true;
            }
        }

        // Sprite mở khoá = sprite sẵn trong prefab. Cố ý KHÔNG lấy Icon từ SO_UnlockSchedule
        // (bản cũ trong GameplayUiRoot có làm) — ghi đè lên art HUD trong prefab.
        private void CacheUnlockedSprites()
        {
            if (_backgroundImage != null) _unlockedBgSprite = _backgroundImage.sprite;
            if (_iconImage != null) _unlockedIconSprite = _iconImage.sprite;
        }

        private void OnButtonClicked()
        {
            if (ViewModel == null) return;

            switch (_state)
            {
                case State.HasStock:
                    ViewModel.OnButtonClicked();
                    break;

                // Mua xong BoosterManager bắn InventoryChanged → Count = 1 → Refresh sang HasStock.
                case State.Buyable:
                    Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(ViewModel.BoosterId)));
                    break;

                case State.WatchAd:
                    Bus.Global.Fire(new RewardedBoosterRequestedEvent(ViewModel.BoosterId));
                    break;
            }
        }

        private State Evaluate()
        {
            if (!IsUnlocked()) return State.Locked;
            if (ViewModel.Count.CurrentValue > 0) return State.HasStock;

            int coins = _currencyService != null ? _currencyService.Coins.CurrentValue : 0;
            // Chưa có giá (catalog chưa gán) thì không kết luận được là thiếu coin.
            return !_hasPrice || coins >= _price ? State.Buyable : State.WatchAd;
        }

        private bool IsUnlocked()
        {
            if (_unlockSchedule == null) return true;
            if (_unlockSchedule.CheatGrantBoosters) return true;
            return _unlockSchedule.TryGetBoosterUnlockLevel(ViewModel.BoosterId, out int unlockLevel)
                   && unlockLevel <= _currentLevel;
        }

        private void Refresh()
        {
            if (ViewModel == null) return;

            _state = Evaluate();
            bool locked = _state == State.Locked;

            ApplyLockVisuals(locked);
            SetActive(_countRoot, _state == State.HasStock);
            SetActive(_priceRoot, _state == State.Buyable);
            SetActive(_adRoot, _state == State.WatchAd);

            if (_countLabel != null) _countLabel.text = ViewModel.Count.CurrentValue.ToString();
            if (_priceLabel != null) _priceLabel.text = _hasPrice ? _price.ToString() : "—";

            // CanvasGroup gốc chỉ khoá input lúc Locked (thắng mọi Button.interactable bên dưới).
            if (_canvasGroup != null) _canvasGroup.interactable = !locked;

            bool unusable = !locked && !ViewModel.IsUsable.CurrentValue;
            if (_button != null) _button.interactable = !unusable;
            if (_buttonFadeGroup != null) _buttonFadeGroup.alpha = unusable ? _unusableAlpha : 1f;
        }

        private void ApplyLockVisuals(bool locked)
        {
            if (_backgroundImage != null)
                _backgroundImage.sprite = locked ? _lockedBgSprite : _unlockedBgSprite;

            if (_iconImage != null)
            {
                Sprite shown = locked ? _lockedIconSprite : _unlockedIconSprite;
                _iconImage.sprite = shown;
                SetActive(_iconImage.gameObject, shown != null);
            }

            if (_unlockLevelText != null)
            {
                SetActive(_unlockLevelText.gameObject, locked);
                if (locked && _unlockSchedule != null
                    && _unlockSchedule.TryGetBoosterUnlockLevel(ViewModel.BoosterId, out int unlockLevel))
                    _unlockLevelText.text = "Lv." + unlockLevel;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        // LevelTitle dạng "Level N" — cùng cách GameplayUiRoot đọc trước đây.
        private static int ParseLevelNumber(string levelTitle)
        {
            if (string.IsNullOrEmpty(levelTitle)) return 0;
            string[] parts = levelTitle.Split(' ');
            return parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int n) ? n : 0;
        }
    }
}
