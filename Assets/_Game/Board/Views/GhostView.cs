// Thẻ đang kéo. Giữ toàn bộ "feel" kéo-thả kiểu Balatro: lerp đuổi con trỏ + xoay Z theo độ
// trễ chuyển động + lắc sin/cos + pop scale. Mọi hằng feel là SerializeField để chỉnh trong
// Inspector mà không phải build lại.
//
// KHÔNG dùng DOTween cho phần đuổi con trỏ: đích di chuyển mỗi frame, không phải tween có
// điểm đến cố định. DOTween chỉ lo cú pop lúc nhấc lên.
using UnityEngine;
using DG.Tweening;

namespace WordStack.Board
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
        [SerializeField] float shadowLift = 0.10f;   // bóng lùi ra xa = thẻ rời mặt bàn

        [Header("Bóng dạt theo hướng + tốc độ kéo")]
        [SerializeField] float shadowSwing = 0.22f;      // dạt TỐI ĐA, tính bằng world unit
                                                         // (ô thẻ rộng 0.67 → 0.22 ≈ 1/3 bề ngang thẻ)
        [SerializeField] float shadowSwingAt = 6f;       // kéo nhanh bao nhiêu (unit/giây) thì đạt tối đa
        [SerializeField] float shadowSwingSmooth = 14f;  // bóng trôi tới đích nhanh/chậm
        [SerializeField] float shadowSwingHold = 0.5f;   // dưới tốc độ này coi như tay đứng yên

        Vector3 moveDelta, rotDelta;
        Vector3 shadowHome, swingTarget, swingCur;
        Vector2 lastPt;
        bool hasLastPt;
        float lift;                                  // 0..shadowLift, DOTween nhấc lúc Begin

        public Transform TileAnchor { get { return tileAnchor; } }

        void Awake()
        {
            if (shadow != null) shadowHome = shadow.localPosition;
        }

        public void Begin(Vector2 pt)
        {
            transform.position = new Vector3(pt.x, pt.y, 0f);
            transform.localScale = Vector3.one;
            moveDelta = Vector3.zero;
            rotDelta = Vector3.zero;
            swingTarget = Vector3.zero;
            swingCur = Vector3.zero;
            hasLastPt = false;
            lift = 0f;
            transform.DOScale(dragScale, 0.12f).SetEase(Ease.OutBack).SetLink(gameObject);

            // Bóng lùi ra xa lúc nhấc (CardVisual.PointerDown). Ghost chết lúc thả nên
            // không cần trả về chỗ cũ. Tween biến `lift` chứ không tween thẳng transform:
            // Follow() đặt lại vị trí bóng mỗi frame, hai bên ghi cùng một transform thì đá nhau.
            if (shadow != null)
                DOTween.To(() => lift, v => lift = v, shadowLift, 0.12f)
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

            // Bóng dạt NGANG theo hướng và tốc độ kéo: kéo sang phải thì bóng lệch sang phải, kéo
            // càng nhanh lệch càng xa. Đích của bóng được CHỐT lại — ngừng kéo mà vẫn giữ chuột thì bóng
            // nằm yên ở chỗ cú kéo cuối để lại, không trôi về giữa thẻ. Đổi hướng kéo thì chốt lại
            // đích mới. Đây là lý do phải tách `swingTarget` (đích, chỉ đổi khi tay đang di chuyển)
            // khỏi `swingCur` (chỗ bóng đang ở, luôn trôi về đích).
            //
            // Đo vận tốc con trỏ trực tiếp (chia dt → world unit/giây) thay vì mượn `moveDelta`.
            // `moveDelta` là độ trễ của thẻ, mà độ trễ đó tỉ lệ với dt: cùng một cú kéo, máy chạy
            // 144fps ra độ trễ chỉ bằng ~2/5 máy 60fps, nên bóng sẽ dạt khác nhau tuỳ máy.
            //
            // Cộng ở toạ độ THẾ GIỚI (không phải local): thẻ đang xoay Z tới ±40°, cộng ở local
            // thì "sang phải" bị góc xoay bẻ thành phải-chéo-xuống.
            if (shadow != null)
            {
                // CHỈ dạt ngang: chỉ đo vận tốc trục X, thành phần dọc không tham gia kể cả ở
                // ngưỡng xét — nên kéo thẳng đứng bị coi như tay đứng yên và bóng giữ nguyên chỗ
                // cũ, thay vì trôi về giữa thẻ.
                float velX = hasLastPt ? (pt.x - lastPt.x) / Mathf.Max(dt, 1e-4f) : 0f;
                lastPt = pt;
                hasLastPt = true;

                // Ngưỡng này cũng nuốt luôn nhịp giật do chuột poll thưa hơn tốc độ khung hình:
                // frame nào hệ điều hành không báo toạ độ mới thì velX = 0, đích giữ nguyên.
                if (Mathf.Abs(velX) > shadowSwingHold)
                    swingTarget = new Vector3(
                        Mathf.Clamp(velX * (shadowSwing / Mathf.Max(shadowSwingAt, 0.01f)),
                                    -shadowSwing, shadowSwing), 0f, 0f);

                swingCur = Vector3.Lerp(swingCur, swingTarget, shadowSwingSmooth * dt);
                shadow.position = tilt.TransformPoint(shadowHome + Vector3.down * lift) + swingCur;
            }
        }
    }
}
