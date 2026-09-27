using LogosGame.Features.Currency.Transactions;

namespace LogosGame.Features.Currency
{
    /// <summary>
    /// Các loại tài nguyên người chơi sở hữu. Unity lưu enum trong asset bằng SỐ, nên giá trị
    /// đã gán thì KHÔNG đổi/không sắp xếp lại — thêm loại mới thì dùng số mới.
    /// </summary>
    public enum ResourceType
    {
        Coin = 0,
        Heart = 1,
        BoosterShuffle = 2,
        BoosterMagnet = 3,
        BoosterUndo = 4,
    }

    public static class ResourceTypeExtensions
    {
        /// Mã item mà TransactionItemDispatcher hiểu. Coin không có — coin đi thẳng vào ví.
        public static string ToItemId(this ResourceType type) => type switch
        {
            ResourceType.Heart => ItemIds.Heart,
            ResourceType.BoosterShuffle => ItemIds.BoosterShuffle,
            ResourceType.BoosterMagnet => ItemIds.BoosterMagnet,
            ResourceType.BoosterUndo => ItemIds.BoosterUndo,
            _ => null,
        };
    }
}
