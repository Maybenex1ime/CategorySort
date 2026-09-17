using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class TweenFx
    {
        // Nảy kiểu thạch: bẹt ngang → cao dọc → về gốc, ba nhịp bằng nhau.
        // Port Fu.DOBouncingScale (Mukbang). Toàn Append nên nghĩa giống hệt DOTween.
        public static MotionHandle BouncingScale(Transform target, float duration = 0.5f, float strength = 0.2f, Ease ease = Ease.Linear)
        {
            var original = target.localScale;
            var hs = strength / 2f;
            var t0 = Vector3.Scale(original, new Vector3(1 + hs, 1 - strength, 1 + hs));
            var t1 = Vector3.Scale(original, new Vector3(1 - hs, 1 + hs, 1 - hs));
            float step = duration / 3f;

            return LSequence.Create()
                .Append(LMotion.Create(original, t0, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Append(LMotion.Create(t0, t1, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Append(LMotion.Create(t1, original, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Run(b => b.WithCancelOnError());
        }
    }
}
