// Booster Shuffle: xáo lại nội dung thẻ để mở đường cho người chơi, KHÔNG phát clear
// miễn phí — mỗi nhóm nó dựng ra tốn đúng 1 nước đi để nổ. Tách khỏi Game.cs để file đó
// chỉ còn luật bàn chơi; đây là luật của booster.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System.Collections.Generic;

namespace WordStack.Board
{
    /// <summary>Địa chỉ một ô trên bàn. Box = 0 là hộp trên cùng.</summary>
    public struct SlotRef
    {
        public int Stack, Box, Slot;
    }

    /// <summary>Một thẻ đổi chỗ trong lượt shuffle — view dùng để animate.</summary>
    public struct ShuffleMove
    {
        public string Uid;
        public SlotRef From, To;
    }

    public struct ShuffleResult
    {
        public bool Ok;
        public ShuffleMove[] Moves;   // rỗng khi Ok = false
        public int PrimedGroups;
    }

    public partial class Game
    {
        /// <summary>
        /// Thẻ ở ô này có đang ĐỨNG LẺ trong hộp không (nhóm của nó có &lt;2 thẻ trong
        /// chính hộp đó). Đúng bằng điều kiện BoxColorIndices dùng để KHÔNG cấp màu, nên
        /// "trắng" ở đây là thứ người chơi thật sự nhìn thấy là trắng.
        ///
        /// Ô trống trả false: không có thẻ thì không phải thẻ trắng.
        /// </summary>
        public static bool IsWhite(Box box, int slot)
        {
            if (box == null || slot < 0 || slot >= box.Slots.Length) return false;
            Tile t = box.Slots[slot];
            if (t == null) return false;

            int same = 0;
            for (int i = 0; i < box.Slots.Length; i++)
            {
                Tile o = box.Slots[i];
                if (o != null && o.GroupId == t.GroupId) same++;
            }
            return same < 2;
        }

        /// <summary>Tổng số thẻ ở lớp trên cùng. Bất biến 1 của Shuffle giữ con số này.</summary>
        public int TopLayerTileCount()
        {
            int n = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] != null) n++;
            }
            return n;
        }

        /// <summary>
        /// Có hộp nào trên TOÀN BÀN đủ 4 thẻ cùng nhóm không — kể cả hộp bị chôn.
        ///
        /// Phải phủ cả hộp chôn: SettleStep chỉ soi top box nên bộ đủ 4 nằm dưới sẽ im
        /// lặng rồi nổ đúng lúc hộp trên bị xoá, người chơi tốn 0 nước. Đó là clear miễn
        /// phí đến trễ một nhịp, vẫn vi phạm nguyên tắc của Shuffle.
        /// </summary>
        public bool AnyBoxHasFullGroup()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    var count = new Dictionary<string, int>();
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        int n;
                        count.TryGetValue(slots[i].GroupId, out n);
                        count[slots[i].GroupId] = n + 1;
                        if (n + 1 >= Rules.GroupSize) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Bàn có xáo được không. Cần ≥1 ô trống ở lớp trên để hộp chủ chừa được chỗ cho
        /// người chơi thả thẻ thứ 4 — không có ô trống thì Nhóm mồi 3+1 vô nghĩa.
        ///
        /// Chỉ là điều kiện cần, rẻ — nút Shuffle sáng theo ShuffleWouldChange (chạy thử
        /// trên bản sao), không theo hàm này.
        /// </summary>
        public bool CanShuffle()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;   // hộp đóng không nhận thẻ
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null) return true;
            }
            return false;
        }

        /// <summary>
        /// Số Nhóm mồi 3+1 đang có sẵn: một top box giữ đúng 3 thẻ cùng nhóm VÀ còn ô
        /// trống, cộng thêm ≥1 thẻ nữa của nhóm đó ở top box khác.
        ///
        /// Ô trống là bắt buộc: MoveTile từ chối hộp đích đầy, nên hộp chủ đầy 4 ô thì
        /// người chơi không thả được thẻ thứ 4 vào — không còn là "đúng 1 nước".
        /// </summary>
        public int CountPrimedGroups()
        {
            var primed = new HashSet<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box host = TopBox(s);
                if (host == null) continue;

                bool hasFree = false;
                var count = new Dictionary<string, int>();
                for (int i = 0; i < host.Slots.Length; i++)
                {
                    if (host.Slots[i] == null) { hasFree = true; continue; }
                    int n;
                    count.TryGetValue(host.Slots[i].GroupId, out n);
                    count[host.Slots[i].GroupId] = n + 1;
                }
                if (!hasFree) continue;

                foreach (var kv in count)
                {
                    if (kv.Value != Rules.GroupSize - 1) continue;
                    if (CountGroupOnTopLayerExcept(kv.Key, s) > 0) primed.Add(kv.Key);
                }
            }
            return primed.Count;
        }

        int CountGroupOnTopLayerExcept(string gid, int exceptStack)
        {
            int n = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                if (s == exceptStack) continue;
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] != null && top.Slots[i].GroupId == gid) n++;
            }
            return n;
        }

        /// <summary>
        /// Mọi nhóm đáng dựng mồi theo thứ tự thử, nhiều nhất <paramref name="max"/> nhóm.
        ///
        /// Nhận nhóm có ĐỦ 4 thẻ đang tồn tại trên bàn (nhóm cha còn nhóm con chưa collapse
        /// thì thiếu thẻ — cùng ràng buộc với Magnet), KHÔNG thẻ nào nằm trong hộp khoá, và
        /// tối đa MỘT thẻ bất động (băng hoặc đóng đinh). Thẻ bất động đó phải ở lớp trên:
        /// nó không dời được nên hộp chứa nó chính là hộp chủ (spec 2026-10-02 Mục 1).
        ///
        /// Thứ tự: nhóm không băng trước; giữa các nhóm băng thì băng còn ít nước tan trước
        /// (mồi có băng chỉ nổ khi băng tan); rồi nhiều thẻ sẵn ở lớp trên trước (ít phải kéo
        /// donor); hoà thì theo group id cho kết quả xác định, test lại được.
        /// </summary>
        public List<string> PickPrimeCandidates(int max)
        {
            var onBoard = new Dictionary<string, int>();
            var onTop = new Dictionary<string, int>();
            var pinned = new Dictionary<string, int>();
            var iceLeft = new Dictionary<string, int>();
            var bad = new HashSet<string>();
            var order = new List<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        Tile t = slots[i];
                        if (t == null) continue;
                        string gid = t.GroupId;
                        if (!onBoard.ContainsKey(gid))
                        {
                            order.Add(gid);
                            onBoard[gid] = 0; onTop[gid] = 0; pinned[gid] = 0; iceLeft[gid] = 0;
                        }
                        onBoard[gid]++;
                        if (b == 0) onTop[gid]++;
                        if (!IsOpen(boxes[b].Lock)) bad.Add(gid);   // hộp khoá: bỏ cả nhóm
                        if (!IsFrozen(t) && !IsFixed(t)) continue;
                        pinned[gid]++;
                        if (b != 0) bad.Add(gid);                   // băng bị chôn không làm mốc được
                        if (IsFrozen(t)) iceLeft[gid] = t.Lock.Need - t.Lock.Have;
                    }
                }
            }

            var eligible = new List<string>();
            for (int k = 0; k < order.Count; k++)
            {
                string gid = order[k];
                if (onBoard[gid] == Rules.GroupSize && !bad.Contains(gid) && pinned[gid] <= 1) eligible.Add(gid);
            }

            eligible.Sort(delegate (string a, string b)
            {
                if (iceLeft[a] != iceLeft[b]) return iceLeft[a] - iceLeft[b];   // 0 = không băng → lên đầu
                if (onTop[a] != onTop[b]) return onTop[b] - onTop[a];
                return string.CompareOrdinal(a, b);
            });

            if (eligible.Count > max) eligible.RemoveRange(max, eligible.Count - max);
            return eligible;
        }

        static int SlotKey(int stack, int slot) { return stack * Rules.BoxCapacity + slot; }

        /// <summary>
        /// Các ô ở lớp trên cùng Shuffle được phép đụng: ô đang giữ thẻ TRẮNG, và ô TRỐNG
        /// (đích để dịch chỗ). Ô giữ thẻ có màu bị loại — đó là cụm người chơi đã gom.
        /// </summary>
        public List<SlotRef> AssignableTopSlots()
        {
            var result = new List<SlotRef>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;   // hộp đóng: không đụng (spec 4.3)
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null || (IsWhite(top, i) && !IsFrozen(top.Slots[i]) && !IsFixed(top.Slots[i])))
                        result.Add(new SlotRef { Stack = s, Box = 0, Slot = i });
            }
            return result;
        }

        /// <summary>
        /// Nhấc mọi thẻ ở ô khả dụng vào tay. Sau bước này MỌI ô khả dụng đều trống —
        /// đó là thứ làm cả lớp lỗi "pha sau đè pha trước" biến mất, vì không pha nào
        /// còn phải drain lại.
        /// </summary>
        public void DrainAll(List<SlotRef> pool, List<Tile> hand)
        {
            for (int k = 0; k < pool.Count; k++)
            {
                Box box = TopBox(pool[k].Stack);
                Tile t = box.Slots[pool[k].Slot];
                if (t == null) continue;
                hand.Add(t);
                box.Slots[pool[k].Slot] = null;
            }
        }

        /// <summary>
        /// Dựng Nhóm mồi 3+1: 3 thẻ trong hộp chủ (hộp đó còn ĐÚNG 1 ô trống được giữ chỗ),
        /// thẻ thứ 4 ở top box khác. Người chơi kéo một nước là nổ.
        ///
        /// Hộp chủ chọn theo thứ tự (spec 2026-10-02 Mục 2):
        ///   1. Có MỐC — thẻ gid Shuffle không dời được (băng, đóng đinh, cụm người chơi đã
        ///      gom) nằm gọn trong MỘT hộp → hộp đó là hộp chủ, mốc tính vào 3 thẻ.
        ///   2. Hai cụm đôi gid ở hai hộp → TryPrimeFromTwoPairs.
        ///   3. Hộp có đủ 4 ô mở, ưu tiên hộp có layer 2 nhiều thẻ nhất: nổ xong hộp chủ bị
        ///      xoá, hộp dưới lộ ra, ngồi trên hộp đầy thì lượt sau có nhiều nguyên liệu nhất.
        ///   4. Không hộp nào đủ 4 ô → dời một cụm đôi để giải phóng hộp (PlanPairMerge).
        /// Mốc rải theo hình khác (vd băng ở một hộp + đôi ở hộp khác) → bỏ nhóm này.
        ///
        /// <paramref name="movers"/> nhận uid thẻ có màu bị dời hợp lệ — ValidateShuffle
        /// miễn kiểm "thẻ có màu đứng yên" cho đúng những thẻ đó.
        ///
        /// Trả false khi không xếp nổi — bên gọi phải coi đó là bình thường, không phải lỗi.
        /// </summary>
        public bool TryPrimeGroup(string gid, List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand,
                                  HashSet<string> movers = null)
        {
            // Sau DrainAll thẻ trắng đã vào tay, nên thẻ gid còn ở lớp trên đều là mốc.
            var anchorStacks = new List<int>();
            int anchors = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                int n = top == null ? 0 : CountGroupInBox(top, gid);
                if (n == 0) continue;
                anchorStacks.Add(s);
                anchors += n;
            }
            if (anchorStacks.Count == 2)
                return TryPrimeFromTwoPairs(gid, anchorStacks[0], anchorStacks[1], pool, reserved, movers);
            if (anchorStacks.Count > 2) return false;

            int host, mergeTo = -1;
            if (anchorStacks.Count == 1)
            {
                host = anchorStacks[0];
                // Đủ 4 thẻ trong một hộp mà chưa nổ = còn thẻ băng, chờ tan — không có gì để dựng.
                if (anchors >= Rules.GroupSize) return false;
                if (OpenCount(pool, reserved, host) < Rules.GroupSize - anchors) return false;
            }
            else
            {
                host = -1;
                for (int s = 0; s < Stacks.Count; s++)
                {
                    if (OpenCount(pool, reserved, s) < Rules.GroupSize) continue;
                    if (host < 0 || Layer2TileCount(s) > Layer2TileCount(host)) host = s;
                }
                if (host < 0 && !PlanPairMerge(pool, reserved, out host, out mergeTo)) return false;
            }

            // Hộp mang thẻ thứ 4: ưu tiên hộp mở ĐANG RỖNG — thẻ thứ 4 lấp luôn hộp đó. Lấy "stack
            // đầu tiên còn ô mở" thì ba thẻ thứ 4 của ba nhóm mồi dồn chung một hộp, pha lấp hộp
            // (Mục 4) hết thẻ mượn và cả lượt xáo rollback: bàn 14 thẻ / 8 hộp từng xám nút vì vậy.
            int carrier = -1;
            for (int s = 0; s < Stacks.Count && carrier < 0; s++)
                if (s != host && BoxTileCount(TopBox(s)) == 0 && OpenAfterMerge(pool, reserved, s, mergeTo) > 0) carrier = s;
            for (int s = 0; s < Stacks.Count && carrier < 0; s++)
                if (s != host && OpenAfterMerge(pool, reserved, s, mergeTo) > 0) carrier = s;
            if (carrier < 0) return false;

            // Gom thẻ TRƯỚC khi dời cặp: gom thất bại thì lớp trên chưa bị đụng gì.
            var need = new List<Tile>();
            for (int k = 0; k < Rules.GroupSize - anchors; k++)
            {
                Tile t = TakeFromHand(hand, gid);
                if (t == null) t = SwapDonorIntoHand(gid, hand);
                if (t == null) { hand.AddRange(need); return false; }
                need.Add(t);
            }

            if (mergeTo >= 0) MergePair(host, mergeTo, pool, reserved, movers);
            ReserveGroup(host, gid, reserved);   // mốc: pha sau không được xé

            for (int k = 0; k < need.Count - 1; k++)
            {
                int slot = FirstOpen(pool, reserved, host);
                TopBox(host).Slots[slot] = need[k];
                reserved.Add(SlotKey(host, slot));
            }

            // Ô CHỪA TRỐNG — reserve để pha sau không lấp mất chỗ thả thẻ thứ 4.
            reserved.Add(SlotKey(host, FirstOpen(pool, reserved, host)));

            int cslot = FirstOpen(pool, reserved, carrier);
            TopBox(carrier).Slots[cslot] = need[need.Count - 1];
            reserved.Add(SlotKey(carrier, cslot));
            return true;
        }

        // Hai cụm đôi gid ở hai hộp, không thẻ nào băng/đóng đinh: dời một thẻ sang hộp kia
        // thành cụm 3, thẻ ở lại chính là thẻ thứ 4. Hộp nhận cần 2 ô mở (1 cho thẻ dời,
        // 1 chừa trống); hộp nhiều ô mở hơn làm hộp chủ, hoà lấy stack nhỏ hơn.
        bool TryPrimeFromTwoPairs(string gid, int a, int b, List<SlotRef> pool, HashSet<int> reserved,
                                  HashSet<string> movers)
        {
            if (!IsMovablePair(TopBox(a), gid) || !IsMovablePair(TopBox(b), gid)) return false;
            int host = OpenCount(pool, reserved, b) > OpenCount(pool, reserved, a) ? b : a;
            int from = host == a ? b : a;
            if (OpenCount(pool, reserved, host) < 2) return false;

            Box src = TopBox(from);
            int i = 0;
            while (src.Slots[i] == null || src.Slots[i].GroupId != gid) i++;
            Tile moved = src.Slots[i];
            src.Slots[i] = null;   // ô này không thuộc pool nên pha sau không lấp — đúng ý

            int slot = FirstOpen(pool, reserved, host);
            TopBox(host).Slots[slot] = moved;
            if (movers != null) movers.Add(moved.Uid);
            ReserveGroup(host, gid, reserved);
            ReserveGroup(from, gid, reserved);
            reserved.Add(SlotKey(host, FirstOpen(pool, reserved, host)));   // ô chừa trống
            return true;
        }

        static bool IsMovablePair(Box box, string gid)
        {
            int n = 0;
            foreach (Tile t in box.Slots)
            {
                if (t == null || t.GroupId != gid) continue;
                if (IsFrozen(t) || IsFixed(t)) return false;
                n++;
            }
            return n == 2;
        }

        void ReserveGroup(int stack, string gid, HashSet<int> reserved)
        {
            Box top = TopBox(stack);
            for (int i = 0; i < top.Slots.Length; i++)
                if (top.Slots[i] != null && top.Slots[i].GroupId == gid) reserved.Add(SlotKey(stack, i));
        }

        /// <summary>
        /// Không hộp nào đủ 4 ô mở: tìm hộp X mà thứ duy nhất chiếm chỗ là MỘT cụm đôi, và
        /// hộp D nhận được cặp đó (≥2 ô mở, chưa có thẻ nhóm đó — 2+2 cùng nhóm là tự nổ).
        /// Dời cặp đi thì X trống 4 ô, làm hộp chủ được. Chỉ LẬP kế hoạch, chưa dời — gom
        /// thẻ còn có thể thất bại; MergePair chạy sau khi đã chắc chắn.
        ///
        /// X: layer 2 nhiều thẻ nhất (cùng lý do với hộp chủ thường), hoà lấy stack nhỏ.
        /// D: stack nhỏ nhất thoả điều kiện VÀ sau khi nhận cặp vẫn còn hộp mang thẻ thứ 4.
        /// </summary>
        bool PlanPairMerge(List<SlotRef> pool, HashSet<int> reserved, out int host, out int dest)
        {
            host = -1; dest = -1;
            for (int x = 0; x < Stacks.Count; x++)
            {
                string pair = LonePairGroup(pool, reserved, x);
                if (pair == null) continue;
                if (host >= 0 && Layer2TileCount(x) <= Layer2TileCount(host)) continue;
                for (int d = 0; d < Stacks.Count; d++)
                {
                    if (d == x || OpenCount(pool, reserved, d) < 2) continue;
                    if (CountGroupInBox(TopBox(d), pair) > 0) continue;
                    bool carrier = false;
                    for (int s = 0; s < Stacks.Count && !carrier; s++)
                        if (s != x && OpenAfterMerge(pool, reserved, s, d) > 0) carrier = true;
                    if (!carrier) continue;
                    host = x; dest = d;
                    break;
                }
            }
            return host >= 0;
        }

        // Nhóm của cụm đôi nếu hộp chỉ còn đúng cụm đôi đó (2 thẻ cùng nhóm, không băng/đinh,
        // chưa reserved) + 2 ô mở. Ngược lại null.
        string LonePairGroup(List<SlotRef> pool, HashSet<int> reserved, int x)
        {
            Box top = TopBox(x);
            if (top == null || OpenCount(pool, reserved, x) != Rules.GroupSize - 2) return null;
            string gid = null;
            int n = 0;
            for (int i = 0; i < top.Slots.Length; i++)
            {
                Tile t = top.Slots[i];
                if (t == null) continue;
                if (reserved.Contains(SlotKey(x, i)) || IsFrozen(t) || IsFixed(t)) return null;
                if (gid != null && t.GroupId != gid) return null;
                gid = t.GroupId;
                n++;
            }
            return n == 2 ? gid : null;
        }

        int OpenAfterMerge(List<SlotRef> pool, HashSet<int> reserved, int stack, int mergeTo)
        {
            return OpenCount(pool, reserved, stack) - (stack == mergeTo ? 2 : 0);
        }

        // Dời cụm đôi của hộp x sang hộp d, vẫn là một cặp. Ô cũ của cặp vào pool để x thành
        // hộp chủ 4 ô mở.
        void MergePair(int x, int d, List<SlotRef> pool, HashSet<int> reserved, HashSet<string> movers)
        {
            Box src = TopBox(x), dst = TopBox(d);
            for (int i = 0; i < src.Slots.Length; i++)
            {
                Tile t = src.Slots[i];
                if (t == null) continue;
                int slot = FirstOpen(pool, reserved, d);
                dst.Slots[slot] = t;
                reserved.Add(SlotKey(d, slot));
                if (movers != null) movers.Add(t.Uid);
                src.Slots[i] = null;
                pool.Add(new SlotRef { Stack = x, Box = 0, Slot = i });
            }
        }

        /// <summary>
        /// Mỗi top box ĐANG MỞ phải giữ ≥1 thẻ. Hộp top rỗng bị SettleStep xoá, hộp dưới lộ
        /// ra, tổng thẻ lớp trên tăng — vỡ bất biến 1. Hộp khoá rỗng thì bỏ qua: nó không
        /// nhận thẻ và không bị xoá, đó là lỗi level chứ không phải việc của Shuffle.
        ///
        /// Tay cạn thì mượn: trước hết thẻ trắng chưa reserved; hết thì XÉ một cụm người chơi
        /// đã gom (spec 2026-10-02 Mục 3) — thẻ bị xé ghi vào <paramref name="movers"/>.
        ///
        /// Chạy TRƯỚC ClusterHand chứ không phải sau: gom cụm reserve hết ô trống, chạy
        /// sau thì không còn thẻ nào mượn được và cả lượt shuffle bị rollback oan.
        /// </summary>
        public bool EnsureEveryTopBoxOccupied(List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand,
                                              HashSet<string> movers = null)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                if (box == null) return false;
                if (!IsOpen(box.Lock) || BoxTileCount(box) > 0) continue;

                int slot = FirstOpen(pool, reserved, s);
                if (slot < 0) return false;

                if (hand.Count > 0)
                {
                    box.Slots[slot] = hand[0];
                    hand.RemoveAt(0);
                    reserved.Add(SlotKey(s, slot));
                    continue;
                }

                SlotRef donor;
                bool split = false;
                if (!FindLooseDonor(pool, reserved, s, out donor))
                {
                    if (!FindSplitDonor(reserved, s, out donor)) return false;
                    split = true;
                }

                Box db = TopBox(donor.Stack);
                Tile moved = db.Slots[donor.Slot];
                box.Slots[slot] = moved;
                db.Slots[donor.Slot] = null;
                reserved.Add(SlotKey(s, slot));
                if (split && movers != null) movers.Add(moved.Uid);
            }
            return true;
        }

        // Thẻ trắng CHƯA reserved ở hộp đang có ≥2 thẻ — ô đã reserved là Nhóm mồi, đụng
        // vào là phá thứ vừa dựng.
        bool FindLooseDonor(List<SlotRef> pool, HashSet<int> reserved, int target, out SlotRef donor)
        {
            donor = default(SlotRef);
            for (int d = 0; d < Stacks.Count; d++)
            {
                if (d == target) continue;
                Box db = TopBox(d);
                if (db == null || BoxTileCount(db) < 2) continue;
                for (int i = 0; i < db.Slots.Length; i++)
                {
                    if (db.Slots[i] == null || reserved.Contains(SlotKey(d, i)) || !InPool(pool, d, i)) continue;
                    donor = new SlotRef { Stack = d, Box = 0, Slot = i };
                    return true;
                }
            }
            return false;
        }

        // Thẻ trong một cụm người chơi đã gom, để xé lấp hộp rỗng. Không lấy thẻ băng, đóng
        // đinh, hộp khoá, hay ô reserved (mồi). Ưu tiên: xé đôi trước xé ba (cụm 3 gần nổ
        // hơn) → hộp nhiều thẻ nhất → stack nhỏ nhất → ô nhỏ nhất.
        bool FindSplitDonor(HashSet<int> reserved, int target, out SlotRef donor)
        {
            donor = default(SlotRef);
            bool found = false;
            int bestPair = 0, bestTiles = 0;
            for (int d = 0; d < Stacks.Count; d++)
            {
                Box db = TopBox(d);
                if (d == target || db == null || !IsOpen(db.Lock)) continue;
                int tiles = BoxTileCount(db);
                if (tiles < 2) continue;
                for (int i = 0; i < db.Slots.Length; i++)
                {
                    Tile t = db.Slots[i];
                    if (t == null || reserved.Contains(SlotKey(d, i)) || IsFrozen(t) || IsFixed(t)) continue;
                    int size = CountGroupInBox(db, t.GroupId);
                    if (size < 2) continue;
                    int pair = size == 2 ? 1 : 0;
                    if (found && (pair < bestPair || (pair == bestPair && tiles <= bestTiles))) continue;
                    found = true; bestPair = pair; bestTiles = tiles;
                    donor = new SlotRef { Stack = d, Box = 0, Slot = i };
                }
            }
            return found;
        }

        /// <summary>
        /// Gom cụm phần còn lại trong tay: dồn thẻ cùng nhóm về CHUNG một hộp, tối đa
        /// GroupSize-1 thẻ mỗi hộp (chạm GroupSize là tự nổ).
        ///
        /// Tối ưu theo KÍCH THƯỚC cụm chứ không phải số cụm: một hộp 3 thẻ P cách clear
        /// 1 nước, hai hộp mỗi hộp 2 thẻ P cách 2 nước. Nên duyệt nhóm nhiều thẻ trước và
        /// dồn hết mức cho từng nhóm.
        /// </summary>
        public void ClusterHand(List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand)
        {
            var count = new Dictionary<string, int>();
            var order = new List<string>();
            for (int k = 0; k < hand.Count; k++)
            {
                int n;
                if (!count.TryGetValue(hand[k].GroupId, out n)) order.Add(hand[k].GroupId);
                count[hand[k].GroupId] = n + 1;
            }
            order.Sort(delegate (string a, string b)
            {
                if (count[a] != count[b]) return count[b] - count[a];
                return string.CompareOrdinal(a, b);
            });

            for (int k = 0; k < order.Count; k++)
            {
                string gid = order[k];
                while (true)
                {
                    int have = CountInHand(hand, gid);
                    if (have == 0) break;

                    int chunk = have < Rules.GroupSize - 1 ? have : Rules.GroupSize - 1;
                    int target = -1;
                    while (chunk > 0 && target < 0)
                    {
                        target = FindStackForChunk(pool, reserved, gid, chunk);
                        if (target < 0) chunk--;
                    }
                    if (target < 0) break;

                    for (int c = 0; c < chunk; c++)
                    {
                        Tile t = TakeFromHand(hand, gid);
                        int slot = FirstOpen(pool, reserved, target);
                        TopBox(target).Slots[slot] = t;
                        reserved.Add(SlotKey(target, slot));
                    }
                }
            }

            // Thẻ sót lại: rải từng ô một, bỏ qua hộp sắp chạm GroupSize.
            while (hand.Count > 0)
            {
                int st = FindStackForChunk(pool, reserved, hand[0].GroupId, 1);
                if (st < 0) break;
                int slot = FirstOpen(pool, reserved, st);
                TopBox(st).Slots[slot] = hand[0];
                reserved.Add(SlotKey(st, slot));
                hand.RemoveAt(0);
            }
        }

        // --- phụ trợ -----------------------------------------------------------------

        static bool InPool(List<SlotRef> pool, int stack, int slot)
        {
            for (int k = 0; k < pool.Count; k++)
                if (pool[k].Stack == stack && pool[k].Slot == slot) return true;
            return false;
        }

        // Ô dùng được cho pha hiện tại: thuộc pool, chưa reserved, và đang trống.
        bool IsOpen(List<SlotRef> pool, HashSet<int> reserved, int stack, int slot)
        {
            if (reserved.Contains(SlotKey(stack, slot))) return false;
            if (!InPool(pool, stack, slot)) return false;
            return TopBox(stack).Slots[slot] == null;
        }

        int OpenCount(List<SlotRef> pool, HashSet<int> reserved, int stack)
        {
            Box top = TopBox(stack);
            if (top == null) return 0;
            int n = 0;
            for (int i = 0; i < top.Slots.Length; i++) if (IsOpen(pool, reserved, stack, i)) n++;
            return n;
        }

        int FirstOpen(List<SlotRef> pool, HashSet<int> reserved, int stack)
        {
            Box top = TopBox(stack);
            if (top == null) return -1;
            for (int i = 0; i < top.Slots.Length; i++) if (IsOpen(pool, reserved, stack, i)) return i;
            return -1;
        }

        int FindStackForChunk(List<SlotRef> pool, HashSet<int> reserved, string gid, int chunk)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                if (OpenCount(pool, reserved, s) < chunk) continue;
                if (CountGroupInBox(TopBox(s), gid) + chunk > Rules.GroupSize - 1) continue;
                return s;
            }
            return -1;
        }

        // Số thẻ ở layer 2 của một stack. Stack chỉ có một hộp thì đếm 0, xếp cuối.
        int Layer2TileCount(int stack)
        {
            List<Box> boxes = Stacks[stack].Boxes;
            if (boxes.Count < 2) return 0;
            return BoxTileCount(boxes[1]);
        }

        static int BoxTileCount(Box box)
        {
            int n = 0;
            for (int i = 0; i < box.Slots.Length; i++) if (box.Slots[i] != null) n++;
            return n;
        }

        static int CountGroupInBox(Box box, string gid)
        {
            int n = 0;
            for (int i = 0; i < box.Slots.Length; i++)
                if (box.Slots[i] != null && box.Slots[i].GroupId == gid) n++;
            return n;
        }

        static int CountInHand(List<Tile> hand, string gid)
        {
            int n = 0;
            for (int k = 0; k < hand.Count; k++) if (hand[k].GroupId == gid) n++;
            return n;
        }

        static Tile TakeFromHand(List<Tile> hand, string gid)
        {
            for (int k = 0; k < hand.Count; k++)
                if (hand[k].GroupId == gid) { Tile t = hand[k]; hand.RemoveAt(k); return t; }
            return null;
        }

        // Thiếu thẻ nhóm gid ở lớp trên thì đổi với donor ở layer dưới: thẻ donor lên tay,
        // một thẻ trong tay xuống thế chỗ nó. Đổi 1-đổi-1 nên mọi số đếm giữ nguyên.
        Tile SwapDonorIntoHand(string gid, List<Tile> hand)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 1; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null || t.GroupId != gid) continue;

                        for (int h = 0; h < hand.Count; h++)
                        {
                            // Thẻ đẩy xuống KHÔNG được làm hộp đó đủ GroupSize cùng nhóm —
                            // nó sẽ nổ đúng lúc hộp trên bị xoá, người chơi tốn 0 nước.
                            int after = CountGroupInBox(boxes[b], hand[h].GroupId)
                                      - (t.GroupId == hand[h].GroupId ? 1 : 0);
                            if (after + 1 >= Rules.GroupSize) continue;

                            // Không đẩy thẻ xuống dưới thẻ đóng đinh cùng nhóm (kể cả nhóm cha của nó):
                            // hộp chứa thẻ đinh chỉ rỗng khi nhóm đó gom xong, mà gom cần đúng thẻ
                            // đang bị chôn → kẹt vĩnh viễn (spec 2026-09-29-fixed-tile Mục 4).
                            if (PinnedAbove(boxes, b, hand[h].GroupId)) continue;

                            Tile down = hand[h];
                            hand.RemoveAt(h);
                            boxes[b].Slots[i] = down;
                            return t;
                        }
                    }
            }
            return null;
        }

        // Có thẻ đóng đinh ở hộp nào phía trên box b mà nhóm gid thuộc nhóm của nó không.
        bool PinnedAbove(List<Box> boxes, int b, string gid)
        {
            for (int a = 0; a < b; a++)
                foreach (Tile u in boxes[a].Slots)
                    if (IsFixed(u) && InGroup(gid, u.GroupId)) return true;
            return false;
        }

        /// <summary>
        /// Xáo lại lớp trên cùng theo luật Shuffle: dựng tối đa 3 Nhóm mồi 3+1, đảm bảo
        /// mỗi top box còn thẻ, rồi gom cụm phần còn lại.
        ///
        /// KHÔNG đụng danh sách Boxes và KHÔNG đổi Status — gọi Settle() ngay sau như một
        /// nước đi thường. KHÔNG tăng Moves: booster không tính là nước đi.
        ///
        /// Vi phạm bất kỳ bất biến nào thì khôi phục nguyên trạng và trả Ok = false; không
        /// thẻ nào đổi chỗ cũng trả Ok = false (bàn chưa đổi nên không cần khôi phục). Bên
        /// gọi PHẢI không trừ lượt trong cả hai trường hợp — nút sáng theo ShuffleWouldChange
        /// nên thực tế không xảy ra.
        /// </summary>
        public ShuffleResult ApplyShuffle()
        {
            var fail = new ShuffleResult { Ok = false, Moves = new ShuffleMove[0] };
            if (!CanShuffle()) return fail;

            int topBefore = TopLayerTileCount();
            var whiteBefore = new HashSet<string>();
            Dictionary<string, SlotRef> before = SnapshotPositions(whiteBefore);
            Game backup = Clone();

            // Chọn ứng viên TRƯỚC khi nhấc thẻ: PickPrimeCandidates đếm thẻ trên BÀN, mà
            // sau DrainAll thẻ trắng đã nằm trong tay nên nó sẽ đếm thiếu và trả rỗng.
            // Lấy HẾT ứng viên: ứng viên đầu dựng hỏng thì ứng viên sau vẫn có cơ hội, vòng
            // dựng tự dừng ở 3 mồi.
            List<string> candidates = PickPrimeCandidates(int.MaxValue);

            List<SlotRef> pool = AssignableTopSlots();
            var reserved = new HashSet<int>();
            var hand = new List<Tile>();
            var movers = new HashSet<string>();   // thẻ có màu được dời hợp lệ — xem ValidateShuffle
            DrainAll(pool, hand);

            int primed = 0;
            for (int k = 0; k < candidates.Count && primed < 3; k++)
                if (TryPrimeGroup(candidates[k], pool, reserved, hand, movers)) primed++;

            // Seed TRƯỚC cluster — xem ghi chú trong EnsureEveryTopBoxOccupied.
            bool seeded = EnsureEveryTopBoxOccupied(pool, reserved, hand, movers);
            ClusterHand(pool, reserved, hand);

            // hand còn thẻ = có thẻ không tìm được chỗ đặt, tức là thẻ đã rời khỏi bàn.
            if (!seeded || hand.Count > 0 || !ValidateShuffle(topBefore, before, whiteBefore, movers))
            {
                RestoreFrom(backup);
                return fail;
            }

            // Không thẻ nào đổi chỗ = bàn đã là kết quả xáo (vd bấm hai lần liên tiếp). Người chơi
            // vẫn chọn tiêu lượt, nên đổi chỗ các nhóm thẻ giữa các hộp trên (spec Mục 6). Không đổi
            // được thì mới thất bại; RotateTopBoxTiles không đụng bàn khi trả false nên không cần khôi phục.
            ShuffleMove[] moves = DiffPositions(before);
            if (moves.Length == 0)
            {
                if (!RotateTopBoxTiles()) return fail;
                moves = DiffPositions(before);
            }

            return new ShuffleResult
            {
                Ok = true,
                Moves = moves,
                PrimedGroups = CountPrimedGroups(),
            };
        }

        /// <summary>
        /// Xoay vòng NHÓM THẺ (nguyên bộ ô, giữ vị trí ô) giữa các hộp trên đang mở và không chứa thẻ
        /// băng / đóng đinh (hai loại đó không bao giờ dời). Hộp, khoá hộp và hộp chôn bên dưới đứng
        /// yên — chỉ thẻ đổi hộp, view vẫn animate thẻ bay như mọi lần xáo. Cụm đi nguyên bộ nên bất
        /// biến 1–3 giữ; bất biến 4 nới cho riêng bước này. Dưới 2 hộp đổi được thì trả false, bàn y nguyên.
        /// </summary>
        bool RotateTopBoxTiles()
        {
            var idx = new List<int>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;
                bool pinned = false;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (IsFrozen(top.Slots[i]) || IsFixed(top.Slots[i])) pinned = true;
                if (!pinned) idx.Add(s);
            }
            if (idx.Count < 2) return false;

            Tile[] lastSlots = TopBox(idx[idx.Count - 1]).Slots;
            var last = new Tile[lastSlots.Length];
            for (int i = 0; i < last.Length; i++) last[i] = lastSlots[i];
            for (int k = idx.Count - 1; k > 0; k--)
            {
                Tile[] from = TopBox(idx[k - 1]).Slots, to = TopBox(idx[k]).Slots;
                for (int i = 0; i < to.Length; i++) to[i] = from[i];
            }
            Tile[] first = TopBox(idx[0]).Slots;
            for (int i = 0; i < first.Length; i++) first[i] = last[i];
            return true;
        }

        /// <summary>
        /// Bấm Shuffle lúc này có làm bàn đổi không — chạy thử trên bản sao. ApplyShuffle xác
        /// định (cùng bàn → cùng kết quả), nên nút sáng theo hàm này thì bấm thật luôn thành
        /// công: không bao giờ ăn lượt mà bàn đứng yên (spec 2026-10-02 Mục 6).
        /// </summary>
        public bool ShuffleWouldChange()
        {
            return CanShuffle() && Clone().ApplyShuffle().Ok;
        }

        // Bốn bất biến của spec 2026-08-26 Mục 5, nới theo spec 2026-10-02-shuffle-redesign
        // Mục 5: thẻ có màu trong movers (cặp dời, thẻ dời giữa hai cụm đôi, thẻ xé) được đổi chỗ.
        bool ValidateShuffle(int topBefore, Dictionary<string, SlotRef> before, HashSet<string> whiteBefore,
                             HashSet<string> movers)
        {
            if (TopLayerTileCount() != topBefore) return false;
            if (AnyBoxHasFullGroup()) return false;

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || (IsOpen(top.Lock) && BoxTileCount(top) == 0)) return false;   // hộp khoá được rỗng
            }

            // Thẻ vốn CÓ MÀU ở lớp trên phải còn nguyên chỗ cũ và nguyên nội dung. Xét theo
            // trạng thái TRƯỚC khi xáo — sau khi xáo màu đã khác nên không suy ngược được.
            Dictionary<string, SlotRef> now = SnapshotPositions(null);
            foreach (var kv in before)
            {
                if (kv.Value.Box != 0) continue;              // vốn không ở lớp trên
                if (whiteBefore.Contains(kv.Key)) continue;   // vốn trắng, được phép đổi chỗ
                if (movers.Contains(kv.Key)) continue;        // dời hợp lệ (spec 2026-10-02 Mục 5)

                SlotRef cur;
                if (!now.TryGetValue(kv.Key, out cur)) return false;
                if (cur.Stack != kv.Value.Stack || cur.Box != 0 || cur.Slot != kv.Value.Slot) return false;
            }
            return true;
        }

        // whiteBefore null thì bỏ qua việc ghi nhận thẻ trắng — dùng cho lần chụp thứ hai.
        Dictionary<string, SlotRef> SnapshotPositions(HashSet<string> whiteBefore)
        {
            var map = new Dictionary<string, SlotRef>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null) continue;
                        map[t.Uid] = new SlotRef { Stack = s, Box = b, Slot = i };
                        if (whiteBefore != null && b == 0 && IsWhite(boxes[b], i)) whiteBefore.Add(t.Uid);
                    }
            }
            return map;
        }

        ShuffleMove[] DiffPositions(Dictionary<string, SlotRef> before)
        {
            var moves = new List<ShuffleMove>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null) continue;

                        SlotRef old;
                        if (!before.TryGetValue(t.Uid, out old)) continue;
                        if (old.Stack == s && old.Box == b && old.Slot == i) continue;

                        moves.Add(new ShuffleMove
                        {
                            Uid = t.Uid,
                            From = old,
                            To = new SlotRef { Stack = s, Box = b, Slot = i },
                        });
                    }
            }
            return moves.ToArray();
        }

        // Khôi phục NỘI DUNG ô từ bản sao. KHÔNG thay danh sách Boxes — CheckStatus() đọc
        // st.Boxes[0] mà không kiểm rỗng, đụng vào cấu trúc là rủi ro không cần thiết.
        void RestoreFrom(Game backup)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> mine = Stacks[s].Boxes;
                List<Box> theirs = backup.Stacks[s].Boxes;
                for (int b = 0; b < mine.Count && b < theirs.Count; b++)
                    for (int i = 0; i < mine[b].Slots.Length; i++)
                        mine[b].Slots[i] = theirs[b].Slots[i];
            }
        }
    }
}
