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

        // Blocker — CHƯA GÁN, người dùng tự làm phần nhìn (nhịp 2). Tất cả nullable:
        // chưa kéo gì vào thì SetIce là no-op và bàn chạy y như không có blocker.
        [Header("Băng (blocker) — để trống, gắn art sau")]
        [SerializeField] GameObject iceRoot;     // lớp phủ băng, bật/tắt cả cụm
        [SerializeField] TextMesh iceCountText;  // số nước còn lại

        // Chìa: MỘT sprite trắng cho mọi id, màu lấy từ SO_KeyColors rồi tô qua
        // SpriteRenderer.color. Màu nhân vào vertex nên không thêm texture hay material, các thẻ
        // vẫn chung batch. Thay art thì sprite mới cũng phải trắng hoặc xám sáng.
        [Header("Chìa (blocker) — sprite trắng, màu tô theo id")]
        [SerializeField] SpriteRenderer keyIcon;
        [SerializeField] KeyColorPalette keyColors;   // SO_KeyColors — dùng chung với Box.prefab

        // Chỉ để soi trong Inspector lúc Play: blocker đang gắn trên thẻ, ví dụ "ice 3 · key k1".
        // Trống là thẻ thường. Code không đọc lại field này nên sửa tay trong Inspector vô tác dụng.
        [Header("Debug — blocker trên thẻ (chỉ đọc)")]
        [SerializeField] string blockers;

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

        // Thẻ băng: bất động và không tính bộ 4 (luật ở Domain). Ở đây chỉ hiện trạng thái.
        // movesLeft là số nước CÒN LẠI, đã tính sẵn bên gọi — view không đọc Lock.
        public void SetIce(bool frozen, int movesLeft)
        {
            if (iceRoot != null) iceRoot.SetActive(frozen);
            if (iceCountText != null)
            {
                iceCountText.gameObject.SetActive(frozen);
                if (frozen) ViewText.Apply(iceCountText, movesLeft.ToString(), 1f, 0.4f);
            }
        }

        // Thẻ mang chìa (keyId khác null): hiện icon, tô màu theo SO_KeyColors.
        public void SetKey(string keyId)
        {
            if (keyIcon == null) return;
            keyIcon.gameObject.SetActive(keyId != null);
            if (keyId != null) keyIcon.color = keyColors != null ? keyColors.Get(keyId) : Color.white;
        }

        // Bên gọi tính sẵn số băng còn lại và id chìa — view không đọc Lock, giống SetIce.
        public void SetBlockerDebug(int iceLeft, string keyId)
        {
            string ice = iceLeft > 0 ? "ice " + iceLeft : null;
            string key = keyId != null ? "key " + keyId : null;
            blockers = ice != null && key != null ? ice + " · " + key : ice ?? key ?? "";
        }

        // Thẻ đang bay nổi lên trên mọi hộp/thẻ, hạ cánh thì trả về order author trong prefab.
        // Đây là chỗ DUY NHẤT code còn đụng sortingOrder — mọi giá trị tĩnh sống ở prefab.
        const int FlyOrder = 90;
        int bgOrder, artOrder, keyOrder;

        void Awake()
        {
            bgOrder = bg.sortingOrder;
            artOrder = art.sortingOrder;
            if (keyIcon != null) keyOrder = keyIcon.sortingOrder;
        }

        public void SetFlying(bool flying)
        {
            bg.sortingOrder = flying ? FlyOrder : bgOrder;
            art.sortingOrder = flying ? FlyOrder + 1 : artOrder;
            // Chìa bay theo thẻ: giữ order prefab thì nền thẻ đang bay (90) che mất nó.
            if (keyIcon != null) keyIcon.sortingOrder = flying ? FlyOrder + 2 : keyOrder;
        }
    }
}
