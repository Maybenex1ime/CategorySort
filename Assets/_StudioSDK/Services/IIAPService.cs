using System.Collections.Generic;
using UnityEngine;

namespace LogosSDK.Services
{
    public enum IapProductKind
    {
        Consumable = 0,
        NonConsumable = 1,
    }

    public readonly struct IapProduct
    {
        public readonly string Id;
        public readonly IapProductKind Kind;

        public IapProduct(string id, IapProductKind kind)
        {
            Id = id;
            Kind = kind;
        }
    }

    /// <summary>
    /// Bên trao thưởng cho một giao dịch tiền thật.
    /// Trả true = đã trao VÀ đã lưu xuống đĩa → store mới được xác nhận giao dịch.
    /// Trả false = chưa trao được (thiếu service, id lạ) → giao dịch ở lại Pending,
    /// store gửi lại ở lần khởi tạo sau.
    /// Phải chạy được khi KHÔNG có UI nào mở: store gửi lại giao dịch dở ngay lúc boot.
    /// </summary>
    public interface IIapFulfillment
    {
        bool Fulfill(string productId, string transactionId);
    }

    public interface IIAPService
    {
        bool IsReady { get; }

        /// Gọi nhiều lần là an toàn — lần sau trả ngay kết quả lần đầu.
        Awaitable<bool> Initialize(IReadOnlyList<IapProduct> products, IIapFulfillment fulfillment);

        /// true = giao dịch đã được TRAO (qua IIapFulfillment) và xác nhận với store.
        Awaitable<bool> Purchase(string productId);

        Awaitable RestorePurchases();
        bool IsOwned(string productId);

        /// Giá đã bản địa hoá từ store; null khi chưa khởi tạo hoặc store không có id này.
        string GetLocalizedPrice(string productId);
    }
}
