using System;
using System.Collections.Generic;

namespace LogosMeta.Economy
{
    [Serializable]
    public class CurrencyData
    {
        // 0 = uninitialized (fresh install). CurrencyService bumps to 1 after applying initial state.
        public int SchemaVersion = 0;
        public int Coins = 0;

        // Mã các lần cộng "chỉ một lần" (AddOnce) — nằm CHUNG file với Coins để hai thứ được
        // ghi nguyên tử. File cũ không có trường này → null, CurrencyService tự khởi tạo.
        public List<string> GrantIds = new List<string>();
    }
}
