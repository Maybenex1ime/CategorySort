// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Đổi số ở đó
// thì phải đổi hằng layout trong BoardController theo, vì hit-test tính từ hằng code.
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace WordStack.Board
{
    public class BoxView : MonoBehaviour
    {
        [SerializeField] Transform[] slotAnchors = new Transform[4];

        // Blocker của hộp — mỗi loại một root riêng trong Box.prefab, dựng art độc lập.
        // Hộp mở thì cả hai root tắt. Field nào chưa nối thì bỏ qua, bàn vẫn chạy như thường.
        [Header("Hộp khoá theo số nhóm (locked)")]
        [SerializeField] GameObject lockedRoot;
        [SerializeField] TextMeshPro lockedCountText;   // số nhóm CÒN phải gom (TMP 3D, world-space)

        [Header("Hộp khoá theo nhóm (grouplock)")]
        [FormerlySerializedAs("keyLockRoot")] [SerializeField] GameObject groupLockRoot;
        // Renderer hiện art của nhóm phải gom sạch để mở — sprite do bên gọi load (GroupDef.Art).
        // Xích, khiên là lớp riêng giữ màu gốc.
        [FormerlySerializedAs("keyLockTint")] [SerializeField] SpriteRenderer groupArt;

        [Header("Animation khoá")]
        [Tooltip("Hộp khoá theo số: root nảy bao nhiêu mỗi lần số còn cần giảm (0 = tắt)")]
        [SerializeField] float lockPunch = 0.12f;
        [SerializeField] float lockPunchDur = 0.25f;
        [Tooltip("Mở khoá: root bung lên rồi co về 0 (giây)")]
        [SerializeField] float unlockDur = 0.3f;

        SpriteRenderer[] renderers;
        float[] baseAlpha;
        Vector3 lockedScale = Vector3.one, groupScale = Vector3.one;   // scale author trong prefab của hai root

        void Awake()
        {
            // Lấy lúc Awake, TRƯỚC khi thẻ được mount vào slot — nên mảng này chỉ gồm phần
            // thân hộp, fade hộp không kéo theo thẻ đang nằm trong nó.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
            if (lockedRoot != null) lockedScale = lockedRoot.transform.localScale;
            if (groupLockRoot != null) groupScale = groupLockRoot.transform.localScale;
        }

        Vector3 BaseScale(GameObject root) { return root == lockedRoot ? lockedScale : groupScale; }

        public Transform Slot(int i) { return slotAnchors[i]; }

        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain). Ba trạng thái
        // nhìn: mở, khoá theo số nhóm (label = số nhóm còn cần), khoá theo nhóm (sprite nhóm).
        // Animation suy từ chuyển trạng thái giữa hai lần gọi: số giảm = nảy, đóng → mở = bung.
        // Lần gọi đầu sau Awake/ResetVisual (hộp vừa dựng / vừa lộ / Undo) thì snap.
        // ResetVisual() cố ý KHÔNG đụng hai root: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        bool lockKnown;
        GameObject shownRoot;   // root đang hiện, null = mở
        string lastLabel;

        public void SetOpen()
        {
            var was = lockKnown ? shownRoot : null;
            lockKnown = true; shownRoot = null;
            if (was != null) Unlock(was);
            else ShowRoots(null);
        }

        public void SetCountLock(string label)
        {
            bool progressed = lockKnown && shownRoot == lockedRoot && label != lastLabel;
            lockKnown = true; shownRoot = lockedRoot; lastLabel = label;
            ShowRoots(lockedRoot);
            // Chỉ đổi chữ — font, size, outline giữ nguyên như author trong prefab.
            if (lockedCountText != null) lockedCountText.text = label ?? "";
            if (progressed && lockedRoot != null && lockPunch > 0f)
                lockedRoot.transform.DOPunchScale(lockedScale * lockPunch, lockPunchDur, 8, 0.6f).SetLink(lockedRoot);
        }

        public void SetGroupLock(Sprite sprite)
        {
            lockKnown = true; shownRoot = groupLockRoot;
            ShowRoots(groupLockRoot);
            if (groupArt == null) return;
            groupArt.sprite = sprite;
            var c = groupArt.color;
            groupArt.color = new Color(1f, 1f, 1f, c.a);   // art nhóm tự mang màu; alpha thuộc SetAlpha
        }

        // Bật đúng một root (hoặc không cái nào), giết tween dở và trả scale về giá trị author.
        void ShowRoots(GameObject keep)
        {
            foreach (var r in new[] { lockedRoot, groupLockRoot })
            {
                if (r == null) continue;
                r.transform.DOKill(true);
                r.transform.localScale = BaseScale(r);
                r.SetActive(r == keep);
            }
        }

        void Unlock(GameObject root)
        {
            var tr = root.transform;
            var s0 = BaseScale(root);
            tr.DOKill(true);
            var seq = DOTween.Sequence().SetLink(root);
            seq.Append(tr.DOScale(s0 * 1.2f, unlockDur * 0.35f).SetEase(Ease.OutQuad));
            seq.Append(tr.DOScale(0f, unlockDur * 0.65f).SetEase(Ease.InBack));
            seq.OnComplete(() => { root.SetActive(false); tr.localScale = s0; });
        }

        public void SetAlpha(float a)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = baseAlpha[i] * a;
                renderers[i].color = c;
            }
        }

        public void ResetVisual()
        {
            transform.localScale = Vector3.one;
            SetAlpha(1f);
            lockKnown = false;   // hộp vừa lộ / vừa khôi phục: lần Set* tiếp theo snap, không diễn
        }
    }
}
