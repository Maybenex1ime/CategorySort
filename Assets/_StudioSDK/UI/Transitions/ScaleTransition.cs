using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public sealed class ScaleTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private float _enterDuration = 0.22f;
        [SerializeField] private float _exitDuration = 0.18f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        public async Awaitable PlayEnter(RectTransform target)
        {
            target.localScale = Vector3.zero;
            await target
                .DOScale(Vector3.one, _enterDuration)
                .SetEase(_enterEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await target
                .DOScale(Vector3.zero, _exitDuration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
            target.localScale = Vector3.one;
        }
    }
}
