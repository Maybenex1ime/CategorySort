using System.Collections;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class LitMotionAnimationExtensions
    {
        // Chờ LitMotionAnimation chạy xong trong coroutine. playForward = false: tua ngược từ cuối
        // bằng PlaybackSpeed = -1. Port Fu.RunCoroutine (Mukbang).
        public static IEnumerator RunCoroutine(this LitMotionAnimation animation, bool playForward = true, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (playForward)
            {
                if (animation.IsActive) animation.Restart();
                else animation.Play();
                yield return new WaitWhile(() => animation.IsPlaying);
            }
            else
            {
                float maxTime = 0f;
                foreach (var component in animation.Components)
                {
                    var handle = component.TrackedHandle;
                    if (!handle.IsActive()) continue;
                    handle.Time = handle.Duration;   // về cuối trước khi tua ngược
                    handle.PlaybackSpeed = -1f;
                    if (handle.Duration > maxTime) maxTime = handle.Duration;
                }
                yield return new WaitForSeconds(maxTime);
            }
        }
    }
}
