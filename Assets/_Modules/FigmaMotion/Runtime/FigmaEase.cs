// Dựng AnimationCurve từ keyframe Figma để curve trong Inspector trùng số Figma.
//
// Mỗi đoạn giữa hai key là một cubic-bezier CSS (x1, y1, x2, y2) — đúng thứ Figma dùng cho easing.
// Key weighted của Unity biến mỗi đoạn Hermite thành đúng cubic bezier đó: handle ra của key đầu
// dài x1 đoạn và dốc tới y1, handle vào của key sau dài (1 − x2) đoạn và dốc từ y2.
// Ease-out của Figma = cubic-bezier(0, 0, 0.58, 1): handle ra ~0 (P1 ≡ P0), handle vào nằm ngang
// dài 0.42 đoạn.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FigmaMotion
{
    public static class FigmaEase
    {
        const float ZeroWeight = 0.0001f;   // 0 tuyệt đối để Unity tự xử; gần 0 là đủ trùng P1 ≡ P0

        public static readonly Vector4 EaseOutBezier = new Vector4(0f, 0f, 0.58f, 1f);
        // Bezier có hai handle nằm trên đường thẳng tại 1/3 và 2/3 = đoạn thẳng; weight 1/3 cũng là
        // mặc định của key không weighted nên curve y hệt bản nội suy thẳng cũ.
        public static readonly Vector4 LinearBezier = new Vector4(1f / 3f, 1f / 3f, 2f / 3f, 2f / 3f);

        /// <summary>
        /// Curve qua mọi key; đoạn i (key i → key i+1) dùng bezier segs[i] (x1, y1, x2, y2).
        /// segs.Count phải bằng keys.Count − 1.
        /// </summary>
        public static AnimationCurve Curve(IReadOnlyList<(float t, float v)> keys, IReadOnlyList<Vector4> segs)
        {
            if (keys.Count < 2) throw new ArgumentException("Cần ít nhất 2 key.");
            if (segs.Count != keys.Count - 1) throw new ArgumentException("Số đoạn ease phải bằng số key − 1.");

            var k = new Keyframe[keys.Count];
            for (int i = 0; i < k.Length; i++)
                k[i] = new Keyframe(keys[i].t, keys[i].v, 0f, 0f, 1f / 3f, 1f / 3f) { weightedMode = WeightedMode.Both };

            for (int i = 0; i < segs.Count; i++)
            {
                float dt = keys[i + 1].t - keys[i].t, dv = keys[i + 1].v - keys[i].v;
                var b = segs[i];
                float wOut = Mathf.Max(b.x, ZeroWeight), wIn = Mathf.Max(1f - b.z, ZeroWeight);
                k[i].outWeight = wOut;
                k[i].outTangent = b.y * dv / (wOut * dt);
                k[i + 1].inWeight = wIn;
                k[i + 1].inTangent = (1f - b.w) * dv / (wIn * dt);
            }
            return new AnimationCurve(k);
        }

        /// <summary>Mỗi đoạn giữa hai key kề nhau là ease-out Figma.</summary>
        public static AnimationCurve EaseOut(params (float t, float v)[] keys)
        {
            return Curve(keys, Repeat(EaseOutBezier, keys.Length - 1));
        }

        /// <summary>Ease-out Figma chuẩn hoá 0..1 — dùng làm Custom Ease Curve của LitMotion.</summary>
        public static AnimationCurve CssEaseOut01() { return EaseOut((0f, 0f), (1f, 1f)); }

        /// <summary>Nội suy thẳng giữa các mẫu (Figma bake sẵn).</summary>
        public static AnimationCurve Linear(params (float t, float v)[] keys)
        {
            return Curve(keys, Repeat(LinearBezier, keys.Length - 1));
        }

        /// <summary>
        /// Tên easing Figma/CSS → bezier: linear, ease, ease-in, ease-out, ease-in-out, hoặc
        /// cubic-bezier(x1, y1, x2, y2). Không hiểu thì false.
        /// </summary>
        public static bool TryParse(string s, out Vector4 bezier)
        {
            bezier = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            // "Ease out", "ease_out", "ease-out" đều được — chỉ chuẩn hoá khi so từ khoá.
            switch (s.Replace('_', '-').Replace(' ', '-'))
            {
                case "linear": bezier = LinearBezier; return true;
                case "ease": bezier = new Vector4(0.25f, 0.1f, 0.25f, 1f); return true;
                case "ease-in": bezier = new Vector4(0.42f, 0f, 1f, 1f); return true;
                case "ease-out": bezier = EaseOutBezier; return true;
                case "ease-in-out": bezier = new Vector4(0.42f, 0f, 0.58f, 1f); return true;
            }
            if (!s.StartsWith("cubic-bezier(") || !s.EndsWith(")")) return false;
            var parts = s.Substring(13, s.Length - 14).Split(',');
            if (parts.Length != 4) return false;
            var v = new float[4];
            for (int i = 0; i < 4; i++)
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v[i])) return false;
            // x1, x2 là thời gian nên phải nằm trong 0..1 (luật CSS); y được vượt để có overshoot.
            if (v[0] < 0f || v[0] > 1f || v[2] < 0f || v[2] > 1f) return false;
            bezier = new Vector4(v[0], v[1], v[2], v[3]);
            return true;
        }

        static Vector4[] Repeat(Vector4 b, int n)
        {
            var r = new Vector4[Mathf.Max(n, 0)];
            for (int i = 0; i < r.Length; i++) r[i] = b;
            return r;
        }
    }
}
