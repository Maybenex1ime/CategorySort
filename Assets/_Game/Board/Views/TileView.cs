// Một thẻ. Component mỏng: giữ tham chiếu + bind hiển thị. KHÔNG chứa luật, không gọi domain.
//
// Kích thước Bg và Art author THẲNG TRONG PREFAB — Bind() không còn đụng scale của chúng
// (đổi 2026-08-06: chỉnh bằng Inspector mà không bị code ghi đè mỗi lần bind). Root vẫn giữ
// scale 1 để tween scale (hover / nhấc lên / CLEAR) đọc thẳng 0..1 không phải nhân hằng nào.
// `slotSize` giờ chỉ còn lo VỊ TRÍ ảnh/chữ và bề rộng co chữ, không lo kích thước nữa.
using UnityEngine;

namespace WordStack.Board
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bg;
        [SerializeField] SpriteRenderer art;
        [SerializeField] TextMesh label;
        // Vành ngoài: cùng sprite với bg, tô màu viền, phóng to vài % và nằm sau. Bỏ trống cũng
        // chạy. Màu và độ dày author thẳng trong prefab — code không đụng, chỉ lo sorting order
        // để lúc thẻ bay lên (FlyOrder) vành không bị bỏ lại dưới bàn.
        [SerializeField] SpriteRenderer outline;

        public string Uid { get; private set; }

        // Ba trường hợp như demo: chỉ ảnh / chỉ chữ / cả hai.
        public void Bind(Tile t, Sprite sprite, Color bgColor, int order, float slotSize)
        {
            Uid = t != null ? t.Uid : null;

            bg.color = bgColor;

            bool hasArt = t != null && sprite != null;
            bool hasText = t != null && t.Text != null;
            bool both = hasArt && hasText;

            art.gameObject.SetActive(hasArt);
            if (hasArt)
            {
                art.sprite = sprite;
                art.transform.localPosition = new Vector3(0f, both ? slotSize * 0.14f : 0f, 0f);
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
            // order-1 nên lúc thẻ nằm yên vành trùng order với bóng đáy slot. Hoà nhau ở đây vô
            // hại: cả hai đều bị mặt thẻ đục che kín, mà bóng chỉ đen 14%.
            if (outline != null) outline.sortingOrder = order - 1;
        }
    }
}
