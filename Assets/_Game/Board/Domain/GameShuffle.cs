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
        /// Điều kiện này trùng với định nghĩa Playing trong CheckStatus(), nên nút Shuffle
        /// gần như luôn sáng khi bàn còn chơi được.
        /// </summary>
        public bool CanShuffle()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) continue;
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
        /// Các nhóm đáng dựng mồi, nhiều nhất <paramref name="max"/> nhóm.
        ///
        /// Chỉ nhận nhóm có ĐỦ 4 thẻ đang tồn tại trên bàn: nhóm cha còn nhóm con chưa
        /// collapse thì thành viên thiếu chưa tồn tại dưới dạng thẻ, không kéo lên được —
        /// cùng ràng buộc với booster Magnet.
        ///
        /// Thứ tự: nhiều thẻ sẵn ở lớp trên nhất trước (ít phải kéo donor nhất), hoà thì
        /// theo group id. Bậc cuối chỉ để kết quả xác định, test lại được.
        /// </summary>
        public List<string> PickPrimeCandidates(int max)
        {
            var onBoard = new Dictionary<string, int>();
            var onTop = new Dictionary<string, int>();
            var order = new List<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        string gid = slots[i].GroupId;
                        int n;
                        if (!onBoard.TryGetValue(gid, out n)) { order.Add(gid); onTop[gid] = 0; }
                        onBoard[gid] = n + 1;
                        if (b == 0) onTop[gid] = onTop[gid] + 1;
                    }
                }
            }

            var eligible = new List<string>();
            for (int k = 0; k < order.Count; k++)
                if (onBoard[order[k]] == Rules.GroupSize) eligible.Add(order[k]);

            eligible.Sort(delegate (string a, string b)
            {
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
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null || IsWhite(top, i))
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
        /// Dựng Nhóm mồi 3+1: 3 thẻ vào hộp chủ (hộp đó còn ĐÚNG 1 ô trống được giữ chỗ),
        /// thẻ thứ 4 sang top box khác. Người chơi kéo một nước là nổ.
        ///
        /// Trả false khi không xếp nổi — bên gọi phải coi đó là bình thường, không phải lỗi.
        /// </summary>
        public bool TryPrimeGroup(string gid, List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand)
        {
            // Hộp chủ cần GroupSize ô mở: GroupSize-1 cho thẻ, 1 chừa trống cho người chơi.
            // Ưu tiên hộp có layer 2 nhiều thẻ nhất: nổ xong hộp chủ bị xoá, hộp dưới lộ ra,
            // chọn hộp ngồi trên hộp đầy thì lượt sau người chơi có nhiều nguyên liệu nhất.
            int host = -1;
            for (int s = 0; s < Stacks.Count; s++)
            {
                if (OpenCount(pool, reserved, s) < Rules.GroupSize) continue;
                if (host < 0 || Layer2TileCount(s) > Layer2TileCount(host)) host = s;
            }
            if (host < 0) return false;

            int carrier = -1;
            for (int s = 0; s < Stacks.Count && carrier < 0; s++)
                if (s != host && OpenCount(pool, reserved, s) > 0) carrier = s;
            if (carrier < 0) return false;

            var need = new List<Tile>();
            for (int k = 0; k < Rules.GroupSize; k++)
            {
                Tile t = TakeFromHand(hand, gid);
                if (t == null) t = SwapDonorIntoHand(gid, hand);
                if (t == null) { hand.AddRange(need); return false; }
                need.Add(t);
            }

            for (int k = 0; k < Rules.GroupSize - 1; k++)
            {
                int slot = FirstOpen(pool, reserved, host);
                TopBox(host).Slots[slot] = need[k];
                reserved.Add(SlotKey(host, slot));
            }

            // Ô CHỪA TRỐNG — reserve để pha sau không lấp mất chỗ thả thẻ thứ 4.
            int keep = FirstOpen(pool, reserved, host);
            if (keep < 0) { hand.AddRange(need); return false; }
            reserved.Add(SlotKey(host, keep));

            int cslot = FirstOpen(pool, reserved, carrier);
            TopBox(carrier).Slots[cslot] = need[Rules.GroupSize - 1];
            reserved.Add(SlotKey(carrier, cslot));
            return true;
        }

        /// <summary>
        /// Mỗi top box phải giữ ≥1 thẻ. Hộp top rỗng bị SettleStep xoá, hộp dưới lộ ra,
        /// tổng thẻ lớp trên tăng — vỡ bất biến 1.
        ///
        /// Chạy TRƯỚC ClusterHand chứ không phải sau: gom cụm reserve hết ô trống, chạy
        /// sau thì không còn thẻ nào mượn được và cả lượt shuffle bị rollback oan.
        /// </summary>
        public bool EnsureEveryTopBoxOccupied(List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                if (box == null) return false;
                if (BoxTileCount(box) > 0) continue;

                int slot = FirstOpen(pool, reserved, s);
                if (slot < 0) return false;

                if (hand.Count > 0)
                {
                    box.Slots[slot] = hand[0];
                    hand.RemoveAt(0);
                    reserved.Add(SlotKey(s, slot));
                    continue;
                }

                // Tay đã cạn: mượn từ ô CHƯA reserved của hộp đang có ≥2 thẻ — ô đã
                // reserved là Nhóm mồi, đụng vào là phá thứ vừa dựng.
                int donorStack = -1, donorSlot = -1;
                for (int d = 0; d < Stacks.Count && donorStack < 0; d++)
                {
                    if (d == s) continue;
                    Box db = TopBox(d);
                    if (db == null || BoxTileCount(db) < 2) continue;
                    for (int i = 0; i < db.Slots.Length; i++)
                    {
                        if (db.Slots[i] == null) continue;
                        if (reserved.Contains(SlotKey(d, i))) continue;
                        if (!InPool(pool, d, i)) continue;
                        donorStack = d; donorSlot = i; break;
                    }
                }
                if (donorStack < 0) return false;

                box.Slots[slot] = TopBox(donorStack).Slots[donorSlot];
                TopBox(donorStack).Slots[donorSlot] = null;
                reserved.Add(SlotKey(s, slot));
            }
            return true;
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

                            Tile down = hand[h];
                            hand.RemoveAt(h);
                            boxes[b].Slots[i] = down;
                            return t;
                        }
                    }
            }
            return null;
        }

        /// <summary>
        /// Xáo lại lớp trên cùng theo luật Shuffle: dựng tối đa 3 Nhóm mồi 3+1, đảm bảo
        /// mỗi top box còn thẻ, rồi gom cụm phần còn lại.
        ///
        /// KHÔNG đụng danh sách Boxes và KHÔNG đổi Status — gọi Settle() ngay sau như một
        /// nước đi thường. KHÔNG tăng Moves: booster không tính là nước đi.
        ///
        /// Vi phạm bất kỳ bất biến nào thì khôi phục nguyên trạng và trả Ok = false; bên
        /// gọi PHẢI không trừ lượt trong trường hợp đó.
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
            List<string> candidates = PickPrimeCandidates(3);

            List<SlotRef> pool = AssignableTopSlots();
            var reserved = new HashSet<int>();
            var hand = new List<Tile>();
            DrainAll(pool, hand);

            int primed = 0;
            for (int k = 0; k < candidates.Count && primed < 3; k++)
                if (TryPrimeGroup(candidates[k], pool, reserved, hand)) primed++;

            // Seed TRƯỚC cluster — xem ghi chú trong EnsureEveryTopBoxOccupied.
            bool seeded = EnsureEveryTopBoxOccupied(pool, reserved, hand);
            ClusterHand(pool, reserved, hand);

            // hand còn thẻ = có thẻ không tìm được chỗ đặt, tức là thẻ đã rời khỏi bàn.
            if (!seeded || hand.Count > 0 || !ValidateShuffle(topBefore, before, whiteBefore))
            {
                RestoreFrom(backup);
                return fail;
            }

            return new ShuffleResult
            {
                Ok = true,
                Moves = DiffPositions(before),
                PrimedGroups = CountPrimedGroups(),
            };
        }

        // Bốn bất biến của spec Mục 5.
        bool ValidateShuffle(int topBefore, Dictionary<string, SlotRef> before, HashSet<string> whiteBefore)
        {
            if (TopLayerTileCount() != topBefore) return false;
            if (AnyBoxHasFullGroup()) return false;

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || BoxTileCount(top) == 0) return false;
            }

            // Thẻ vốn CÓ MÀU ở lớp trên phải còn nguyên chỗ cũ và nguyên nội dung. Xét theo
            // trạng thái TRƯỚC khi xáo — sau khi xáo màu đã khác nên không suy ngược được.
            Dictionary<string, SlotRef> now = SnapshotPositions(null);
            foreach (var kv in before)
            {
                if (kv.Value.Box != 0) continue;              // vốn không ở lớp trên
                if (whiteBefore.Contains(kv.Key)) continue;   // vốn trắng, được phép đổi chỗ

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
