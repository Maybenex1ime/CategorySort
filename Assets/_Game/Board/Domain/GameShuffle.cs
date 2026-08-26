// Booster Shuffle: xáo lại nội dung thẻ để mở đường cho người chơi, KHÔNG phát clear
// miễn phí — mỗi nhóm nó dựng ra tốn đúng 1 nước đi để nổ. Tách khỏi Game.cs để file đó
// chỉ còn luật bàn chơi; đây là luật của booster.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System.Collections.Generic;

namespace WordStack.Board
{
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

        /// <summary>Địa chỉ một ô trên bàn. Box = 0 là hộp trên cùng.</summary>
        public struct SlotRef
        {
            public int Stack, Box, Slot;
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
    }
}
