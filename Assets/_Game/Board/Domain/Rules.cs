// Hằng luật của WordStack. Nguồn: docs/wordstack-rules.md + demo/wordstack-clear-demo.html.
//
// RÀNG BUỘC CỨNG (cả thư mục Domain/): KHÔNG import UnityEngine — ./selfcheck.sh compile
// mấy file này bằng csc và chạy luật ngoài Unity trong ~2 giây.
namespace WordStack.Board
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
        // Mọi level ship chạy được ở CẢ HAI chế độ — SelfCheck kiểm cả hai bất kể cờ này.
        // Chốt 2026-08-04: giữ true. Xem docs/wordstack-rules.md Mục 4.
        public const bool RemoveEmptyNonBottomBox = true;
    }

    public enum GameStatus { Playing, Won, Stuck }
}
