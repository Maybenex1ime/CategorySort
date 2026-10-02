// Chuỗi scale/góc của các hiệu ứng thẻ booster để xem trước trong Inspector của SO_BoosterAnim
// (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md Mục 5). Không tạo motion —
// scheduler mặc định của LitMotion không chạy ngoài Play mode — chỉ tính giá trị theo thời gian.
//
// Ease cố định (OutQuad, OutBack) lặp lại từ BoardController.MagnetAnimation / AppendParentFlight /
// UndoAnimation: sửa bên đó thì sửa cả ở đây.
using LitMotion;
using UnityEngine;

namespace WordStack.Board.Editor
{
    public static class BoosterTilePreview
    {
        /// <summary>Một nhịp: scale (bội của cỡ gốc) và góc z (độ, cộng vào góc gốc) đi từ From tới To.</summary>
        public struct Step
        {
            public float Duration;
            public float ScaleFrom, ScaleTo;
            public Ease ScaleEase;
            public float SpinFrom, SpinTo;
            public Ease SpinEase;
        }

        public static Step[] MagnetTile(BoosterAnimSettings a)
        {
            float spin = Mathf.Abs(a.magnetSpin) > 0.01f ? a.magnetSpin : 0f;   // như BoardController: |spin| ≤ 0.01 = tắt
            return new[]
            {
                Scale(a.magnetPopDur, 1f, a.magnetPopScale, Ease.OutQuad),
                new Step
                {
                    Duration = a.magnetFlyDur,
                    ScaleFrom = a.magnetPopScale, ScaleTo = a.magnetGatherScale, ScaleEase = Ease.OutQuad,
                    SpinFrom = 0f, SpinTo = spin, SpinEase = a.magnetFlyEase,
                },
                Hold(a.magnetHold, a.magnetGatherScale, spin),
                new Step
                {
                    Duration = a.magnetBurstDur,
                    ScaleFrom = a.magnetGatherScale, ScaleTo = 0f, ScaleEase = a.magnetBurstEase,
                    SpinFrom = spin, SpinTo = spin,
                },
            };
        }

        public static Step[] ParentTile(BoosterAnimSettings a)
        {
            return new[]
            {
                Scale(a.magnetParentBloomDur, 0f, a.magnetGatherScale, Ease.OutBack),
                Hold(a.magnetParentHold, a.magnetGatherScale, 0f),
                Scale(a.magnetParentFlyDur, a.magnetGatherScale, 1f, Ease.OutQuad),
            };
        }

        public static Step[] UndoTile(BoosterAnimSettings a)
        {
            return new[]
            {
                Scale(a.undoPopDur, 1f, a.undoPopScale, Ease.OutQuad),
                Scale(a.undoFlyDur, a.undoPopScale, 1f, Ease.OutQuad),
            };
        }

        public static Step[] RevealTile(BoosterAnimSettings a)
        {
            return new[] { Scale(a.magnetRevealDur, 0f, 1f, Ease.OutBack) };
        }

        public static float TotalDuration(Step[] steps)
        {
            float t = 0f;
            foreach (var s in steps) t += Mathf.Max(0f, s.Duration);
            return t;
        }

        /// <summary>
        /// Giá trị tại thời điểm t (giây từ lúc bắt đầu). Trả false khi đã qua hết chuỗi — lúc đó scale/spin là
        /// giá trị cuối của nhịp cuối.
        /// </summary>
        public static bool Sample(Step[] steps, float t, out float scale, out float spin)
        {
            scale = 1f; spin = 0f;
            if (steps == null || steps.Length == 0) return false;
            float start = 0f;
            foreach (var s in steps)
            {
                float d = Mathf.Max(0f, s.Duration);
                if (t < start + d)
                {
                    float k = d <= 0f ? 1f : Mathf.Clamp01((t - start) / d);
                    scale = Mathf.LerpUnclamped(s.ScaleFrom, s.ScaleTo, EaseUtility.Evaluate(k, s.ScaleEase));
                    spin = Mathf.LerpUnclamped(s.SpinFrom, s.SpinTo, EaseUtility.Evaluate(k, s.SpinEase));
                    return true;
                }
                start += d;
            }
            var last = steps[steps.Length - 1];
            scale = last.ScaleTo; spin = last.SpinTo;
            return false;
        }

        static Step Scale(float dur, float from, float to, Ease ease)
        {
            return new Step { Duration = dur, ScaleFrom = from, ScaleTo = to, ScaleEase = ease };
        }

        static Step Hold(float dur, float scale, float spin)
        {
            return new Step { Duration = dur, ScaleFrom = scale, ScaleTo = scale, SpinFrom = spin, SpinTo = spin };
        }
    }
}
