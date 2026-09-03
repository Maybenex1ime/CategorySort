// Bộ kiểm luật chạy NGOÀI Unity qua ./selfcheck.sh (entry point: SelfCheckMain.cs).
// Fail = throw. Mỗi assert đối chiếu 1-1 với demo/check.mjs — lệch chỗ nào là bug chỗ đó.
// EditMode test (Tests/BoardRulesTests.cs) chỉ bọc mấy ca chính; bộ đầy đủ nằm ở đây.
//
// KHÔNG import UnityEngine (xem Rules.cs).
using System;
using System.Collections.Generic;
using System.Linq;

namespace WordStack.Board
{
    public static class SelfCheck
    {
        static void Ok(bool cond, string msg) { if (!cond) throw new Exception(msg); }

        // Level mini cho COLLAPSE: leaf (4 thẻ, cha = root) đủ bộ NGAY trong hộp trên của
        // stack 0 → nổ ngay nhịp Settle đầu tiên. root (3 thẻ + 1 con) nằm ở stack 1.
        // Stack 0 có hộp đáy rỗng bên dưới để test luật xoá hộp sau collapse ở chế độ chặt.
        const string CollapseLv = @"{
          ""id"":""t-collapse"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""l1"",""l2"",""l3"",""l4""] },
                                          { ""slots"":[null,null,null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""r1"",""r2"",""r3"",null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""leaf"", ""text"":""Leaf"", ""group"":""root"", ""cards"":[
              { ""id"":""l1"",""text"":""L1"" },{ ""id"":""l2"",""text"":""L2"" },
              { ""id"":""l3"",""text"":""L3"" },{ ""id"":""l4"",""text"":""L4"" } ]},
            { ""id"":""root"", ""text"":""Root"", ""cards"":[
              { ""id"":""r1"",""text"":""R1"" },{ ""id"":""r2"",""text"":""R2"" },
              { ""id"":""r3"",""text"":""R3"" } ]}
          ]}
        }";

        // Level dựng riêng cho phần kiểm LUẬT (validate, nước đi, CLEAR, thắng/kẹt).
        // Trước đây mấy phần đó chạy trên level ship đầu tiên — đổi bố cục một level là test
        // đỏ dù luật chẳng sai gì (đúng lý do BoardRulesTests.cs đã tự chứa fixture từ đầu).
        // Hình dạng ở đây phục vụ đúng các giả định bên dưới:
        //   stack 0: hộp trên ĐÚNG 2 thẻ (dọn rỗng được) + hộp đáy có thẻ bị che
        //   stack 1: hộp đáy ĐẦY nhưng KHÔNG đủ bộ (thả vào phải bị từ chối, không CLEAR sẵn)
        //   stack 2: vừa có thẻ vừa còn chỗ (đích của nước đi hợp lệ)
        //   stack 4: rỗng hoàn toàn (chỗ trung chuyển)
        // Toàn thẻ chỉ-chữ nên không phụ thuộc thư mục art.
        const string RulesLv = @"{
          ""id"":""t-rules"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""c1"",""c2"",null,null] },
                                          { ""slots"":[""c3"",""c4"",null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""d1"",""d2"",""d3"",""e1""] } ] },
            { ""pos"":[0,1], ""boxes"":[ { ""slots"":[""d4"",""e2"",null,null] } ] },
            { ""pos"":[1,1], ""boxes"":[ { ""slots"":[""e3"",""e4"",null,null] } ] },
            { ""pos"":[0,2], ""boxes"":[ { ""slots"":[null,null,null,null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              { ""id"":""c1"",""text"":""C1"" },{ ""id"":""c2"",""text"":""C2"" },
              { ""id"":""c3"",""text"":""C3"" },{ ""id"":""c4"",""text"":""C4"" } ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              { ""id"":""d1"",""text"":""D1"" },{ ""id"":""d2"",""text"":""D2"" },
              { ""id"":""d3"",""text"":""D3"" },{ ""id"":""d4"",""text"":""D4"" } ]},
            { ""id"":""gc"", ""text"":""C"", ""cards"":[
              { ""id"":""e1"",""text"":""E1"" },{ ""id"":""e2"",""text"":""E2"" },
              { ""id"":""e3"",""text"":""E3"" },{ ""id"":""e4"",""text"":""E4"" } ]}
          ]}
        }";

        public static void Run(Action<string> log, IList<string> levelJsons, Predicate<string> hasArt)
        {
            Ok(levelJsons != null && levelJsons.Count >= 2, "cần ít nhất 2 file level");

            // ---- Preflight art: báo MỘT LẦN đủ danh sách file thiếu, thay vì chết ở
            // assert đầu tiên với một tên file duy nhất.
            var missing = new List<string>();
            foreach (var json in levelJsons)
                foreach (var key in LevelData.Parse(json).ArtKeys())
                    if (!hasArt(key) && !missing.Contains(key)) missing.Add(key);
            if (missing.Count > 0)
                throw new Exception("thiếu " + missing.Count + " file art: " +
                    string.Join(", ", missing.Select(k => k + ".png").ToArray()) +
                    " — thả vào Assets/_Game/Content/Resources/Art/");

            Func<int, LevelData> fresh = i => LevelData.Parse(levelJsons[i]);
            Func<LevelData> freshR = () => LevelData.Parse(RulesLv);
            Func<bool, Game> load = drain =>
            {
                var lv = freshR();
                lv.Validate(hasArt);
                return Game.Build(lv).Settle(drain);
            };

            // ---- 1. Validate bắt được level hỏng ----
            Action<Action<LevelData>, string, Predicate<string>> brokenAs = (mutate, label, art) =>
            {
                var lv = freshR();
                mutate(lv);
                bool threw = false;
                try { lv.Validate(art); } catch { threw = true; }
                Ok(threw, "validate phải ném lỗi: " + label);
            };
            Action<Action<LevelData>, string> broken = (mutate, label) => brokenAs(mutate, label, hasArt);

            broken(l => l.Stacks[0].Boxes[0].Slots[2] = "c1", "card trùng trên bàn");
            broken(l => l.Stacks[0].Boxes[0].Slots[0] = null, "card thiếu trên bàn");
            broken(l => l.Stacks[0].Boxes[0].Slots[0] = "ga", "đặt group lên bàn");
            broken(l => l.Groups[0].Cards.RemoveAt(0), "group thiếu thành viên");
            // ---- 1b. COLLAPSE: level cha-con hợp lệ phải pass, level hỏng phải chết ----
            {
                Func<LevelData> freshC = () => LevelData.Parse(CollapseLv);
                Action<Action<LevelData>, string> brokenC = (mutate, label) =>
                {
                    var lv = freshC();
                    mutate(lv);
                    bool threw = false;
                    try { lv.Validate(hasArt); } catch { threw = true; }
                    Ok(threw, "validate phải ném lỗi: " + label);
                };

                freshC().Validate(hasArt);   // hợp lệ: leaf (4 card, cha root) + root (3 card + 1 con)

                brokenC(l => l.Groups[0].ParentId = "ghost", "nhóm cha không tồn tại");
                brokenC(l => l.Groups[0].ParentId = "leaf", "nhóm tự làm cha chính nó");
                brokenC(l => l.Groups[1].ParentId = "leaf", "chu trình leaf ↔ root (và hết nhóm gốc)");
                brokenC(l =>
                {
                    l.Groups[0].Cards.Add(new CardDef { Id = "l5", Text = "L5" });
                    l.Stacks[1].Boxes[0].Slots[3] = "l5";
                }, "nhóm 4 card + 1 con = 5 thành viên");

                var lvB = freshC();
                lvB.Validate(hasArt);
                var gB = Game.Build(lvB);
                Ok(gB.GroupDefs != null && gB.GroupDefs["leaf"].ParentId == "root"
                   && gB.GroupDefs["root"].ParentId == null,
                   "Game phải mang bảng nhóm gid → def");
                Ok(ReferenceEquals(gB.GroupDefs, gB.Clone().GroupDefs),
                   "Clone chia sẻ bảng nhóm (chỉ-đọc), không sao sâu");
                var boxB = new Box { HadCollapse = true };
                Ok(boxB.Clone().HadCollapse, "Box.Clone phải chép cờ HadCollapse");

                // Collapse nổ ngay nhịp Settle đầu: 4 thẻ leaf cùng hộp → 1 thẻ "Leaf"
                var gC = Game.Build(lvB).Settle(false);
                var topC = gC.TopBox(0);
                Ok(gC.Stacks[0].Boxes.Count == 2, "collapse KHÔNG xoá hộp, hộp dưới KHÔNG lộ");
                Ok(topC.HadCollapse, "hộp phải ghi nhớ đã collapse");
                Ok(topC.Slots[0] != null && topC.Slots[0].CardId == "leaf"
                   && topC.Slots[0].GroupId == "root" && topC.Slots[0].Text == "Leaf"
                   && topC.Slots[0].Art == null,
                   "thẻ sinh ra mang mặt nhóm leaf, thuộc nhóm root, ở ô trống đầu tiên");
                Ok(topC.Slots[1] == null && topC.Slots[2] == null && topC.Slots[3] == null,
                   "ba ô còn lại phải trống");
                Ok(gC.Cleared == 1, "collapse tính là một lần clear");

                // Chặt: kéo thẻ sinh ra đi → hộp rỗng DO COLLAPSE → vẫn bị xoá (luật mới).
                string leafUid = topC.Slots[0].Uid;
                Ok(gC.MoveTile(0, leafUid, 1), "kéo thẻ sinh ra sang stack chứa root");
                gC.Settle(false);
                Ok(gC.Stacks[0].Boxes.Count == 1,
                   "chặt: hộp rỗng SAU COLLAPSE vẫn bị xoá, hộp đáy lộ ra");
                Ok(gC.Cleared == 2 && gC.Status == GameStatus.Won,
                   "root đủ 4 (r1+r2+r3+Leaf) → CLEAR gốc → sạch bàn → thắng");

                // Đối chứng: hộp rỗng KHÔNG do clear/collapse thì chặt giữ nguyên như cũ.
                var strictG = load(false);
                var t0 = strictG.TopBox(0);
                string mA = t0.Slots[0].Uid, mB = t0.Slots[1].Uid;
                int emptyS = strictG.Stacks.FindIndex(st => Game.IsEmpty(st.Boxes[0]));
                Ok(emptyS >= 0 && strictG.MoveTile(0, mA, emptyS) && strictG.MoveTile(0, mB, emptyS),
                   "dọn rỗng được hộp trên của stack 0");
                int depth0 = strictG.Stacks[0].Boxes.Count;
                strictG.Settle(false);
                Ok(strictG.Stacks[0].Boxes.Count == depth0,
                   "chặt: hộp rỗng vì bị kéo sạch thẻ thì Ở LẠI");

                // Solver giải được level collapse ở CẢ HAI chế độ, đúng 1 nước.
                foreach (bool dr in new[] { false, true })
                {
                    var lvS = freshC();
                    lvS.Validate(hasArt);
                    var rS = Solver.Solve(Game.Build(lvS).Settle(dr), dr);
                    Ok(rS.Ok && rS.Depth == 1,
                       "mini collapse phải giải được trong 1 nước, chế độ " + (dr ? "rộng" : "chặt"));
                }
            }
            broken(l => l.AllCards().First(c => c.Art == null).Text = null, "card không có text lẫn art");
            // Fixture toàn thẻ chỉ-chữ nên tự cấp art (stub hasArt luôn true) để dựng đúng ca
            // "hai thẻ chung một ảnh" — không mượn art của level ship.
            brokenAs(l =>
            {
                l.Groups[0].Cards[0].Art = "chung";
                l.Groups[0].Cards[1].Art = "chung";
            }, "hai thẻ dùng chung một ảnh", _ => true);
            broken(l => l.Stacks[1].Pos = l.Stacks[0].Pos, "hai stack trùng pos");
            broken(l => l.Stacks[0].Boxes[0].Slots = new string[3], "box không đủ 4 slot");
            brokenAs(l => l.Groups[0].Cards[0].Art = "khong-ton-tai", "art trỏ file không có", _ => false);
            broken(l => l.Stacks.First(s => s.Boxes.Count > 1).Boxes
                         .Insert(0, new BoxDef { Slots = new string[Rules.BoxCapacity] }),
                   "box rỗng không phải box đáy");
            for (int i = 0; i < levelJsons.Count; i++)
            {
                var lv = fresh(i);
                lv.Validate(hasArt);                       // level ship phải sạch
            }

            // ---- 2. Luật nước đi ----
            {
                var g = load(Rules.RemoveEmptyNonBottomBox);
                string uid = g.TopBox(0).Slots.First(t => t != null).Uid;
                Ok(!g.MoveTile(0, uid, 0), "thả về chính stack cũ phải bị từ chối");

                // Đích đã có thẻ sẵn + còn chỗ, để chứng minh thẻ vào slot trống ĐẦU TIÊN
                // chứ không phải slot 0.
                int di = -1;
                for (int s = 1; s < g.Stacks.Count; s++)
                    if (g.TopBox(s).Slots.Any(t => t != null) && Game.FreeCount(g.TopBox(s)) > 0) { di = s; break; }
                Ok(di > 0, "level phải có ít nhất 1 stack đích vừa có thẻ vừa còn chỗ");
                var dst = g.TopBox(di);
                int j = Array.FindIndex(dst.Slots, t => t == null);
                Ok(g.MoveTile(0, uid, di), "thả sang stack khác còn chỗ phải được");
                Ok(dst.Slots[j] != null && dst.Slots[j].Uid == uid,
                   "thẻ phải vào slot trống ĐẦU TIÊN (slot " + j + ")");

                // Slot ưu tiên = slot người chơi thả trúng. Trống thì thẻ nằm ĐÚNG đó;
                // bị chiếm / ngoài biên / -1 (solver, mọi call site cũ) thì rơi về slot
                // trống đầu tiên. Hộp đích dựng tay cho xác định: slot 0 có thẻ group
                // riêng (không gây CLEAR), slot 1..3 trống.
                Func<int, int> dropAt = want =>
                {
                    var p = load(Rules.RemoveEmptyNonBottomBox);
                    string u = p.TopBox(0).Slots.First(t => t != null).Uid;
                    var d = p.TopBox(1);
                    d.Slots[0] = new Tile { Uid = "px", CardId = "px", GroupId = "zz", Text = "x" };
                    for (int k = 1; k < d.Slots.Length; k++) d.Slots[k] = null;
                    Ok(p.MoveTile(0, u, 1, want), "nước đi sang hộp còn chỗ phải được nhận");
                    return Array.FindIndex(d.Slots, t => t != null && t.Uid == u);
                };
                Ok(dropAt(3) == 3, "thả trúng slot trống → thẻ vào ĐÚNG slot đó (3)");
                Ok(dropAt(1) == 1, "thả trúng slot trống khác → vẫn đúng slot đó (1)");
                Ok(dropAt(0) == 1, "thả trúng slot đã có thẻ → rơi về slot trống đầu tiên");
                Ok(dropAt(-1) == 1, "không chỉ định slot (solver) → slot trống đầu tiên");
                Ok(dropAt(99) == 1, "slot ngoài biên → slot trống đầu tiên");

                var deep = g.Stacks.First(s => s.Boxes.Count > 1);
                string buried = deep.Boxes[1].Slots.First(t => t != null).Uid;
                Ok(!g.MoveTile(g.Stacks.IndexOf(deep), buried, di),
                   "không kéo được thẻ trong box bị che");

                var full = load(Rules.RemoveEmptyNonBottomBox);
                int fi = -1;
                for (int s = 0; s < full.Stacks.Count; s++)
                    if (Game.FreeCount(full.TopBox(s)) == 0) { fi = s; break; }
                Ok(fi >= 0, "level phải có ít nhất 1 top box đầy để test");
                int si2 = fi == 0 ? 1 : 0;
                string before = Solver.Encode(full);
                Ok(!full.MoveTile(si2, full.TopBox(si2).Slots.First(t => t != null).Uid, fi),
                   "thả vào box đầy phải bị từ chối");
                Ok(Solver.Encode(full) == before, "thả hụt thì state không đổi");
            }

            // ---- 3. CLEAR + xoá box + lộ box dưới ----
            {
                var g = load(Rules.RemoveEmptyNonBottomBox);
                int si = g.Stacks.FindIndex(s => s.Boxes.Count > 1);
                var box = g.TopBox(si);
                int depthBefore = g.Stacks[si].Boxes.Count;
                string gid = box.Slots.First(t => t != null).GroupId;
                for (int i = 0; i < box.Slots.Length; i++)
                    box.Slots[i] = new Tile { Uid = "g" + i, CardId = "c" + i, GroupId = gid, Text = "x" };
                int clearedBefore = g.Cleared;
                g.Settle(Rules.RemoveEmptyNonBottomBox);
                Ok(g.Cleared == clearedBefore + 1, "đủ 4 thành viên cùng group → CLEAR");
                Ok(g.Stacks[si].Boxes.Count == depthBefore - 1,
                   "box không-đáy rỗng sau CLEAR → bị xoá, box dưới lộ ra");

                var bot = load(Rules.RemoveEmptyNonBottomBox);
                int bi = bot.Stacks.FindIndex(s => s.Boxes.Count == 1);
                Ok(bi >= 0 && bot.TopBox(bi).IsBottom, "level phải có stack chỉ gồm box đáy");
                var bbox = bot.TopBox(bi);
                for (int i = 0; i < bbox.Slots.Length; i++)
                    bbox.Slots[i] = new Tile { Uid = "b" + i, CardId = "c" + i, GroupId = "zz", Text = "x" };
                bot.Settle(Rules.RemoveEmptyNonBottomBox);
                Ok(bot.Stacks[bi].Boxes.Count == 1 && Game.IsEmpty(bot.TopBox(bi)),
                   "CLEAR ở box đáy → box ở lại và rỗng");
            }

            // ---- 4. Màu gợi ý (R2) ----
            {
                Func<string, string, Tile> mk = (uid, gid) =>
                    new Tile { Uid = uid, CardId = uid, GroupId = gid };
                var b1 = new Box { Slots = new[] { mk("a", "fruit"), mk("b", "animal"), null, null } };
                Ok(Game.BoxColorIndices(b1).Count == 0, "mỗi group 1 thẻ → không tô màu");

                var b2 = new Box { Slots = new[] { mk("a", "fruit"), mk("b", "animal"), mk("c", "fruit"), null } };
                var c2 = Game.BoxColorIndices(b2);
                Ok(c2.Count == 2 && c2["a"] == c2["c"], "2 thẻ cùng group → cùng màu, thẻ lẻ không màu");

                var b3 = new Box { Slots = new[] { mk("a", "fruit"), mk("b", "animal"), mk("c", "fruit"), mk("d", "animal") } };
                Ok(Game.BoxColorIndices(b3).Values.Distinct().Count() == 2,
                   "hai group ≥2 thẻ → hai màu khác nhau");
            }

            // ---- 5. Thắng / kẹt ----
            {
                var g = load(Rules.RemoveEmptyNonBottomBox);
                foreach (var st in g.Stacks)
                    foreach (var b in st.Boxes)
                        for (int i = 0; i < b.Slots.Length; i++) b.Slots[i] = null;
                Ok(g.CheckStatus() == GameStatus.Won, "sạch bàn → won");

                var jam = load(Rules.RemoveEmptyNonBottomBox);
                foreach (var st in jam.Stacks)
                {
                    st.Boxes.RemoveRange(1, st.Boxes.Count - 1);
                    for (int i = 0; i < st.Boxes[0].Slots.Length; i++)
                        st.Boxes[0].Slots[i] = new Tile
                        { Uid = st.X + "_" + st.Y + "_" + i, CardId = "x", GroupId = "g" + i };
                }
                Ok(jam.CheckStatus() == GameStatus.Stuck,
                   "mọi top box đầy, không nhóm nào đủ → stuck");
            }

            // ---- 6. Mọi level giải được, ở CẢ HAI cách đọc luật xoá hộp ----
            // Chế độ CHẶT là bắt buộc: mọi hộp ẩn chỉ mở bằng một CLEAR dùng thẻ đang với
            // tới được. Level chỉ giải được ở chế độ rộng nghĩa là nó bắt người chơi tự mò
            // ra luật "kéo rỗng hộp thì hộp biến mất" — không dạy đúng vòng lặp lõi.
            for (int i = 0; i < levelJsons.Count; i++)
            {
                foreach (bool drain in new[] { false, true })
                {
                    var start = DateTime.UtcNow;
                    var lvShip = fresh(i);
                    lvShip.Validate(hasArt);
                    var r = Solver.Solve(Game.Build(lvShip).Settle(drain), drain);
                    string mode = drain ? "rộng" : "chặt";
                    Ok(r.Ok, "level " + fresh(i).Id + " phải giải được ở chế độ " + mode +
                             " (" + (r.Why ?? "") + ")");
                    log("  " + fresh(i).Id + " " + mode + " — " + r.Depth + " nước (" + r.Nodes +
                        " nút, " + (int)(DateTime.UtcNow - start).TotalMilliseconds + "ms)");
                }
            }

            // ---- 7. Undo (booster) ----
            // Nước đi dựng tay để CHẮC CHẮN gây CLEAR: undo phải gỡ được cả cascade, không
            // riêng việc thẻ đổi ô. Nhóm "zz" không có trong GroupDefs nên đi nhánh CLEAR
            // thường, không nhánh COLLAPSE — kết quả xác định, không phụ thuộc level.
            {
                var g = load(0, Rules.RemoveEmptyNonBottomBox);
                g.UndoEnabled = true;
                Ok(!g.CanUndo, "bàn vừa nạp thì chưa có gì để lùi");

                var src = g.TopBox(1);
                var dst = g.TopBox(0);
                for (int i = 0; i < dst.Slots.Length; i++) dst.Slots[i] = null;
                for (int i = 0; i < src.Slots.Length; i++) src.Slots[i] = null;
                for (int i = 0; i < Rules.GroupSize - 1; i++)
                    dst.Slots[i] = new Tile { Uid = "z" + i, CardId = "z", GroupId = "zz", Text = "z" };
                src.Slots[0] = new Tile { Uid = "zlast", CardId = "z", GroupId = "zz", Text = "z" };

                string before = Solver.Encode(g);
                int movesBefore = g.Moves, clearedBefore = g.Cleared;

                Ok(g.MoveTile(1, "zlast", 0), "thẻ thứ 4 của nhóm phải kéo sang được");
                Ok(g.CanUndo, "nước đi được nhận thì phải có ảnh chụp");
                g.Settle(Rules.RemoveEmptyNonBottomBox);
                Ok(g.Cleared == clearedBefore + 1, "đủ 4 thẻ cùng nhóm thì phải CLEAR");

                var back = g.ApplyUndo();
                Ok(back != null, "có ảnh chụp thì ApplyUndo phải trả về bàn");
                Ok(Solver.Encode(back) == before, "undo phải trả bàn về đúng trạng thái trước nước đi");
                Ok(back.Cleared == clearedBefore, "undo gỡ cả CLEAR — Cleared tụt lại");
                Ok(back.Moves == movesBefore, "undo gỡ cả nước đi — Moves tụt lại");
                Ok(!back.CanUndo, "chỉ lùi được ĐÚNG một bước");
                Ok(back.UndoEnabled, "bàn khôi phục phải tiếp tục chụp được nước sau");

                // Nước bị từ chối không được để lại ảnh — undo sau đó sẽ lùi nhầm.
                var rej = load(0, Rules.RemoveEmptyNonBottomBox);
                rej.UndoEnabled = true;
                string u0 = rej.TopBox(0).Slots.First(t => t != null).Uid;
                Ok(!rej.MoveTile(0, u0, 0) && !rej.CanUndo, "nước đi bị từ chối thì không chụp");

                // Mặc định TẮT: Solver gọi MoveTile hàng vạn lần, bật lên là clone mỗi nút.
                var solverLike = load(0, Rules.RemoveEmptyNonBottomBox);
                int d0 = solverLike.Stacks.FindIndex(s => Game.FreeCount(s.Boxes[0]) > 0 && s != solverLike.Stacks[0]);
                Ok(d0 > 0 && solverLike.MoveTile(0, solverLike.TopBox(0).Slots.First(t => t != null).Uid, d0),
                   "cần một nước đi hợp lệ để kiểm cờ tắt");
                Ok(!solverLike.CanUndo, "cờ tắt (mặc định) thì KHÔNG chụp");
                Ok(!back.Clone().UndoEnabled, "Clone không mang cờ sang — bàn con của solver luôn tắt");

                // Dùng booster khác thì mất quyền undo (ảnh chụp là TOÀN bàn).
                var dropped = load(0, Rules.RemoveEmptyNonBottomBox);
                dropped.UndoEnabled = true;
                int d1 = dropped.Stacks.FindIndex(s => Game.FreeCount(s.Boxes[0]) > 0 && s != dropped.Stacks[0]);
                Ok(d1 > 0 && dropped.MoveTile(0, dropped.TopBox(0).Slots.First(t => t != null).Uid, d1),
                   "cần một nước đi hợp lệ để kiểm ClearUndo");
                dropped.ClearUndo();
                Ok(!dropped.CanUndo && dropped.ApplyUndo() == null, "ClearUndo xoá hẳn ảnh chụp");
            }

            log("SelfCheck OK — " + levelJsons.Count + " level, luật khớp demo/check.mjs");
        }
    }
}
