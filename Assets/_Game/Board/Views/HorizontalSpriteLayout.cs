// Dàn hàng ngang các con (world-space sprite) quanh tâm mình — bản mini của
// HorizontalLayoutGroup cho object ngoài Canvas. Gắn vào TileMarkerHolder: con nào
// active thì được xếp đều với khoảng cách spacing, cụm luôn căn giữa; SetActive
// bật/tắt marker (StackView.ShowDepth) tự đẩy layout ở LateUpdate frame đó.
using UnityEngine;

namespace WordStack.Board
{
    [ExecuteAlways]
    public class HorizontalSpriteLayout : MonoBehaviour
    {
        [SerializeField] float spacing = 0.4f;

        void LateUpdate()
        {
            int n = 0;
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).gameObject.activeSelf) n++;
            if (n == 0) return;

            // ponytail: chạy mỗi frame cho 4 con — rẻ hơn mọi cơ chế dirty-flag.
            float x = -(n - 1) * spacing * 0.5f;
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                c.localPosition = new Vector3(x, 0f, 0f);
                x += spacing;
            }
        }
    }
}
