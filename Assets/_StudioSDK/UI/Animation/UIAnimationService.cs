using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
using LogosSDK.Core.Logging;
using LogosSDK.Tween;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.UI.Animation
{
    public sealed class UIAnimationService : IUIAnimationService
    {
        // SetUpdate(true) của bản cũ = unscaled time. CancelOnError: target bị huỷ giữa
        // chừng thì huỷ cả chuỗi (bản cũ dựa vào safe mode).
        private static readonly Action<MotionBuilder<double, NoOptions, DoubleMotionAdapter>> Unscaled =
            b => b.WithCancelOnError().WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);

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
                case PanelEnterType.SlideUpBounce:   await PlaySlideBounceEnter(rt, cg, profile, -300f); break;
                case PanelEnterType.SlideDownBounce: await PlaySlideBounceEnter(rt, cg, profile, 300f); break;
                case PanelEnterType.DropBounce:      await PlayDropBounceEnter(rt, cg); break;
                case PanelEnterType.FadeOnly:        await PlayFadeEnter(cg, profile); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.EnterType), profile.EnterType, null);
            }
        }

        public async Awaitable PlayPanelExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (rt == null || profile == null) return;
            switch (profile.ExitType)
            {
                case PanelExitType.ScaleFade: await PlayScaleFadeExit(rt, cg, profile); break;
                case PanelExitType.SlideDown: await PlaySlideExit(rt, cg, profile, -300f); break;
                case PanelExitType.SlideUp:   await PlaySlideExit(rt, cg, profile, 300f); break;
                case PanelExitType.FadeOnly:  await PlayFadeExit(cg, profile); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.ExitType), profile.ExitType, null);
            }
        }

        public async Awaitable PlayStagger(IReadOnlyList<RectTransform> elements, UIStaggerAnimationSO profile)
        {
            if (elements == null || elements.Count == 0 || profile == null) return;

            GameObject linkTarget = null;
            for (int i = 0; i < elements.Count; i++)
                if (elements[i] != null) { linkTarget = elements[i].gameObject; break; }
            if (linkTarget == null) return;

            var seq = LSequence.Create();

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
                    {
                        var peak = Vector3.one * 1.15f;
                        element.localScale = Vector3.zero;
                        seq.Insert(delay, Scale(element, Vector3.zero, peak, dur * 0.6f, profile.ElementEase));
                        seq.Insert(delay + dur * 0.6f, Scale(element, peak, Vector3.one, dur * 0.4f, Ease.OutQuad));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.5f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.SlideUp:
                    {
                        var endPos = element.anchoredPosition;
                        var startPos = new Vector2(endPos.x, endPos.y - 24f);
                        element.anchoredPosition = startPos;
                        seq.Insert(delay, Move(element, startPos, endPos, dur, profile.ElementEase));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.7f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.FadeIn:
                        if (elemCg == null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug($"[UIAnimationService] FadeIn stagger: '{element.name}' has no CanvasGroup — skipping");
                            break;
                        }
                        elemCg.alpha = 0f;
                        seq.Insert(delay, Fade(elemCg, 0f, 1f, dur, profile.ElementEase));
                        break;

                    case StaggerType.PopWithSpin:
                    {
                        var peak = Vector3.one * 1.1f;
                        var tilt = Quaternion.Euler(0f, 0f, -15f);
                        element.localScale = Vector3.zero;
                        element.localRotation = tilt;
                        seq.Insert(delay, Scale(element, Vector3.zero, peak, dur * 0.6f, profile.ElementEase));
                        seq.Insert(delay + dur * 0.6f, Scale(element, peak, Vector3.one, dur * 0.4f, Ease.OutQuad));
                        seq.Insert(delay, LMotion.Create(tilt, Quaternion.identity, dur * 0.7f)
                                                 .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalRotation(element));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.5f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.StampDrop:
                    {
                        var rest = element.anchoredPosition;
                        var high = new Vector2(rest.x, rest.y + 60f);
                        var squash = new Vector3(1.18f, 0.82f, 1f);
                        element.anchoredPosition = high;
                        element.localScale = Vector3.one;
                        seq.Insert(delay, Move(element, high, rest, dur * 0.35f, Ease.InQuad));
                        seq.Insert(delay + dur * 0.35f, Scale(element, Vector3.one, squash, dur * 0.15f, Ease.OutQuad));
                        seq.Insert(delay + dur * 0.5f, Scale(element, squash, Vector3.one, dur * 0.5f, Ease.OutBack));
                        break;
                    }

                    default:
                        seq.Dispose();
                        throw new ArgumentOutOfRangeException(nameof(profile.StaggerType), profile.StaggerType, null);
                }
            }

            await seq.Run(Unscaled).AddTo(linkTarget).WaitAsync();
        }

        // ── Panel Enter helpers ──────────────────────────────────────────────

        private async Awaitable PlayScaleFadeEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            rt.localScale = Vector3.zero;
            var seq = LSequence.Create();
            seq.Insert(0f, Scale(rt, Vector3.zero, Vector3.one, profile.EnterDuration, profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, profile.EnterDuration, Ease.OutCubic)); }
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        // offsetY âm = trồi từ dưới lên (SlideUpBounce), dương = rơi từ trên xuống (SlideDownBounce).
        private async Awaitable PlaySlideBounceEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile, float offsetY)
        {
            var restPos = rt.anchoredPosition;
            var startPos = new Vector2(restPos.x, restPos.y + offsetY);
            rt.anchoredPosition = startPos;
            var seq = LSequence.Create();
            seq.Insert(0f, Move(rt, startPos, restPos, profile.EnterDuration, profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, profile.EnterDuration * 0.7f, Ease.OutCubic)); }
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        // Mốc thời gian chép đúng bản cũ: Append đặt ở CUỐI toàn chuỗi, tính cả cú mờ vào đã
        // Join — có CanvasGroup thì cú co bắt đầu sau 0.1s, không có thì bắt đầu ngay.
        private async Awaitable PlayDropBounceEnter(RectTransform rt, CanvasGroup cg)
        {
            var big = Vector3.one * 1.3f;
            var small = Vector3.one * 0.92f;
            rt.localScale = big;
            var seq = LSequence.Create();
            float at = 0f;
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, 0.1f, Ease.OutCubic)); at = 0.1f; }
            seq.Insert(at, Scale(rt, big, small, 0.12f, Ease.InQuad));
            seq.Insert(at + 0.12f, Scale(rt, small, Vector3.one, 0.1f, Ease.OutBack));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        private async Awaitable PlayFadeEnter(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            cg.alpha = 0f;
            await LMotion.Create(0f, 1f, profile.EnterDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(cg)
                .AddTo(cg.gameObject)
                .WaitAsync();
        }

        // ── Panel Exit helpers ───────────────────────────────────────────────

        private async Awaitable PlayScaleFadeExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var seq = LSequence.Create();
            seq.Insert(0f, Scale(rt, rt.localScale, Vector3.zero, profile.ExitDuration, profile.ExitEase));
            if (cg != null) seq.Insert(0f, Fade(cg, cg.alpha, 0f, profile.ExitDuration, Ease.InCubic));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
            if (rt != null) rt.localScale = Vector3.one;
        }

        // offsetY âm = trượt xuống (SlideDown), dương = trượt lên (SlideUp).
        private async Awaitable PlaySlideExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile, float offsetY)
        {
            var currentPos = rt.anchoredPosition;
            var seq = LSequence.Create();
            seq.Insert(0f, Move(rt, currentPos, new Vector2(currentPos.x, currentPos.y + offsetY), profile.ExitDuration, profile.ExitEase));
            if (cg != null) seq.Insert(0f, Fade(cg, cg.alpha, 0f, profile.ExitDuration * 0.5f, Ease.InCubic));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
            if (rt != null) rt.anchoredPosition = currentPos;
        }

        private async Awaitable PlayFadeExit(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            await LMotion.Create(cg.alpha, 0f, profile.ExitDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(cg)
                .AddTo(cg.gameObject)
                .WaitAsync();
        }

        // ── Motion con cho LSequence (scheduler đặt ở Run, không đặt ở đây) ─

        private static MotionHandle Scale(Transform t, Vector3 from, Vector3 to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToLocalScale(t);
        }

        private static MotionHandle Move(RectTransform rt, Vector2 from, Vector2 to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToAnchoredPosition(rt);
        }

        private static MotionHandle Fade(CanvasGroup cg, float from, float to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToAlpha(cg);
        }
    }
}
