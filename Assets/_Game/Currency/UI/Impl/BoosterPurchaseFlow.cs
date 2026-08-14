using System;
using BoosterModule;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.Gameplay.Content;
using LogosGame.Features.UI.Popups;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using LogosSDK.UI.Core;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Currency.UI.Impl
{
    /// <summary>
    /// Nghe PurchaseRequestedEvent (các nút booster bắn khi count = 0) qua Bus.Global
    /// và mở BoosterPurchasePopup — caller ở scope nào cũng gọi được, không cần
    /// inject UIManager.
    ///
    /// IPurchaseService có thể VẮNG (CurrencyInstaller chưa được gán
    /// SO_TransactionCatalog): popup vẫn mở với giá "—", nút Mua chỉ log stub —
    /// không trừ coin/cộng booster, không sập.
    /// </summary>
    public sealed class BoosterPurchaseFlow : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BoosterPurchaseFlow>();

        private readonly UIManager _uiManager;
        private readonly IPurchaseService _purchaseService;
        private readonly ICurrencyService _currencyService;
        private readonly IUnlockSchedule _unlockSchedule;

        public BoosterPurchaseFlow(UIManager uiManager, IPurchaseService purchaseService,
            ICurrencyService currencyService, IUnlockSchedule unlockSchedule)
        {
            _uiManager = uiManager;
            _purchaseService = purchaseService;
            _currencyService = currencyService;
            _unlockSchedule = unlockSchedule;
            Bus.Global.On<PurchaseRequestedEvent>(OnPurchaseRequested);
        }

        public void Dispose()
        {
            Bus.Global.Off<PurchaseRequestedEvent>(OnPurchaseRequested);
        }

        private void OnPurchaseRequested(PurchaseRequestedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TransactionId)) return;

            if (TransactionIds.TryGetBoosterId(evt.TransactionId, out BoosterId boosterId))
            {
                ShowForBooster(boosterId);
                return;
            }

            // Giao dịch ngoài booster (vd heart từ NoHeartsPopup): mua thẳng,
            // không qua popup — như aquapark.
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
            if (!result.IsSuccess)
                _logger.Warn($"[BoosterPurchaseFlow] Mua '{transactionId}' thất bại: {result.Code}.");
        }

        private void ShowForBooster(BoosterId boosterId)
        {
            if (_uiManager == null)
            {
                _logger.Warn($"[BoosterPurchaseFlow] UIManager chưa được bind — không mở được popup mua {boosterId}.");
                return;
            }

            string transactionId = TransactionIds.ForBooster(boosterId);
            bool hasEntry = false;
            TransactionDefinition transaction = default;
            if (_purchaseService != null)
            {
                hasEntry = _purchaseService.TryGetTransaction(transactionId, out transaction);
                if (!hasEntry)
                    _logger.Warn($"[BoosterPurchaseFlow] Catalog không có entry '{transactionId}' — popup mở với giá '—'.");
            }

            string displayName = null;
            string description = null;
            Sprite icon = null;
            _unlockSchedule?.TryGetBoosterInfo(boosterId, out displayName, out description, out icon);
            if (string.IsNullOrEmpty(displayName) && hasEntry) displayName = transaction.Name;
            if (string.IsNullOrEmpty(description) && hasEntry) description = transaction.Description;

            // Nút booster chỉ sống trong HUD gameplay → popup luôn đè lên bàn đang chơi.
            // Gate như PausePopup (AppFlowContext): block trước khi mở, unblock ở MỌI
            // đường thoát — popup gọi đúng một trong hai callback dưới khi Dismiss.
            WordStack.Contracts.LevelCommands.SetInputBlocked(true);

            BoosterPurchasePopupArgs args = new BoosterPurchasePopupArgs
            {
                Icon = icon,
                BoosterName = string.IsNullOrEmpty(displayName) ? boosterId.ToString() : displayName,
                Description = description,
                Price = hasEntry ? transaction.Price : 0,
                Coins = _currencyService?.Coins,
                OnPurchaseConfirmed = () =>
                {
                    WordStack.Contracts.LevelCommands.SetInputBlocked(false);
                    ExecutePurchase(transactionId);
                },
                OnClose = () => WordStack.Contracts.LevelCommands.SetInputBlocked(false)
            };

            ShowPopupInBackground(args);
        }

        private async void ShowPopupInBackground(BoosterPurchasePopupArgs args)
        {
            try
            {
                await _uiManager.ShowPopupImmediate<BoosterPurchasePopup, BoosterPurchasePopupArgs>(args);
            }
            catch (Exception e)
            {
                // Mở fail (vd prefab chưa đăng ký address) mà không unblock là bàn
                // khoá vĩnh viễn — trả input rồi mới báo lỗi.
                WordStack.Contracts.LevelCommands.SetInputBlocked(false);
                _logger.Error($"[BoosterPurchaseFlow] Không mở được BoosterPurchasePopup: {e.Message}");
            }
        }
    }
}
