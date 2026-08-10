using System;

namespace LogosMeta.Economy
{
    public sealed class HeartSettings
    {
        public int MaxHearts { get; }
        public TimeSpan RegenInterval { get; }

        public HeartSettings(int maxHearts, TimeSpan regenInterval)
        {
            MaxHearts = maxHearts < 1 ? 1 : maxHearts;
            RegenInterval = regenInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : regenInterval;
        }
    }
}
