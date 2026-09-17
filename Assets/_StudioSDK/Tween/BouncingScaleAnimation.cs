using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace LogosSDK.Tween
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Bouncing Scale")]
    public sealed class BouncingScaleAnimation : LitMotionAnimationComponent
    {
        [SerializeField] Transform target;
        [SerializeField] float duration = 0.5f;
        [SerializeField] Ease easing = Ease.Linear;
        [SerializeField] float strength = 0.2f;

        Vector3 originalScale;

        public override MotionHandle Play()
        {
            originalScale = target.localScale;
            return TweenFx.BouncingScale(target, duration, strength, easing);
        }

        public override void OnStop()
        {
            if (target != null) target.localScale = originalScale;
        }
    }
}
