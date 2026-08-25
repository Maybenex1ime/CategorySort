// Nam châm (booster): gom trọn MỘT nhóm, kể cả khi thẻ của nó đang bị chôn dưới đáy.
// Tách khỏi Game.cs để file đó chỉ còn luật bàn chơi — đây là luật của booster, không
// phải luật chơi: Solver/SelfCheck vẫn kiểm level giải được KHÔNG dùng booster.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System.Collections.Generic;

namespace WordStack.Board
{
    /// <summary>Một thẻ bị hút, kèm chỗ nó đứng TRƯỚC khi bị hút (view cần để animate).</summary>
    public struct MagnetPick
    {
        public string Uid;
        public int Stack;
        public int Box;    // 0 = hộp trên cùng; > 0 = đang bị chôn, view phải dựng thẻ tạm
        public int Slot;
    }

    public struct MagnetResult
    {
        public bool Ok;
        public string GroupId;
        public MagnetPick[] Picks;
        public int TargetStack;      // hộp hội tụ: top box đang giữ nhiều thẻ nhóm đó nhất
        public string NewTileUid;    // COLLAPSE sinh thẻ cha; null nếu là nhóm gốc
        public int NewTileStack;     // stack nhận thẻ cha; -1 nếu không có
    }

    public partial class Game
    {
        /// <summary>
        /// Nhóm mà nam châm nên gom; null nghĩa là KHÔNG có mục tiêu hợp lệ — nút phải
        /// xám và KHÔNG được trừ lượt (lượt này người chơi mua bằng coin).
        ///
        /// Thứ tự chốt: nhiều thẻ ở hộp trên cùng nhất → nhóm gốc (không đẻ thẻ cha)
        /// → thẻ chôn nông nhất → khoá vị trí nhỏ nhất. Bậc cuối chỉ để kết quả XÁC ĐỊNH:
        /// cùng một bàn phải luôn ra cùng một nhóm, không thì không test lại được.
        /// </summary>
        public string FindMagnetTarget()
        {
            var onBoard = new Dictionary<string, int>();
            var onTop = new Dictionary<string, int>();
            var deepest = new Dictionary<string, int>();
            var firstAt = new Dictionary<string, int>();
            var order = new List<string>();   // thứ tự gặp — KHÔNG duyệt Dictionary, thứ tự đó không đảm bảo

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

                        int n;
                        if (!onBoard.TryGetValue(gid, out n))
                        {
                            order.Add(gid);
                            onTop[gid] = 0;
                            deepest[gid] = b;
                            firstAt[gid] = int.MaxValue;
                        }
                        onBoard[gid] = n + 1;

                        if (b == 0) onTop[gid] = onTop[gid] + 1;
                        if (b > deepest[gid]) deepest[gid] = b;

                        int key = (s * Rules.BoxCapacity) + i;
                        if (key < firstAt[gid]) firstAt[gid] = key;
                    }
                }
            }

            string best = null;
            for (int k = 0; k < order.Count; k++)
            {
                string gid = order[k];

                // Nhóm cha còn nhóm con CHƯA collapse thì chưa đủ 4 thẻ trên bàn —
                // thành viên còn thiếu chưa tồn tại dưới dạng thẻ, không moi được.
                if (onBoard[gid] != Rules.GroupSize) continue;

                // Phải neo vào thứ người chơi đang nhìn thấy. Bỏ chặn này thì nam châm
                // nổ ở tận đáy, trông như không liên quan gì tới màn hình.
                if (onTop[gid] <= 0) continue;

                if (best == null || IsBetterTarget(gid, best, onTop, deepest, firstAt)) best = gid;
            }
            return best;
        }

        bool IsBetterTarget(string a, string b, Dictionary<string, int> onTop,
                            Dictionary<string, int> deepest, Dictionary<string, int> firstAt)
        {
            if (onTop[a] != onTop[b]) return onTop[a] > onTop[b];

            // Hoà số thẻ ở top mới xét tới chuyện đẻ thẻ cha: clear nhóm gốc là -4 thẻ,
            // clear nhóm con là -4 +1 = -3 và thẻ cha còn chiếm mất một ô.
            bool rootA = IsRootGroup(a), rootB = IsRootGroup(b);
            if (rootA != rootB) return rootA;

            if (deepest[a] != deepest[b]) return deepest[a] < deepest[b];
            return firstAt[a] < firstAt[b];
        }

        bool IsRootGroup(string gid)
        {
            GroupDef def;
            return GroupDefs == null || !GroupDefs.TryGetValue(gid, out def) || def.ParentId == null;
        }

        /// <summary>Hút nhóm do FindMagnetTarget chọn. Ok = false khi không có mục tiêu.</summary>
        public MagnetResult ApplyMagnet()
        {
            string gid = FindMagnetTarget();
            return gid == null ? Fail() : ApplyMagnet(gid);
        }

        /// <summary>
        /// Hút trọn nhóm <paramref name="gid"/> ở MỌI hộp, kể cả hộp bị chôn.
        ///
        /// KHÔNG đụng danh sách Boxes và KHÔNG đổi Status — gọi Settle() ngay sau để hộp
        /// rỗng được dọn, cascade chạy và thắng/kẹt được chốt, y như sau một nước đi
        /// thường. Tự xoá hộp ở đây là hỏng: CheckStatus() đọc st.Boxes[0] mà không kiểm
        /// rỗng, stack mất sạch hộp là IndexOutOfRangeException.
        ///
        /// KHÔNG tăng Moves: booster không tính là một nước đi.
        /// </summary>
        public MagnetResult ApplyMagnet(string gid)
        {
            var picks = new List<MagnetPick>();
            int[] topCount = new int[Stacks.Count];

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        Tile t = slots[i];
                        if (t == null || t.GroupId != gid) continue;
                        picks.Add(new MagnetPick { Uid = t.Uid, Stack = s, Box = b, Slot = i });
                        if (b == 0) topCount[s]++;
                    }
                }
            }

            if (picks.Count != Rules.GroupSize) return Fail();

            // Hộp hội tụ = top box đang giữ nhiều thẻ nhóm đó nhất; hoà thì stack nhỏ hơn.
            // Duyệt theo chỉ số stack để kết quả xác định.
            int target = -1;
            for (int s = 0; s < Stacks.Count; s++)
                if (topCount[s] > 0 && (target < 0 || topCount[s] > topCount[target])) target = s;
            if (target < 0) return Fail();   // không thẻ nào ở top → không đủ điều kiện

            GroupDef def = null;
            bool collapses = GroupDefs != null && GroupDefs.TryGetValue(gid, out def) && def.ParentId != null;

            // Chốt chỗ đặt thẻ cha TRƯỚC khi xoá. Xoá xong mới phát hiện không còn chỗ là
            // thẻ cha rơi mất → nhóm cha vĩnh viễn thiếu thành viên → màn không thể thắng,
            // mà không có dấu hiệu gì báo ra.
            int hostStack = -1, hostSlot = -1;
            if (collapses) PickCollapseHost(def.ParentId, picks, target, out hostStack, out hostSlot);

            for (int k = 0; k < picks.Count; k++)
                Stacks[picks[k].Stack].Boxes[picks[k].Box].Slots[picks[k].Slot] = null;

            Cleared++;

            string newUid = null;
            if (collapses && hostStack >= 0)
            {
                Box host = TopBox(hostStack);
                Tile nt = new Tile
                {
                    Uid = "t" + (++uidSeq),
                    CardId = gid, GroupId = def.ParentId, Text = def.Text, Art = def.Art
                };
                host.Slots[hostSlot] = nt;
                // Cờ đặt ở hộp NHẬN, giữ đúng tinh thần luật gốc (ở đó hộp clear cũng chính
                // là hộp nhận). Lưu ý cho chế độ chặt (RemoveEmptyNonBottomBox = false):
                // hộp có nguy cơ thành nắp đậy vĩnh viễn lại là hộp BỊ LẤY thẻ, không phải
                // hộp này. Cờ đang true nên mọi top box rỗng đều bị xoá, chưa thành vấn đề.
                host.HadCollapse = true;
                newUid = nt.Uid;
            }

            return new MagnetResult
            {
                Ok = true,
                GroupId = gid,
                Picks = picks.ToArray(),
                TargetStack = target,
                NewTileUid = newUid,
                NewTileStack = hostStack
            };
        }

        // Luật đặt thẻ cha của nam châm KHÁC luật gốc ("ô trống đầu tiên của chính hộp vừa
        // clear"): ưu tiên hộp đang có sẵn thẻ nhóm cha, vì thẻ mới lấp vào đó có thể làm
        // nhóm cha đủ bộ và nổ luôn ở nhịp Settle kế tiếp — cascade miễn phí.
        void PickCollapseHost(string parentId, List<MagnetPick> picks, int target,
                              out int stack, out int slot)
        {
            stack = -1; slot = -1;
            int bestParent = 0;

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                if (box == null) continue;
                int free = FirstFreeAfterPull(s, box, picks);
                if (free < 0) continue;

                int parentTiles = 0;
                for (int i = 0; i < box.Slots.Length; i++)
                {
                    Tile t = box.Slots[i];
                    if (t != null && t.GroupId == parentId && !IsPicked(picks, s, i)) parentTiles++;
                }
                if (parentTiles > bestParent) { bestParent = parentTiles; stack = s; slot = free; }
            }
            if (stack >= 0) return;

            // Không hộp nào có thẻ nhóm cha → hộp hội tụ trước (khớp luật gốc "cùng hộp").
            Box tb = TopBox(target);
            int f = tb == null ? -1 : FirstFreeAfterPull(target, tb, picks);
            if (f >= 0) { stack = target; slot = f; return; }

            // Lưới an toàn, đúng ra không bao giờ chạm tới: nhóm được chọn luôn có ≥1 thẻ ở
            // top box nên hộp hội tụ chắc chắn vừa trống ra một ô.
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                if (box == null) continue;
                int fs = FirstFreeAfterPull(s, box, picks);
                if (fs >= 0) { stack = s; slot = fs; return; }
            }
        }

        // Ô trống SAU khi hút: đang null, hoặc đang giữ đúng một thẻ sắp bị hút đi.
        static int FirstFreeAfterPull(int stack, Box box, List<MagnetPick> picks)
        {
            for (int i = 0; i < box.Slots.Length; i++)
                if (box.Slots[i] == null || IsPicked(picks, stack, i)) return i;
            return -1;
        }

        // Chỉ xét hộp trên cùng (Box == 0) — thẻ cha không bao giờ đặt xuống hộp bị chôn,
        // đặt xuống đó là nó biến mất khỏi tầm với ngay lập tức.
        static bool IsPicked(List<MagnetPick> picks, int stack, int slot)
        {
            for (int k = 0; k < picks.Count; k++)
                if (picks[k].Stack == stack && picks[k].Box == 0 && picks[k].Slot == slot) return true;
            return false;
        }

        static MagnetResult Fail() { return new MagnetResult { Ok = false, NewTileStack = -1 }; }
    }
}
