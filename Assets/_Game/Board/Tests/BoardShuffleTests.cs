// Luật booster Shuffle. Level viết thẳng trong file như BoardRulesTests: test luật thì
// không được phụ thuộc file level ship, sửa level sẽ làm test đỏ mà luật chẳng sai gì.
//
// Chỉ Parse + Build, KHÔNG gọi Validate — Validate đòi level có cả thẻ chỉ-ảnh lẫn thẻ
// chỉ-chữ, thứ không liên quan gì tới Shuffle.
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class BoardShuffleTests
    {
        static Game Build(string json) { return Game.Build(LevelData.Parse(json)); }

        // Stack 0 top: a1,a2 cùng nhóm ga → thành cụm → có màu. b1 đứng lẻ → trắng. 1 ô trống.
        // Stack 1 top: b2,b3. Stack 2: a3 ở trên, hộp dưới a4,b4,c1,c2.
        const string Lv = @"{
          ""id"":""s-base"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",""b1"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""b2"",""b3"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""a3"",null,null,null] },
                                          { ""slots"":[""a4"",""b4"",""c1"",""c2""] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              {""id"":""a1"",""text"":""A1""},{""id"":""a2"",""text"":""A2""},
              {""id"":""a3"",""text"":""A3""},{""id"":""a4"",""text"":""A4""} ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              {""id"":""b1"",""text"":""B1""},{""id"":""b2"",""text"":""B2""},
              {""id"":""b3"",""text"":""B3""},{""id"":""b4"",""text"":""B4""} ]},
            { ""id"":""gc"", ""text"":""C"", ""cards"":[
              {""id"":""c1"",""text"":""C1""},{""id"":""c2"",""text"":""C2""},
              {""id"":""c3"",""text"":""C3""},{""id"":""c4"",""text"":""C4""} ]}
          ]}
        }";

        [Test]
        public void TheDungLeLaTrang_TheThanhCumLaCoMau()
        {
            var g = Build(Lv);
            Box top0 = g.TopBox(0);

            Assert.IsFalse(Game.IsWhite(top0, 0), "a1 có a2 cùng hộp → thành cụm → có màu");
            Assert.IsFalse(Game.IsWhite(top0, 1), "a2 tương tự");
            Assert.IsTrue(Game.IsWhite(top0, 2), "b1 đứng lẻ trong hộp → trắng");
            Assert.IsFalse(Game.IsWhite(top0, 3), "ô trống không phải thẻ trắng");
        }

        [Test]
        public void DemDungTongTheOLopTrenCung()
        {
            // stack 0: 3 thẻ, stack 1: 2 thẻ, stack 2 top: 1 thẻ. Hộp dưới KHÔNG tính.
            Assert.AreEqual(6, Build(Lv).TopLayerTileCount());
        }

        [Test]
        public void PhatHienDuocHopDuBonTheCungNhomOMoiLayer()
        {
            Assert.IsFalse(Build(Lv).AnyBoxHasFullGroup(), "level nền không hộp nào đủ 4");

            var g = Build(Lv);
            // Nhét 4 thẻ gc vào hộp bị chôn của stack 2 — SettleStep không soi hộp chôn,
            // nhưng bất biến của Shuffle phải bắt được.
            Box buried = g.Stacks[2].Boxes[1];
            for (int i = 0; i < buried.Slots.Length; i++)
                buried.Slots[i] = new Tile { Uid = "z" + i, CardId = "c" + i, GroupId = "gc" };

            Assert.IsTrue(g.AnyBoxHasFullGroup(), "4 thẻ gc trong hộp bị chôn phải bị bắt");
        }

        [Test]
        public void CanShuffleTheoOTrongOLopTren()
        {
            Assert.IsTrue(Build(Lv).CanShuffle(), "lớp trên còn ô trống");

            var g = Build(Lv);
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                Box top = g.TopBox(s);
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null)
                        top.Slots[i] = new Tile { Uid = "f" + s + i, CardId = "f" + s + i, GroupId = "gz" };
            }
            Assert.IsFalse(g.CanShuffle(), "lớp trên đầy kín → không tạo được ô trống cho hộp chủ");
        }
    }
}
