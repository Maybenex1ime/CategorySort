// Nút + coin ở Home cuộn Shop tới Coin Packs (feedback #8): phép tính quãng cuộn.
using LogosGame.Features.UI.Popups;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    public class ShopScrollTests
    {
        [Test]
        public void ItemBelowTheTopScrollsDownByTheGap()
        {
            // Mục nằm thấp hơn mép trên khung 500 world, 1 world = 1 đơn vị content.
            Assert.AreEqual(500f, ShopPopup.ScrollYToShow(0f, 1000f, 1500f, 1f, 3000f), 1e-3f);
        }

        [Test]
        public void ConvertsWorldToContentUnits()
        {
            // Canvas thu nhỏ 0.5: 500 world = 1000 đơn vị content; đang cuộn sẵn 200.
            Assert.AreEqual(1200f, ShopPopup.ScrollYToShow(200f, 1000f, 1500f, 0.5f, 3000f), 1e-3f);
        }

        [Test]
        public void ClampsToTheScrollableRange()
        {
            Assert.AreEqual(800f, ShopPopup.ScrollYToShow(0f, 0f, 5000f, 1f, 800f), 1e-3f, "không cuộn quá đáy");
            Assert.AreEqual(0f, ShopPopup.ScrollYToShow(100f, 2000f, 1500f, 1f, 800f), 1e-3f, "không cuộn quá đỉnh");
            Assert.AreEqual(0f, ShopPopup.ScrollYToShow(0f, 0f, 5000f, 1f, -50f), 1e-3f, "nội dung ngắn hơn khung");
        }
    }
}
