namespace BoosterModule
{
    public enum BoosterId
    {
        None = 0,

        // Shuffle chiem so 1 theo thu tu booster; Hand nhuong lai va nhan so 6.
        // KHONG the cho Shuffle = 0: None = 0 la sentinel cua ba chot trong
        // BoosterManager va la gia tri cua default(BoosterId).
        Shuffle = 1,
        AddQueue = 2,
        AddBelt = 3,
        Hammer = 4,

        // Nam cham. KHONG danh so 0 duoc: None = 0 la gia tri sentinel, va
        // BoosterManager co 3 chot "if (evt.Id == BoosterId.None) return;" —
        // Magnet = 0 se bi may chot do nuot (mua thi tru coin ma khong cong luot,
        // bam thi khong chay).
        Magnet = 5,

        Hand = 6,
    }
}
