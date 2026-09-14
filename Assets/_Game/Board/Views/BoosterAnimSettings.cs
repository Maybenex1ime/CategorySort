// Thông số animation của ba booster (Nam châm, Xáo, Undo) — một asset (SO_BoosterAnim) để
// chỉnh trong Inspector mà không mở scene. Giá trị mặc định ở đây là bản chạy được khi
// asset chưa gán.
using DG.Tweening;
using UnityEngine;

namespace WordStack.Board
{
    [CreateAssetMenu(menuName = "WordStack/Booster Anim Settings", fileName = "SO_BoosterAnim")]
    public class BoosterAnimSettings : ScriptableObject
    {
        [Header("Nam châm — 4 thẻ bay về một điểm rồi nổ")]
        [Tooltip("Điểm hội tụ theo toạ độ viewport của camera: (0.5, 0.5) = giữa màn hình")]
        public Vector2 magnetGatherViewport = new Vector2(0.5f, 0.5f);
        [Tooltip("Thẻ đang chôn nhô lên từ giữa hộp che trước khi bay (giây)")]
        public float magnetRevealDur = 0.15f;
        [Tooltip("Thẻ phồng lên một nhịp trước khi bị hút")]
        public float magnetPopScale = 1.12f;
        public float magnetPopDur = 0.10f;
        public float magnetFlyDur = 0.45f;
        public Ease magnetFlyEase = Ease.InOutCubic;
        [Tooltip("Lệch pha giữa 4 thẻ")]
        public float magnetStagger = 0.05f;
        [Tooltip("Cỡ thẻ khi tới điểm hội tụ (1 = không co)")]
        public float magnetGatherScale = 0.75f;
        [Tooltip("Thẻ xoay bao nhiêu độ trên đường bay (0 = tắt)")]
        public float magnetSpin = 0f;
        [Tooltip("Khựng ở điểm hội tụ trước khi nổ")]
        public float magnetHold = 0.08f;
        public float magnetBurstDur = 0.20f;
        public Ease magnetBurstEase = Ease.InBack;

        [Header("Xáo — cả lớp trên xoáy vào tâm bàn rồi bung ra ô mới")]
        [Tooltip("Pha hút vào tâm — chậm để đọc được xoáy (giây)")]
        public float shuffleInDur = 1.1f;
        [Tooltip("Pha bung ra ô mới")]
        public float shuffleOutDur = 0.55f;
        [Tooltip("Số vòng xoáy mỗi pha")]
        public float shuffleTurns = 2f;
        [Tooltip("Cỡ thẻ lúc dồn về tâm — 0 thì không thấy hội tụ")]
        public float shuffleGatherScale = 0.4f;
        [Tooltip("Xoay pivot; thẻ quay ngược dùng CÙNG ease này để luôn thẳng")]
        public Ease shuffleSpinEase = Ease.OutCubic;
        public Ease shuffleMoveInEase = Ease.InBack;
        public Ease shuffleMoveOutEase = Ease.OutBack;
        public Ease shuffleScaleInEase = Ease.InQuad;
        public Ease shuffleScaleOutEase = Ease.OutQuad;

        [Header("Undo — thẻ bay ngược về ô cũ")]
        public float undoPopScale = 1.10f;
        public float undoPopDur = 0.08f;
        public float undoFlyDur = 0.22f;
        public Ease undoFlyEase = Ease.OutCubic;

        [Header("Undo — hộp cũ hiện lại (nước trước đã làm nó lùi ra)")]
        [Tooltip("Vị trí xuất phát so với chỗ đứng thật (world unit): y dương = từ trên trượt xuống")]
        public Vector2 undoBoxSlideFrom = new Vector2(0f, 0.6f);
        public float undoBoxSlideDur = 0.25f;
        public Ease undoBoxSlideEase = Ease.OutCubic;
        [Tooltip("Thẻ trong hộp cũ nở ra sau khi hộp về chỗ")]
        public float undoBoxTilePopDur = 0.12f;
    }
}
