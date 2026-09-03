namespace LogosGame.Features.UI.Popups.Args
{
    /// <summary>
    /// Rỗng có chủ đích: ShopPopup lấy data qua [Inject] IShopService (giống
    /// MainMenuScreen inject ICurrencyService), không cần caller bơm gì. Lớp này
    /// tồn tại vì PopupBase&lt;TArgs&gt; bắt buộc có kiểu args.
    ///
    /// Cần deep-link (mở thẳng tab Coin từ popup "hết coin") thì thêm 1 field
    /// InitialTab ở đây — chưa có nhu cầu nên chưa thêm.
    /// </summary>
    public sealed class ShopPopupArgs
    {
    }
}
