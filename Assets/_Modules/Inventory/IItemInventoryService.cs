using R3;

namespace LogosMeta.Inventory
{
    public interface IItemInventoryService
    {
        // Reactive count for one item id; safe to call before the item was
        // ever granted (starts at 0).
        ReadOnlyReactiveProperty<int> GetCount(string itemId);

        void Add(string itemId, int amount);
        bool TryConsume(string itemId, int amount = 1);
        // Cheat/debug: overwrite the count (clamped to >= 0).
        void SetCount(string itemId, int amount);
    }
}
