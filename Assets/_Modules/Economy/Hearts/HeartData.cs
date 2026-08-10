using System;

namespace LogosMeta.Economy
{
    [Serializable]
    public class HeartData
    {
        public int  SchemaVersion = 1;
        public int  Hearts = 5;
        public long LastRegenUtcTicks = 0;
    }
}
