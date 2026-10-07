// Feedback #4: người chơi đi tiếp ở hộp khác trong lúc cascade. View chỉ khoá hộp đang diễn +
// hộp chờ lượt (Game.SettlePending); domain phải chịu được nước đi chen giữa hai SettleStep.
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class BoardSettleInputTests
    {
        // Stack 0: hộp trên đủ 4 fruit (chờ nổ) + hộp đáy có dog.
        // Stack 1: cat, bear. Stack 2: fox. Nhóm animal = dog, cat, bear, fox.
        const string Lv = @"{
          ""id"":""t-settle-input"", ""title"":""t"",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""apple"",""banana"",""orange"",""pear""] },
                                          { ""slots"":[""dog"",null,null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""cat"",""bear"",null,null] },
                                      { ""slots"":[null,null,null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""fox"",null,null,null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""fruit"", ""text"":""Fruit"", ""cards"":[
              { ""id"":""apple"",""text"":""Apple"" },{ ""id"":""banana"",""text"":""Banana"" },
              { ""id"":""orange"",""text"":""Orange"" },{ ""id"":""pear"",""text"":""Pear"" } ]},
            { ""id"":""animal"", ""text"":""Animal"", ""cards"":[
              { ""id"":""dog"",""text"":""Dog"" },{ ""id"":""cat"",""text"":""Cat"" },
              { ""id"":""bear"",""text"":""Bear"" },{ ""id"":""fox"",""text"":""Fox"" } ]}
          ]}
        }";

        const bool Drain = Rules.RemoveEmptyNonBottomBox;

        static Game Load() { return Game.Build(LevelData.Parse(Lv)); }

        static string UidAt(Game g, int stack, int slot) { return g.TopBox(stack).Slots[slot].Uid; }

        [Test]
        public void SettlePending_FollowsSettleStep()
        {
            var g = Load();
            Assert.IsTrue(g.SettlePending(0, Drain), "hộp đủ nhóm đang chờ nổ");
            Assert.IsFalse(g.SettlePending(1, Drain), "hộp thường");
            Assert.IsFalse(g.SettlePending(2, Drain), "hộp thường");

            var clear = g.SettleStep(Drain);
            Assert.AreEqual(SettleKind.Clear, clear.Kind);
            Assert.IsTrue(clear.BoxRemoved, "hộp vừa nổ rỗng thì bị lấy đi ngay trong cùng bước");
            Assert.IsFalse(g.SettlePending(0, Drain), "giờ là hộp đáy có dog — không còn gì chờ");

            // Rút hết thẻ khỏi hộp trên của stack 1 → hộp rỗng, không phải đáy → chờ bị lấy đi.
            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 0), 2));
            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 1), 2));
            Assert.IsTrue(g.SettlePending(1, Drain), "hộp rỗng chờ bị lấy đi");

            var remove = g.SettleStep(Drain);
            Assert.IsTrue(remove.BoxRemoved);
            Assert.AreEqual(1, remove.Stack);
            Assert.IsFalse(g.SettlePending(1, Drain), "còn hộp đáy rỗng — đáy không bị lấy đi");
            Assert.AreEqual(SettleKind.None, g.SettleStep(Drain).Kind);
        }

        [Test]
        public void MovesBetweenSettleSteps_ChainIntoTheSameCascade()
        {
            var g = Load();
            Assert.AreEqual(SettleKind.Clear, g.SettleStep(Drain).Kind, "fruit nổ");

            // Người chơi đi tiếp trong lúc hộp 0 đang diễn — hai hộp kia không bận.
            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 0), 2), "cat sang stack 2");
            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 1), 2), "bear sang stack 2");
            Assert.IsTrue(g.MoveTile(0, UidAt(g, 0, 0), 2), "dog (đã lộ) sang stack 2 → đủ animal");
            Assert.IsTrue(g.SettlePending(2, Drain));

            g.Settle(Drain);
            Assert.AreEqual(2, g.Cleared, "nhóm tạo giữa chuỗi nổ nối luôn");
            Assert.AreEqual(3, g.Moves);
            Assert.AreEqual(GameStatus.Won, g.Status);
        }
    }
}
