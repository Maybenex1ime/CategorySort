using LitMotion;
using LogosSDK.Tween;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Screens
{
    public sealed class LoadingScreen : ScreenBase
    {
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private float _fillDuration = 2f;

        private MotionHandle _fillTween;

        public override async Awaitable Show(object args = null)
        {
            if (_loadingSlider != null)
            {
                _loadingSlider.value = 0f;
                _fillTween = LMotion.Create(0f, 0.9f, _fillDuration)
                    .WithEase(Ease.OutCubic)
                    .WithCancelOnError()
                    .Bind(_loadingSlider, (v, slider) => slider.value = v)
                    .AddTo(gameObject);
            }

            await base.Show(args);
        }

        public override async Awaitable Hide()
        {
            if (_loadingSlider != null)
            {
                _fillTween.TryCancel();

                await LMotion.Create(_loadingSlider.value, 1f, 0.3f)
                    .WithEase(Ease.OutQuad)
                    .WithCancelOnError()
                    .Bind(_loadingSlider, (v, slider) => slider.value = v)
                    .AddTo(gameObject)
                    .WaitAsync();
            }

            await base.Hide();
        }
    }
}
