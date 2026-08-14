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
    /// KHÁC AQUAPARK: chưa có IPurchaseService/catalog giao dịch nên Price = 0
    /// (popup hiện "—") và nút Mua chỉ log stub, KHÔNG trừ coin/cộng booster.
    /// Khi port hệ mua: đổi ExecutePurchase sang IPurchaseService.TryPurchase như
    /// bản aquapark (Currency/UI/Impl/BoosterPurchaseFlow.cs bên đó).
    /// </summary>
    public sealed class BoosterPurchaseFlow : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BoosterPurchaseFlow>();

        private readonly UIManager _uiManager;
        private readonly ICurrencyService _currencyService;
        private readonly IUnlockSchedule _unlockSchedule;

        public BoosterPurchaseFlow(UIManager uiManager, ICurrencyService currencyService,
            IUnlockSchedule unlockSchedule)
        {
            _uiManager = uiManager;
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

            // Giao dịch ngoài booster (vd heart từ NoHeartsPopup): chưa có hệ mua.
            _logger.Warn($"[BoosterPurchaseFlow] Hệ mua chưa có — bỏ qua transaction '{evt.TransactionId}'.");
        }

        private void ShowForBooster(BoosterId boosterId)
        {
            if (_uiManager == null)
            {
                _logger.Warn($"[BoosterPurchaseFlow] UIManager chưa được bind — không mở được popup mua {boosterId}.");
                return;
            }

            string displayName = null;
            string description = null;
            Sprite icon = null;
            _unlockSchedule?.TryGetBoosterInfo(boosterId, out displayName, out description, out icon);

            BoosterPurchasePopupArgs args = new BoosterPurchasePopupArgs
            {
                Icon = icon,
                BoosterName = string.IsNullOrEmpty(displayName) ? boosterId.ToString() : displayName,
                Description = description,
                Price = 0,
                Coins = _currencyService?.Coins,
                OnPurchaseConfirmed = () => _logger.Info(
                    $"[BoosterPurchaseFlow] Mua {boosterId}: hệ mua chưa có — chưa trừ coin, chưa cộng booster."),
                OnClose = null
            };

            ShowPopupInBackground(args);
        }

        private async void ShowPopupInBackground(BoosterPurchasePopupArgs args)
        {
            await _uiManager.ShowPopupImmediate<BoosterPurchasePopup, BoosterPurchasePopupArgs>(args);
        }
    }
}
