// Trạng thái bàn chơi + luật vận hành: nước đi duy nhất, CLEAR, cascade từng bước,
// thắng/kẹt, màu gợi ý. View chỉ vẽ lại thứ file này quyết định.
//
// KHÔNG import UnityEngine (xem Rules.cs).
using System;
using System.Collections.Generic;
using System.Linq;

namespace WordStack.Board
{
    public class Tile
    {
        public string Uid, CardId, GroupId, Text, Art;
        public Tile Clone() { return (Tile)MemberwiseClone(); }
    }

    public class Box
    {
        public bool IsBottom;
        public bool HadCollapse;   // đã từng xảy ra collapse — chế độ chặt dùng để xoá hộp rỗng
        public Tile[] Slots = new Tile[Rules.BoxCapacity];
        public Box Clone()
        {
            var b = new Box { IsBottom = IsBottom, HadCollapse = HadCollapse, Slots = new Tile[Slots.Length] };
            for (int i = 0; i < Slots.Length; i++) b.Slots[i] = Slots[i] == null ? null : Slots[i].Clone();
            return b;
        }
    }

    public class Stack
    {
        public double X, Y;
        public List<Box> Boxes = new List<Box>();
        public Stack Clone()
        {
            var s = new Stack { X = X, Y = Y };
            foreach (var b in Boxes) s.Boxes.Add(b.Clone());
            return s;
        }
    }

    public enum SettleKind { None, Clear, RemoveBox, Collapse }

    public struct SettleEvent
    {
        public SettleKind Kind;
        public int Stack;
        public string GroupId;
        public string[] DoomedUids;   // uid các thẻ vừa bị xoá — view animate rồi mới Rebuild
        public string NewTileUid;     // Collapse: uid thẻ vừa sinh ra, view spawn nó
        public bool BoxRemoved;
    }

    public class Game
    {
        public string LevelId, Title;
        public int TotalGroups, Cleared, Moves;
        public GameStatus Status = GameStatus.Playing;
        public List<Stack> Stacks = new List<Stack>();

        // gid → định nghĩa nhóm (ParentId/Text/Art) — SettleStep tra khi gộp. Chỉ-đọc sau
        // Build nên Clone chia sẻ tham chiếu: solver clone hàng vạn lần, sao sâu là phí thật.
        public Dictionary<string, GroupDef> GroupDefs;

        int uidSeq;

        public static Game Build(LevelData lv)
        {
            var card = new Dictionary<string, CardDef>();
            var ownerGroup = new Dictionary<string, string>();
            foreach (var grp in lv.Groups)
                foreach (var c in grp.Cards) { card[c.Id] = c; ownerGroup[c.Id] = grp.Id; }

            var g = new Game { LevelId = lv.Id, Title = lv.Title, TotalGroups = lv.Groups.Count };
            g.GroupDefs = lv.Groups.ToDictionary(x => x.Id);
            foreach (var sd in lv.Stacks)
            {
                var st = new Stack { X = sd.Pos[0], Y = sd.Pos[1] };
                for (int bi = 0; bi < sd.Boxes.Count; bi++)
                {
                    var box = new Box { IsBottom = bi == sd.Boxes.Count - 1 };
                    for (int i = 0; i < sd.Boxes[bi].Slots.Length; i++)
                    {
                        var id = sd.Boxes[bi].Slots[i];
                        if (id == null) continue;
                        var c = card[id];
                        box.Slots[i] = new Tile
                        {
                            Uid = "t" + (++g.uidSeq),
                            CardId = c.Id, GroupId = ownerGroup[c.Id], Text = c.Text, Art = c.Art
                        };
                    }
                    st.Boxes.Add(box);
                }
                g.Stacks.Add(st);
            }
            return g;
        }

        public Box TopBox(int s) { return Stacks[s].Boxes.Count > 0 ? Stacks[s].Boxes[0] : null; }
        public static int FreeCount(Box b) { return b.Slots.Count(t => t == null); }
        public static bool IsEmpty(Box b) { return b.Slots.All(t => t == null); }

        public int TotalTiles()
        {
            return Stacks.Sum(st => st.Boxes.Sum(b => b.Slots.Count(t => t != null)));
        }

        // Nước đi DUY NHẤT: thẻ bất kỳ trong top box → top box của stack khác.
        // Thả về chính stack cũ hoặc box đích đầy = từ chối.
        //
        // preferSlot = slot người chơi thả trúng: trống thì thẻ nằm ĐÚNG đó, bị chiếm
        // (hoặc -1) thì rơi về slot trống ĐẦU TIÊN. Solver/SelfCheck luôn gọi bản -1 và
        // PHẢI giữ vậy: slot nào không đổi luật gom nhóm, nhưng Solver.Encode có tính vị
        // trí slot nên cho nó chọn slot là nở state vô ích và lệch số nút với check.mjs.
        public bool MoveTile(int from, string uid, int to, int preferSlot = -1)
        {
            if (from == to) return false;
            if (from < 0 || from >= Stacks.Count || to < 0 || to >= Stacks.Count) return false;
            var src = TopBox(from);
            var dst = TopBox(to);
            if (src == null || dst == null) return false;
            int i = Array.FindIndex(src.Slots, t => t != null && t.Uid == uid);
            if (i < 0) return false;                       // không phải thẻ của top box
            int j = preferSlot >= 0 && preferSlot < dst.Slots.Length && dst.Slots[preferSlot] == null
                  ? preferSlot
                  : Array.FindIndex(dst.Slots, t => t == null);
            if (j < 0) return false;                       // box đích đầy
            dst.Slots[j] = src.Slots[i];
            src.Slots[i] = null;
            Moves++;
            return true;
        }

        static string CompletedGroupIn(Box box)
        {
            var count = new Dictionary<string, int>();
            foreach (var t in box.Slots)
            {
                if (t == null) continue;
                int n;
                count[t.GroupId] = count.TryGetValue(t.GroupId, out n) ? n + 1 : 1;
            }
            foreach (var kv in count) if (kv.Value == Rules.GroupSize) return kv.Key;
            return null;
        }

        // MỘT bước giải quyết. View gọi từng bước để có nhịp cascade + khoá input;
        // solver/SelfCheck gọi Settle() lặp tới khi None.
        public SettleEvent SettleStep(bool drain)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                var box = TopBox(s);
                if (box == null) continue;
                string gid = CompletedGroupIn(box);
                if (gid == null) continue;

                var doomed = new List<string>();
                for (int i = 0; i < box.Slots.Length; i++)
                    if (box.Slots[i] != null && box.Slots[i].GroupId == gid)
                    {
                        doomed.Add(box.Slots[i].Uid);
                        box.Slots[i] = null;
                    }
                Cleared++;
                // Nhóm có cha → COLLAPSE: sinh 1 thẻ mang mặt nhóm vừa gộp, là thành viên
                // của nhóm cha, đặt vào ô trống đầu tiên của CHÍNH hộp này. Hộp không rỗng
                // nên luật xoá hộp bên dưới tự im — hộp dưới không lộ ra.
                // CardId = id nhóm (không phải null): Solver.Encode mã hoá bằng CardId,
                // để null là hai trạng thái khác nhau memo thành một.
                GroupDef def;
                if (GroupDefs != null && GroupDefs.TryGetValue(gid, out def) && def.ParentId != null)
                {
                    int j = Array.FindIndex(box.Slots, t => t == null);
                    var nt = new Tile
                    {
                        Uid = "t" + (++uidSeq),
                        CardId = gid, GroupId = def.ParentId, Text = def.Text, Art = def.Art
                    };
                    box.Slots[j] = nt;
                    box.HadCollapse = true;
                    return new SettleEvent
                    {
                        Kind = SettleKind.Collapse, Stack = s, GroupId = gid,
                        DoomedUids = doomed.ToArray(), NewTileUid = nt.Uid
                    };
                }
                bool removed = IsEmpty(box) && !box.IsBottom;
                if (removed) Stacks[s].Boxes.RemoveAt(0);
                return new SettleEvent
                {
                    Kind = SettleKind.Clear, Stack = s, GroupId = gid,
                    DoomedUids = doomed.ToArray(), BoxRemoved = removed
                };
            }

            // Hộp trên cùng rỗng, không phải đáy → lùi ra. Chế độ rộng (drain) xoá mọi hộp
            // rỗng; chế độ chặt chỉ xoá hộp ĐÃ TỪNG collapse — vì collapse là một lần ghép
            // bộ, chỉ khác CLEAR ở chỗ để lại một thẻ. Không có luật này thì hộp sau
            // collapse thành nắp đậy vĩnh viễn ở chế độ chặt (spec Mục 5).
            for (int s = 0; s < Stacks.Count; s++)
            {
                var box = TopBox(s);
                if (box != null && !box.IsBottom && IsEmpty(box) && (drain || box.HadCollapse))
                {
                    Stacks[s].Boxes.RemoveAt(0);
                    return new SettleEvent { Kind = SettleKind.RemoveBox, Stack = s, BoxRemoved = true };
                }
            }

            Status = CheckStatus();
            return new SettleEvent { Kind = SettleKind.None };
        }

        public Game Settle(bool drain)
        {
            while (SettleStep(drain).Kind != SettleKind.None) { }
            return this;
        }

        public GameStatus CheckStatus()
        {
            if (TotalTiles() == 0) return GameStatus.Won;
            foreach (var st in Stacks) if (FreeCount(st.Boxes[0]) > 0) return GameStatus.Playing;
            return GameStatus.Stuck;
        }

        // Cấp màu CỤC BỘ theo từng box, theo thứ tự thẻ xuất hiện. Group có ≥2 thẻ mới
        // được màu; thẻ đứng một mình giữ nền mặc định. Domain trả INDEX để test được
        // ngoài Unity — màu hex thật sống ở view.
        public static Dictionary<string, int> BoxColorIndices(Box box)
        {
            var order = new List<string>();
            var byGroup = new Dictionary<string, List<Tile>>();
            foreach (var t in box.Slots)
            {
                if (t == null) continue;
                List<Tile> l;
                if (!byGroup.TryGetValue(t.GroupId, out l))
                {
                    l = new List<Tile>();
                    byGroup[t.GroupId] = l;
                    order.Add(t.GroupId);
                }
                l.Add(t);
            }
            var res = new Dictionary<string, int>();
            int i = 0;
            foreach (var gid in order)
            {
                var l = byGroup[gid];
                if (l.Count < 2) continue;
                int c = i++ % Rules.PaletteSize;
                foreach (var t in l) res[t.Uid] = c;
            }
            return res;
        }

        public Game Clone()
        {
            var g = new Game
            {
                LevelId = LevelId, Title = Title, TotalGroups = TotalGroups,
                Cleared = Cleared, Moves = Moves, Status = Status, uidSeq = uidSeq,
                GroupDefs = GroupDefs
            };
            foreach (var st in Stacks) g.Stacks.Add(st.Clone());
            return g;
        }
    }
}
