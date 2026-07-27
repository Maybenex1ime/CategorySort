// ============================================================================
// THROWAWAY PROTOTYPE — Giai đoạn 1b (xem docs/development-plan.md).
// Code này KHÔNG mang sang production. Mục tiêu duy nhất: core loop có vui không?
//
// Luật chơi thuần C#, không UnityEngine — nguồn luật: design/gdd/game-concept.md (Rev 3).
// Rev 3: collector nằm CỘT GIỮA cố định + deck collector lấp ô trống;
// tray không còn riêng — mọi slot trống đều là chỗ thả. Chỉ còn 2 loại nước đi.
// Các hằng (?) trong Knobs chỉnh sau khi chơi game gốc (bước prototype research).
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CategorySort.Prototype
{
    public static class Knobs
    {
        public const int MoveBudget = 30;        // (?) ~1.4× lời giải DFS 21 nước; tune theo game gốc
        public const int MoveCost = 1;           // chốt: mọi thao tác tốn 1 move như nhau
        public const int BoardSlots = 12;        // slot chứa chồng thẻ trên lưới (4 cột × 3 hàng)
        public const int TraySlots = 5;          // hàng slot trống sẵn dưới đáy (cùng luật với slot lưới)
        public const int CollectorCells = 3;     // (?) số ô cột giữa hoạt động (game gốc mở thêm bằng ads — Tier 3)
        public static int TotalSlots => BoardSlots + TraySlots;
    }

    public enum Cat { Fruit, Animal, Vehicle, Smiley }
    public enum GameStatus { Playing, Won, Lost }
    public enum LoseReason { None, OutOfMoves, Stuck }

    // Thẻ item (trong slot) hoặc thẻ collector (trong ô cột giữa / deck).
    public class Entry
    {
        public bool IsCollector;
        public Cat Cat;
        public int Variant;   // item: index vào Art.Emoji/ItemName
        public int Quota;     // collector
        public int Progress;  // collector

        public static Entry Item(Cat c, int variant) => new Entry { Cat = c, Variant = variant };
        public static Entry Collector(Cat c, int quota) => new Entry { IsCollector = true, Cat = c, Quota = quota };
        public Entry Clone() => (Entry)MemberwiseClone();
    }

    public class Game
    {
        // Slots[0..BoardSlots-1] = lưới, [BoardSlots..] = hàng khay. Phần tử CUỐI list = thẻ trên cùng.
        // Slot trống (Count==0) là chỗ thả hợp lệ — khay hay lưới như nhau.
        public List<List<Entry>> Slots = new List<List<Entry>>();
        // Cột giữa: các ô collector (null = trống). Deck ở góc phải trên lấp vào ô trống theo thứ tự.
        public Entry[] Cells = new Entry[Knobs.CollectorCells];
        public Queue<Entry> Deck = new Queue<Entry>();
        public int MovesLeft = Knobs.MoveBudget;
        public GameStatus Status = GameStatus.Playing;
        public LoseReason LoseReason = LoseReason.None;

        public Entry Top(int slot) => Slots[slot].Count > 0 ? Slots[slot][Slots[slot].Count - 1] : null;

        // --- 2 loại nước đi ---

        // Kéo thẻ trên cùng của 1 slot vào ô collector cùng category.
        // Kéo vào collector SAI category: thẻ không vào nhưng VẪN trừ move (phạt thao tác sai).
        public bool Collect(int slot, int cell)
        {
            if (Status != GameStatus.Playing) return false;
            var t = Top(slot);
            var c = cell >= 0 && cell < Cells.Length ? Cells[cell] : null;
            if (t == null || c == null) return false;
            if (c.Cat != t.Cat) { Spend(); return false; }
            Slots[slot].RemoveAt(Slots[slot].Count - 1);
            c.Progress++;
            if (c.Progress >= c.Quota)
                Cells[cell] = Deck.Count > 0 ? Deck.Dequeue() : null; // (?) giả định lấp ngay lập tức
            Spend();
            return true;
        }

        // Kéo thẻ trên cùng của 1 slot sang slot trống bất kỳ (khay hay lưới như nhau).
        public bool MoveToSlot(int from, int to)
        {
            if (Status != GameStatus.Playing) return false;
            var t = Top(from);
            if (t == null || from == to || Slots[to].Count > 0) return false;
            Slots[from].RemoveAt(Slots[from].Count - 1);
            Slots[to].Add(t);
            Spend();
            return true;
        }

        void Spend()
        {
            MovesLeft -= Knobs.MoveCost;
            if (AllCollectorsDone()) { Status = GameStatus.Won; return; }
            if (MovesLeft <= 0) { Status = GameStatus.Lost; LoseReason = LoseReason.OutOfMoves; return; }
            if (IsStuck()) { Status = GameStatus.Lost; LoseReason = LoseReason.Stuck; }
        }

        public bool AllCollectorsDone() => Deck.Count == 0 && Cells.All(c => c == null);

        // Kẹt = không gom được thẻ nào VÀ không còn slot trống để đảo thẻ.
        public bool IsStuck()
        {
            bool anyTop = false, anyEmpty = false;
            for (int s = 0; s < Slots.Count; s++)
            {
                var t = Top(s);
                if (t == null) { anyEmpty = true; continue; }
                anyTop = true;
                foreach (var c in Cells)
                    if (c != null && c.Cat == t.Cat) return false; // còn nước gom
            }
            return !(anyEmpty && anyTop); // còn nước đảo slot thì chưa kẹt
        }

        public Game Clone()
        {
            return new Game
            {
                MovesLeft = MovesLeft,
                Status = Status,
                LoseReason = LoseReason,
                Slots = Slots.Select(p => p.Select(e => e.Clone()).ToList()).ToList(),
                Cells = Cells.Select(c => c?.Clone()).ToArray(),
                Deck = new Queue<Entry>(Deck.Select(e => e.Clone())),
            };
        }

        // ---- Level 1 hardcode (deterministic — pillar 3) ----
        // Layout: mỗi slot lưới liệt kê TỪ TRÊN XUỐNG. Khay (5 slot) trống.
        // Invariant cân bằng: mỗi category số thẻ = quota collector → tổng thẻ (22) = tổng quota
        // (F7 A6 V4 S5) — thắng là dọn sạch bàn. SelfCheck bắt buộc invariant này.
        // Cột giữa khởi đầu: F, A, V (lấy từ deck); deck còn lại: S.
        // S-collector chỉ xuất hiện sau khi 1 ô clear → thẻ S lộ sớm buộc phải đem đi gửi slot trống.
        public static Game Level1()
        {
            Entry I(Cat c, int v) => Entry.Item(c, v);
            var topDown = new[]
            {
                new[] { I(Cat.Smiley, 0), I(Cat.Animal, 0), I(Cat.Fruit, 0) },
                new[] { I(Cat.Smiley, 4), I(Cat.Vehicle, 0), I(Cat.Fruit, 1) },
                new[] { I(Cat.Animal, 1), I(Cat.Smiley, 1) },
                new[] { I(Cat.Fruit, 2), I(Cat.Animal, 2) },
                new[] { I(Cat.Vehicle, 1), I(Cat.Fruit, 3) },
                new[] { I(Cat.Animal, 3), I(Cat.Smiley, 2) },
                new[] { I(Cat.Smiley, 3), I(Cat.Fruit, 4) },
                new[] { I(Cat.Animal, 4), I(Cat.Vehicle, 2) },
                new[] { I(Cat.Fruit, 5), I(Cat.Animal, 5) },
                new[] { I(Cat.Vehicle, 3) },
                new[] { I(Cat.Fruit, 6) },
                new Entry[] { },
            };
            var g = new Game();
            foreach (var pile in topDown)
                g.Slots.Add(pile.Reverse().ToList()); // list lưu đáy→đỉnh
            for (int i = 0; i < Knobs.TraySlots; i++)
                g.Slots.Add(new List<Entry>());
            var deck = new Queue<Entry>();
            deck.Enqueue(Entry.Collector(Cat.Fruit, 7));
            deck.Enqueue(Entry.Collector(Cat.Animal, 6));
            deck.Enqueue(Entry.Collector(Cat.Vehicle, 4));
            deck.Enqueue(Entry.Collector(Cat.Smiley, 5));
            g.Deck = deck;
            for (int c = 0; c < g.Cells.Length && g.Deck.Count > 0; c++)
                g.Cells[c] = g.Deck.Dequeue();
            return g;
        }
    }

    // Bảng art placeholder — emoji hệ thống + tên (view dùng; domain chỉ giữ để 1 nguồn).
    public static class Art
    {
        public static readonly string[] CatName = { "Trái cây", "Động vật", "Xe cộ", "Mặt cười" };
        public static readonly string[][] Emoji =
        {
            new[] { "🍎", "🍌", "🍇", "🍊", "🍓", "🍉", "🍍" },
            new[] { "🐶", "🐱", "🐭", "🐰", "🦊", "🐻" },
            new[] { "🚗", "🚕", "🚌", "🚓" },
            new[] { "😀", "😎", "😴", "🤠", "🥳" },
        };
        public static readonly string[][] ItemName =
        {
            new[] { "Táo", "Chuối", "Nho", "Cam", "Dâu", "Dưa", "Dứa" },
            new[] { "Chó", "Mèo", "Chuột", "Thỏ", "Cáo", "Gấu" },
            new[] { "Ô tô", "Taxi", "Buýt", "Cảnh sát" },
            new[] { "Cười", "Ngầu", "Ngủ", "Cao bồi", "Tiệc" },
        };
    }

    // Self-check: chạy console (ngoài Unity) hoặc trong Editor lúc Play.
    // Fail = throw. Gồm DFS solver nhỏ chứng minh Level1 giải được trong budget
    // (mầm của "Level Solver" trong development plan).
    public static class SelfCheck
    {
        public static void Run(Action<string> log)
        {
            // 1. Cấu trúc level + luật gom
            var g = Game.Level1();
            if (g.Slots.Count != Knobs.TotalSlots) throw new Exception("Level1: sai số slot");
            if (g.Slots.Sum(p => p.Count) != 22) throw new Exception("Level1: sai số item");
            if (g.Cells.Count(c => c != null) != 3 || g.Deck.Count != 1) throw new Exception("Level1: sai cột giữa/deck");
            // Invariant cân bằng: mỗi category số thẻ = tổng quota (bằng tổng là hệ quả;
            // lệch ở bất kỳ category nào là level không thể thắng hoặc có thẻ thừa).
            foreach (Cat cat in Enum.GetValues(typeof(Cat)))
            {
                int items = g.Slots.Sum(p => p.Count(e => e.Cat == cat));
                int quota = g.Cells.Where(c => c != null && c.Cat == cat).Sum(c => c.Quota)
                          + g.Deck.Where(c => c.Cat == cat).Sum(c => c.Quota);
                if (items != quota)
                    throw new Exception("Level1: category " + cat + " có " + items + " thẻ nhưng tổng quota " + quota);
            }
            int fruitCell = Array.FindIndex(g.Cells, c => c != null && c.Cat == Cat.Fruit);
            int animalCell = Array.FindIndex(g.Cells, c => c != null && c.Cat == Cat.Animal);
            if (g.Collect(3, animalCell)) throw new Exception("Gom sai category phải bị từ chối"); // F3 vào ô Animal
            if (g.MovesLeft != Knobs.MoveBudget - Knobs.MoveCost) throw new Exception("Gom sai vẫn phải trừ move (phạt)");
            if (g.Top(3) == null || g.Top(3).Cat != Cat.Fruit) throw new Exception("Gom sai thì thẻ phải nằm nguyên chỗ cũ");
            if (!g.Collect(3, fruitCell)) throw new Exception("Gom đúng category phải được");
            if (g.MovesLeft != Knobs.MoveBudget - 2 * Knobs.MoveCost) throw new Exception("Gom đúng phải trừ move");

            // 2. Luật đảo slot: chỉ vào slot trống, khay hay lưới như nhau
            var g2 = Game.Level1();
            if (g2.MoveToSlot(0, 1)) throw new Exception("Thả vào slot có thẻ phải bị từ chối");
            if (g2.MoveToSlot(11, 12)) throw new Exception("Kéo từ slot trống phải bị từ chối");
            if (!g2.MoveToSlot(0, 11)) throw new Exception("Thả vào slot lưới trống phải được");
            if (!g2.MoveToSlot(1, 12)) throw new Exception("Thả vào slot khay phải được");
            if (g2.Top(12) == null || g2.Top(12).Cat != Cat.Smiley) throw new Exception("Thẻ phải nằm ở slot đích");

            // 3. Deck lấp ô trống ngay khi collector clear
            var g3 = new Game();
            g3.Slots.Add(new List<Entry> { Entry.Item(Cat.Fruit, 0) });
            g3.Slots.Add(new List<Entry>());
            g3.Cells[0] = Entry.Collector(Cat.Fruit, 1);
            g3.Deck.Enqueue(Entry.Collector(Cat.Smiley, 1));
            g3.Collect(0, 0);
            if (g3.Cells[0] == null || g3.Cells[0].Cat != Cat.Smiley) throw new Exception("Deck phải lấp ô collector trống");

            // 4. Kẹt: không slot trống + không thẻ nào gom được
            var g4 = new Game();
            g4.Slots.Add(new List<Entry> { Entry.Item(Cat.Animal, 0) });
            g4.Cells[0] = Entry.Collector(Cat.Fruit, 2);
            if (!g4.IsStuck()) throw new Exception("Phải phát hiện kẹt (không slot trống, không gom được)");
            g4.Slots.Add(new List<Entry>());
            if (g4.IsStuck()) throw new Exception("Còn slot trống thì chưa kẹt");

            // 5. Hết move → thua
            var g5 = Game.Level1();
            g5.MovesLeft = 1;
            g5.MoveToSlot(0, 12);
            if (g5.Status != GameStatus.Lost || g5.LoseReason != LoseReason.OutOfMoves) throw new Exception("Hết move phải thua");

            // 6. Level1 giải được trong budget — nhưng PHẢI cần tới nước đảo slot
            // (nếu chỉ gom thẳng mà thắng thì level không test được cơ chế đào/gửi thẻ).
            var solution = Solve(Game.Level1(), allowSlotMoves: true);
            if (solution == null) throw new Exception("Level1 KHÔNG giải được trong move budget " + Knobs.MoveBudget);
            if (Solve(Game.Level1(), allowSlotMoves: false) != null)
                throw new Exception("Level1 giải được không cần đảo slot — layout quá dễ, cần sửa");
            log("SelfCheck OK — Level1 giải được trong " + solution.Count + " nước (budget " + Knobs.MoveBudget
                + "), nước đảo slot là bắt buộc");
        }

        // DFS tìm 1 lời giải bất kỳ. Memo theo best MovesLeft từng thấy per state
        // (cùng trạng thái bàn có thể đến với chi phí khác nhau nên chỉ prune khi
        // đến lại mà không dư move hơn). Encode chuẩn hóa: mọi slot trống tương
        // đương nhau và vị trí slot không ảnh hưởng luật → sort để gộp trạng thái.
        static List<string> Solve(Game start, bool allowSlotMoves)
        {
            var visited = new Dictionary<string, int>();
            var path = new List<string>();
            return Dfs(start, path, visited, allowSlotMoves) ? path : null;
        }

        static bool Dfs(Game g, List<string> path, Dictionary<string, int> visited, bool allowSlotMoves)
        {
            if (g.Status == GameStatus.Won) return true;
            if (g.Status != GameStatus.Playing) return false;
            var key = Encode(g);
            if (visited.TryGetValue(key, out var best) && g.MovesLeft <= best) return false;
            visited[key] = g.MovesLeft;

            // Ưu tiên gom (thường tối ưu), sau đó mới đảo slot.
            for (int cell = 0; cell < g.Cells.Length; cell++)
            {
                if (g.Cells[cell] == null) continue;
                var cat = g.Cells[cell].Cat;
                for (int s = 0; s < g.Slots.Count; s++)
                {
                    var t = g.Top(s);
                    if (t == null || t.Cat != cat) continue;
                    var n = g.Clone();
                    n.Collect(s, cell);
                    path.Add("slot" + s + "->cell" + cell);
                    if (Dfs(n, path, visited, allowSlotMoves)) return true;
                    path.RemoveAt(path.Count - 1);
                }
            }
            if (!allowSlotMoves) return false;
            int empty = -1; // mọi slot trống tương đương — chỉ thử 1 đại diện
            for (int s = 0; s < g.Slots.Count; s++)
                if (g.Slots[s].Count == 0) { empty = s; break; }
            if (empty < 0) return false;
            for (int s = 0; s < g.Slots.Count; s++)
            {
                if (g.Slots[s].Count == 0) continue;
                var n = g.Clone();
                n.MoveToSlot(s, empty);
                path.Add("slot" + s + "->slot" + empty);
                if (Dfs(n, path, visited, allowSlotMoves)) return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        static string Encode(Game g)
        {
            var pileKeys = new List<string>();
            foreach (var pile in g.Slots)
                pileKeys.Add(string.Join(",", pile.Select(e => (int)e.Cat + "." + e.Variant)));
            pileKeys.Sort();
            var cellKeys = g.Cells.Select(c => c == null ? "-" : (int)c.Cat + "." + c.Progress).OrderBy(x => x);
            return string.Join("|", pileKeys) + "#" + string.Join("|", cellKeys) + "#" + g.Deck.Count;
        }
    }
}
