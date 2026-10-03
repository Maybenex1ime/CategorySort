using System;
using LogosGame.Features.Currency;
using LogosGame.Features.Currency.Services.Impl;
using LogosMeta.Economy;
using LogosSDK.Save;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Tim vô hạn theo thời gian: trong hạn thì chơi không mất tim, hết hạn thì trở lại bình
    /// thường. Mốc hết hạn nằm trong HeartData nên tắt app mở lại vẫn đúng. Thời gian giả để
    /// tua nhanh — không đợi thật.
    /// </summary>
    public sealed class HeartServiceUnlimitedTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TrongHan_ConsumeOne_KhongTruTim()
        {
            var (hearts, _, _) = Build(heartsStart: 3);

            hearts.AddUnlimited(TimeSpan.FromMinutes(60));
            hearts.ConsumeOne();

            Assert.IsTrue(hearts.IsUnlimited.CurrentValue);
            Assert.AreEqual(3, hearts.Current.CurrentValue, "đang vô hạn thì thua màn không mất tim");
        }

        [Test]
        public void AddUnlimited_KhiDangVoHan_CongDonThoiGianConLai()
        {
            var (hearts, time, _) = Build();

            hearts.AddUnlimited(TimeSpan.FromMinutes(30));
            time.UtcNow = T0.AddMinutes(10);
            hearts.AddUnlimited(TimeSpan.FromMinutes(30));

            Assert.AreEqual(TimeSpan.FromMinutes(50), hearts.UnlimitedTimeLeft.CurrentValue,
                "mua thêm khi còn 20 phút thì phải thành 50, không reset về 30");
        }

        [Test]
        public void HetHan_UpdateTick_TatVoHan_VaTruTimBinhThuong()
        {
            var (hearts, time, _) = Build(heartsStart: 3);
            hearts.AddUnlimited(TimeSpan.FromMinutes(60));

            time.UtcNow = T0.AddMinutes(61);
            hearts.UpdateTick();
            hearts.ConsumeOne();

            Assert.IsFalse(hearts.IsUnlimited.CurrentValue);
            Assert.AreEqual(TimeSpan.Zero, hearts.UnlimitedTimeLeft.CurrentValue);
            Assert.AreEqual(2, hearts.Current.CurrentValue);
        }

        [Test]
        public void UpdateTick_KhiDayTim_VanDemNguocVoHan()
        {
            // Đầy tim thì UpdateTick bỏ qua phần hồi tim — nhưng không được bỏ qua đồng hồ vô hạn.
            var (hearts, time, _) = Build(heartsStart: 5);
            hearts.AddUnlimited(TimeSpan.FromMinutes(60));

            time.UtcNow = T0.AddMinutes(15);
            hearts.UpdateTick();

            Assert.AreEqual(TimeSpan.FromMinutes(45), hearts.UnlimitedTimeLeft.CurrentValue);
        }

        [Test]
        public void MoLaiApp_DocMocHetHanTuDia()
        {
            var (hearts, time, save) = Build();
            hearts.AddUnlimited(TimeSpan.FromMinutes(60));
            Assert.Greater(save.ImmediateCalls, 0, "mốc hết hạn phải ghi đĩa ngay — mua bằng tiền thật");

            time.UtcNow = T0.AddMinutes(20);
            var reopened = new HeartService(save, time, Settings);
            Assert.IsTrue(reopened.IsUnlimited.CurrentValue);
            Assert.AreEqual(TimeSpan.FromMinutes(40), reopened.UnlimitedTimeLeft.CurrentValue);

            time.UtcNow = T0.AddMinutes(90);
            var expired = new HeartService(save, time, Settings);
            Assert.IsFalse(expired.IsUnlimited.CurrentValue);
        }

        [Test]
        public void AddUnlimited_ThoiLuongKhongDuong_BoQua()
        {
            var (hearts, _, save) = Build();

            hearts.AddUnlimited(TimeSpan.Zero);
            hearts.AddUnlimited(TimeSpan.FromMinutes(-5));

            Assert.IsFalse(hearts.IsUnlimited.CurrentValue);
            Assert.AreEqual(0, save.ImmediateCalls);
        }

        [Test]
        public void GoiShop_UnlimitedHeart2_QuaDispatcher_Thanh2GioVoHan()
        {
            // Đúng đường gói combo đi: ShopReward(UnlimitedHeart, 2) → ToItemId → dispatcher. Amount là GIỜ.
            var (hearts, _, _) = Build(heartsStart: 0);
            var dispatcher = new TransactionItemDispatcher(hearts);

            dispatcher.Grant(ResourceType.UnlimitedHeart.ToItemId(), 2);

            Assert.IsTrue(hearts.IsUnlimited.CurrentValue);
            Assert.AreEqual(TimeSpan.FromHours(2), hearts.UnlimitedTimeLeft.CurrentValue);
            Assert.AreEqual(0, hearts.Current.CurrentValue, "vô hạn không cộng tim — chỉ không trừ");
        }

        // --- helpers ------------------------------------------------------------

        private static readonly HeartSettings Settings = new HeartSettings(5, TimeSpan.FromMinutes(30));

        private static (HeartService, FakeTime, FakeSave) Build(int heartsStart = 5)
        {
            var time = new FakeTime { UtcNow = T0 };
            var save = new FakeSave(new HeartData { Hearts = heartsStart, LastRegenUtcTicks = T0.Ticks });
            var hearts = new HeartService(save, time, Settings);
            save.ImmediateCalls = 0;   // bỏ các lần ghi lúc khởi tạo, chỉ đếm từ thao tác của test
            return (hearts, time, save);
        }

        private sealed class FakeTime : ITimeProvider
        {
            public DateTime UtcNow { get; set; }
        }

        private sealed class FakeSave : ISaveManager
        {
            private readonly HeartData _data;
            public int ImmediateCalls;

            public FakeSave(HeartData data) => _data = data;

            public void Register<T>(IStorageProvider provider, string key) where T : class, new() { }
            public T Load<T>() where T : class, new() => _data as T;
            public void Save<T>(T data) where T : class, new() { }
            public void SaveImmediate<T>(T data) where T : class, new() => ImmediateCalls++;
            public void SaveAll() { }
            public void DeleteDomain<T>() where T : class, new() { }
            public bool HasDomain<T>() where T : class, new() => true;
        }
    }
}
