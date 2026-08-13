using DG.Tweening;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Screens
{
    public sealed class LoadingScreen : ScreenBase
    {
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private float _fillDuration = 2f;

        private Tweener _fillTween;

        public override async Awaitable Show(object args = null)
        {
            if (_loadingSlider != null)
            {
                _loadingSlider.value = 0f;
                _fillTween = DOTween
                    .To(() => _loadingSlider.value, v => _loadingSlider.value = v, 0.9f, _fillDuration)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject);
            }

            await base.Show(args);
        }

        public override async Awaitable Hide()
        {
            if (_loadingSlider != null)
            {
                if (_fillTween != null && _fillTween.IsActive())
                    _fillTween.Kill();

                await DOTween
                    .To(() => _loadingSlider.value, v => _loadingSlider.value = v, 1f, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject)
                    .AsyncWaitForCompletion();
            }

            await base.Hide();
        }
    }
}
