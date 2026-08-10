using System;
using R3;

namespace LogosMeta.Economy
{
    public interface IHeartService
    {
        ReadOnlyReactiveProperty<int>      Current        { get; }
        ReadOnlyReactiveProperty<bool>     IsFull         { get; }
        ReadOnlyReactiveProperty<TimeSpan> TimeUntilNext  { get; }

        void ConsumeOne();
        void Add(int amount);
        // Cheat/debug: set exact heart count and restart the regen timer.
        void SetHearts(int hearts);
    }
}
