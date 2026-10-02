using LogosGame.Features.Shop;
using LogosGame.Features.Shop.Impl;
using LogosSDK.Save;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Cờ Remove Ads là quyền đã trả tiền thật: bật là ghi đĩa NGAY (store chỉ được xác nhận
    /// đơn sau khi Fulfill trả về), và đọc lại đúng sau khi khởi động lại.
    /// </summary>
    public sealed class NoAdsServiceTests
    {
        [Test]
        public void Grant_BatCo_GhiDiaNgay()
        {
            var save = new FakeSave(new NoAdsData());
            var noAds = new NoAdsService(save);

            noAds.Grant();

            Assert.IsTrue(noAds.IsNoAds.CurrentValue);
            Assert.IsTrue(save.Data.Owned, "cờ phải nằm trong data đã ghi");
            Assert.AreEqual(1, save.ImmediateCalls, "tiền thật: phải SaveImmediate, không chờ lần ghi trễ");
        }

        [Test]
        public void Grant_LanHai_KhongGhiLai()
        {
            var save = new FakeSave(new NoAdsData());
            var noAds = new NoAdsService(save);

            noAds.Grant();
            noAds.Grant();

            Assert.AreEqual(1, save.ImmediateCalls, "đã bật rồi thì không ghi thêm");
        }

        [Test]
        public void KhoiDongLai_DocCoTuSave()
        {
            var save = new FakeSave(new NoAdsData { SchemaVersion = 1, Owned = true });

            var noAds = new NoAdsService(save);

            Assert.IsTrue(noAds.IsNoAds.CurrentValue, "cài lại / mở lại game phải giữ quyền đã mua");
            Assert.AreEqual(0, save.ImmediateCalls);
        }

        private sealed class FakeSave : ISaveManager
        {
            public readonly NoAdsData Data;
            public int ImmediateCalls;

            public FakeSave(NoAdsData data) => Data = data;

            public void Register<T>(IStorageProvider provider, string key) where T : class, new() { }
            public T Load<T>() where T : class, new() => Data as T;
            public void Save<T>(T data) where T : class, new() { }
            public void SaveImmediate<T>(T data) where T : class, new() => ImmediateCalls++;
            public void SaveAll() { }
            public void DeleteDomain<T>() where T : class, new() { }
            public bool HasDomain<T>() where T : class, new() => true;
        }
    }
}
