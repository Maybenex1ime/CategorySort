namespace LogosGame.Features.Gameplay.Flow
{
    /// <summary>
    /// Vì sao thua — quyết định cách hồi sinh:
    ///   OutOfMoves — hết nước, bàn vẫn đi được → hồi sinh = cộng thêm nước.
    ///   Stuck      — bàn kẹt (không còn nước hợp lệ) → hồi sinh = nam châm miễn phí.
    /// Kẹt đúng ở nước cuối thì tính là Stuck (board báo trước, adapter chỉ xét hết nước
    /// khi board chưa báo thua).
    /// </summary>
    public enum LoseReason
    {
        None,
        OutOfMoves,
        Stuck,
    }
}
