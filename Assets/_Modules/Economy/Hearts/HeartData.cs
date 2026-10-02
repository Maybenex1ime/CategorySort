using System;

namespace LogosMeta.Economy
{
    [Serializable]
    public class HeartData
    {
        public int  SchemaVersion = 1;
        public int  Hearts = 5;
        public long LastRegenUtcTicks = 0;

        // Tim vô hạn tới mốc này (UTC ticks); 0 = chưa từng có. File save cũ thiếu field
        // → mặc định 0, không cần tăng SchemaVersion.
        public long UnlimitedUntilUtcTicks = 0;
    }
}
