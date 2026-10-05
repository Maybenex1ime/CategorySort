// Quay vòng level khi chơi hết catalog (LevelCatalog.ToCatalogIndex).
using LogosGame.Features.Gameplay.Content;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    public class LevelCatalogLoopTests
    {
        [Test]
        public void PlaysStraightThenLoopsFromLevel6()
        {
            for (int i = 0; i < 12; i++)
                Assert.AreEqual(i, LevelCatalog.ToCatalogIndex(i, 12, 6), "chưa hết catalog thì đi thẳng");

            Assert.AreEqual(5, LevelCatalog.ToCatalogIndex(12, 12, 6), "màn 13 = màn 6");
            Assert.AreEqual(11, LevelCatalog.ToCatalogIndex(18, 12, 6), "màn 19 = màn 12");
            Assert.AreEqual(5, LevelCatalog.ToCatalogIndex(19, 12, 6), "màn 20 = màn 6 lần nữa");
        }

        [Test]
        public void EdgeCases()
        {
            Assert.AreEqual(0, LevelCatalog.ToCatalogIndex(12, 12, 1), "loop từ màn 1 = quay về đầu");
            Assert.AreEqual(11, LevelCatalog.ToCatalogIndex(30, 12, 99), "loop vượt số màn → lặp màn cuối");
            Assert.AreEqual(0, LevelCatalog.ToCatalogIndex(-3, 12, 6), "index âm → màn đầu");
            Assert.AreEqual(0, LevelCatalog.ToCatalogIndex(5, 0, 6), "catalog rỗng không chia cho 0");
        }
    }
}
