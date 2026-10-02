using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
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
            await LMotion.Create(Vector3.zero, Vector3.one, _enterDuration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(target)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await LMotion.Create(target.localScale, Vector3.zero, _exitDuration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(target)
                .AddTo(gameObject)
                .WaitAsync();
            target.localScale = Vector3.one;
        }
    }
}
