using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    /// <summary>
    /// IAP giả: luôn "mua thành công", KHÔNG gọi store nào, KHÔNG có giao dịch thật.
    /// Đủ để chạy trọn luồng shop trong editor và build nội bộ.
    ///
    /// Trước khi phát hành phải thay bằng impl Unity IAP thật (đổi 1 dòng trong
    /// ShopInstaller) — để nguyên là phát coin miễn phí cho mọi người.
    /// </summary>
    public sealed class StubIAPService : IIAPService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<StubIAPService>();

        // Chỉ sống trong phiên: stub không có receipt nên restart là mất. Non-consumable
        // (No-Ads) muốn bền phải đợi store thật.
        private readonly HashSet<string> _owned = new HashSet<string>();

        public Awaitable<bool> Purchase(string productId)
        {
            AwaitableCompletionSource<bool> source = new AwaitableCompletionSource<bool>();

            if (string.IsNullOrEmpty(productId))
            {
                source.SetResult(false);
                return source.Awaitable;
            }

            _owned.Add(productId);
            _logger.Warn($"[StubIAPService] GIẢ LẬP mua '{productId}' — không có giao dịch thật.");
            source.SetResult(true);
            return source.Awaitable;
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
    }
}
