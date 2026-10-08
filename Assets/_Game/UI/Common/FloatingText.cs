using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace LogosGame.Features.UI.Common
{
    /// Chữ nổi lên rồi mờ dần cạnh một ô số — "-N" khi tiêu coin (report UI animation, AddCoinAnim "Trừ coin":
    /// bay lên 50px trong 1s, mờ từ nửa sau). Nhân bản chính ô chữ mẫu nên cùng font/cỡ, không cần prefab riêng.
    public static class FloatingText
    {
        public const float Rise = 50f, Duration = 1f;
        static readonly Color SpendTint = new Color(1f, 0.45f, 0.45f);

        public static void Spawn(TMP_Text like, string text)
        {
            if (like == null || !like.gameObject.activeInHierarchy) return;

            var go = Object.Instantiate(like.gameObject, like.transform.parent, false);
            go.name = "FloatingText";
            foreach (Transform child in go.transform) Object.Destroy(child.gameObject);   // chỉ giữ chữ
            var copy = go.GetComponent<TMP_Text>();
            copy.text = text;
            copy.color = SpendTint;
            copy.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            Vector2 from = rt.anchoredPosition;
            LMotion.Create(from, from + Vector2.up * Rise, Duration)
                .WithEase(Ease.OutQuad).WithCancelOnError()
                .BindToAnchoredPosition(rt).AddTo(go);
            LMotion.Create(1f, 0f, Duration * 0.5f)
                .WithDelay(Duration * 0.5f).WithCancelOnError()
                .WithOnComplete(() => Object.Destroy(go))
                .Bind(copy, (a, t) => t.alpha = a).AddTo(go);
        }
    }
}
