namespace LogosGame.Features.Currency.Events
{
    /// <summary>
    /// STUB. Nút booster bắn event này khi count = 0 (mời mua). WordStack chưa có
    /// popup mua nên hiện KHÔNG AI NGHE — bấm nút hết booster sẽ không xảy ra gì.
    ///
    /// Giữ đúng tên + namespace của aquapark để các *BoosterButtonView chép sang
    /// compile được nguyên vẹn. Khi làm cửa hàng thì viết bên nghe, không phải
    /// sửa lại View.
    ///
    /// Phải là struct: IEventBus ràng buộc `where T : struct`.
    /// </summary>
    public readonly struct PurchaseRequestedEvent
    {
        public string TransactionId { get; }

        public PurchaseRequestedEvent(string transactionId)
        {
            TransactionId = transactionId;
        }
    }
}
