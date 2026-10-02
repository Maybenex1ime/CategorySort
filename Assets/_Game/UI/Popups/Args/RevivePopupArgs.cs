using System;
using LogosGame.Features.Gameplay.Flow;
using R3;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class RevivePopupArgs
    {
        /// <summary>Thua vì sao — popup hiện nội dung tương ứng (+nước hay nam châm).</summary>
        public LoseReason Reason { get; set; }

        /// <summary>Số nước được cộng khi hồi sinh vì hết nước.</summary>
        public int ExtraMoves { get; set; }

        public int Price { get; set; }
        public ReadOnlyReactiveProperty<int> Coins { get; set; }

        /// <summary>Trả true = đã hồi sinh (popup tự đóng); false = không làm gì, popup ở lại.</summary>
        public Func<bool> OnReviveWithCoins { get; set; }

        /// <inheritdoc cref="OnReviveWithCoins"/>
        public Func<bool> OnReviveWithAd { get; set; }

        /// <summary>Người chơi bỏ cuộc → đi tiếp đường thua (FailedPopup).</summary>
        public Action OnGiveUp { get; set; }
    }
}
