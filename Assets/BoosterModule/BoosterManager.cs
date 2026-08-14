using System.Collections.Generic;
using UnityEngine;
using LogosSDK.Core.Events;

namespace BoosterModule
{
    public class BoosterManager : SingletonComp<BoosterManager>
    {
        private Dictionary<BoosterId, int> _inventory = new();

        protected override void RightAfterAwake()
        {
            base.RightAfterAwake();
            LoadInventory();
        }

        private void OnEnable()
        {
            Bus.Global.On<BoosterAddedEvent>(OnBoosterAdded);
            Bus.Global.On<BoosterUseEvent>(OnUseBooster);
            Bus.Global.On<BoosterSetEvent>(OnBoosterSet);
        }

        private void OnDisable()
        {
            Bus.Global.Off<BoosterAddedEvent>(OnBoosterAdded);
            Bus.Global.Off<BoosterUseEvent>(OnUseBooster);
            Bus.Global.Off<BoosterSetEvent>(OnBoosterSet);
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
                Bus.Global.Fire(new BoosterExhaustedEvent(evt.Id, _inventory[evt.Id]));
                Debug.LogWarning($"[BoosterManager] Cannot use booster {evt.Id}: Not enough in inventory.");
                return;
            }

            _inventory[evt.Id] = count - 1;
            SaveInventory();
            
            Bus.Global.Fire(new BoosterInventoryChangedEvent(evt.Id, _inventory[evt.Id]));
            Bus.Global.Fire(new BoosterActivatedEvent(evt.Id));
        }

        [System.Serializable]
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
