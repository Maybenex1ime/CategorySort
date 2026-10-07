// Thứ tự file level trong catalog (LevelCatalogBuilder.NaturalCompare): sắp sai là "Level N" trong game lệch file.
using System;
using NUnit.Framework;
using WordStack.Meta.Editor;

namespace WordStack.Meta.Tests
{
    public class LevelCatalogOrderTests
    {
        [Test]
        public void SortsLevelFilesByNumberNotText()
        {
            var paths = new[] { "Levels/Level_10.json", "Levels/Level_2.json", "Levels/Level_1.json", "Levels/Level_21.json", "Levels/Level_3.json" };
            Array.Sort(paths, LevelCatalogBuilder.NaturalCompare);
            CollectionAssert.AreEqual(
                new[] { "Levels/Level_1.json", "Levels/Level_2.json", "Levels/Level_3.json", "Levels/Level_10.json", "Levels/Level_21.json" },
                paths);
        }
    }
}
