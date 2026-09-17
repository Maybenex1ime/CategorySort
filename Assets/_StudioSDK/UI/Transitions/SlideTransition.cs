using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public enum SlideDirection { Left, Right, Up, Down }

    public sealed class SlideTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private SlideDirection _enterFrom = SlideDirection.Right;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Ease _enterEase = Ease.OutCubic;
        [SerializeField] private Ease _exitEase = Ease.InCubic;

        public async Awaitable PlayEnter(RectTransform target)
        {
            Vector2 startPos = GetOffscreenPos(target, _enterFrom);
            target.anchoredPosition = startPos;
            await LMotion.Create(startPos, Vector2.zero, _duration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAnchoredPosition(target)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            Vector2 endPos = GetOffscreenPos(target, _enterFrom);
            await LMotion.Create(target.anchoredPosition, endPos, _duration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAnchoredPosition(target)
                .AddTo(gameObject)
                .WaitAsync();
            target.anchoredPosition = Vector2.zero;
        }

        private static Vector2 GetOffscreenPos(RectTransform rt, SlideDirection dir)
        {
            Vector2 size = rt.rect.size;
            return dir switch
            {
                SlideDirection.Left  => new Vector2(-size.x, 0f),
                SlideDirection.Right => new Vector2(size.x, 0f),
                SlideDirection.Up    => new Vector2(0f, size.y),
                SlideDirection.Down  => new Vector2(0f, -size.y),
                _                   => Vector2.zero
            };
        }
    }
}
