using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public sealed class FadeTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _enterDuration = 0.25f;
        [SerializeField] private float _exitDuration = 0.2f;
        [SerializeField] private Ease _enterEase = Ease.OutCubic;
        [SerializeField] private Ease _exitEase = Ease.InCubic;

        public async Awaitable PlayEnter(RectTransform target)
        {
            _canvasGroup.alpha = 0f;
            await _canvasGroup
                .DOFade(1f, _enterDuration)
                .SetEase(_enterEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await _canvasGroup
                .DOFade(0f, _exitDuration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
        }
    }
}
