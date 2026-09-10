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

        // Blocker — CHƯA GÁN, xem ghi chú cùng loại trong TileView.
        [Header("Khoá (blocker) — để trống, gắn art sau")]
        [SerializeField] GameObject lockRoot;     // ổ khoá phủ lên hộp
        [SerializeField] TextMesh lockLabelText;  // số nhóm còn cần, hoặc id chìa
        // Hình ổ tô màu khi hộp khoá bằng chìa: sprite trắng, màu lấy từ SO_KeyColors theo id,
        // cùng asset với chìa trên Tile.prefab. Hộp khoá theo số nhóm giữ màu author trong prefab.
        [SerializeField] SpriteRenderer lockIcon;
        [SerializeField] KeyColorPalette keyColors;

        SpriteRenderer[] renderers;
        float[] baseAlpha;
        Color lockBaseColor = Color.white;

        void Awake()
        {
            // Lấy lúc Awake, TRƯỚC khi thẻ được mount vào slot — nên mảng này chỉ gồm phần
            // thân hộp, fade hộp không kéo theo thẻ đang nằm trong nó.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
            if (lockIcon != null) lockBaseColor = lockIcon.color;
        }

        public Transform Slot(int i) { return slotAnchors[i]; }

        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain).
        // label do bên gọi dựng — hộp khoá là số nhóm còn cần, hộp có ổ là id chìa.
        // keyId: id chìa khi hộp có ổ, null khi hộp khoá theo số nhóm.
        // ResetVisual() cố ý KHÔNG đụng lockRoot: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        public void SetLock(bool closed, string label, string keyId)
        {
            if (lockRoot != null) lockRoot.SetActive(closed);
            if (lockLabelText != null)
            {
                lockLabelText.gameObject.SetActive(closed && !string.IsNullOrEmpty(label));
                if (closed && !string.IsNullOrEmpty(label))
                    ViewText.Apply(lockLabelText, label, 1f, 0.9f);
            }
            if (closed && lockIcon != null)
            {
                var c = keyId != null && keyColors != null ? keyColors.Get(keyId) : lockBaseColor;
                c.a = lockIcon.color.a;   // alpha thuộc SetAlpha (hộp mờ dần), ở đây chỉ đổi màu
                lockIcon.color = c;
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
