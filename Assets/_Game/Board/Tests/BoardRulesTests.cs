// Lưới an toàn mỏng cho luật bàn chơi: nước đi hợp lệ/bị từ chối, CLEAR + xoá hộp,
// cascade tới thắng, kẹt. Bộ kiểm ĐẦY ĐỦ (validate 15 ca, solver, collapse, màu) nằm ở
// Domain/SelfCheck.cs và chạy bằng ./selfcheck.sh — đừng chép nó xuống đây.
//
// Level dùng ở đây viết thẳng trong file: test luật thì không được phụ thuộc file level
// ship, vì sửa level sẽ làm test đỏ mà luật chẳng sai gì.
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class BoardRulesTests
    {
        // Stack 0: hộp trên 3 thẻ fruit + hộp đáy 1 thẻ. Stack 1: hộp đáy có thẻ fruit thứ 4
        // + 1 chỗ trống. Kéo thẻ fruit cuối sang stack 0 → đủ 4 → CLEAR → hộp trên rỗng bị
        // xoá → hộp đáy lộ ra.
        const string Lv = @"{
          ""id"":""t-rules"", ""title"":""t"",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""apple"",""banana"",""orange"",null] },
                                          { ""slots"":[""dog"",null,null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""pear"",""cat"",""bear"",null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""fruit"", ""text"":""Fruit"", ""cards"":[
              { ""id"":""apple"",""text"":""Apple"" },{ ""id"":""banana"",""text"":""Banana"" },
              { ""id"":""orange"",""text"":""Orange"" },{ ""id"":""pear"",""art"":""apple"" } ]},
            { ""id"":""animal"", ""text"":""Animal"", ""cards"":[
              { ""id"":""dog"",""text"":""Dog"" },{ ""id"":""cat"",""text"":""Cat"" },
              { ""id"":""bear"",""text"":""Bear"" },{ ""id"":""fox"",""text"":""Fox"" } ]}
          ]}
        }";

        static Game Load()
        {
            var lv = LevelData.Parse(Lv);
            // Level test cố tình thiếu thẻ "fox" trên bàn nên KHÔNG gọi Validate — nó kiểm
            // tính toàn vẹn của level ship, không phải luật vận hành.
            return Game.Build(lv);
        }

        static string UidAt(Game g, int stack, int slot) { return g.TopBox(stack).Slots[slot].Uid; }

        [Test]
        public void MoveTile_VaoSlotTrongDauTien()
        {
            var g = Load();
            string uid = UidAt(g, 1, 0);
            Assert.IsTrue(g.MoveTile(1, uid, 0), "kéo sang stack khác còn chỗ phải được");
            Assert.AreEqual(uid, g.TopBox(0).Slots[3].Uid, "phải vào slot trống ĐẦU TIÊN");
            Assert.AreEqual(1, g.Moves);
        }

        [Test]
        public void MoveTile_TuChoiNuocSai()
        {
            var g = Load();
            string uid = UidAt(g, 0, 0);
            Assert.IsFalse(g.MoveTile(0, uid, 0), "thả về chính stack cũ");

            string buried = g.Stacks[0].Boxes[1].Slots[0].Uid;
            Assert.IsFalse(g.MoveTile(0, buried, 1), "thẻ trong hộp bị che");

            g.TopBox(1).Slots[3] = new Tile { Uid = "filler", CardId = "x", GroupId = "x" };
            Assert.IsFalse(g.MoveTile(0, uid, 1), "hộp đích đầy");
            Assert.AreEqual(0, g.Moves, "nước bị từ chối không được tính");
        }

        [Test]
        public void Clear_XoaHopKhongPhaiDay_LoHopDuoi()
        {
            var g = Load();
            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 0), 0));
            g.Settle(Rules.RemoveEmptyNonBottomBox);

            Assert.AreEqual(1, g.Cleared, "đủ 4 thẻ cùng group trong một hộp → CLEAR");
            Assert.AreEqual(1, g.Stacks[0].Boxes.Count, "hộp rỗng không phải đáy → bị xoá");
            Assert.AreEqual("dog", g.TopBox(0).Slots[0].CardId, "hộp đáy lộ ra");
        }

        [Test]
        public void Clear_OHopDay_HopOLaiVaRong()
        {
            var g = Load();
            var bottom = g.TopBox(1);                     // stack 1 chỉ có hộp đáy
            for (int i = 0; i < bottom.Slots.Length; i++)
                bottom.Slots[i] = new Tile { Uid = "b" + i, CardId = "c" + i, GroupId = "zz" };
            g.Settle(Rules.RemoveEmptyNonBottomBox);

            Assert.AreEqual(1, g.Stacks[1].Boxes.Count);
            Assert.IsTrue(Game.IsEmpty(g.TopBox(1)), "hộp đáy ở lại và rỗng");
        }

        [Test]
        public void Cascade_ClearNoiTiepNhauTrongMotLanSettle()
        {
            var g = Load();
            // Hộp đáy stack 0 nạp sẵn 3 thẻ animal; sau khi CLEAR fruit làm nó lộ ra, thẻ
            // animal thứ 4 bay sang là dây chuyền chạy tiếp mà không cần thêm nước đi.
            var buried = g.Stacks[0].Boxes[1];
            buried.Slots[1] = new Tile { Uid = "a2", CardId = "cat2", GroupId = "animal" };
            buried.Slots[2] = new Tile { Uid = "a3", CardId = "bear2", GroupId = "animal" };
            buried.Slots[3] = new Tile { Uid = "a4", CardId = "fox", GroupId = "animal" };

            Assert.IsTrue(g.MoveTile(1, UidAt(g, 1, 0), 0));
            g.Settle(Rules.RemoveEmptyNonBottomBox);

            Assert.AreEqual(2, g.Cleared, "một lần Settle phải chạy hết dây chuyền");
            Assert.IsTrue(Game.IsEmpty(g.TopBox(0)), "hộp đáy còn lại rỗng sau CLEAR thứ hai");
        }

        [Test]
        public void Status_ThangKhiSachBan_KetKhiMoiHopTrenDay()
        {
            var g = Load();
            foreach (var st in g.Stacks)
                foreach (var b in st.Boxes)
                    for (int i = 0; i < b.Slots.Length; i++) b.Slots[i] = null;
            Assert.AreEqual(GameStatus.Won, g.CheckStatus());

            var jam = Load();
            foreach (var st in jam.Stacks)
            {
                st.Boxes.RemoveRange(1, st.Boxes.Count - 1);
                for (int i = 0; i < st.Boxes[0].Slots.Length; i++)
                    st.Boxes[0].Slots[i] = new Tile
                    { Uid = st.X + "_" + i, CardId = "x", GroupId = "g" + i };   // mỗi thẻ một group
            }
            Assert.AreEqual(GameStatus.Stuck, jam.CheckStatus());
        }
    }
}
