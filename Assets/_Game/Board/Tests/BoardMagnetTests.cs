// Luật booster Nam châm: chọn mục tiêu + hút trọn nhóm + chỗ đặt thẻ cha.
//
// Level viết thẳng trong file (như BoardRulesTests): test luật thì không được phụ thuộc
// file level ship, sửa level sẽ làm test đỏ mà luật chẳng sai gì. Chỉ Parse + Build, KHÔNG
// gọi Validate — Validate đòi level phải có thẻ chỉ-ảnh lẫn thẻ chỉ-chữ, thứ không liên
// quan gì tới nam châm.
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class BoardMagnetTests
    {
        static Game Build(string json) { return Game.Build(LevelData.Parse(json)); }

        // ga: 2 thẻ ở top stack 0 + 2 ở top stack 1 = 4 ở lớp trên.
        // gb: 2 ở top stack 2 + 2 bị chôn.
        const string LvTop = @"{
          ""id"":""m-top"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""a3"",""a4"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""b1"",""b2"",null,null] },
                                          { ""slots"":[""b3"",""b4"",null,null] } ] }
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
        public void ChonNhomCoNhieuTheONhatOLopTren()
        {
            var g = Build(LvTop);
            Assert.AreEqual("ga", g.FindMagnetTarget(), "ga có 4 thẻ ở lớp trên, gb chỉ có 2");
        }

        [Test]
        public void HutXongKhongXoaHopVaKhongTinhNuocDi()
        {
            var g = Build(LvTop);
            MagnetResult r = g.ApplyMagnet();

            Assert.IsTrue(r.Ok);
            Assert.AreEqual(4, r.Picks.Length);
            Assert.AreEqual(1, g.Cleared, "hút trọn một nhóm là clear một nhóm");
            Assert.AreEqual(0, g.Moves, "booster KHÔNG tính là một nước đi");
            Assert.IsNull(r.NewTileUid, "ga là nhóm gốc nên không đẻ thẻ cha");

            // Nam châm chỉ được null ô, việc dọn hộp để Settle làm — tự xoá ở đây là
            // CheckStatus() vấp st.Boxes[0] trên stack rỗng.
            Assert.AreEqual(1, g.Stacks[0].Boxes.Count);
            Assert.AreEqual(1, g.Stacks[1].Boxes.Count);
            Assert.AreEqual(2, g.Stacks[2].Boxes.Count);
            Assert.IsNull(g.Stacks[0].Boxes[0].Slots[0]);
            Assert.IsNull(g.Stacks[1].Boxes[0].Slots[0]);
        }

        // ga: 3 thẻ ở top stack 0, thẻ thứ 4 bị chôn ở stack 1 hộp 1.
        // gb: chỉ 2 thẻ ở lớp trên → thua ga.
        const string LvDig = @"{
          ""id"":""m-dig"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",""a3"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""b1"",null,null,null] },
                                          { ""slots"":[""a4"",null,null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""b2"",null,null,null] },
                                          { ""slots"":[""b3"",""b4"",null,null] } ] }
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
        public void MoiDuocTheDangBiChon()
        {
            var g = Build(LvDig);
            MagnetResult r = g.ApplyMagnet();

            Assert.IsTrue(r.Ok);
            Assert.AreEqual("ga", r.GroupId);
            Assert.AreEqual(0, r.TargetStack, "hộp hội tụ là top box giữ nhiều thẻ ga nhất");

            bool dugFromBuried = false;
            foreach (MagnetPick p in r.Picks)
                if (p.Box > 0) dugFromBuried = true;
            Assert.IsTrue(dugFromBuried, "a4 nằm ở hộp bị chôn, phải moi lên");

            Assert.IsNull(g.Stacks[1].Boxes[1].Slots[0], "a4 phải bị lấy khỏi hộp chôn");
            Assert.AreEqual(1, g.Cleared);
        }

        // root = 3 thẻ + 1 nhóm con (leaf) = 4 thành viên, nhưng mới có 3 THẺ trên bàn.
        // Cả root lẫn leaf đều có 3 thẻ ở lớp trên — root phải bị loại vì chưa đủ 4 thẻ.
        const string LvParent = @"{
          ""id"":""m-parent"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""r1"",""r2"",""r3"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""l1"",""l2"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""l3"",null,null,null] },
                                          { ""slots"":[""l4"",null,null,null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""leaf"", ""text"":""Leaf"", ""group"":""root"", ""cards"":[
              {""id"":""l1"",""text"":""L1""},{""id"":""l2"",""text"":""L2""},
              {""id"":""l3"",""text"":""L3""},{""id"":""l4"",""text"":""L4""} ]},
            { ""id"":""root"", ""text"":""Root"", ""cards"":[
              {""id"":""r1"",""text"":""R1""},{""id"":""r2"",""text"":""R2""},
              {""id"":""r3"",""text"":""R3""} ]}
          ]}
        }";

        [Test]
        public void LoaiNhomChaConChoNhomConCollapse()
        {
            var g = Build(LvParent);
            Assert.AreEqual("leaf", g.FindMagnetTarget(),
                "root cũng có 3 thẻ ở lớp trên nhưng mới 3/4 thành viên là thẻ — không moi được thứ chưa tồn tại");
        }

        // gx (gốc) và gc (con của gp) đều có 2 thẻ ở lớp trên, cùng độ chôn.
        // Hoà thì luật 1 vào cuộc: ưu tiên nhóm KHÔNG đẻ thẻ cha.
        const string LvRootFirst = @"{
          ""id"":""m-root-first"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""x1"",""x2"",null,null] },
                                          { ""slots"":[""x3"",""x4"",null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""c1"",""c2"",null,null] },
                                          { ""slots"":[""c3"",""c4"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""p1"",""p2"",""p3"",null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""gx"", ""text"":""X"", ""cards"":[
              {""id"":""x1"",""text"":""X1""},{""id"":""x2"",""text"":""X2""},
              {""id"":""x3"",""text"":""X3""},{""id"":""x4"",""text"":""X4""} ]},
            { ""id"":""gc"", ""text"":""C"", ""group"":""gp"", ""cards"":[
              {""id"":""c1"",""text"":""C1""},{""id"":""c2"",""text"":""C2""},
              {""id"":""c3"",""text"":""C3""},{""id"":""c4"",""text"":""C4""} ]},
            { ""id"":""gp"", ""text"":""P"", ""cards"":[
              {""id"":""p1"",""text"":""P1""},{""id"":""p2"",""text"":""P2""},
              {""id"":""p3"",""text"":""P3""} ]}
          ]}
        }";

        [Test]
        public void HoaSoTheOTopThiUuTienNhomKhongDeTheCha()
        {
            var g = Build(LvRootFirst);
            Assert.AreEqual("gx", g.FindMagnetTarget(),
                "gx và gc cùng 2 thẻ ở lớp trên; gx là nhóm gốc nên clear được -4 thẻ, gc chỉ -3");
        }

        // gc đủ 4 thẻ trong hộp hội tụ (stack 0). Thẻ cha PHẢI vào stack 1 vì hộp đó
        // đang giữ 2 thẻ nhóm cha, chứ không rơi về hộp hội tụ.
        const string LvHost = @"{
          ""id"":""m-host"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""c1"",""c2"",""c3"",""c4""] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""p1"",""p2"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""p3"",null,null,null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""gc"", ""text"":""C"", ""group"":""gp"", ""cards"":[
              {""id"":""c1"",""text"":""C1""},{""id"":""c2"",""text"":""C2""},
              {""id"":""c3"",""text"":""C3""},{""id"":""c4"",""text"":""C4""} ]},
            { ""id"":""gp"", ""text"":""P"", ""cards"":[
              {""id"":""p1"",""text"":""P1""},{""id"":""p2"",""text"":""P2""},
              {""id"":""p3"",""text"":""P3""} ]}
          ]}
        }";

        [Test]
        public void TheChaVaoHopDangCoSanNhieuTheNhomChaNhat()
        {
            var g = Build(LvHost);
            MagnetResult r = g.ApplyMagnet();

            Assert.IsTrue(r.Ok);
            Assert.AreEqual("gc", r.GroupId);
            Assert.AreEqual(0, r.TargetStack, "hộp hội tụ vẫn là stack 0");
            Assert.IsNotNull(r.NewTileUid, "gc có nhóm cha nên phải đẻ thẻ cha");
            Assert.AreEqual(1, r.NewTileStack,
                "stack 1 đang giữ 2 thẻ gp — nhiều nhất, nên nhận thẻ cha thay vì hộp hội tụ");

            Tile placed = null;
            foreach (Tile t in g.Stacks[1].Boxes[0].Slots)
                if (t != null && t.Uid == r.NewTileUid) placed = t;

            Assert.IsNotNull(placed, "thẻ cha phải nằm trong hộp trên cùng của stack 1");
            Assert.AreEqual("gp", placed.GroupId, "thẻ cha thuộc nhóm cha");
            Assert.AreEqual("gc", placed.CardId, "thẻ cha mang mặt của nhóm vừa gộp");
            Assert.IsTrue(g.Stacks[1].Boxes[0].HadCollapse, "hộp nhận phải được đánh dấu đã collapse");
        }

        [Test]
        public void ThemThechaXongNhomChaDuBoThiSettleNoLuon()
        {
            var g = Build(LvHost);
            g.ApplyMagnet();

            // gp mới có p1, p2 + thẻ cha = 3/4 (p3 còn ở stack 2) nên chưa nổ.
            g.Settle(Rules.RemoveEmptyNonBottomBox);
            Assert.AreEqual(1, g.Cleared, "chỉ gc được gom, gp còn thiếu p3");

            // Kéo nốt p3 sang là đủ bộ → Settle nổ nhóm cha.
            Tile p3 = g.Stacks[2].Boxes[0].Slots[0];
            Assert.IsTrue(g.MoveTile(2, p3.Uid, 1), "p3 phải kéo được sang stack 1");
            g.Settle(Rules.RemoveEmptyNonBottomBox);
            Assert.AreEqual(2, g.Cleared, "gp đủ bộ 4 → clear tiếp");
        }

        [Test]
        public void MaNhomLaThiTuChoiVaKhongDungVaoBan()
        {
            var g = Build(LvTop);
            MagnetResult r = g.ApplyMagnet("khong-co-nhom-nay");

            Assert.IsFalse(r.Ok);
            Assert.AreEqual(0, g.Cleared, "từ chối thì không được đụng gì vào bàn");
            Assert.IsNotNull(g.Stacks[0].Boxes[0].Slots[0]);
        }
    }
}
