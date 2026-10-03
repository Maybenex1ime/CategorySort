using BoosterModule;
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

        // Tim vô hạn theo thời gian — Amount tính bằng GIỜ, không phải số lượng.
        UnlimitedHeart = 5,
    }

    /// <summary>
    /// Chỗ DUY NHẤT đổi ResourceType qua các định danh cũ: BoosterId (BoosterModule) và
    /// ItemIds (chuỗi trong SO_TransactionCatalog). Thêm loại mới thì sửa ở đây.
    /// </summary>
    public static class ResourceTypeExtensions
    {
        /// Mã item mà TransactionItemDispatcher hiểu. Coin không có — coin đi thẳng vào ví.
        public static string ToItemId(this ResourceType type) => type switch
        {
            ResourceType.Heart => ItemIds.Heart,
            ResourceType.BoosterShuffle => ItemIds.BoosterShuffle,
            ResourceType.BoosterMagnet => ItemIds.BoosterMagnet,
            ResourceType.BoosterUndo => ItemIds.BoosterUndo,
            ResourceType.UnlimitedHeart => ItemIds.UnlimitedHeart,
            _ => null,
        };

        public static bool TryParseItemId(string itemId, out ResourceType type)
        {
            switch (itemId)
            {
                case ItemIds.Heart: type = ResourceType.Heart; return true;
                case ItemIds.BoosterShuffle: type = ResourceType.BoosterShuffle; return true;
                case ItemIds.BoosterMagnet: type = ResourceType.BoosterMagnet; return true;
                case ItemIds.BoosterUndo: type = ResourceType.BoosterUndo; return true;
                case ItemIds.UnlimitedHeart: type = ResourceType.UnlimitedHeart; return true;
                default: type = default; return false;
            }
        }

        public static bool TryGetBoosterId(this ResourceType type, out BoosterId id)
        {
            id = type switch
            {
                ResourceType.BoosterShuffle => BoosterId.Shuffle,
                ResourceType.BoosterMagnet => BoosterId.Magnet,
                ResourceType.BoosterUndo => BoosterId.Undo,
                _ => BoosterId.None,
            };
            return id != BoosterId.None;
        }

        public static bool TryFromBooster(BoosterId id, out ResourceType type)
        {
            switch (id)
            {
                case BoosterId.Shuffle: type = ResourceType.BoosterShuffle; return true;
                case BoosterId.Magnet: type = ResourceType.BoosterMagnet; return true;
                case BoosterId.Undo: type = ResourceType.BoosterUndo; return true;
                default: type = default; return false;
            }
        }
    }
}
