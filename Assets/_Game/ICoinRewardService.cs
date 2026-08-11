namespace WordStack.Meta
{
    /// <summary>
    /// Thưởng coin khi thắng màn. Giữ lại số vừa thưởng để màn hình kết quả
    /// đọc mà hiển thị — giống <c>ICoinRewardService</c> bên aquapark.
    /// </summary>
    public interface ICoinRewardService
    {
        int LastAwardedAmount { get; }
        void ResetLastAwarded();
    }
}
