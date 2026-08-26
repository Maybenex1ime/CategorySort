namespace BoosterModule
{
    public enum BoosterId
    {
        None = 0,
        Hand = 1,
        AddQueue = 2,
        AddBelt = 3,
        Hammer = 4,

        // Nam cham. KHONG danh so 0 duoc: None = 0 la gia tri sentinel, va
        // BoosterManager co 3 chot "if (evt.Id == BoosterId.None) return;" —
        // Magnet = 0 se bi may chot do nuot (mua thi tru coin ma khong cong luot,
        // bam thi khong chay). Thu tu HIEN THI tren HUD do mang _boosterSlots cua
        // GameplayUiRoot quyet dinh, khong phai gia tri enum.
        Magnet = 5,

        // Xao lai the trang o lop tren de mo duong. Cung ly do khong dung 0 nhu Magnet.
        Shuffle = 6,
    }
}
