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
    }
}
