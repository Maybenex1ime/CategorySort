// Chuyển động xoáy ốc: quay quanh điểm đích, bán kính co tuyến tính về 0 nên vật xoáy vào
// đúng endValue. Port từ Mukbang ASMR (Utils/LitAnim/SpiralMotionAdapter.cs @ b050e5bb).
using System;
using LitMotion;
using Unity.Jobs;
using UnityEngine;
using LogosSDK.Tween;

// AOT (IL2CPP): đăng ký job generic của MotionUpdateJob cho SpiralOption/SpiralMotionAdapter —
// không đăng ký thì build IL2CPP thiếu job cụ thể hoá này, theo đúng cách LitMotion tự đăng ký
// cho các adapter build-in của nó (xem Runtime/Adapters/FixedStringMotionAdapters.cs).
[assembly: RegisterGenericJobType(typeof(MotionUpdateJob<Vector3, SpiralOption, SpiralMotionAdapter>))]

namespace LogosSDK.Tween
{
    [Serializable]
    public struct SpiralOption : IMotionOptions, IEquatable<SpiralOption>
    {
        public float revolutions;
        public OrbitPlane plane;

        public enum OrbitPlane
        {
            XZ = 0,
            XY = 1
        }

        public static SpiralOption Default => new();

        public SpiralOption(float revolutions = 1f, OrbitPlane plane = OrbitPlane.XY)
        {
            this.revolutions = Mathf.Max(0f, revolutions);
            this.plane = plane;
        }

        public readonly bool Equals(SpiralOption other)
        {
            return revolutions.Equals(other.revolutions) && plane == other.plane;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is SpiralOption other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(revolutions, (int)plane);
        }
    }

    public readonly struct SpiralMotionAdapter : IMotionAdapter<Vector3, SpiralOption>
    {
        public Vector3 Evaluate(ref Vector3 startValue, ref Vector3 endValue, ref SpiralOption options, in MotionEvaluationContext context)
        {
            // Lệch từ điểm đích — giữ nguyên góc xuất phát quanh đích.
            Vector3 startOffset = startValue - endValue;
            float initialRadius = startOffset.magnitude;

            if (initialRadius < 0.0001f)
                return endValue;

            float initialAngle = options.plane == SpiralOption.OrbitPlane.XZ
                ? Mathf.Atan2(startOffset.z, startOffset.x)
                : Mathf.Atan2(startOffset.y, startOffset.x);

            float angle = initialAngle + options.revolutions * Mathf.PI * 2f * context.Progress;
            float currRadius = initialRadius * (1f - context.Progress);

            Vector3 offset = options.plane == SpiralOption.OrbitPlane.XZ
                ? new Vector3(Mathf.Cos(angle) * currRadius, 0f, Mathf.Sin(angle) * currRadius)
                : new Vector3(Mathf.Cos(angle) * currRadius, Mathf.Sin(angle) * currRadius, 0f);

            return endValue + offset;
        }
    }
}
