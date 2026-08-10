using System.Collections.Generic;

namespace LogosSDK.Core.Commands
{
    public class CommandHistory
    {
        private readonly Stack<IGameCommand> _undoStack = new();

        public bool CanUndo => _undoStack.Count > 0;

        public void Execute(IGameCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
        }

        public void Undo()
        {
            if (CanUndo)
                _undoStack.Pop().Undo();
        }

        public void Clear() => _undoStack.Clear();
    }
}
