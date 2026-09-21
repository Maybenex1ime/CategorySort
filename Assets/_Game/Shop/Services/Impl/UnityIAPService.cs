// File DUY NHẤT trong project được biết Unity IAP là gì. Mọi thứ khác nói chuyện qua IIAPService.
// Chỉ biên dịch khi package com.unity.purchasing đã cài — define do versionDefines của
// WordStack.Meta.asmdef bật; chưa cài thì file này rỗng và ShopInstaller rơi về StubIAPService.
#if CATEGORYSORT_UNITY_IAP
using System;
using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    /// <summary>
    /// Điểm móc xác thực hoá đơn. Pha 1 chưa có khoá Google Play nên mặc định chấp nhận hết;
    /// pha 2 thay bằng bản dùng CrossPlatformValidator (spec 2026-09-21, quyết định 5).
    /// </summary>
    public interface IReceiptValidator
    {
        bool IsValid(string receipt);
    }

    public sealed class AcceptAllReceiptValidator : IReceiptValidator
    {
        public bool IsValid(string receipt) => true;
    }

    /// <summary>
    /// Nguyên tắc: ProcessPurchase LUÔN trả Pending, chỉ ConfirmPendingPurchase sau khi
    /// IIapFulfillment báo đã trao + đã lưu. App chết ở bất kỳ đâu trước Confirm thì store gửi
    /// lại giao dịch ở lần khởi tạo sau — kể cả khi không có Purchase nào đang chờ.
    /// </summary>
    public sealed class UnityIAPService : IIAPService, IDetailedStoreListener
    {
        private static readonly ILogger _logger = LogManager.GetLogger<UnityIAPService>();

        private readonly IReceiptValidator _validator;

        private IStoreController _controller;
        private IExtensionProvider _extensions;
        private IIapFulfillment _fulfillment;

        private AwaitableCompletionSource<bool> _initSource;
        private AwaitableCompletionSource<bool> _purchaseSource;
        private string _purchasingProductId;

        public UnityIAPService() : this(new AcceptAllReceiptValidator())
        {
            _logger.Warn("[UnityIAPService] Chưa bật xác thực hoá đơn (pha 2) — mọi receipt đều được chấp nhận.");
        }

        public UnityIAPService(IReceiptValidator validator)
        {
            _validator = validator ?? new AcceptAllReceiptValidator();
        }

        public bool IsReady => _controller != null && _fulfillment != null;

        // ------------------------------------------------------------------ init

        public Awaitable<bool> Initialize(IReadOnlyList<IapProduct> products, IIapFulfillment fulfillment)
        {
            if (_initSource != null) return _initSource.Awaitable;   // gọi lại: trả kết quả lần đầu

            _initSource = new AwaitableCompletionSource<bool>();
            _fulfillment = fulfillment;

            if (fulfillment == null || products == null || products.Count == 0)
            {
                _logger.Warn("[UnityIAPService] Không có sản phẩm hoặc thiếu bên trao thưởng — bỏ khởi tạo store.");
                _initSource.SetResult(false);
                return _initSource.Awaitable;
            }

            InitializeInBackground(products);
            return _initSource.Awaitable;
        }

        private async void InitializeInBackground(IReadOnlyList<IapProduct> products)
        {
            try
            {
                // Unity IAP 4.x cảnh báo nếu Gaming Services chưa khởi tạo. Thất bại ở đây
                // (project chưa link UGS, offline) KHÔNG chặn store.
                try
                {
                    await UnityServices.InitializeAsync();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[UnityIAPService] UnityServices.InitializeAsync lỗi, vẫn khởi tạo store: {ex.Message}");
                }

                ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                for (int i = 0; i < products.Count; i++)
                {
                    builder.AddProduct(products[i].Id,
                        products[i].Kind == IapProductKind.NonConsumable
                            ? ProductType.NonConsumable
                            : ProductType.Consumable);
                }

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UnityIAPService] Lỗi khi khởi tạo store.");
                _initSource.TrySetResult(false);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _logger.Info($"[UnityIAPService] Store sẵn sàng, {controller.products.all.Length} sản phẩm.");
            _initSource.TrySetResult(true);
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _logger.Warn($"[UnityIAPService] Khởi tạo store thất bại: {error} {message}");
            _initSource.TrySetResult(false);
        }

        // -------------------------------------------------------------- purchase

        public Awaitable<bool> Purchase(string productId)
        {
            AwaitableCompletionSource<bool> source = new AwaitableCompletionSource<bool>();

            if (!IsReady || string.IsNullOrEmpty(productId) || _purchaseSource != null)
            {
                // Chưa sẵn sàng, id rỗng, hoặc đang có giao dịch dở — một lần mua tại một thời điểm.
                source.SetResult(false);
                return source.Awaitable;
            }

            Product product = _controller.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                _logger.Warn($"[UnityIAPService] Store không bán '{productId}' (chưa tạo trên console?).");
                source.SetResult(false);
                return source.Awaitable;
            }

            _purchaseSource = source;
            _purchasingProductId = productId;
            _controller.InitiatePurchase(product);
            return source.Awaitable;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;
            string productId = product.definition.id;
            bool fulfilled = false;

            try
            {
                if (!_validator.IsValid(product.receipt))
                {
                    // Hoá đơn giả: xác nhận cho store thôi gửi lại, KHÔNG trao.
                    _logger.Warn($"[UnityIAPService] Hoá đơn '{productId}' không hợp lệ — không trao.");
                    _controller.ConfirmPendingPurchase(product);
                }
                else if (_fulfillment != null && _fulfillment.Fulfill(productId, product.transactionID))
                {
                    fulfilled = true;
                    _controller.ConfirmPendingPurchase(product);
                }
                else
                {
                    _logger.Warn($"[UnityIAPService] Chưa trao được '{productId}' — để Pending, store sẽ gửi lại.");
                }
            }
            catch (Exception ex)
            {
                // Không Confirm: thà để store gửi lại còn hơn nuốt giao dịch của user.
                _logger.Error(ex, $"[UnityIAPService] Lỗi khi trao '{productId}' — để Pending.");
            }

            CompletePurchase(productId, fulfilled);
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
        {
            string productId = product != null ? product.definition.id : failure.productId;
            if (failure.reason == PurchaseFailureReason.UserCancelled)
                _logger.Info($"[UnityIAPService] User huỷ mua '{productId}'.");
            else
                _logger.Warn($"[UnityIAPService] Mua '{productId}' thất bại: {failure.reason} {failure.message}");

            CompletePurchase(productId, false);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
            OnPurchaseFailed(product, new PurchaseFailureDescription(
                product != null ? product.definition.id : null, reason, null));

        // Giao dịch store gửi lại lúc boot không có Purchase nào chờ → _purchaseSource null, bỏ qua.
        private void CompletePurchase(string productId, bool result)
        {
            if (_purchaseSource == null || _purchasingProductId != productId) return;

            AwaitableCompletionSource<bool> source = _purchaseSource;
            _purchaseSource = null;
            _purchasingProductId = null;
            source.TrySetResult(result);
        }

        // ---------------------------------------------------------------- others

        public Awaitable RestorePurchases()
        {
            AwaitableCompletionSource source = new AwaitableCompletionSource();

            if (!IsReady || Application.platform != RuntimePlatform.IPhonePlayer)
            {
                // Google Play tự khôi phục lúc khởi tạo — không có gì để làm.
                source.SetResult();
                return source.Awaitable;
            }

            _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, error) =>
            {
                if (!ok) _logger.Warn($"[UnityIAPService] Khôi phục giao dịch thất bại: {error}");
                source.TrySetResult();
            });
            return source.Awaitable;
        }

        public bool IsOwned(string productId)
        {
            if (!IsReady || string.IsNullOrEmpty(productId)) return false;

            Product product = _controller.products.WithID(productId);
            return product != null
                   && product.definition.type != ProductType.Consumable
                   && product.hasReceipt;
        }

        public string GetLocalizedPrice(string productId)
        {
            if (!IsReady || string.IsNullOrEmpty(productId)) return null;

            Product product = _controller.products.WithID(productId);
            string price = product?.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(price) ? null : price;
        }
    }
}
#endif
