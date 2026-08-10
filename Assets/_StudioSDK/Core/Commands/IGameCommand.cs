namespace LogosSDK.Core.Commands
{
    public interface IGameCommand
    {
        void Execute();
        void Undo();
    }
}
