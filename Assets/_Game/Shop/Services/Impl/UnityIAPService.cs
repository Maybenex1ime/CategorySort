// File DUY NHẤT trong project được biết Unity IAP là gì. Mọi thứ khác nói chuyện qua IIAPService.
// Chỉ biên dịch khi package com.unity.purchasing đã cài — define do versionDefines của
// WordStack.Meta.asmdef bật; chưa cài thì file này rỗng và ShopInstaller rơi về StubIAPService.
// Viết theo API Unity IAP 5 (StoreController + event); IAP 4 hết hỗ trợ từ 08/06/2026.
#if CATEGORYSORT_UNITY_IAP
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
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
    /// Nguyên tắc: chỉ ConfirmPurchase sau khi IIapFulfillment báo đã trao + đã lưu. App chết ở bất
    /// kỳ đâu trước Confirm thì đơn còn Pending — FetchPurchases() lúc khởi tạo gửi lại qua
    /// OnPurchasePending, kể cả khi không có Purchase nào đang chờ.
    /// Non-consumable đã xác nhận từ trước (cài lại game, máy mới, Restore iOS) không quay lại
    /// OnPurchasePending mà tới dưới dạng ConfirmedOrder trong OnPurchasesFetched — trao lại ở đó.
    /// </summary>
    public sealed class UnityIAPService : IIAPService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<UnityIAPService>();

        private readonly IReceiptValidator _validator;

        private StoreController _store;
        private IIapFulfillment _fulfillment;
        private bool _productsReady;

        private AwaitableCompletionSource<bool> _initSource;
        private AwaitableCompletionSource<bool> _purchaseSource;
        private string _purchasingProductId;

        private readonly HashSet<string> _nonConsumableIds = new HashSet<string>();

        // Non-consumable đã trao trong phiên (Fulfill trả true) — nguồn của IsOwned.
        private readonly HashSet<string> _ownedIds = new HashSet<string>();

        // Ai đang chờ kết quả FetchPurchases (init, Restore, mua trùng). Mỗi callback gọi đúng một lần.
        private readonly List<Action> _fetchWaiters = new List<Action>();

        // Store không trả lời fetch thì Purchase/Restore không được treo (khoá popup cả phiên).
        private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);

        public UnityIAPService() : this(new AcceptAllReceiptValidator())
        {
            _logger.Warn("[UnityIAPService] Chưa bật xác thực hoá đơn (pha 2) — mọi receipt đều được chấp nhận.");
        }

        public UnityIAPService(IReceiptValidator validator)
        {
            _validator = validator ?? new AcceptAllReceiptValidator();
        }

        public bool IsReady => _store != null && _productsReady && _fulfillment != null;

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

            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].Kind == IapProductKind.NonConsumable) _nonConsumableIds.Add(products[i].Id);
            }

            InitializeInBackground(products);
            return _initSource.Awaitable;
        }

        private async void InitializeInBackground(IReadOnlyList<IapProduct> products)
        {
            try
            {
                // IAP cảnh báo nếu Gaming Services chưa khởi tạo. Thất bại ở đây (project chưa
                // link UGS, offline) KHÔNG chặn store.
                try
                {
                    await UnityServices.InitializeAsync();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[UnityIAPService] UnityServices.InitializeAsync lỗi, vẫn khởi tạo store: {ex.Message}");
                }

                _store = UnityIAPServices.StoreController();
                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                _store.OnPurchaseFailed += OnPurchaseFailed;
                _store.OnPurchaseDeferred += OnPurchaseDeferred;
                _store.OnProductsFetched += OnProductsFetched;
                _store.OnProductsFetchFailed += OnProductsFetchFailed;
                _store.OnPurchasesFetched += OnPurchasesFetched;
                _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                _store.OnStoreDisconnected += OnStoreDisconnected;

                await _store.Connect();

                var definitions = new List<ProductDefinition>(products.Count);
                for (int i = 0; i < products.Count; i++)
                {
                    definitions.Add(new ProductDefinition(products[i].Id,
                        products[i].Kind == IapProductKind.NonConsumable
                            ? ProductType.NonConsumable
                            : ProductType.Consumable));
                }

                _store.FetchProducts(definitions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UnityIAPService] Lỗi khi khởi tạo store.");
                _initSource.TrySetResult(false);
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            _productsReady = true;
            _logger.Info($"[UnityIAPService] Store sẵn sàng, {products.Count} sản phẩm.");

            // Sau khi có sản phẩm mới hỏi đơn cũ: đơn Pending (trả tiền rồi mà chưa trao) được
            // gửi lại qua OnPurchasePending — đây là lý do phải khởi tạo ngay lúc boot.
            // Init chỉ xong khi đơn cũ đã về: ShopService đọc IsOwned ngay sau init, nên Remove Ads
            // đã sở hữu phải được trao (OnPurchasesFetched) trước đó. Fetch lỗi vẫn bán được → true.
            FetchPurchases(() => _initSource.TrySetResult(true));
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            _logger.Warn($"[UnityIAPService] Store không trả {failure.FailedFetchProducts.Count} sản phẩm: {failure.FailureReason}");

            // Lỗi một phần vẫn bán được phần còn lại; chỉ báo thất bại khi không có sản phẩm nào.
            if (_store.GetProducts().Count == 0) _initSource.TrySetResult(false);
        }

        // Gọi _store.FetchPurchases() và báo `done` khi có kết quả (thành công, lỗi, mất kết nối
        // hoặc quá FetchTimeout). Đăng ký TRƯỚC khi fetch: store chưa kết nối / FakeStore trả lời
        // đồng bộ ngay trong FetchPurchases().
        private void FetchPurchases(Action done)
        {
            _fetchWaiters.Add(done);
            TimeoutFetchWaiter(done);
            _store.FetchPurchases();
        }

        // Task.Delay theo giờ thật (không phụ thuộc timeScale); async void bắt đầu trên main thread
        // nên chạy tiếp trên main thread qua UnitySynchronizationContext.
        private async void TimeoutFetchWaiter(Action done)
        {
            await Task.Delay(FetchTimeout);
            if (!_fetchWaiters.Remove(done)) return;   // đã có kết quả

            _logger.Warn("[UnityIAPService] Store không trả kết quả FetchPurchases — bỏ chờ.");
            RunWaiter(done);
        }

        private void CompleteFetchWaiters()
        {
            if (_fetchWaiters.Count == 0) return;

            Action[] waiters = _fetchWaiters.ToArray();
            _fetchWaiters.Clear();
            for (int i = 0; i < waiters.Length; i++) RunWaiter(waiters[i]);
        }

        private static void RunWaiter(Action done)
        {
            try
            {
                done();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[UnityIAPService] Lỗi khi xử lý kết quả FetchPurchases.");
            }
        }

        // Package đã chuyển đơn Pending của lần fetch này sang OnPurchasePending TRƯỚC event này
        // (PurchaseService.OnFetchSuccess). Ở đây chỉ xử lý đơn ĐÃ xác nhận: non-consumable user
        // đã sở hữu. Không Confirm gì cả — đơn đã xác nhận rồi, và consumable không đi đường này.
        private void OnPurchasesFetched(Orders orders)
        {
            IReadOnlyList<ConfirmedOrder> confirmed = orders?.ConfirmedOrders;
            for (int i = 0; confirmed != null && i < confirmed.Count; i++) GrantOwned(confirmed[i]);

            CompleteFetchWaiters();
        }

        private void GrantOwned(ConfirmedOrder order)
        {
            string productId = ProductIdOf(order);
            if (productId == null || !_nonConsumableIds.Contains(productId)) return;

            try
            {
                if (!_validator.IsValid(order.Info?.Receipt))
                {
                    _logger.Warn($"[UnityIAPService] Hoá đơn sở hữu '{productId}' không hợp lệ — không trao.");
                    return;
                }

                // Đơn đã xác nhận có thể không mang mã (FakeStore luôn để rỗng); ShopService từ chối
                // mã rỗng. Mã chỉ để ghi log — trao non-consumable là bật cờ, idempotent.
                string transactionId = order.Info?.TransactionID;
                if (string.IsNullOrEmpty(transactionId)) transactionId = "owned:" + productId;

                if (_fulfillment != null && _fulfillment.Fulfill(productId, transactionId))
                    _ownedIds.Add(productId);
                else
                    _logger.Warn($"[UnityIAPService] Store báo đã sở hữu '{productId}' nhưng chưa trao được.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[UnityIAPService] Lỗi khi trao lại '{productId}'.");
            }
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            _logger.Warn($"[UnityIAPService] Không lấy được đơn cũ: {failure.FailureReason} {failure.Message}");
            CompleteFetchWaiters();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            _logger.Warn($"[UnityIAPService] Mất kết nối store: {failure.Message}");
            _initSource?.TrySetResult(false);
            CompleteFetchWaiters();
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

            Product product = _store.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                _logger.Warn($"[UnityIAPService] Store không bán '{productId}' (chưa tạo trên console?).");
                source.SetResult(false);
                return source.Awaitable;
            }

            _purchaseSource = source;
            _purchasingProductId = productId;
            _store.PurchaseProduct(product);
            return source.Awaitable;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string productId = ProductIdOf(order);
            bool fulfilled = false;

            try
            {
                if (!_validator.IsValid(order.Info.Receipt))
                {
                    // Hoá đơn giả: xác nhận cho store thôi gửi lại, KHÔNG trao.
                    _logger.Warn($"[UnityIAPService] Hoá đơn '{productId}' không hợp lệ — không trao.");
                    _store.ConfirmPurchase(order);
                }
                else if (_fulfillment != null && _fulfillment.Fulfill(productId, order.Info.TransactionID))
                {
                    fulfilled = true;
                    if (_nonConsumableIds.Contains(productId)) _ownedIds.Add(productId);
                    _store.ConfirmPurchase(order);
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
        }

        // Confirm hỏng thì đơn vẫn Pending, lần sau gửi lại; ví AddOnce chặn trao lần hai.
        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                _logger.Warn($"[UnityIAPService] Xác nhận '{ProductIdOf(order)}' với store thất bại: {failed.FailureReason} {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            string productId = ProductIdOf(order);

            // Mua lại non-consumable đã sở hữu (Google ItemAlreadyOwned, Apple đơn đã xác nhận):
            // hỏi lại store để OnPurchasesFetched trao nó; Purchase trả true chỉ khi đã trao thật.
            if (order.FailureReason == PurchaseFailureReason.DuplicateTransaction
                && productId != null && _nonConsumableIds.Contains(productId))
            {
                _logger.Info($"[UnityIAPService] '{productId}' đã sở hữu — lấy lại đơn từ store để trao.");
                FetchPurchases(() => CompletePurchase(productId, _ownedIds.Contains(productId)));
                return;
            }

            if (order.FailureReason == PurchaseFailureReason.UserCancelled)
                _logger.Info($"[UnityIAPService] User huỷ mua '{productId}'.");
            else
                _logger.Warn($"[UnityIAPService] Mua '{productId}' thất bại: {order.FailureReason} {order.Details}");

            CompletePurchase(productId, false);
        }

        // Ask to Buy / thanh toán chờ duyệt: chưa có tiền nên chưa trao. Khi duyệt xong đơn tới lại
        // qua OnPurchasePending như mọi đơn khác.
        private void OnPurchaseDeferred(DeferredOrder order)
        {
            string productId = ProductIdOf(order);
            _logger.Info($"[UnityIAPService] Đơn '{productId}' đang chờ duyệt — trao khi store xác nhận.");
            CompletePurchase(productId, false);
        }

        private static string ProductIdOf(Order order)
        {
            IReadOnlyList<CartItem> items = order?.CartOrdered?.Items();
            return items != null && items.Count > 0 ? items[0].Product?.definition?.id : null;
        }

        // Đơn store gửi lại lúc boot không có Purchase nào chờ → _purchaseSource null, bỏ qua.
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
                // Google Play tự khôi phục qua FetchPurchases lúc khởi tạo — không có gì để làm.
                source.SetResult();
                return source.Awaitable;
            }

            _store.RestoreTransactions((ok, error) =>
            {
                if (!ok) _logger.Warn($"[UnityIAPService] Khôi phục giao dịch thất bại: {error}");

                // Package tự FetchPurchases khi ok nhưng gọi callback này ngay, trước khi đơn về.
                // Fetch lại và chờ: non-consumable khôi phục được trao trước khi Restore xong.
                FetchPurchases(() => source.TrySetResult());
            });
            return source.Awaitable;
        }

        // Đã trao trong phiên này (mua mới, hoặc store báo đã sở hữu lúc init / Restore / mua trùng).
        public bool IsOwned(string productId) => productId != null && _ownedIds.Contains(productId);

        public string GetLocalizedPrice(string productId)
        {
            if (!IsReady || string.IsNullOrEmpty(productId)) return null;

            Product product = _store.GetProductById(productId);
            string price = product?.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(price) ? null : price;
        }
    }
}
#endif
