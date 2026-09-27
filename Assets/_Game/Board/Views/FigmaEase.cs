// Dựng AnimationCurve từ keyframe Figma để curve trong Inspector trùng số Figma.
//
// Ease-out của Figma là CSS cubic-bezier(0, 0, 0.58, 1). Key weighted biến mỗi đoạn Hermite thành
// đúng cubic bezier đó: handle ra của key đầu dài ~0 (P1 ≡ P0), handle vào của key sau nằm ngang và
// dài 0.42 đoạn (P2 ở 58 % thời gian, đã tới giá trị đích).
using UnityEngine;

namespace WordStack.Board
{
    public static class FigmaEase
    {
        const float EaseOutInWeight = 0.42f;
        const float ZeroWeight = 0.0001f;   // 0 tuyệt đối để Unity tự xử; gần 0 là đủ trùng P1 ≡ P0

        /// <summary>Mỗi đoạn giữa hai key kề nhau là ease-out Figma.</summary>
        public static AnimationCurve EaseOut(params (float t, float v)[] keys)
        {
            var k = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                k[i] = new Keyframe(keys[i].t, keys[i].v, 0f, 0f, EaseOutInWeight, ZeroWeight) { weightedMode = WeightedMode.Both };
            return new AnimationCurve(k);
        }

        /// <summary>Ease-out Figma chuẩn hoá 0..1 — dùng làm Custom Ease Curve của LitMotion.</summary>
        public static AnimationCurve CssEaseOut01() { return EaseOut((0f, 0f), (1f, 1f)); }

        /// <summary>Nội suy thẳng giữa các mẫu (Figma bake sẵn): tangent hai phía = độ dốc đoạn kề.</summary>
        public static AnimationCurve Linear(params (float t, float v)[] keys)
        {
            var k = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                float inT = i > 0 ? Slope(keys[i - 1], keys[i]) : Slope(keys[i], keys[i + 1]);
                float outT = i < keys.Length - 1 ? Slope(keys[i], keys[i + 1]) : inT;
                k[i] = new Keyframe(keys[i].t, keys[i].v, inT, outT);
            }
            return new AnimationCurve(k);
        }

        static float Slope((float t, float v) a, (float t, float v) b) { return (b.v - a.v) / (b.t - a.t); }
    }
}
