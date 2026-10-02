using System.Collections.Generic;
using LogosMeta.Economy;
using LogosSDK.Save;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// AddOnce là chốt chống trao trùng của đường tiền thật: store có thể gửi lại cùng một
    /// giao dịch, ví phải cộng đúng một lần và ghi đĩa NGAY (coin + mã chung một file).
    /// </summary>
    public sealed class CurrencyServiceTests
    {
        [Test]
        public void AddOnce_MaMoi_CongCoin_LuuMa_GhiDiaNgay()
        {
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 100 });
            var wallet = new CurrencyService(save, null);

            bool granted = wallet.AddOnce(1000, "tx-1");

            Assert.IsTrue(granted);
            Assert.AreEqual(1100, wallet.Coins.CurrentValue);
            Assert.IsTrue(wallet.HasGrant("tx-1"));
            Assert.AreEqual(1, save.ImmediateCalls, "tiền thật: phải SaveImmediate, không được chờ lần ghi trễ");
            Assert.AreEqual(1100, save.Data.Coins);
            CollectionAssert.Contains(save.Data.GrantIds, "tx-1", "mã phải nằm CHUNG file với số coin");
        }

        [Test]
        public void AddOnce_MaCu_KhongCong_TraFalse_KhongGhiDia()
        {
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 100 });
            var wallet = new CurrencyService(save, null);
            wallet.AddOnce(1000, "tx-1");

            bool again = wallet.AddOnce(1000, "tx-1");

            Assert.IsFalse(again, "store gửi lại giao dịch cũ thì không được cộng lần hai");
            Assert.AreEqual(1100, wallet.Coins.CurrentValue);
            Assert.AreEqual(1, save.ImmediateCalls);
        }

        [Test]
        public void AddOnce_AmountKhongDuong_HoacMaRong_TraFalse()
        {
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 100 });
            var wallet = new CurrencyService(save, null);

            Assert.IsFalse(wallet.AddOnce(0, "tx-1"));
            Assert.IsFalse(wallet.AddOnce(-5, "tx-2"));
            Assert.IsFalse(wallet.AddOnce(1000, null));
            Assert.IsFalse(wallet.AddOnce(1000, ""));

            Assert.AreEqual(100, wallet.Coins.CurrentValue);
            Assert.AreEqual(0, save.ImmediateCalls);
            Assert.IsFalse(wallet.HasGrant("tx-1"));
        }

        [Test]
        public void AddOnce_Vuot200Ma_BoMaCuNhat()
        {
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 0 });
            var wallet = new CurrencyService(save, null);

            for (int i = 0; i < 201; i++) wallet.AddOnce(1, "tx-" + i);

            Assert.AreEqual(200, save.Data.GrantIds.Count, "danh sách mã không được phình vô hạn");
            Assert.IsFalse(wallet.HasGrant("tx-0"), "mã cũ nhất bị đẩy ra");
            Assert.IsTrue(wallet.HasGrant("tx-1"));
            Assert.IsTrue(wallet.HasGrant("tx-200"));
        }

        [Test]
        public void AddOnce_FileCuKhongCoGrantIds_KhongNem()
        {
            // File save trước bản này không có trường GrantIds → deserialize ra null.
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 50, GrantIds = null });
            var wallet = new CurrencyService(save, null);

            Assert.IsFalse(wallet.HasGrant("tx-1"));
            Assert.IsTrue(wallet.AddOnce(10, "tx-1"));
            Assert.AreEqual(60, wallet.Coins.CurrentValue);
        }

        [Test]
        public void Add_VanDungSaveTre()
        {
            var save = new FakeSave(new CurrencyData { SchemaVersion = 1, Coins = 0 });
            var wallet = new CurrencyService(save, null);

            wallet.Add(5);

            Assert.AreEqual(5, wallet.Coins.CurrentValue);
            Assert.AreEqual(1, save.DeferredCalls);
            Assert.AreEqual(0, save.ImmediateCalls, "đường coin thường không cần ghi đĩa ngay");
        }

        // Chỉ phục vụ đúng một domain — CurrencyService không đụng domain nào khác.
        private sealed class FakeSave : ISaveManager
        {
            public readonly CurrencyData Data;
            public int DeferredCalls;
            public int ImmediateCalls;

            public FakeSave(CurrencyData data) => Data = data;

            public void Register<T>(IStorageProvider provider, string key) where T : class, new() { }
            public T Load<T>() where T : class, new() => Data as T;
            public void Save<T>(T data) where T : class, new() => DeferredCalls++;
            public void SaveImmediate<T>(T data) where T : class, new() => ImmediateCalls++;
            public void SaveAll() { }
            public void DeleteDomain<T>() where T : class, new() { }
            public bool HasDomain<T>() where T : class, new() => true;
        }
    }
}
