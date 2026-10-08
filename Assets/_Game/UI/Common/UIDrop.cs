using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace LogosGame.Features.UI.Common
{
    /// Nút (thường là X đóng popup) rơi từ trên xuống, nảy rồi đứng yên — report UI animation, DetailPopup.PlayUIDrop:
    /// rơi 200px + nghiêng −18° → bật lên 40px, +12° → chạm lại, −8° → về 0°; 0.5s chia 50 / 20 / 15 / 15%.
    /// Popup gọi Hide trước base.Show() (X không lơ lửng trong lúc popup đang mở) rồi Play sau đó.
    public static class UIDrop
    {
        public const float Duration = 0.5f, Height = 200f;

        /// Ẩn (scale 0) và trả scale gốc để Play khôi phục.
        public static Vector3 Hide(Component target)
        {
            if (target == null) return Vector3.one;
            var scale = target.transform.localScale;
            target.transform.localScale = Vector3.zero;
            return scale == Vector3.zero ? Vector3.one : scale;   // lần mở trước bị đóng giữa chừng
        }

        /// previous: handle của lần trước — hoàn tất nó trước để không đọc vị trí gốc lúc đang bay.
        public static MotionHandle Play(Component target, Vector3 scale, MotionHandle previous)
        {
            previous.TryComplete();
            if (target == null || !(target.transform is RectTransform rt)) return default;
            rt.localScale = scale;

            Vector2 home = rt.anchoredPosition;
            Vector2 top = home + Vector2.up * Height;
            Vector2 bump = home + Vector2.up * (Height * 0.2f);
            float a = Duration * 0.5f, b = Duration * 0.2f, c = Duration * 0.15f, d = Duration * 0.15f;

            return LSequence.Create()
                .Append(LMotion.Create(top, home, a).WithEase(Ease.InQuad).WithCancelOnError().BindToAnchoredPosition(rt))
                .Join(LMotion.Create(0f, -18f, a).WithEase(Ease.InQuad).WithCancelOnError().BindToLocalEulerAnglesZ(rt))
                .Append(LMotion.Create(home, bump, b).WithEase(Ease.OutQuad).WithCancelOnError().BindToAnchoredPosition(rt))
                .Join(LMotion.Create(-18f, 12f, b).WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalEulerAnglesZ(rt))
                .Append(LMotion.Create(bump, home, c).WithEase(Ease.InQuad).WithCancelOnError().BindToAnchoredPosition(rt))
                .Join(LMotion.Create(12f, -8f, c).WithEase(Ease.InQuad).WithCancelOnError().BindToLocalEulerAnglesZ(rt))
                .Append(LMotion.Create(-8f, 0f, d).WithEase(Ease.OutBounce).WithCancelOnError().BindToLocalEulerAnglesZ(rt))
                .Run()
                .AddTo(rt.gameObject);
        }
    }
}
