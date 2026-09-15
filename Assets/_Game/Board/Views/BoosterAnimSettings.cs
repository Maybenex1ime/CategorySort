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
        [Header("Tấm nền xám suốt lúc booster diễn (BoardController.boosterBackdrop)")]
        [Tooltip("Mờ dần vào (giây) — chỉ khi Panel có CanvasGroup; 0 = bật khan")]
        public float backdropFadeIn = 0.15f;
        public float backdropFadeOut = 0.15f;

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
        [Tooltip("Cỡ thẻ khi tới điểm hội tụ — >1 phóng to dần trên đường bay, 1 = giữ nguyên")]
        public float magnetGatherScale = 1.6f;
        [Tooltip("Thẻ xoay bao nhiêu độ trên đường bay (0 = tắt)")]
        public float magnetSpin = 0f;
        [Tooltip("Khựng ở điểm hội tụ trước khi nổ")]
        public float magnetHold = 0.08f;
        public float magnetBurstDur = 0.20f;
        public Ease magnetBurstEase = Ease.InBack;
        [Tooltip("Nhóm có cha (COLLAPSE): thẻ cha nở ra tại điểm gộp lúc thẻ cuối nổ (giây)")]
        public float magnetParentBloomDur = 0.25f;
        [Tooltip("Thẻ cha đứng ở điểm gộp bao lâu trước khi bay về hộp")]
        public float magnetParentHold = 0.12f;
        [Tooltip("Thẻ cha bay từ điểm gộp về ô của nó trong hộp, co về cỡ thường")]
        public float magnetParentFlyDur = 0.35f;
        public Ease magnetParentFlyEase = Ease.InOutCubic;

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
