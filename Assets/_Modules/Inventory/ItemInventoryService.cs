using System;
using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Inventory
{
    public sealed class ItemInventoryService : IItemInventoryService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ItemInventoryService>();

        private readonly ISaveManager _save;
        private readonly Dictionary<string, ReactiveProperty<int>> _counts = new(StringComparer.Ordinal);

        private InventoryData _data;

        public ItemInventoryService(ISaveManager save)
        {
            _save = save;
            _data = _save.Load<InventoryData>();

            // Old save files may deserialize with an explicit null dictionary
            // (Newtonsoft overwrites the field initializer).
            if (_data.Counts == null)
                _data.Counts = new Dictionary<string, int>();
        }

        public ReadOnlyReactiveProperty<int> GetCount(string itemId) => GetOrCreate(itemId);

        public void Add(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

            int next = ReadCount(itemId) + amount;
            WriteCount(itemId, next);
            _logger.Info($"[ItemInventoryService] +{amount} '{itemId}' → {next}");
        }

        public bool TryConsume(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            int current = ReadCount(itemId);
            if (current < amount) return false;

            WriteCount(itemId, current - amount);
            return true;
        }

        public void SetCount(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (amount < 0) amount = 0;

            WriteCount(itemId, amount);
            _logger.Info($"[ItemInventoryService] SetCount('{itemId}', {amount})");
        }

        public void Dispose()
        {
            foreach (ReactiveProperty<int> property in _counts.Values)
                property.Dispose();
            _counts.Clear();
        }

        private int ReadCount(string itemId)
        {
            _data.Counts.TryGetValue(itemId, out int count);
            return count < 0 ? 0 : count;
        }

        private void WriteCount(string itemId, int count)
        {
            _data.Counts[itemId] = count;
            _save.Save(_data);
            GetOrCreate(itemId).Value = count;
        }

        private ReactiveProperty<int> GetOrCreate(string itemId)
        {
            if (!_counts.TryGetValue(itemId, out ReactiveProperty<int> property))
            {
                property = new ReactiveProperty<int>(ReadCount(itemId));
                _counts[itemId] = property;
            }
            return property;
        }
    }
}
