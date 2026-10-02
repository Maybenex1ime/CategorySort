using System;
using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    /// <summary>
    /// IAP giả: luôn "mua thành công", KHÔNG gọi store nào, KHÔNG có giao dịch thật.
    /// Đủ để chạy trọn luồng shop trong editor và build nội bộ — đi đúng đường
    /// IIapFulfillment như store thật, chỉ khác là mã giao dịch tự sinh.
    ///
    /// Build phát hành phải bật _useRealStore trên ShopInstaller — để stub là phát coin
    /// miễn phí cho mọi người.
    /// </summary>
    public sealed class StubIAPService : IIAPService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<StubIAPService>();

        // Chỉ sống trong phiên: stub không có receipt nên restart là mất. Non-consumable
        // (No-Ads) muốn bền phải đợi store thật.
        private readonly HashSet<string> _owned = new HashSet<string>();
        private IIapFulfillment _fulfillment;

        public bool IsReady { get; private set; }

        public Awaitable<bool> Initialize(IReadOnlyList<IapProduct> products, IIapFulfillment fulfillment)
        {
            _fulfillment = fulfillment;
            IsReady = true;
            return Completed(true);
        }

        public Awaitable<bool> Purchase(string productId)
        {
            if (string.IsNullOrEmpty(productId) || !IsReady || _fulfillment == null)
                return Completed(false);

            _logger.Warn($"[StubIAPService] GIẢ LẬP mua '{productId}' — không có giao dịch thật.");
            bool fulfilled = _fulfillment.Fulfill(productId, "stub-" + Guid.NewGuid().ToString("N"));
            if (fulfilled) _owned.Add(productId);
            return Completed(fulfilled);
        }

        public Awaitable RestorePurchases()
        {
            _logger.Warn("[StubIAPService] RestorePurchases không làm gì — chưa có store.");
            AwaitableCompletionSource source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        public bool IsOwned(string productId) =>
            !string.IsNullOrEmpty(productId) && _owned.Contains(productId);

        // Stub không có giá — UI rơi về PriceLabelFallback của catalog.
        public string GetLocalizedPrice(string productId) => null;

        private static Awaitable<bool> Completed(bool value)
        {
            AwaitableCompletionSource<bool> source = new AwaitableCompletionSource<bool>();
            source.SetResult(value);
            return source.Awaitable;
        }
    }
}
