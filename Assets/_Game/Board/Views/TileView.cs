// Một thẻ. Component mỏng: giữ tham chiếu + bind hiển thị. KHÔNG chứa luật, không gọi domain.
//
// Thẻ chỉ còn ảnh (bỏ label chữ + vành outline 2026-08-17 — hướng art mới là icon thuần).
// Kích thước và vị trí Bg/Art author THẲNG TRONG PREFAB, code không đụng scale — root giữ
// scale 1 để tween (hover / nhấc lên / CLEAR) đọc thẳng 0..1.
using UnityEngine;

namespace WordStack.Board
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bg;
        [SerializeField] SpriteRenderer art;

        // Nền theo số thẻ CÙNG NHÓM trong hộp. Field nào null = giữ sprite hiện tại.
        // Màu bg luôn trắng (Bind) — sprite tự mang màu, hệ tint palette cũ đã bỏ.
        [Header("Nền theo trạng thái trùng nhóm")]
        [SerializeField] Sprite bgAlone;        // không thẻ nào khác trùng nhóm
        [SerializeField] Sprite bgPairFirst;    // Option 1 — CẶP THỨ NHẤT trong hộp (cả 2 thẻ của cặp)
        [SerializeField] Sprite bgPairSecond;   // Option 2 — cặp thứ hai, khi hộp có 2 cặp
        [SerializeField] Sprite bgTriple;       // có 2 thẻ khác trùng nhóm
        [SerializeField] Sprite bgFull;         // có 3 thẻ khác — đủ bộ, chỉ thoáng qua trước CLEAR
        // Placeholder: nền cho thẻ sinh ra từ COLLAPSE (4 thẻ nhỏ gộp lại).
        // CHƯA có logic gán — chờ chốt luật COLLAPSE, đừng xoá field.
        [SerializeField] Sprite bgCollapsed;

        public string Uid { get; private set; }

        // Card không có art (text-only trong level data) → thẻ chỉ hiện nền trống.
        // Sorting order KHÔNG set từ code — author trong Tile.prefab (bg 10, art 11).
        public void Bind(Tile t, Sprite sprite)
        {
            Uid = t != null ? t.Uid : null;

            bg.color = Color.white;   // sprite trạng thái tự mang màu, không tint gì thêm

            bool hasArt = t != null && sprite != null;
            art.gameObject.SetActive(hasArt);
            if (hasArt)
            {
                art.sprite = sprite;
                art.transform.localPosition = Vector3.zero;
            }
        }

        // groupCountInBox = tổng thẻ cùng nhóm trong hộp (kể cả thẻ này);
        // groupOrdinal = thứ tự NHÓM của thẻ giữa các nhóm ≥2 thẻ trong hộp, theo
        // thứ tự xuất hiện (BoxColorIndices) — hộp có 2 cặp thì cặp đầu 0, cặp sau 1.
        public void SetMatchState(int groupCountInBox, int groupOrdinal)
        {
            Sprite next =
                groupCountInBox <= 1 ? bgAlone
                : groupCountInBox == 2 ? (groupOrdinal == 0 ? bgPairFirst : bgPairSecond)
                : groupCountInBox == 3 ? bgTriple
                : bgFull;
            if (next != null) bg.sprite = next;
        }
    }
}
