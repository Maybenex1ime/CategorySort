using System;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class NoHeartsPopupArgs
    {
        /// <summary>Đóng mà KHÔNG nhận thêm tim (Ok / X).</summary>
        public Action OnClose { get; set; }

        /// <summary>
        /// Đóng SAU KHI nhận được tim (mua bằng coin / xem ad) — bên gọi chạy tiếp hành
        /// động đang bị chặn. Để null thì rơi về OnClose.
        /// </summary>
        public Action OnHeartGranted { get; set; }
    }
}
