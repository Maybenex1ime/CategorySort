using System.Collections.Generic;
using DG.Tweening;
using LogosSDK.Core.Logging;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.UI.Animation
{
    public sealed class UIAnimationService : IUIAnimationService
    {
        private readonly UIAnimationSettingsSO _settings;
        private readonly ILogger _logger = LogManager.GetLogger<UIAnimationService>();

        private UIPanelAnimationSO _cachedPanelProfile;
        private UIPanelAnimationSO _cachedPopupProfile;
        private UIButtonFeedbackSO _cachedButtonProfile;

        public UIAnimationService(UIAnimationSettingsSO settings)
        {
            _settings = settings;
        }

        public UIPanelAnimationSO GetPanelProfile(bool isPopup)
        {
            if (isPopup)
            {
                if (_cachedPopupProfile == null) _cachedPopupProfile = _settings.DefaultPopupProfile;
                return _cachedPopupProfile;
            }
            if (_cachedPanelProfile == null) _cachedPanelProfile = _settings.DefaultPanelProfile;
            return _cachedPanelProfile;
        }

        public UIButtonFeedbackSO GetButtonProfile()
        {
            if (_cachedButtonProfile == null) _cachedButtonProfile = _settings.DefaultButtonProfile;
            return _cachedButtonProfile;
        }

        public async Awaitable PlayPanelEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (rt == null || profile == null) return;
            switch (profile.EnterType)
            {
                case PanelEnterType.ScaleFade:       await PlayScaleFadeEnter(rt, cg, profile); break;
                case PanelEnterType.SlideUpBounce:   await PlaySlideUpBounceEnter(rt, cg, profile); break;
                case PanelEnterType.SlideDownBounce: await PlaySlideDownBounceEnter(rt, cg, profile); break;
                case PanelEnterType.DropBounce:      await PlayDropBounceEnter(rt, cg, profile); break;
                case PanelEnterType.FadeOnly:        await PlayFadeEnter(cg, profile); break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(profile.EnterType), profile.EnterType, null);
            }
        }

        public async Awaitable PlayPanelExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (rt == null || profile == null) return;
            switch (profile.ExitType)
            {
                case PanelExitType.ScaleFade: await PlayScaleFadeExit(rt, cg, profile); break;
                case PanelExitType.SlideDown: await PlaySlideDownExit(rt, cg, profile); break;
                case PanelExitType.SlideUp:   await PlaySlideUpExit(rt, cg, profile); break;
                case PanelExitType.FadeOnly:  await PlayFadeExit(cg, profile); break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(profile.ExitType), profile.ExitType, null);
            }
        }

        public async Awaitable PlayStagger(IReadOnlyList<RectTransform> elements, UIStaggerAnimationSO profile)
        {
            if (elements == null || elements.Count == 0 || profile == null) return;

            GameObject linkTarget = null;
            for (int i = 0; i < elements.Count; i++)
                if (elements[i] != null) { linkTarget = elements[i].gameObject; break; }
            if (linkTarget == null) return;

            var masterSeq = DOTween.Sequence();
            masterSeq.SetUpdate(true).SetLink(linkTarget);

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;

                float delay = i * profile.DelayBetweenElements;
                float dur = profile.ElementDuration;
                element.TryGetComponent<CanvasGroup>(out var elemCg);

                switch (profile.StaggerType)
                {
                    case StaggerType.Pop:
                        element.localScale = Vector3.zero;
                        masterSeq.Insert(delay, element.DOScale(1.15f, dur * 0.6f).SetEase(profile.ElementEase).SetUpdate(true));
                        masterSeq.Insert(delay + dur * 0.6f, element.DOScale(1f, dur * 0.4f).SetEase(Ease.OutQuad).SetUpdate(true));
                        if (elemCg != null) { elemCg.alpha = 0f; masterSeq.Insert(delay, elemCg.DOFade(1f, dur * 0.5f).SetUpdate(true)); }
                        break;

                    case StaggerType.SlideUp:
                        var startPos = new Vector2(element.anchoredPosition.x, element.anchoredPosition.y - 24f);
                        var endPos = element.anchoredPosition;
                        element.anchoredPosition = startPos;
                        masterSeq.Insert(delay, element.DOAnchorPos(endPos, dur).SetEase(profile.ElementEase).SetUpdate(true));
                        if (elemCg != null) { elemCg.alpha = 0f; masterSeq.Insert(delay, elemCg.DOFade(1f, dur * 0.7f).SetUpdate(true)); }
                        break;

                    case StaggerType.FadeIn:
                        if (elemCg == null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug($"[UIAnimationService] FadeIn stagger: '{element.name}' has no CanvasGroup — skipping");
                            break;
                        }
                        elemCg.alpha = 0f;
                        masterSeq.Insert(delay, elemCg.DOFade(1f, dur).SetEase(profile.ElementEase).SetUpdate(true));
                        break;

                    case StaggerType.PopWithSpin:
                        element.localScale = Vector3.zero;
                        element.localRotation = Quaternion.Euler(0f, 0f, -15f);
                        masterSeq.Insert(delay, element.DOScale(1.1f, dur * 0.6f).SetEase(profile.ElementEase).SetUpdate(true));
                        masterSeq.Insert(delay + dur * 0.6f, element.DOScale(1f, dur * 0.4f).SetEase(Ease.OutQuad).SetUpdate(true));
                        masterSeq.Insert(delay, element.DOLocalRotate(Vector3.zero, dur * 0.7f).SetEase(Ease.OutBack).SetUpdate(true));
                        if (elemCg != null) { elemCg.alpha = 0f; masterSeq.Insert(delay, elemCg.DOFade(1f, dur * 0.5f).SetUpdate(true)); }
                        break;

                    case StaggerType.StampDrop:
                        var stampRestPos = element.anchoredPosition;
                        element.anchoredPosition = new Vector2(stampRestPos.x, stampRestPos.y + 60f);
                        element.localScale = Vector3.one;
                        masterSeq.Insert(delay, element.DOAnchorPos(stampRestPos, dur * 0.35f).SetEase(Ease.InQuad).SetUpdate(true));
                        masterSeq.Insert(delay + dur * 0.35f, element.DOScale(new Vector3(1.18f, 0.82f, 1f), dur * 0.15f).SetEase(Ease.OutQuad).SetUpdate(true));
                        masterSeq.Insert(delay + dur * 0.5f, element.DOScale(Vector3.one, dur * 0.5f).SetEase(Ease.OutBack).SetUpdate(true));
                        break;

                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(profile.StaggerType), profile.StaggerType, null);
                }
            }

            await masterSeq.AsyncWaitForCompletion();
        }

        // ── Panel Enter helpers ──────────────────────────────────────────────

        private async Awaitable PlayScaleFadeEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            rt.localScale = Vector3.zero;
            var s = DOTween.Sequence();
            s.Join(rt.DOScale(Vector3.one, profile.EnterDuration).SetEase(profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; s.Join(cg.DOFade(1f, profile.EnterDuration).SetEase(Ease.OutCubic)); }
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
        }

        private async Awaitable PlaySlideUpBounceEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var restPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(restPos.x, restPos.y - 300f);
            var s = DOTween.Sequence();
            s.Join(rt.DOAnchorPos(restPos, profile.EnterDuration).SetEase(profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; s.Join(cg.DOFade(1f, profile.EnterDuration * 0.7f).SetEase(Ease.OutCubic)); }
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
        }

        private async Awaitable PlaySlideDownBounceEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var restPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(restPos.x, restPos.y + 300f);
            var s = DOTween.Sequence();
            s.Join(rt.DOAnchorPos(restPos, profile.EnterDuration).SetEase(profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; s.Join(cg.DOFade(1f, profile.EnterDuration * 0.7f).SetEase(Ease.OutCubic)); }
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
        }

        private async Awaitable PlayDropBounceEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            rt.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            var s = DOTween.Sequence();
            if (cg != null) { cg.alpha = 0f; s.Join(cg.DOFade(1f, 0.1f).SetEase(Ease.OutCubic)); }
            s.Append(rt.DOScale(new Vector3(0.92f, 0.92f, 0.92f), 0.12f).SetEase(Ease.InQuad));
            s.Append(rt.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack));
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
        }

        private async Awaitable PlayFadeEnter(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            cg.alpha = 0f;
            await cg.DOFade(1f, profile.EnterDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(cg.gameObject)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        // ── Panel Exit helpers ───────────────────────────────────────────────

        private async Awaitable PlayScaleFadeExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var s = DOTween.Sequence();
            s.Join(rt.DOScale(Vector3.zero, profile.ExitDuration).SetEase(profile.ExitEase));
            if (cg != null) s.Join(cg.DOFade(0f, profile.ExitDuration).SetEase(Ease.InCubic));
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
            if (rt != null) rt.localScale = Vector3.one;
        }

        private async Awaitable PlaySlideDownExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var currentPos = rt.anchoredPosition;
            var s = DOTween.Sequence();
            s.Join(rt.DOAnchorPos(new Vector2(currentPos.x, currentPos.y - 300f), profile.ExitDuration).SetEase(profile.ExitEase));
            if (cg != null) s.Join(cg.DOFade(0f, profile.ExitDuration * 0.5f).SetEase(Ease.InCubic));
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
            if (rt != null) rt.anchoredPosition = currentPos;
        }

        private async Awaitable PlaySlideUpExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var currentPos = rt.anchoredPosition;
            var s = DOTween.Sequence();
            s.Join(rt.DOAnchorPos(new Vector2(currentPos.x, currentPos.y + 300f), profile.ExitDuration).SetEase(profile.ExitEase));
            if (cg != null) s.Join(cg.DOFade(0f, profile.ExitDuration * 0.5f).SetEase(Ease.InCubic));
            s.SetLink(rt.gameObject).SetUpdate(true);
            await s.AsyncWaitForCompletion();
            if (rt != null) rt.anchoredPosition = currentPos;
        }

        private async Awaitable PlayFadeExit(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            await cg.DOFade(0f, profile.ExitDuration)
                .SetEase(Ease.InCubic)
                .SetLink(cg.gameObject)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }
    }
}
