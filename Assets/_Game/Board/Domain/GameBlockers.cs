// Blocker: Hộp khoá, Thẻ băng, Ổ và chìa. Spec: docs/superpowers/specs/2026-09-10-blocker-locks-design.md
//
// Cả ba là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ" — không thêm loại
// nước đi nào. Một struct Lock gắn lên Box (Clears/Key) và Tile (Moves). Thẻ chìa không
// bị khoá gì nên chìa là field riêng Tile.KeyId, không nằm trong Lock.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System;

namespace WordStack.Board
{
    public enum LockKind { None, Clears, Moves, Key }

    public struct Lock
    {
        public LockKind Kind;
        public int Need;
        public int Have;       // chỉ có nghĩa với Moves — hai loại kia tính động từ bàn
        public string KeyId;   // chỉ có nghĩa với Key
    }

    /// <summary>
    /// Sổ đăng ký id blocker trong data. Thêm blocker mới = thêm hằng + thêm vào mảng đúng
    /// phía. Blocker thẻ mới còn phải thêm cặp được phép vào CardPairs nếu nó được đứng
    /// chung với cái khác — validator đọc bảng này, không cần sửa code kiểm.
    /// </summary>
    public static class Blockers
    {
        public const string Locked = "locked";   // hộp: đóng tới khi Cleared ≥ N
        public const string KeyLock = "keylock"; // hộp: đóng tới khi thẻ mang key cùng id biến mất
        public const string Ice = "ice";         // thẻ: bất động N nước kể từ lúc lộ ở hộp trên
        public const string Key = "key";         // thẻ: không khoá gì; bị gom là mở keylock cùng id

        public static readonly string[] BoxIds = { Locked, KeyLock };
        public static readonly string[] CardIds = { Ice, Key };

        static readonly string[][] CardPairs = { new[] { Ice, Key } };

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
        /// Hộp/khoá này đang mở không. Clears và Key KHÔNG lưu trạng thái riêng — chúng suy
        /// ra từ bàn (Cleared chỉ tăng; thẻ chìa mất là mất hẳn), nên Solver.Encode không
        /// phải mã hoá gì cho hai loại đó. Chỉ Moves mang Have.
        /// </summary>
        public bool IsOpen(Lock l)
        {
            switch (l.Kind)
            {
                case LockKind.Clears: return Cleared >= l.Need;
                case LockKind.Moves:  return l.Have >= l.Need;
                case LockKind.Key:    return !KeyOnBoard(l.KeyId);
                default:              return true;
            }
        }

        /// <summary>Thẻ còn băng. Băng tan thì Lock về default nên thẻ tan = thẻ thường.</summary>
        public static bool IsFrozen(Tile t) { return t != null && t.Lock.Kind == LockKind.Moves; }

        /// <summary>Booster hút/xáo được thẻ này không: không băng, và hộp chứa nó đang mở.</summary>
        public bool IsPullable(Tile t, Box b) { return !IsFrozen(t) && IsOpen(b.Lock); }

        bool KeyOnBoard(string keyId)
        {
            foreach (var st in Stacks)
                foreach (var b in st.Boxes)
                    foreach (var t in b.Slots)
                        if (t != null && t.KeyId == keyId) return true;
            return false;
        }
    }
}
