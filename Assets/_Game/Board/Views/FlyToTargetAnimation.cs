// Thẻ mini bay vào ô của hộp (Stack Tile Holder — spec 2026-09-29-stack-tile-holder-design Mục 5).
// Component LitMotion tự viết (cùng kiểu SpriteGroupAlphaAnimation) vì đích là ô của hộp — hộp sống ngoài
// Stack.prefab, nên StackView gán Destination ngay trước khi Play, không lưu vào prefab. Play chụp vị trí
// hiện tại (sau khi HorizontalSpriteLayout đã căn giữa) rồi bay tới vị trí world của đích; Stop trả về chỗ
// đã chụp. Thiếu target hoặc đích thì không bay.
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Fly To Target")]
    public sealed class FlyToTargetAnimation : LitMotionAnimationComponent
    {
        [SerializeField] Transform target;          // thẻ mini
        [SerializeField] int slot;                  // ô đích trong hộp (0..3) — StackView tra Destination theo số này
        [SerializeField] float duration = 0.3f;
        [SerializeField] float delay;
        [SerializeField] Ease ease = Ease.OutQuad;

        Vector3 from;
        bool captured;

        public int Slot => slot;
        public Transform Destination { get; set; }

        public override MotionHandle Play()
        {
            if (target == null || Destination == null) return default;
            from = target.position;
            captured = true;
            return LMotion.Create(from, Destination.position, duration)
                          .WithDelay(delay).WithEase(ease).WithCancelOnError()
                          .Bind(target, (p, t) => t.position = p);
        }

        public override void OnStop()
        {
            if (captured && target != null) target.position = from;
            captured = false;
        }
    }
}
