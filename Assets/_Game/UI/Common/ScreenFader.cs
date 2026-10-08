using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Common
{
    /// Tấm đen phủ màn khi chuyển Home ↔ Gameplay (report UI animation, SceneTransition: 0.25s OutQuad / InQuad).
    /// Canvas riêng tự dựng lần đầu dùng, sorting cao nhất, chặn bấm suốt lúc còn che.
    public static class ScreenFader
    {
        public const float Duration = 0.25f;

        static CanvasGroup _group;
        static MotionHandle _fade;

        public static Awaitable Cover() => FadeTo(1f, Ease.OutQuad);
        public static Awaitable Reveal() => FadeTo(0f, Ease.InQuad);

        static Awaitable FadeTo(float alpha, Ease ease)
        {
            var group = Group();
            _fade.TryComplete();   // lượt trước xong ngay — await của nó không bị huỷ
            group.blocksRaycasts = true;
            _fade = LMotion.Create(group.alpha, alpha, Duration)
                .WithEase(ease)
                .WithOnComplete(() => { if (group != null) group.blocksRaycasts = alpha > 0f; })
                .Bind(group, (a, g) => g.alpha = a)
                .AddTo(group.gameObject);
            return _fade.ToAwaitable();
        }

        static CanvasGroup Group()
        {
            if (_group != null) return _group;

            var go = new GameObject("ScreenFader", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)black.transform;
            rt.SetParent(go.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            black.GetComponent<Image>().color = Color.black;

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            return _group;
        }

        // Domain Reload tắt: static sống qua lần Play sau, GameObject cũ thì đã bị huỷ.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            _group = null;
            _fade = default;
        }
    }
}
