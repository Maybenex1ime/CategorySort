// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Đổi số ở đó
// thì phải đổi hằng layout trong BoardController theo, vì hit-test tính từ hằng code.
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

        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain). Ba trạng thái
        // nhìn: mở, khoá theo số nhóm (label = số nhóm còn cần), khoá theo nhóm (sprite nhóm).
        // ResetVisual() cố ý KHÔNG đụng hai root: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        public void SetOpen() { ShowRoots(false, false); }

        public void SetCountLock(string label)
        {
            ShowRoots(true, false);
            // Chỉ đổi chữ — font, size, outline giữ nguyên như author trong prefab.
            if (lockedCountText != null) lockedCountText.text = label ?? "";
        }

        public void SetGroupLock(Sprite sprite)
        {
            ShowRoots(false, true);
            if (groupArt == null) return;
            groupArt.sprite = sprite;
            var c = groupArt.color;
            groupArt.color = new Color(1f, 1f, 1f, c.a);   // art nhóm tự mang màu; alpha thuộc SetAlpha
        }

        void ShowRoots(bool counted, bool grouped)
        {
            if (lockedRoot != null) lockedRoot.SetActive(counted);
            if (groupLockRoot != null) groupLockRoot.SetActive(grouped);
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
