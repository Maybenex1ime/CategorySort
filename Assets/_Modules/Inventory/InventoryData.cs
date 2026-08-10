using System;
using System.Collections.Generic;

namespace LogosMeta.Inventory
{
    [Serializable]
    public class InventoryData
    {
        public int SchemaVersion = 1;
        // Item id → owned count. Ids are free-form strings owned by the game.
        public Dictionary<string, int> Counts = new();
    }
}
