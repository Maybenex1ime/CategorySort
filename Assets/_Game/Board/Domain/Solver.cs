// Beam search chứng minh một level giải được. Chỉ dùng cho SelfCheck/tool level —
// runtime không gọi.
//
// KHÔNG import UnityEngine (xem Rules.cs).
using System;
using System.Collections.Generic;
using System.Linq;

namespace WordStack.Board
{
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
}
