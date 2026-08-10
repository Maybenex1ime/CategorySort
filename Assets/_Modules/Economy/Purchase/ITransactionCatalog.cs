namespace LogosMeta.Economy
{
    // Implemented by the game's catalog asset (ScriptableObject) so the
    // module never depends on a concrete Unity asset type.
    public interface ITransactionCatalog
    {
        bool TryGet(string transactionId, out TransactionDefinition entry);
    }
}
