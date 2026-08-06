// ============================================================================
// THROWAWAY PROTOTYPE — luật WordStack. Code này KHÔNG mang sang production.
//
// Nguồn luật: demo/wordstack-clear-demo.html (bản đã chơi thử + user duyệt) và bộ
// assert demo/check.mjs. Port 1-1 sang C#; khác biệt nào cũng là bug.
// GDD gốc: GDD_WordStack.md — lưu ý schema level CỐ TÌNH lệch §6.2, xem
// docs/wordstack-design-log.md.
//
// Phạm vi: CHỈ kết cục CLEAR. Chưa có COLLAPSE, Undo, Hint, âm thanh, move budget.
//
// RÀNG BUỘC CỨNG: file này KHÔNG import UnityEngine — để PrototypeSelfCheckMain.cs
// compile bằng csc và chạy SelfCheck không cần mở Unity. Xem production/session-state/active.md.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WordStack.Prototype
{
    public static class Rules
    {
        public const int BoxCapacity = 4;
        public const int GroupSize = 4;
        public const int PaletteSize = 6;

        // Hộp top rỗng mà không phải hộp đáy thì bị xoá, hộp dưới lộ ra — kể cả khi rỗng
        // do người chơi kéo hết thẻ đi, không riêng do CLEAR.
        // false = đọc chặt pseudocode §7 GDD (chỉ CLEAR mới xoá hộp). Phạm vi này KHÔNG
        // có Undo, nên false cho phép người chơi tự khoá chết hộp dưới vĩnh viễn, chỉ gỡ
        // được bằng Restart. Cân nhắc đảo khi Undo xuất hiện.
        // Cả 3 level chạy được ở CẢ HAI chế độ — SelfCheck kiểm cả hai bất kể cờ này.
        // Chốt 2026-08-04: giữ true. Xem docs/wordstack-rules.md Mục 4.
        public const bool RemoveEmptyNonBottomBox = true;
    }

    public enum GameStatus { Playing, Won, Stuck }

    // ---------------------------------------------------------------- JSON
    // Mini reader, subset vừa đủ schema level: object / array / string / number /
    // true / false / null. Không dùng JsonUtility (nằm trong UnityEngine → phá ràng
    // buộc ở đầu file, lại không đọc được null trong mảng) và không thêm Newtonsoft.
    static class Json
    {
        public static object Parse(string s)
        {
            int i = (s.Length > 0 && s[0] == '﻿') ? 1 : 0;
            var v = Value(s, ref i);
            Ws(s, ref i);
            if (i < s.Length) throw Err(s, i, "còn rác sau giá trị JSON");
            return v;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw Err(s, i, "hết chuỗi giữa chừng");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Lit(s, ref i, "true")) return true;
            if (Lit(s, ref i, "false")) return false;
            if (Lit(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;                                    // '{'
            Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            for (;;)
            {
                Ws(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Err(s, i, "chờ tên field trong dấu nháy kép");
                string k = Str(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Err(s, i, "chờ ':' sau tên field");
                i++;
                d[k] = Value(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length) throw Err(s, i, "object chưa đóng");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw Err(s, i, "chờ ',' hoặc '}'");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++;                                    // '['
            Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            for (;;)
            {
                l.Add(Value(s, ref i));
                Ws(s, ref i);
                if (i >= s.Length) throw Err(s, i, "array chưa đóng");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw Err(s, i, "chờ ',' hoặc ']'");
            }
        }

        static string Str(string s, ref int i)
        {
            i++;                                    // '"'
            var sb = new StringBuilder();
            for (;;)
            {
                if (i >= s.Length) throw Err(s, i, "chuỗi chưa đóng");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw Err(s, i, "escape cụt");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Err(s, i, "\\u thiếu 4 chữ số hex");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw Err(s, i - 1, "escape lạ '\\" + e + "'");
                }
            }
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E'
                                    || ((s[i] == '-' || s[i] == '+') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
            double d;
            if (start == i || !double.TryParse(s.Substring(start, i - start),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                throw Err(s, start, "số không hợp lệ");
            return d;
        }

        static bool Lit(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length || string.CompareOrdinal(s, i, lit, 0, lit.Length) != 0) return false;
            i += lit.Length;
            return true;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }

        static Exception Err(string s, int i, string msg)
        {
            int line = 1, col = 1;
            for (int k = 0; k < i && k < s.Length; k++) { if (s[k] == '\n') { line++; col = 1; } else col++; }
            return new Exception("JSON lỗi dòng " + line + " cột " + col + ": " + msg);
        }

        // ---- truy cập có kiểm kiểu, message chỉ rõ chỗ hỏng ----
        public static Dictionary<string, object> AsObj(object o, string where)
        {
            var d = o as Dictionary<string, object>;
            if (d == null) throw new Exception(where + ": chờ object");
            return d;
        }

        public static List<object> AsArr(object o, string where)
        {
            var l = o as List<object>;
            if (l == null) throw new Exception(where + ": chờ array");
            return l;
        }

        public static string AsStr(object o, string where)
        {
            if (o == null) return null;
            var s = o as string;
            if (s == null) throw new Exception(where + ": chờ chuỗi");
            return s;
        }

        public static double AsNum(object o, string where)
        {
            if (!(o is double)) throw new Exception(where + ": chờ số");
            return (double)o;
        }

        public static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) ? v : null;
        }
    }

    // ------------------------------------------------------- tầng khai báo
    public class CardDef  { public string Id, Text, Art; }

    // Card nằm LỒNG trong group của nó. Nhờ vậy hai luật thành bất khả vi phạm về cấu
    // trúc thay vì phải kiểm bằng validate: một card thuộc đúng một group, và không có
    // field "group" nào để trỏ sai. ParentId (quan hệ nhóm-cha, tức COLLAPSE) vẫn là
    // field riêng — nhét nhóm con vào Cards sẽ làm mảng đó thành hỗn hợp hai kiểu.
    public class GroupDef
    {
        public string Id, ParentId, Text, Art;
        public List<CardDef> Cards = new List<CardDef>();
    }

    public class BoxDef   { public string[] Slots; }
    public class StackDef { public double[] Pos; public List<BoxDef> Boxes = new List<BoxDef>(); }

    public class LevelData
    {
        public string Id, Title, Note;
        public List<GroupDef> Groups = new List<GroupDef>();
        public List<StackDef> Stacks = new List<StackDef>();

        public IEnumerable<CardDef> AllCards()
        {
            foreach (var g in Groups) foreach (var c in g.Cards) yield return c;
        }

        public static LevelData Parse(string json)
        {
            var root = Json.AsObj(Json.Parse(json), "root");
            var lv = new LevelData
            {
                Id = Json.AsStr(Json.Get(root, "id"), "id"),
                Title = Json.AsStr(Json.Get(root, "title"), "title"),
                Note = Json.AsStr(Json.Get(root, "note"), "note"),
            };

            var meaning = Json.AsObj(Json.Get(root, "meaning"), "meaning");
            foreach (var o in Json.AsArr(Json.Get(meaning, "groups"), "meaning.groups"))
            {
                var d = Json.AsObj(o, "meaning.groups[]");
                var g = new GroupDef
                {
                    Id = Json.AsStr(Json.Get(d, "id"), "group.id"),
                    ParentId = Json.AsStr(Json.Get(d, "group"), "group.group"),
                    Text = Json.AsStr(Json.Get(d, "text"), "group.text"),
                    Art = Json.AsStr(Json.Get(d, "art"), "group.art"),
                };
                var cards = Json.Get(d, "cards");
                if (cards != null)
                    foreach (var co in Json.AsArr(cards, "group.cards"))
                    {
                        var cd = Json.AsObj(co, "group.cards[]");
                        g.Cards.Add(new CardDef
                        {
                            Id = Json.AsStr(Json.Get(cd, "id"), "card.id"),
                            Text = Json.AsStr(Json.Get(cd, "text"), "card.text"),
                            Art = Json.AsStr(Json.Get(cd, "art"), "card.art"),
                        });
                    }
                lv.Groups.Add(g);
            }

            var layout = Json.AsObj(Json.Get(root, "layout"), "layout");
            foreach (var o in Json.AsArr(Json.Get(layout, "stacks"), "layout.stacks"))
            {
                var d = Json.AsObj(o, "layout.stacks[]");
                var st = new StackDef();
                var pos = Json.Get(d, "pos");
                if (pos != null)
                    st.Pos = Json.AsArr(pos, "stack.pos").Select(x => Json.AsNum(x, "stack.pos[]")).ToArray();
                foreach (var bo in Json.AsArr(Json.Get(d, "boxes"), "stack.boxes"))
                {
                    var bd = Json.AsObj(bo, "stack.boxes[]");
                    st.Boxes.Add(new BoxDef
                    {
                        Slots = Json.AsArr(Json.Get(bd, "slots"), "box.slots")
                                    .Select(x => Json.AsStr(x, "box.slots[]")).ToArray()
                    });
                }
                lv.Stacks.Add(st);
            }
            return lv;
        }

        // hasArt: host quyết định art có tồn tại không (Unity: Resources.Load; console: File.Exists).
        // Domain không biết Sprite hay file là gì.
        public void Validate(Predicate<string> hasArt)
        {
            Action<string> die = m => { throw new Exception("[" + (Id ?? "?") + "] " + m); };

            // Card lồng trong group nên KHÔNG còn phải kiểm "card trỏ group không tồn tại"
            // và "một card thuộc hai group" — cấu trúc đã loại trừ cả hai.
            var gids = new HashSet<string>();
            var cids = new HashSet<string>();
            var artOwner = new Dictionary<string, string>();   // art key → ai đang dùng

            foreach (var g in Groups)
            {
                if (string.IsNullOrEmpty(g.Id)) die("group thiếu id");
                if (!gids.Add(g.Id)) die("group id trùng: " + g.Id);
                if (g.Text == null && g.Art == null) die("group \"" + g.Id + "\" phải có text hoặc art");
                if (g.Art != null)
                {
                    if (!hasArt(g.Art)) die("group \"" + g.Id + "\" trỏ art \"" + g.Art + "\" không tồn tại");
                    if (artOwner.ContainsKey(g.Art))
                        die("art \"" + g.Art + "\" dùng cho cả " + artOwner[g.Art] + " và group \"" + g.Id + "\"");
                    artOwner[g.Art] = "group \"" + g.Id + "\"";
                }
            }

            // -- COLLAPSE: quan hệ nhóm cha-con --
            // Member của một nhóm = card khai trực tiếp + nhóm con trỏ vào nó. Nhóm nào
            // (kể cả gốc) cũng phải đủ đúng GroupSize thành viên thì mới gộp được.
            var childCount = new Dictionary<string, int>();
            bool hasRoot = false;
            foreach (var g in Groups)
            {
                if (g.ParentId == null) { hasRoot = true; continue; }
                if (g.ParentId == g.Id) die("group \"" + g.Id + "\" tự làm cha chính nó");
                if (!gids.Contains(g.ParentId))
                    die("group \"" + g.Id + "\" trỏ nhóm cha \"" + g.ParentId + "\" không tồn tại");
                int n;
                childCount.TryGetValue(g.ParentId, out n);
                childCount[g.ParentId] = n + 1;
            }
            if (!hasRoot)
                die("phải có ít nhất một nhóm gốc (không có \"group\") — không thì không thẻ nào biến mất được");

            // Chu trình: đi theo cha; quá số nhóm là chắc chắn có vòng.
            var byId = Groups.ToDictionary(x => x.Id);
            foreach (var g in Groups)
            {
                var cur = g;
                int steps = 0;
                while (cur.ParentId != null)
                {
                    if (++steps > Groups.Count)
                        die("chuỗi nhóm cha-con có chu trình (đi từ \"" + g.Id + "\")");
                    cur = byId[cur.ParentId];
                }
            }

            foreach (var g in Groups)
            {
                int cn;
                childCount.TryGetValue(g.Id, out cn);
                if (g.Cards.Count + cn != Rules.GroupSize)
                    die("group \"" + g.Id + "\" có " + g.Cards.Count + " thẻ + " + cn +
                        " nhóm con = " + (g.Cards.Count + cn) + " thành viên, phải đúng " + Rules.GroupSize);
            }

            foreach (var g in Groups)
                foreach (var c in g.Cards)
                {
                    if (string.IsNullOrEmpty(c.Id)) die("group \"" + g.Id + "\" có thẻ thiếu id");
                    if (gids.Contains(c.Id)) die("id \"" + c.Id + "\" dùng cho cả card lẫn group");
                    if (!cids.Add(c.Id)) die("card id trùng: " + c.Id);
                    if (c.Text == null && c.Art == null) die("card \"" + c.Id + "\" phải có text hoặc art");
                    if (c.Art == null) continue;
                    if (!hasArt(c.Art)) die("card \"" + c.Id + "\" trỏ art \"" + c.Art + "\" không tồn tại");
                    // Mỗi ảnh thuộc về đúng một thẻ — hai thẻ chung ảnh là kéo nhầm asset.
                    if (artOwner.ContainsKey(c.Art))
                        die("art \"" + c.Art + "\" dùng cho cả " + artOwner[c.Art] + " và card \"" + c.Id + "\"");
                    artOwner[c.Art] = "card \"" + c.Id + "\"";
                }

            // "text và ảnh ngang vai" là thuộc tính của level, không chỉ của schema.
            if (!AllCards().Any(c => c.Art != null && c.Text == null)) die("level không có thẻ nào chỉ-ảnh");
            if (!AllCards().Any(c => c.Text != null && c.Art == null)) die("level không có thẻ nào chỉ-chữ");

            if (Stacks.Count == 0) die("layout không có stack nào");
            var seen = new HashSet<string>();
            var poses = new HashSet<string>();
            for (int si = 0; si < Stacks.Count; si++)
            {
                var st = Stacks[si];
                if (st.Pos == null || st.Pos.Length != 2) die("stack " + si + ": \"pos\" phải là [x, y]");
                string pk = st.Pos[0].ToString("R", CultureInfo.InvariantCulture) + "," +
                            st.Pos[1].ToString("R", CultureInfo.InvariantCulture);
                if (!poses.Add(pk)) die("hai stack cùng pos [" + pk + "]");
                if (st.Boxes.Count == 0) die("stack " + si + " không có box nào");

                for (int bi = 0; bi < st.Boxes.Count; bi++)
                {
                    var box = st.Boxes[bi];
                    if (box.Slots.Length != Rules.BoxCapacity)
                        die("stack " + si + " box " + bi + ": phải đúng " + Rules.BoxCapacity + " slot");
                    if (box.Slots.All(x => x == null) && bi < st.Boxes.Count - 1)
                        die("stack " + si + " box " + bi + ": box rỗng mà không phải box đáy → box dưới không bao giờ với tới được");
                    foreach (var id in box.Slots)
                    {
                        if (id == null) continue;
                        if (gids.Contains(id)) die("\"" + id + "\" là group, không được đặt sẵn trên bàn");
                        if (!cids.Contains(id)) die("\"" + id + "\" trong layout không phải id của thẻ nào");
                        if (!seen.Add(id)) die("card \"" + id + "\" xuất hiện nhiều lần trên bàn");
                    }
                }
            }
            foreach (var c in AllCards())
                if (!seen.Contains(c.Id)) die("card \"" + c.Id + "\" không có mặt trên bàn");

        }

        public IEnumerable<string> ArtKeys()
        {
            foreach (var g in Groups)
            {
                if (g.Art != null) yield return g.Art;
                foreach (var c in g.Cards) if (c.Art != null) yield return c.Art;
            }
        }
    }

    // ------------------------------------------------------------- runtime
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

        // Nước đi DUY NHẤT: thẻ bất kỳ trong top box → top box của stack khác,
        // vào slot trống ĐẦU TIÊN. Thả về chính stack cũ hoặc box đích đầy = từ chối.
        public bool MoveTile(int from, string uid, int to)
        {
            if (from == to) return false;
            if (from < 0 || from >= Stacks.Count || to < 0 || to >= Stacks.Count) return false;
            var src = TopBox(from);
            var dst = TopBox(to);
            if (src == null || dst == null) return false;
            int i = Array.FindIndex(src.Slots, t => t != null && t.Uid == uid);
            if (i < 0) return false;                       // không phải thẻ của top box
            int j = Array.FindIndex(dst.Slots, t => t == null);
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

    // -------------------------------------------------------------- solver
    public class SolveResult
    {
        public bool Ok;
        public int Depth, Nodes;
        public string Why;
    }

    public static class Solver
    {
        // Vị trí slot không đổi luật → sort nội dung top box. Stack hoán vị được → sort.
        // Sai canonical hoá là dedupe gộp nhầm state → báo "không giải được" oan.
        public static string Encode(Game g)
        {
            var keys = new List<string>(g.Stacks.Count);
            foreach (var st in g.Stacks)
            {
                var parts = new List<string>(st.Boxes.Count);
                for (int i = 0; i < st.Boxes.Count; i++)
                {
                    var b = st.Boxes[i];
                    // Dấu "!" cho hộp đã collapse: ở chế độ chặt, hai bàn giống hệt về thẻ
                    // nhưng khác cờ HadCollapse có tương lai khác nhau (một cái rỗng là tự
                    // mở) — memo trộn chúng là solver trả kết quả sai im lặng.
                    if (i == 0)
                    {
                        var ids = b.Slots.Where(t => t != null).Select(t => t.CardId).ToList();
                        ids.Sort(StringComparer.Ordinal);
                        parts.Add((b.HadCollapse ? "!" : "") + string.Join(",", ids));
                    }
                    else parts.Add((b.HadCollapse ? "!" : "") +
                                   string.Join(",", b.Slots.Select(t => t == null ? "_" : t.CardId)));
                }
                keys.Add(string.Join("/", parts));
            }
            keys.Sort(StringComparer.Ordinal);
            return string.Join("|", keys);
        }

        // Gom thẻ cùng group vào một box là tiến bộ; giữ slot trống cũng là tiến bộ.
        static int Score(Game g)
        {
            int s = g.Cleared * 1000 - g.TotalTiles() * 10;
            foreach (var st in g.Stacks)
            {
                var box = st.Boxes[0];
                var cnt = new Dictionary<string, int>();
                foreach (var t in box.Slots)
                {
                    if (t == null) continue;
                    int n;
                    cnt[t.GroupId] = cnt.TryGetValue(t.GroupId, out n) ? n + 1 : 1;
                }
                foreach (var kv in cnt) s += kv.Value * kv.Value;
                s += Game.FreeCount(box);
            }
            return s;
        }

        // Beam search: bộ nhớ có trần, đủ để chứng minh level giải được.
        // (DFS mù đã đo nổ 4GB heap ở lv-002 khi thử bằng node.)
        // Không tìm ra ≠ chắc chắn vô nghiệm — nới beam nếu nghi ngờ.
        public static SolveResult Solve(Game start, bool drain, int beam = 600, int maxDepth = 250)
        {
            var layer = new List<Game> { start };
            int nodes = 0, bestCleared = -1, stale = 0;

            for (int d = 0; d < maxDepth; d++)
            {
                var next = new List<Game>();
                var seen = new HashSet<string>();
                foreach (var cur in layer)
                {
                    if (cur.Status == GameStatus.Won) return new SolveResult { Ok = true, Depth = d, Nodes = nodes };
                    if (cur.Status != GameStatus.Playing) continue;

                    for (int from = 0; from < cur.Stacks.Count; from++)
                    {
                        foreach (var t in cur.Stacks[from].Boxes[0].Slots)
                        {
                            if (t == null) continue;
                            for (int to = 0; to < cur.Stacks.Count; to++)
                            {
                                if (to == from || Game.FreeCount(cur.Stacks[to].Boxes[0]) == 0) continue;
                                var n = cur.Clone();
                                if (!n.MoveTile(from, t.Uid, to)) continue;
                                n.Settle(drain);
                                nodes++;
                                if (n.Status == GameStatus.Won)
                                    return new SolveResult { Ok = true, Depth = d + 1, Nodes = nodes };
                                if (n.Status == GameStatus.Stuck) continue;
                                if (!seen.Add(Encode(n))) continue;
                                next.Add(n);
                            }
                        }
                    }
                }

                if (next.Count == 0)
                    return new SolveResult { Ok = false, Why = "hết nhánh ở độ sâu " + d, Nodes = nodes };

                int top = next.Max(x => x.Cleared);
                if (top > bestCleared) { bestCleared = top; stale = 0; } else stale++;
                if (stale > 40)
                    return new SolveResult { Ok = false, Why = "bế tắc ở " + bestCleared + " nhóm", Nodes = nodes };

                layer = next.Select(x => new { s = Score(x), g = x })
                            .OrderByDescending(x => x.s).Take(beam).Select(x => x.g).ToList();
            }
            return new SolveResult { Ok = false, Why = "quá maxDepth", Nodes = nodes };
        }
    }

    // ----------------------------------------------------------- SelfCheck
    // Fail = throw. Chạy được cả console (csc) lẫn Editor lúc Play.
    // Mỗi assert dưới đây đối chiếu 1-1 với demo/check.mjs.
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
              { ""id"":""l3"",""text"":""L3"" },{ ""id"":""l4"",""art"":""apple"" } ]},
            { ""id"":""root"", ""text"":""Root"", ""cards"":[
              { ""id"":""r1"",""text"":""R1"" },{ ""id"":""r2"",""text"":""R2"" },
              { ""id"":""r3"",""text"":""R3"" } ]}
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
                    " — thả vào Assets/Prototype/Resources/Art/");

            Func<int, LevelData> fresh = i => LevelData.Parse(levelJsons[i]);
            Func<int, bool, Game> load = (i, drain) =>
            {
                var lv = fresh(i);
                lv.Validate(hasArt);
                return Game.Build(lv).Settle(drain);
            };

            // ---- 1. Validate bắt được level hỏng ----
            Action<Action<LevelData>, string, Predicate<string>> brokenAs = (mutate, label, art) =>
            {
                var lv = fresh(0);
                mutate(lv);
                bool threw = false;
                try { lv.Validate(art); } catch { threw = true; }
                Ok(threw, "validate phải ném lỗi: " + label);
            };
            Action<Action<LevelData>, string> broken = (mutate, label) => brokenAs(mutate, label, hasArt);

            broken(l => l.Stacks[0].Boxes[0].Slots[3] = "apple", "card trùng trên bàn");
            broken(l => l.Stacks[0].Boxes[0].Slots[0] = null, "card thiếu trên bàn");
            broken(l => l.Stacks[0].Boxes[0].Slots[0] = "fruit", "đặt group lên bàn");
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
                var strictG = load(0, false);
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
            broken(l => { foreach (var c in l.AllCards()) c.Text = c.Text ?? c.Id; },
                   "level không còn thẻ chỉ-ảnh");
            broken(l =>
            {
                var withArt = l.AllCards().Where(c => c.Art != null).ToList();
                withArt[1].Art = withArt[0].Art;
            }, "hai thẻ dùng chung một ảnh");
            // Mọi thẻ đều có art → không còn thẻ chỉ-chữ. Phải cấp key GIẢ và DUY NHẤT cho
            // từng thẻ, nếu không luật "trùng ảnh" sẽ bắn trước và test này kiểm nhầm thứ.
            brokenAs(l =>
            {
                int i = 0;
                foreach (var c in l.AllCards()) if (c.Art == null) c.Art = "fake-" + (i++);
            }, "level không còn thẻ chỉ-chữ", _ => true);
            broken(l => l.Stacks[1].Pos = l.Stacks[0].Pos, "hai stack trùng pos");
            broken(l => l.Stacks[0].Boxes[0].Slots = new string[3], "box không đủ 4 slot");
            broken(l => l.Groups[0].Cards[0].Art = "khong-ton-tai", "art trỏ file không có");
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
                var g = load(0, Rules.RemoveEmptyNonBottomBox);
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

                var deep = g.Stacks.First(s => s.Boxes.Count > 1);
                string buried = deep.Boxes[1].Slots.First(t => t != null).Uid;
                Ok(!g.MoveTile(g.Stacks.IndexOf(deep), buried, di),
                   "không kéo được thẻ trong box bị che");

                var full = load(0, Rules.RemoveEmptyNonBottomBox);
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
                var g = load(0, Rules.RemoveEmptyNonBottomBox);
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

                var bot = load(0, Rules.RemoveEmptyNonBottomBox);
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
                var g = load(0, Rules.RemoveEmptyNonBottomBox);
                foreach (var st in g.Stacks)
                    foreach (var b in st.Boxes)
                        for (int i = 0; i < b.Slots.Length; i++) b.Slots[i] = null;
                Ok(g.CheckStatus() == GameStatus.Won, "sạch bàn → won");

                var jam = load(0, Rules.RemoveEmptyNonBottomBox);
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
                    var r = Solver.Solve(load(i, drain), drain);
                    string mode = drain ? "rộng" : "chặt";
                    Ok(r.Ok, "level " + fresh(i).Id + " phải giải được ở chế độ " + mode +
                             " (" + (r.Why ?? "") + ")");
                    log("  " + fresh(i).Id + " " + mode + " — " + r.Depth + " nước (" + r.Nodes +
                        " nút, " + (int)(DateTime.UtcNow - start).TotalMilliseconds + "ms)");
                }
            }

            log("SelfCheck OK — " + levelJsons.Count + " level, luật khớp demo/check.mjs");
        }
    }
}
