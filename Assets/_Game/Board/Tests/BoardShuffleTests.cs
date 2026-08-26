// Luật booster Shuffle. Level viết thẳng trong file như BoardRulesTests: test luật thì
// không được phụ thuộc file level ship, sửa level sẽ làm test đỏ mà luật chẳng sai gì.
//
// Chỉ Parse + Build, KHÔNG gọi Validate — Validate đòi level có cả thẻ chỉ-ảnh lẫn thẻ
// chỉ-chữ, thứ không liên quan gì tới Shuffle.
using System.Collections.Generic;
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

        // Stack 0 top: 3 thẻ ga + 1 ô trống. Stack 1 top: a4 + b1. Stack 2 top: 3 thẻ gb + ô trống.
        // Cả ga lẫn gb đều là Nhóm mồi 3+1 sẵn có.
        const string LvPrimed = @"{
          ""id"":""s-primed"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",""a3"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""a4"",""b1"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""b2"",""b3"",""b4"",null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              {""id"":""a1"",""text"":""A1""},{""id"":""a2"",""text"":""A2""},
              {""id"":""a3"",""text"":""A3""},{""id"":""a4"",""text"":""A4""} ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              {""id"":""b1"",""text"":""B1""},{""id"":""b2"",""text"":""B2""},
              {""id"":""b3"",""text"":""B3""},{""id"":""b4"",""text"":""B4""} ]}
          ]}
        }";

        [Test]
        public void DemDungNhomMoiDangCoSan()
        {
            // ga: 3 thẻ ở stack 0 + hộp đó còn ô trống + thẻ thứ 4 ở stack 1 → nhóm mồi.
            // gb: 3 thẻ ở stack 2 + còn ô trống + thẻ thứ 4 ở stack 1 → cũng là nhóm mồi.
            Assert.AreEqual(2, Build(LvPrimed).CountPrimedGroups());
        }

        [Test]
        public void HopDayThiKhongTinhLaNhomMoi()
        {
            var g = Build(LvPrimed);
            // Lấp nốt ô trống của stack 0 → người chơi không thả thẻ thứ 4 vào được nữa.
            g.TopBox(0).Slots[3] = new Tile { Uid = "z", CardId = "z", GroupId = "gz" };

            Assert.AreEqual(1, g.CountPrimedGroups(), "chỉ còn gb là nhóm mồi hợp lệ");
        }

        [Test]
        public void LoaiNhomChuaDuBonTheTrenBan()
        {
            // gc chỉ có c1, c2 nằm trên bàn (c3, c4 không có trong layout) → không dựng nổi.
            List<string> picks = Build(Lv).PickPrimeCandidates(3);

            CollectionAssert.DoesNotContain(picks, "gc", "nhóm chưa đủ 4 thẻ thật thì không dựng nổi");
            CollectionAssert.Contains(picks, "ga");
            CollectionAssert.Contains(picks, "gb");
        }

        [Test]
        public void PickPrimeCandidatesRaKetQuaXacDinh()
        {
            // Cùng một bàn phải luôn ra cùng thứ tự, không thì test không lặp lại được.
            CollectionAssert.AreEqual(Build(Lv).PickPrimeCandidates(3), Build(Lv).PickPrimeCandidates(3));
            Assert.AreEqual(1, Build(Lv).PickPrimeCandidates(1).Count, "max cắt đúng số lượng");
        }

        // ga có đủ 4 thẻ ĐẾN ĐƯỢC: a1/a2/a3 trắng ở lớp trên, a4 nằm trong hộp bị chôn của
        // stack 0. Stack 0 có layer 2 đầy 4 thẻ → phải được chọn làm hộp chủ.
        const string LvPrime = @"{
          ""id"":""s-prime"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""b1"",null,null] },
                                          { ""slots"":[""a4"",""c1"",""c2"",""b2""] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""a2"",""b3"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""a3"",null,null,null] } ] }
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
        public void OKhaDungChiGomTheTrangVaOTrong()
        {
            var g = Build(Lv);
            List<Game.SlotRef> pool = g.AssignableTopSlots();

            Assert.IsFalse(pool.Exists(r => r.Stack == 0 && r.Slot == 0), "a1 có màu → không đụng");
            Assert.IsFalse(pool.Exists(r => r.Stack == 0 && r.Slot == 1), "a2 có màu → không đụng");
            Assert.IsTrue(pool.Exists(r => r.Stack == 0 && r.Slot == 2), "b1 trắng");
            Assert.IsTrue(pool.Exists(r => r.Stack == 0 && r.Slot == 3), "ô trống dùng để dịch chỗ");
            Assert.IsTrue(pool.TrueForAll(r => r.Box == 0), "chỉ lớp trên cùng");
        }

        [Test]
        public void BaPhaGiuDuBatBienVaKhongDeNhomMoiBiXoa()
        {
            var g = Build(LvPrime);
            int before = g.TopLayerTileCount();
            Assert.AreEqual(5, before);

            List<Game.SlotRef> pool = g.AssignableTopSlots();
            var reserved = new HashSet<int>();
            var hand = new List<Tile>();
            g.DrainAll(pool, hand);
            Assert.AreEqual(5, hand.Count, "nhấc hết thẻ trắng vào tay");

            Assert.IsTrue(g.TryPrimeGroup("ga", pool, reserved, hand));
            // Seed chạy TRƯỚC cluster: cluster reserve hết ô trống, chạy sau thì không còn
            // thẻ nào mượn được cho hộp rỗng và cả lượt bị rollback oan.
            Assert.IsTrue(g.EnsureEveryTopBoxOccupied(pool, reserved, hand));
            g.ClusterHand(pool, reserved, hand);

            Assert.AreEqual(0, hand.Count, "không thẻ nào bị bỏ lại trong tay");
            Assert.AreEqual(before, g.TopLayerTileCount(), "bất biến 1: tổng lớp trên");
            Assert.IsFalse(g.AnyBoxHasFullGroup(), "bất biến 3: không hộp nào đủ 4");
            Assert.GreaterOrEqual(g.CountPrimedGroups(), 1,
                "pha sau KHÔNG được xoá hoặc lấp Nhóm mồi pha A vừa dựng");

            for (int s = 0; s < g.Stacks.Count; s++)
            {
                int n = 0;
                foreach (Tile t in g.TopBox(s).Slots) if (t != null) n++;
                Assert.Greater(n, 0, "bất biến 2: top box rỗng sẽ bị SettleStep xoá");
            }
        }

        [Test]
        public void HopChuLaStackCoLayer2NhieuTheNhat()
        {
            var g = Build(LvPrime);
            List<Game.SlotRef> pool = g.AssignableTopSlots();
            var reserved = new HashSet<int>();
            var hand = new List<Tile>();
            g.DrainAll(pool, hand);

            g.TryPrimeGroup("ga", pool, reserved, hand);

            int gaInStack0 = 0, free = 0;
            foreach (Tile t in g.TopBox(0).Slots)
            {
                if (t == null) free++;
                else if (t.GroupId == "ga") gaInStack0++;
            }
            Assert.AreEqual(3, gaInStack0, "stack 0 có layer 2 đầy 4 thẻ → phải làm hộp chủ");
            Assert.GreaterOrEqual(free, 1, "hộp chủ phải còn ô trống cho người chơi thả thẻ thứ 4");
        }
    }
}
