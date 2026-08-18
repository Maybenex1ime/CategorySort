// Tầng khai báo của một màn: parse JSON + validate. Sai luật nào cũng ném Exception có
// thông điệp chỉ đúng chỗ hỏng — level hỏng phải chết lúc load, không im lặng vỡ lúc chơi.
//
// KHÔNG import UnityEngine (xem Rules.cs).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WordStack.Board
{
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
        // Field "difficulty" trong JSON KHÔNG parse ở đây nữa: runtime lấy độ khó
        // từ SO_LevelCatalog (tool CatalogBuilder seed từ JSON lúc edit-time).
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
}
