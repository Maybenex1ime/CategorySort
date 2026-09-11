// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Đổi số ở đó
// thì phải đổi hằng layout trong BoardController theo, vì hit-test tính từ hằng code.
using UnityEngine;

namespace WordStack.Board
{
    public class BoxView : MonoBehaviour
    {
        [SerializeField] Transform[] slotAnchors = new Transform[4];

        // Blocker của hộp — mỗi loại một root riêng trong Box.prefab, dựng art độc lập.
        // Hộp mở thì cả hai root tắt. Field nào chưa nối thì bỏ qua, bàn vẫn chạy như thường.
        [Header("Hộp khoá theo số nhóm (locked)")]
        [SerializeField] GameObject lockedRoot;
        [SerializeField] TextMesh lockedCountText;   // số nhóm CÒN phải gom

        [Header("Hộp có ổ (keylock)")]
        [SerializeField] GameObject keyLockRoot;
        // Lớp DUY NHẤT đổi màu theo chìa: sprite trắng/xám, màu nhân từ SO_KeyColors theo id — cùng
        // asset với chìa trên Tile.prefab. Xích, khiên, lỗ khoá là lớp riêng giữ màu gốc.
        [SerializeField] SpriteRenderer keyLockTint;
        [SerializeField] KeyColorPalette keyColors;

        SpriteRenderer[] renderers;
        float[] baseAlpha;

        void Awake()
        {
            // Lấy lúc Awake, TRƯỚC khi thẻ được mount vào slot — nên mảng này chỉ gồm phần
            // thân hộp, fade hộp không kéo theo thẻ đang nằm trong nó.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
        }

        public Transform Slot(int i) { return slotAnchors[i]; }

        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain).
        // keyId khác null = hộp có ổ; null = hộp khoá theo số nhóm, label là số nhóm còn cần.
        // ResetVisual() cố ý KHÔNG đụng hai root: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        public void SetLock(bool closed, string label, string keyId)
        {
            bool keyed = closed && keyId != null;
            bool counted = closed && keyId == null;
            if (lockedRoot != null) lockedRoot.SetActive(counted);
            if (keyLockRoot != null) keyLockRoot.SetActive(keyed);
            if (counted && lockedCountText != null) ViewText.Apply(lockedCountText, label ?? "", 1f, 0.9f);
            if (keyed && keyLockTint != null && keyColors != null)
            {
                var c = keyColors.Get(keyId);
                c.a = keyLockTint.color.a;   // alpha thuộc SetAlpha (hộp mờ dần), ở đây chỉ đổi màu
                keyLockTint.color = c;
            }
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
        }
    }
}
