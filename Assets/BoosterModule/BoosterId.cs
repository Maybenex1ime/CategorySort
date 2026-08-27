namespace BoosterModule
{
    /// <summary>
    /// Bộ booster của WordStack. Hand/Hammer/AddQueue/AddBelt đã bỏ (2026-08-27) — chúng
    /// port từ aquapark và chưa từng có luật tác động lên bàn.
    ///
    /// None = 0 phải giữ nguyên: đó là sentinel của ba chốt trong BoosterManager, đồng
    /// thời là giá trị default(BoosterId) của mọi struct event chưa khởi tạo. Không
    /// booster nào được đánh số 0.
    /// </summary>
    public enum BoosterId
    {
        None = 0,

        Shuffle = 1,
        Magnet = 2,

        // CHƯA CÓ LUẬT. Id tồn tại để đặt chỗ; MetaSession log cảnh báo khi bấm phải,
        // không để nó im lặng nuốt lượt người chơi đã mua.
        Undo = 3,
    }
}
