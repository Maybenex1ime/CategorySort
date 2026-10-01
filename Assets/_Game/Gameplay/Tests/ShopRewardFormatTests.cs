using LogosGame.Features.Currency;
using LogosGame.Features.UI.Popups;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    public sealed class ShopRewardFormatTests
    {
        [Test]
        public void Coin_DinhDangNhom()
        {
            Assert.AreEqual(2000.ToString("N0"), ShopRewardItemView.FormatAmount(ResourceType.Coin, 2000));
        }

        [Test]
        public void Item_Dang_x()
        {
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.BoosterShuffle, 5));
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.Heart, 5));
        }

        [Test]
        public void TimVoHan_TheoGioHoacPhut()
        {
            Assert.AreEqual("1h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 60));
            Assert.AreEqual("2h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 120));
            Assert.AreEqual("30m", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 30));
            Assert.AreEqual("90m", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 90));
        }
    }
}
