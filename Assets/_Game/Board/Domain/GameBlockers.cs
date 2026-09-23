// Blocker: Hộp khoá, Thẻ băng, Hộp khoá theo nhóm. Spec: docs/superpowers/specs/2026-09-10-blocker-locks-design.md
// (mục "ổ và chìa" của spec đã thay bằng khoá theo nhóm — luật hiện hành ở docs/wordstack-rules.md §11).
//
// Cả ba là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ" — không thêm loại
// nước đi nào. Một struct Lock gắn lên Box (Clears/Group) và Tile (Moves). Hộp khoá theo
// nhóm mở khi nhóm đó bị gom sạch khỏi bàn — không có thẻ chìa, thẻ không mang gì thêm.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System;

namespace WordStack.Board
{
    public enum LockKind { None, Clears, Moves, Group }

    public struct Lock
    {
        public LockKind Kind;
        public int Need;
        public int Have;        // chỉ có nghĩa với Moves — hai loại kia tính động từ bàn
        public string GroupId;  // chỉ có nghĩa với Group — id nhóm phải gom sạch thì hộp mới mở
    }

    /// <summary>
    /// Sổ đăng ký id blocker trong data. Thêm blocker mới = thêm hằng + thêm vào mảng đúng
    /// phía. Blocker thẻ mới còn phải thêm cặp được phép vào CardPairs nếu nó được đứng
    /// chung với cái khác — validator đọc bảng này, không cần sửa code kiểm.
    /// </summary>
    public static class Blockers
    {
        public const string Locked = "locked";       // hộp: đóng tới khi Cleared ≥ N
        public const string GroupLock = "grouplock"; // hộp: đóng tới khi nhóm mang id này không còn thẻ nào trên bàn
        public const string Ice = "ice";             // thẻ: bất động N nước kể từ lúc lộ ở hộp trên

        public static readonly string[] BoxIds = { Locked, GroupLock };
        public static readonly string[] CardIds = { Ice };

        // ponytail: thẻ mới có một blocker (băng) nên bảng cặp rỗng; thêm cặp khi có blocker thẻ thứ hai.
        static readonly string[][] CardPairs = new string[0][];

        public static bool CardPairAllowed(string a, string b)
        {
            foreach (var p in CardPairs)
                if ((p[0] == a && p[1] == b) || (p[0] == b && p[1] == a)) return true;
            return false;
        }

        /// <summary>Giá trị JSON là số nguyên ≥ 1 (Json.Parse trả số dạng double).</summary>
        public static bool IsCount(object v)
        {
            if (!(v is double)) return false;
            double d = (double)v;
            return d >= 1 && d == Math.Floor(d);
        }
    }

    public partial class Game
    {
        /// <summary>
        /// Hộp/khoá này đang mở không. Clears và Group KHÔNG lưu trạng thái riêng — chúng suy
        /// ra từ bàn (Cleared chỉ tăng; nhóm đã gom sạch không quay lại), nên Solver.Encode
        /// không phải mã hoá gì cho hai loại đó. Chỉ Moves mang Have.
        /// </summary>
        public bool IsOpen(Lock l)
        {
            switch (l.Kind)
            {
                case LockKind.Clears: return Cleared >= l.Need;
                case LockKind.Moves:  return l.Have >= l.Need;
                case LockKind.Group:  return !GroupOnBoard(l.GroupId);
                default:              return true;
            }
        }

        /// <summary>Thẻ còn băng. Băng tan thì Lock về default nên thẻ tan = thẻ thường.</summary>
        public static bool IsFrozen(Tile t) { return t != null && t.Lock.Kind == LockKind.Moves; }

        /// <summary>Booster hút/xáo được thẻ này không: không băng, và hộp chứa nó đang mở.</summary>
        public bool IsPullable(Tile t, Box b) { return !IsFrozen(t) && IsOpen(b.Lock); }

        /// <summary>
        /// Sau mỗi nước đi thành công: mọi thẻ băng đang ở hộp trên cùng tiến một bước.
        /// Đếm cả thẻ trong hộp đang khoá ở trên cùng (nó đang lộ), không đếm thẻ chìm.
        /// Tan thì Lock về default — thẻ tan không khác gì thẻ thường, kể cả với Encode.
        /// </summary>
        void TickIce()
        {
            foreach (var st in Stacks)
            {
                if (st.Boxes.Count == 0) continue;
                var top = st.Boxes[0];
                for (int i = 0; i < top.Slots.Length; i++)
                {
                    var t = top.Slots[i];
                    if (t == null || t.Lock.Kind != LockKind.Moves) continue;
                    t.Lock.Have++;
                    if (t.Lock.Have >= t.Lock.Need) t.Lock = default(Lock);
                }
            }
        }

        /// <summary>
        /// Có ít nhất một nước đi mà MoveTile sẽ nhận không. Soi đúng các chốt của MoveTile
        /// (hộp đóng hai đầu, thẻ băng, hộp đích đầy) mà không mutate.
        /// </summary>
        public bool HasAnyMove()
        {
            for (int from = 0; from < Stacks.Count; from++)
            {
                var src = TopBox(from);
                if (src == null || !IsOpen(src.Lock)) continue;
                bool movable = false;
                foreach (var t in src.Slots) if (t != null && !IsFrozen(t)) { movable = true; break; }
                if (!movable) continue;

                for (int to = 0; to < Stacks.Count; to++)
                {
                    if (to == from) continue;
                    var dst = TopBox(to);
                    if (dst != null && IsOpen(dst.Lock) && FreeCount(dst) > 0) return true;
                }
            }
            return false;
        }

        /// <summary>Nhóm gid còn thẻ nào trên bàn không. Tính cả thẻ nhóm con: nhóm cha chỉ
        /// có thẻ sau khi nhóm con gộp lại, nên "gid không còn thẻ" phải xét cả dòng dõi.</summary>
        bool GroupOnBoard(string gid)
        {
            foreach (var st in Stacks)
                foreach (var b in st.Boxes)
                    foreach (var t in b.Slots)
                        if (t != null && InGroup(t.GroupId, gid)) return true;
            return false;
        }

        /// <summary>gid là chính nhóm tileGroup hoặc một tổ tiên của nó (theo ParentId).</summary>
        public bool InGroup(string tileGroup, string gid)
        {
            for (string x = tileGroup; x != null;)
            {
                if (x == gid) return true;
                GroupDef d;
                x = GroupDefs != null && GroupDefs.TryGetValue(x, out d) ? d.ParentId : null;
            }
            return false;
        }
    }
}
