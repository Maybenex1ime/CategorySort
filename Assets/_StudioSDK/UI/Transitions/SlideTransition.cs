using DG.Tweening;
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
            target.anchoredPosition = GetOffscreenPos(target, _enterFrom);
            await target
                .DOAnchorPos(Vector2.zero, _duration)
                .SetEase(_enterEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            Vector2 endPos = GetOffscreenPos(target, _enterFrom);
            await target
                .DOAnchorPos(endPos, _duration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
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
