using System;
using BoosterModule;
using LogosGame.Features.Currency;
using LogosGame.Features.Currency.Services.Impl;
using LogosGame.Features.Currency.UI;
using LogosSDK.Core.Events;
using NUnit.Framework;
using R3;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// ResourceType là định danh chung; các định danh cũ (BoosterId, ItemIds, mã giao dịch) chỉ
    /// được đổi qua ResourceTypeExtensions / TransactionIds. Map lệch là trao nhầm tài nguyên.
    /// </summary>
    public sealed class ResourceTypeTests
    {
        [Test]
        public void Booster_DoiQuaLai_KhongLech()
        {
            foreach (BoosterId id in Enum.GetValues(typeof(BoosterId)))
            {
                if (id == BoosterId.None)
                {
                    Assert.IsFalse(ResourceTypeExtensions.TryFromBooster(id, out _));
                    continue;
                }

                Assert.IsTrue(ResourceTypeExtensions.TryFromBooster(id, out ResourceType type), $"BoosterId.{id} chưa có ResourceType.");
                Assert.IsTrue(type.TryGetBoosterId(out BoosterId back));
                Assert.AreEqual(id, back);
            }

            Assert.IsFalse(ResourceType.Coin.TryGetBoosterId(out _));
            Assert.IsFalse(ResourceType.Heart.TryGetBoosterId(out _));
        }

        [Test]
        public void ItemId_DoiQuaLai_KhongLech()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                string itemId = type.ToItemId();
                if (type == ResourceType.Coin)
                {
                    Assert.IsNull(itemId, "coin vào ví trực tiếp, không có item id");
                    continue;
                }

                Assert.IsTrue(ResourceTypeExtensions.TryParseItemId(itemId, out ResourceType back));
                Assert.AreEqual(type, back);
            }

            Assert.IsFalse(ResourceTypeExtensions.TryParseItemId("khong_ton_tai", out _));
        }

        [Test]
        public void TransactionIds_MoiTaiNguyenMuaDuocBangCoin_CoMaRieng()
        {
            Assert.IsNull(TransactionIds.For(ResourceType.Coin), "không mua coin bằng coin");
            Assert.AreEqual("t_heart", TransactionIds.For(ResourceType.Heart));
            Assert.AreEqual("t_booster_shuffle", TransactionIds.For(ResourceType.BoosterShuffle));
            Assert.AreEqual("t_booster_magnet", TransactionIds.For(ResourceType.BoosterMagnet));
            Assert.AreEqual("t_booster_undo", TransactionIds.For(ResourceType.BoosterUndo));
        }

        [Test]
        public void ResourceService_Booster_CapNhatTheoInventoryChanged()
        {
            using var service = new ResourceService(null, null, null);
            ReadOnlyReactiveProperty<int> magnet = service.Observe(ResourceType.BoosterMagnet);

            Bus.Global.Fire(new BoosterInventoryChangedEvent(BoosterId.Magnet, 7));
            Bus.Global.Fire(new BoosterInventoryChangedEvent(BoosterId.Shuffle, 3));

            Assert.AreEqual(7, magnet.CurrentValue);
            Assert.AreEqual(3, service.Observe(ResourceType.BoosterShuffle).CurrentValue);
            Assert.AreSame(magnet, service.Observe(ResourceType.BoosterMagnet), "mỗi loại một nguồn, không tạo mới mỗi lần gọi");
        }

        [Test]
        public void ResourceService_ThieuVi_HoacTim_TraVe0_KhongNem()
        {
            using var service = new ResourceService(null, null, null);

            Assert.AreEqual(0, service.Observe(ResourceType.Coin).CurrentValue);
            Assert.AreEqual(0, service.Observe(ResourceType.Heart).CurrentValue);
        }
    }
}
