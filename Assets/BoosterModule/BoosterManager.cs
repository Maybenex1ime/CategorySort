using System;
using System.Collections.Generic;
using UnityEngine;
using LogosSDK.Core.Events;

namespace BoosterModule
{
    /// <summary>
    /// Inventory booster — plain C# service, KHÔNG phải MonoBehaviour nên không cần
    /// (và không thể quên) đặt vào scene. Game đăng ký nó Eager trong DI: toàn bộ
    /// việc nằm ở constructor (load save + subscribe bus), Dispose gỡ subscribe.
    /// </summary>
    public sealed class BoosterManager : IDisposable
    {
        // ponytail: static bridge cho BoosterSlotViewModel đọc count lúc khởi tạo —
        // view tự new viewmodel không qua DI. Nếu sau này viewmodel được inject
        // container thì bỏ static này đi.
        public static BoosterManager Instance { get; private set; }

        private readonly Dictionary<BoosterId, int> _inventory = new();

        public BoosterManager()
        {
            LoadInventory();
            Bus.Global.On<BoosterAddedEvent>(OnBoosterAdded);
            Bus.Global.On<BoosterUseEvent>(OnUseBooster);
            Bus.Global.On<BoosterSetEvent>(OnBoosterSet);
            Instance = this;
        }

        public void Dispose()
        {
            Bus.Global.Off<BoosterAddedEvent>(OnBoosterAdded);
            Bus.Global.Off<BoosterUseEvent>(OnUseBooster);
            Bus.Global.Off<BoosterSetEvent>(OnBoosterSet);
            if (Instance == this) Instance = null;
        }

        public int GetCount(BoosterId id)
        {
            return _inventory.TryGetValue(id, out int count) ? count : 0;
        }

        // Cheat: ghi đè số lượng, không cộng dồn.
        private void OnBoosterSet(BoosterSetEvent evt)
        {
            if (evt.Id == BoosterId.None) return;

            _inventory[evt.Id] = evt.Count < 0 ? 0 : evt.Count;
            SaveInventory();
            Bus.Global.Fire(new BoosterInventoryChangedEvent(evt.Id, _inventory[evt.Id]));
        }

        private void OnBoosterAdded(BoosterAddedEvent evt)
        {
            if (evt.Id == BoosterId.None) return;

            _inventory.TryGetValue(evt.Id, out int count);
            _inventory[evt.Id] = count + evt.Count;

            SaveInventory();
            Bus.Global.Fire(new BoosterInventoryChangedEvent(evt.Id, _inventory[evt.Id]));
        }

        private void OnUseBooster(BoosterUseEvent evt)
        {
            if (!_inventory.TryGetValue(evt.Id, out int count) || count <= 0)
            {
                Bus.Global.Fire(new BoosterExhaustedEvent(evt.Id, count));
                Debug.LogWarning($"[BoosterManager] Cannot use booster {evt.Id}: Not enough in inventory.");
                return;
            }

            _inventory[evt.Id] = count - 1;
            SaveInventory();
            Debug.Log($"[BoosterManager] {evt.Id} used, remaining {count - 1}.");

            Bus.Global.Fire(new BoosterInventoryChangedEvent(evt.Id, _inventory[evt.Id]));
            Bus.Global.Fire(new BoosterActivatedEvent(evt.Id));
        }

        [Serializable]
        private class InventoryData
        {
            public List<BoosterId> IDs = new();
            public List<int> Counts = new();
        }

        private const string SaveKey = "BoosterModule_Inventory";

        private void SaveInventory()
        {
            var data = new InventoryData();
            foreach (var kvp in _inventory)
            {
                data.IDs.Add(kvp.Key);
                data.Counts.Add(kvp.Value);
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void LoadInventory()
        {
            _inventory.Clear();
            if (!PlayerPrefs.HasKey(SaveKey)) return;

            string json = PlayerPrefs.GetString(SaveKey);
            var data = JsonUtility.FromJson<InventoryData>(json);

            for (int i = 0; i < data.IDs.Count; i++)
            {
                _inventory[data.IDs[i]] = data.Counts[i];
            }
        }
    }
}
