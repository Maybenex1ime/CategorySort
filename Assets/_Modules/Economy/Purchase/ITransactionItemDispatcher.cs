namespace LogosMeta.Economy
{
    // Game-side hook: PurchaseService calls this once per purchased item after
    // the currency was spent. The game maps item ids to concrete grants
    // (booster events, hearts, ...).
    public interface ITransactionItemDispatcher
    {
        void Grant(string itemId, int amount);
    }
}
