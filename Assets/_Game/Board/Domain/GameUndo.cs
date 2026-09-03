// Booster Undo: trả bàn về đúng trạng thái trước nước kéo thẻ gần nhất. Tách khỏi
// Game.cs để file đó chỉ còn luật bàn chơi — đây là luật của booster.
//
// Không có thuật toán: Clone() đã sao sâu cả bàn kèm Moves/Cleared/uidSeq và giữ
// nguyên Uid từng thẻ, nên undo chỉ là giữ một bản sao rồi lắp lại.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
namespace WordStack.Board
{
    public partial class Game
    {
        // Ảnh chụp bàn TRƯỚC nước đi gần nhất. Đúng một cái: undo lùi một bước, không
        // lùi tiếp. Clone() cố ý không sao field này — bản sao không mang theo lịch sử
        // của bản gốc, và nhờ vậy ảnh chụp không tự lồng vào ảnh chụp.
        Game undoSnapshot;

        /// <summary>
        /// Có chụp ảnh mỗi nước đi hay không. Mặc định TẮT và phải vậy: Solver gọi
        /// MoveTile hàng vạn lần mỗi lần giải, bật lên là mỗi nút của cây tìm kiếm
        /// clone thêm một bàn. Chỉ BoardController bật, cho đúng ván người chơi đang chơi.
        ///
        /// Clone() không sao cờ này nên bàn con của solver luôn tắt, kể cả khi nhân bản
        /// từ bàn thật. Đổi lại, ApplyUndo phải tự bật lại cờ cho bàn nó trả về.
        /// </summary>
        public bool UndoEnabled;

        /// <summary>Có nước nào để lùi không. Điều kiện xám nút phía view.</summary>
        public bool CanUndo { get { return undoSnapshot != null; } }

        /// <summary>
        /// Gọi từ MoveTile, sau toàn bộ chốt từ chối và ngay trước dòng mutate đầu tiên:
        /// nước bị từ chối không chụp phí, nước được nhận thì luôn có ảnh.
        /// </summary>
        void CaptureUndoSnapshot()
        {
            if (UndoEnabled) undoSnapshot = Clone();
        }

        /// <summary>
        /// Vứt ảnh chụp — người chơi mất quyền undo. Gọi khi dùng booster khác: ảnh chụp
        /// là TOÀN bàn, nên khôi phục sau một lần Magnet/Shuffle sẽ nuốt luôn hiệu ứng
        /// người chơi vừa mua bằng coin.
        /// </summary>
        public void ClearUndo()
        {
            undoSnapshot = null;
        }

        /// <summary>
        /// Bàn ở trạng thái trước nước đi gần nhất, hoặc null khi không có gì để lùi.
        /// Người gọi thay thế instance đang giữ bằng cái trả về — đây KHÔNG phải hàm
        /// mutate tại chỗ.
        /// </summary>
        public Game ApplyUndo()
        {
            if (undoSnapshot == null) return null;

            Game restored = undoSnapshot;
            restored.UndoEnabled = UndoEnabled;   // Clone() không sao cờ, phải trao tay
            undoSnapshot = null;
            return restored;
        }
    }
}
