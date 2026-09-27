// Figma cho opacity theo cả group (Group 547 mờ về 0 cuối timeline), SpriteRenderer thì không có
// alpha theo nhóm. Component này nhân một hệ số lên alpha author của mọi SpriteRenderer dưới target.
// Dùng như component có sẵn của LitMotion.Animation: Start/End Value là hệ số (1 = author, 0 = tắt),
// để Relative tắt.
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Sprite Group Alpha")]
    public sealed class SpriteGroupAlphaAnimation : FloatPropertyAnimationComponent<Transform>
    {
        SpriteRenderer[] renderers;
        float[] baseAlpha;
        float current = 1f;

        // Chụp lại alpha author mỗi lần Play: giữa hai lần mở, bên ngoài có thể đã đổi alpha
        // (BoxView mờ Key Tile trước khi gọi Play).
        public override MotionHandle Play()
        {
            renderers = null;
            current = 1f;
            return base.Play();
        }

        protected override float GetValue(Transform target)
        {
            Cache(target);
            return current;
        }

        protected override void SetValue(Transform target, in float value)
        {
            Cache(target);
            current = value;
            ApplyAlpha(renderers, baseAlpha, value);
        }

        void Cache(Transform target)
        {
            if (renderers != null || target == null) return;
            renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
        }

        /// <summary>alpha = alpha author × k cho từng renderer. Static để test không cần chạy motion.</summary>
        public static void ApplyAlpha(SpriteRenderer[] rs, float[] baseAlpha, float k)
        {
            if (rs == null) return;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                var c = rs[i].color;
                c.a = baseAlpha[i] * k;
                rs[i].color = c;
            }
        }
    }
}
