using System;
using LitMotion.Animation;
using LitMotion.Animation.Components;

namespace LogosSDK.Tween
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Spiral Move")]
    public sealed class SpiralMoveAnimation : TransformPositionAnimationBase<SpiralOption, SpiralMotionAdapter> { }
}
