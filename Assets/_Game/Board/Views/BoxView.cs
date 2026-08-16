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
