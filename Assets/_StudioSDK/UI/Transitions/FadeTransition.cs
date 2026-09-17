using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
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
            await LMotion.Create(0f, 1f, _enterDuration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(_canvasGroup)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await LMotion.Create(_canvasGroup.alpha, 0f, _exitDuration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(_canvasGroup)
                .AddTo(gameObject)
                .WaitAsync();
        }
    }
}
