// Thẻ đang kéo. Giữ toàn bộ "feel" kéo-thả kiểu Balatro: lerp đuổi con trỏ + xoay Z theo độ
// trễ chuyển động + lắc sin/cos + pop scale. Mọi hằng feel là SerializeField để chỉnh trong
// Inspector mà không phải build lại.
//
// KHÔNG dùng DOTween cho phần đuổi con trỏ: đích di chuyển mỗi frame, không phải tween có
// điểm đến cố định. DOTween chỉ lo cú pop lúc nhấc lên.
using UnityEngine;
using DG.Tweening;

namespace WordStack.Prototype
{
    public class GhostView : MonoBehaviour
    {
        [SerializeField] Transform tilt;
        [SerializeField] Transform tileAnchor;
        [SerializeField] Transform shadow;

        [Header("Feel")]
        [SerializeField] float followSpeed = 25f;
        [SerializeField] float rotAmount = 70f;
        [SerializeField] float rotSpeed = 20f;
        [SerializeField] float autoTilt = 7f;
        [SerializeField] float manualTilt = 20f;   // nghiêng theo độ trễ so với con trỏ
        [SerializeField] float tiltSpeed = 12f;
        [SerializeField] float dragScale = 1.15f;
        [SerializeField] float shadowLift = 0.10f; // bóng lùi ra xa = thẻ rời mặt bàn

        Vector3 moveDelta, rotDelta;

        public Transform TileAnchor { get { return tileAnchor; } }

        public void Begin(Vector2 pt)
        {
            transform.position = new Vector3(pt.x, pt.y, 0f);
            transform.localScale = Vector3.one;
            moveDelta = Vector3.zero;
            rotDelta = Vector3.zero;
            transform.DOScale(dragScale, 0.12f).SetEase(Ease.OutBack).SetLink(gameObject);

            // Bóng lùi ra xa lúc nhấc (CardVisual.PointerDown). Ghost chết lúc thả nên
            // không cần trả về chỗ cũ.
            if (shadow != null)
                shadow.DOLocalMove(shadow.localPosition + Vector3.down * shadowLift, 0.12f)
                      .SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public void Follow(Vector2 pt, float dt)
        {
            var target = new Vector3(pt.x, pt.y, 0f);
            transform.position = Vector3.Lerp(transform.position, target, followSpeed * dt);

            var movement = transform.position - target;
            moveDelta = Vector3.Lerp(moveDelta, movement, 25f * dt);
            rotDelta = Vector3.Lerp(rotDelta, moveDelta * rotAmount, rotSpeed * dt);
            transform.eulerAngles = new Vector3(0f, 0f, Mathf.Clamp(rotDelta.x, -40f, 40f));

            // Lắc nền sin/cos + nghiêng theo hướng thẻ đang bị bỏ lại sau con trỏ
            // (CardVisual.CardTilt: phần auto + phần manual). `movement` chính là offset
            // giữa thẻ và con trỏ mà bản gốc đo bằng ScreenToWorldPoint.
            float sine = Mathf.Sin(Time.time * 2f) * autoTilt - movement.y * manualTilt;
            float cosine = Mathf.Cos(Time.time * 2f) * autoTilt + movement.x * manualTilt;
            var e = tilt.localEulerAngles;
            tilt.localEulerAngles = new Vector3(
                Mathf.LerpAngle(e.x, sine, tiltSpeed * dt),
                Mathf.LerpAngle(e.y, cosine, tiltSpeed * dt), 0f);
        }
    }
}
