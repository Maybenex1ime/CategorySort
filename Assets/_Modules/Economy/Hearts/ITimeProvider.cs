using System;

namespace LogosMeta.Economy
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
