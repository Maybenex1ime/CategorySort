// Một thẻ. Component mỏng: giữ tham chiếu + bind hiển thị. KHÔNG chứa luật, không gọi domain.
//
// Thẻ chỉ còn ảnh (bỏ label chữ + vành outline 2026-08-17 — hướng art mới là icon thuần).
// Kích thước và vị trí Bg/Art author THẲNG TRONG PREFAB, code không đụng scale — root giữ
// scale 1 để tween (hover / nhấc lên / CLEAR) đọc thẳng 0..1.
using DG.Tweening;
using TMPro;
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
        [Header("Băng (blocker)")]
        [SerializeField] GameObject iceRoot;         // lớp phủ băng, bật/tắt cả cụm
        [SerializeField] SpriteRenderer iceSprite;   // lớp băng — đổi sprite theo nấc
        [Tooltip("Các nấc băng theo tiến trình về 0: [0] = đầy băng … [cuối] = sắp tan")]
        [SerializeField] Sprite[] iceStages = new Sprite[0];
        [SerializeField] TextMeshPro iceCountText;   // số nước còn lại (TMP 3D, con của iceRoot)
        [Tooltip("VFX băng tan: bắn mỗi lần đổi nấc và lúc tan hẳn. Đặt NGOÀI iceRoot để không bị tắt/co theo. Trống = không VFX")]
        [SerializeField] ParticleSystem iceMeltVfx;
        [Tooltip("Băng nảy bao nhiêu mỗi lần bớt một nước (0 = tắt)")]
        [SerializeField] float iceCrackPunch = 0.15f;
        [SerializeField] float iceCrackDur = 0.25f;
        [Tooltip("Tan hẳn: phồng lên rồi co về 0 và mờ dần (giây)")]
        [SerializeField] float iceMeltDur = 0.35f;

        // Chỉ để soi trong Inspector lúc Play: blocker đang gắn trên thẻ, ví dụ "ice 3".
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
        // movesLeft / total là số nước CÒN LẠI và tổng, đã tính sẵn bên gọi — view không đọc Lock.
        // Nấc băng = tiến trình về 0 chia đều lên iceStages. Animation suy từ chuyển trạng thái
        // giữa hai lần gọi: đổi nấc = VFX tan + nảy, về 0 = VFX tan + co mờ. Lần gọi đầu (thẻ
        // vừa dựng / vừa lộ) và chiều ngược (Undo đóng băng lại) thì snap.
        bool iceKnown, iceShown;
        int iceLast, iceStage;
        SpriteRenderer[] iceSprites;
        Vector3 iceScale = Vector3.one;   // scale author trong prefab của iceRoot

        public void SetIce(bool frozen, int movesLeft, int total)
        {
            if (iceRoot == null) return;
            int stage = StageOf(movesLeft, total);
            bool stepped = iceKnown && iceShown && frozen && movesLeft < iceLast;
            bool changedStage = stepped && stage != iceStage;
            bool melt = iceKnown && iceShown && !frozen;
            iceKnown = true; iceShown = frozen; iceLast = movesLeft; iceStage = stage;

            if (iceCountText != null) iceCountText.text = movesLeft.ToString();
            if (melt) { PlayMeltVfx(); Melt(); return; }

            iceRoot.transform.DOKill(true);
            iceRoot.transform.localScale = iceScale;
            SetIceAlpha(1f);
            iceRoot.SetActive(frozen);
            if (frozen && iceSprite != null && stage >= 0) iceSprite.sprite = iceStages[stage];
            if (changedStage) PlayMeltVfx();
            if (stepped && iceCrackPunch > 0f)
                iceRoot.transform.DOPunchScale(iceScale * iceCrackPunch, iceCrackDur, 8, 0.6f).SetLink(iceRoot);
        }

        // -1 khi chưa author nấc nào. total ≤ 0 (thẻ không băng) → nấc đầu.
        int StageOf(int movesLeft, int total)
        {
            int n = iceStages != null ? iceStages.Length : 0;
            if (n == 0) return -1;
            if (total <= 0) return 0;
            float done = 1f - Mathf.Clamp01((float)movesLeft / total);
            return Mathf.Clamp(Mathf.FloorToInt(done * n), 0, n - 1);
        }

        void PlayMeltVfx()
        {
            if (iceMeltVfx != null) iceMeltVfx.Play(true);
        }

        void Melt()
        {
            var tr = iceRoot.transform;
            tr.DOKill(true);
            var seq = DOTween.Sequence().SetLink(iceRoot);
            seq.Append(tr.DOScale(iceScale * 1.15f, iceMeltDur * 0.3f).SetEase(Ease.OutQuad));
            seq.Append(tr.DOScale(0f, iceMeltDur * 0.7f).SetEase(Ease.InBack));
            seq.Join(DOTween.To(() => 1f, SetIceAlpha, 0f, iceMeltDur * 0.7f));
            seq.OnComplete(() => { iceRoot.SetActive(false); tr.localScale = iceScale; SetIceAlpha(1f); });
        }

        void SetIceAlpha(float a)
        {
            if (iceSprites == null) iceSprites = iceRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in iceSprites) { var c = sr.color; c.a = a; sr.color = c; }
            if (iceCountText != null) iceCountText.alpha = a;
        }

        // Bên gọi tính sẵn số băng còn lại — view không đọc Lock, giống SetIce.
        public void SetBlockerDebug(int iceLeft)
        {
            blockers = iceLeft > 0 ? "ice " + iceLeft : "";
        }

        // Thẻ đang bay nổi lên trên mọi hộp/thẻ, hạ cánh thì trả về order author trong prefab.
        // Đây là chỗ DUY NHẤT code còn đụng sortingOrder — mọi giá trị tĩnh sống ở prefab.
        const int FlyOrder = 90;
        int bgOrder, artOrder;

        void Awake()
        {
            bgOrder = bg.sortingOrder;
            artOrder = art.sortingOrder;
            if (iceRoot != null) iceScale = iceRoot.transform.localScale;
        }

        public void SetFlying(bool flying)
        {
            bg.sortingOrder = flying ? FlyOrder : bgOrder;
            art.sortingOrder = flying ? FlyOrder + 1 : artOrder;
        }
    }
}
