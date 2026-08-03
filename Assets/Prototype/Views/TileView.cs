// Một thẻ. Component mỏng: giữ tham chiếu + bind hiển thị. KHÔNG chứa luật, không gọi domain.
//
// Prefab author ở kích thước 1 đơn vị; Bind() co các phần con về SlotSize. Root giữ scale 1
// để tween scale (hover / nhấc lên / CLEAR) đọc thẳng 0..1 không phải nhân hằng nào.
using UnityEngine;

namespace WordStack.Prototype
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bg;
        [SerializeField] SpriteRenderer art;
        [SerializeField] TextMesh label;

        public string Uid { get; private set; }

        // Ba trường hợp như demo: chỉ ảnh / chỉ chữ / cả hai.
        public void Bind(Tile t, Sprite sprite, Color bgColor, int order, float slotSize)
        {
            Uid = t != null ? t.Uid : null;

            bg.transform.localScale = new Vector3(slotSize, slotSize, 1f);
            bg.color = bgColor;

            bool hasArt = t != null && sprite != null;
            bool hasText = t != null && t.Text != null;
            bool both = hasArt && hasText;

            art.gameObject.SetActive(hasArt);
            if (hasArt)
            {
                art.sprite = sprite;
                art.transform.localPosition = new Vector3(0f, both ? slotSize * 0.14f : 0f, 0f);
                var b = sprite.bounds.size;
                float k = slotSize * (both ? 0.46f : 0.72f) / Mathf.Max(Mathf.Max(b.x, b.y), 0.0001f);
                art.transform.localScale = new Vector3(k, k, 1f);
            }

            label.gameObject.SetActive(hasText);
            if (hasText)
            {
                label.transform.localPosition = new Vector3(0f, both ? -slotSize * 0.26f : 0f, 0f);
                ViewText.Apply(label, t.Text, both ? 0.85f : 1.10f, slotSize * 0.92f);
            }

            SetOrder(order);
        }

        public void SetColor(Color c) { bg.color = c; }

        // Thẻ đang bay được kéo lên trên mọi thứ rồi trả về order thường khi hạ cánh.
        public void SetOrder(int order)
        {
            bg.sortingOrder = order;
            art.sortingOrder = order + 1;
            ViewText.Order(label, order + 1);
        }
    }
}
