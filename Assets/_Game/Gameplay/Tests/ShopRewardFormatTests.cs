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
            Assert.AreEqual("2000", ShopRewardItemView.FormatAmount(ResourceType.Coin, 2000));
        }

        [Test]
        public void Item_Dang_x()
        {
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.BoosterShuffle, 5));
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.Heart, 5));
        }

        [Test]
        public void TimVoHan_TheoGio()
        {
            Assert.AreEqual("1h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 1));
            Assert.AreEqual("24h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 24));
        }
    }
}
