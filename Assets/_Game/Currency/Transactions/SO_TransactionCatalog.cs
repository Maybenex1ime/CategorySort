using System;
using LogosMeta.Economy;
using UnityEngine;

namespace LogosGame.Features.Currency.Transactions
{
    [CreateAssetMenu(fileName = "SO_TransactionCatalog", menuName = "WordStack/Config/Transaction Catalog")]
    public sealed class TransactionCatalog : ScriptableObject, ITransactionCatalog
    {
        [SerializeField] private TransactionDefinition[] _entries = Array.Empty<TransactionDefinition>();

        public TransactionDefinition[] Entries => _entries;

        public bool TryGet(string transactionId, out TransactionDefinition entry)
        {
            if (!string.IsNullOrEmpty(transactionId) && _entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].TransactionId == transactionId)
                    {
                        entry = _entries[i];
                        return true;
                    }
                }
            }
            entry = default;
            return false;
        }
    }
}
