using System;
using BoosterModule;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.UI.Popups;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using LogosSDK.UI.Core;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Currency.UI.Impl
{
    /// <summary>
    /// Nghe PurchaseRequestedEvent (nút booster bắn khi hết lượt mà đủ coin) qua
    /// Bus.Global và MUA THẲNG bằng coin — không còn popup xác nhận. Mua xong
    /// BoosterManager cộng lượt, nút tự sang trạng thái còn lượt.
    ///
    /// Thiếu coin thì mở NotEnoughGoldPopup — nút booster tự chuyển sang rewarded ad
    /// khi thiếu coin nên đường này chỉ còn là lưới an toàn (và cho mua tim).
    ///
    /// IPurchaseService có thể VẮNG (CurrencyInstaller chưa được gán
    /// SO_TransactionCatalog): chỉ log, không trừ coin/cộng booster, không sập.
    /// </summary>
    public sealed class BoosterPurchaseFlow : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BoosterPurchaseFlow>();

        private readonly UIManager _uiManager;
        private readonly IPurchaseService _purchaseService;

        public BoosterPurchaseFlow(UIManager uiManager, IPurchaseService purchaseService)
        {
            _uiManager = uiManager;
            _purchaseService = purchaseService;
            Bus.Global.On<PurchaseRequestedEvent>(OnPurchaseRequested);
            Bus.Global.On<BoosterExhaustedEvent>(OnBoosterExhausted);
        }

        public void Dispose()
        {
            Bus.Global.Off<PurchaseRequestedEvent>(OnPurchaseRequested);
            Bus.Global.Off<BoosterExhaustedEvent>(OnBoosterExhausted);
        }

        // Nút BoosterSlotView (module) bấm lúc hết lượt đi đường này; nút
        // *BoosterButtonView (kiểu aquapark) bắn PurchaseRequestedEvent trực tiếp.
        private void OnBoosterExhausted(BoosterExhaustedEvent evt)
        {
            if (evt.Id == BoosterId.None) return;
            ExecutePurchase(TransactionIds.ForBooster(evt.Id));
        }

        private void OnPurchaseRequested(PurchaseRequestedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TransactionId)) return;
            ExecutePurchase(evt.TransactionId);
        }

        private void ExecutePurchase(string transactionId)
        {
            if (_purchaseService == null)
            {
                _logger.Warn($"[BoosterPurchaseFlow] Hệ mua chưa bind (catalog chưa gán) — bỏ qua '{transactionId}'.");
                return;
            }

            PurchaseResult result = _purchaseService.TryPurchase(transactionId);
            if (result.IsSuccess) return;

            if (result.Code == PurchaseResultCode.NotEnoughCurrency)
            {
                ShowNotEnoughGold();
                return;
            }

            _logger.Warn($"[BoosterPurchaseFlow] Mua '{transactionId}' thất bại: {result.Code}.");
        }

        private void ShowNotEnoughGold()
        {
            if (_uiManager == null)
            {
                _logger.Warn("[BoosterPurchaseFlow] UIManager chưa được bind — không mở được NotEnoughGoldPopup.");
                return;
            }

            // Nút booster chỉ sống trong HUD gameplay → popup luôn đè lên bàn đang chơi.
            // Block trước khi mở, unblock khi popup đóng.
            WordStack.Contracts.LevelCommands.SetInputBlocked(true);

            NotEnoughGoldPopupArgs args = new NotEnoughGoldPopupArgs
            {
                OnClose = () => WordStack.Contracts.LevelCommands.SetInputBlocked(false),
            };

            ShowPopupInBackground(args);
        }

        private async void ShowPopupInBackground(NotEnoughGoldPopupArgs args)
        {
            try
            {
                await _uiManager.ShowPopupImmediate<NotEnoughGoldPopup, NotEnoughGoldPopupArgs>(args);
            }
            catch (Exception e)
            {
                // Mở fail (vd prefab chưa đăng ký address) mà không unblock là bàn
                // khoá vĩnh viễn — trả input rồi mới báo lỗi.
                WordStack.Contracts.LevelCommands.SetInputBlocked(false);
                _logger.Error($"[BoosterPurchaseFlow] Không mở được NotEnoughGoldPopup: {e.Message}");
            }
        }
    }
}
