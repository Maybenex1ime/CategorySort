using System;
using UnityEngine;

namespace LogosMeta.Economy
{
    [Serializable]
    public struct TransactionItem
    {
        // Free-form item id (e.g. "booster.hand", "heart"). The game's
        // ITransactionItemDispatcher decides what each id grants — the module
        // never knows concrete item types.
        public string ItemId;
        [Min(1)] public int Amount;
    }

    [Serializable]
    public struct TransactionDefinition
    {
        public string TransactionId;
        public string Name;
        [TextArea] public string Description;
        [Min(0)] public int Price;
        public TransactionItem[] Items;
    }
}
