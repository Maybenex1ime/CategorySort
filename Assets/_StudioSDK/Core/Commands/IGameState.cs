namespace LogosSDK.Core.Commands
{
    public interface IGameState
    {
        bool Evaluate();
        object CreateSnapshot();
        void RestoreSnapshot(object snapshot);
    }
}
