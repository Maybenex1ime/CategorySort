using R3;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Services
{
    public interface IDifficultyStateProvider
    {
        ReadOnlyReactiveProperty<LevelDifficulty> Difficulty { get; }
    }
}
