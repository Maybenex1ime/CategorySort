// ============================================================================
// View của WordStack — retained-mode, dựng từ prefab. Thiết kế: docs/architecture/view-prefabs.md.
//
// Luật nằm hết ở Domain/ (Game.cs, LevelData.cs); file này chỉ vẽ, bắt input và tạo nhịp cascade.
// Đây là ranh giới MonoBehaviour ↔ domain thuần: mọi thứ dưới Domain/ không biết Unity là gì.
// Instance GameObject sống suốt level (khoá là Tile.Uid): mỗi nước đi là tween MỘT thẻ sang
// slot mới — không rebuild. Đó là điều kiện để animate xuyên thời điểm state đổi.
//
// Cái giá của retained-mode: view có sổ sách riêng, và sổ sách lệch là họ bug khó nhất
// (thẻ ma sau CLEAR, thẻ mới không hiện, màu kẹt giá trị cũ). Không bộ test nào trong repo
// nhìn tới lớp view → CheckInvariant() dưới cùng là thứ bắt lệch, đừng gỡ.
//
// Hành vi tham chiếu: demo/wordstack-clear-demo.html. Lệch chỗ nào là bug chỗ đó.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Animation;
using LitMotion.Extensions;
using WordStack.Contracts;

namespace WordStack.Board
{
    public class BoardController : MonoBehaviour
    {
        // ---- Layout (world units) — hằng bố cục gắn với thuật toán đặt lưới, ở lại code.
        // Kích thước bên trong hộp (viền, slot) author trong Box.prefab. Hit-test ô THẺ (Tile)
        // giờ lấy từ bounds sprite Shadow của từng slot (BoxView.SlotRect qua SlotZone bên dưới),
        // không còn tính từ hằng ở đây — chỉ zone STACK (BoxSize) còn dùng mấy hằng này.
        //
        // Bố cục chuẩn đo ở Stack.prefab scale 1.15: hộp 1.6, tâm–tâm 1.88 ngang / 2.92 dọc. Đổi scale
        // Stack thì bố cục tự theo (đọc scale thật của prefab): khe NGANG giữa hai hộp giữ nguyên 0.12
        // (khay Tray.png rộng 1.531 ở scale 1), khe DỌC giãn theo tỉ lệ vì dưới mỗi hộp là lớp lấp ló +
        // Tile Holder — chúng to theo Stack. Vị trí trong level là ô lưới, không phải sửa.
        // Scale Stack đặt ở field stackScale (Inspector), áp lên mọi Stack lúc dựng bàn — scale gốc
        // của Stack.prefab bị ghi đè lúc chạy.
        const float RefStackScale = 1.15f;
        const float TrayWidth = 1.531f;
        const float BoxPad = 0.09f;
        const float SlotGap = 0.08f;
        float BoxSize { get { return 1.6f * StackScale / RefStackScale; } }
        float SlotSize { get { return (BoxSize - 2f * BoxPad - SlotGap) / 2f; } }
        float PitchX { get { return 1.88f + TrayWidth * (StackScale - RefStackScale); } }
        float PitchY { get { return 2.92f * StackScale / RefStackScale; } }   // chừa chỗ cho lớp lấp ló + tile marker

        // Sorting order KHÔNG set từ code nữa — author hết trong prefab
        // (Tile.prefab bg 10 / art 11, Ghost.prefab, Stack.prefab...).

        // Feel hover (tham chiếu D:\Balatro-Feel CardVisual.cs): phồng + giật một cái.
        const float HoverScale = 1.07f;
        const float HoverPunchAngle = 5f;


        [Header("Prefabs")]
        [SerializeField] StackView stackPrefab;
        [SerializeField] BoxView boxPrefab;
        [SerializeField] TileView tilePrefab;
        [SerializeField] GhostView ghostPrefab;

        [Header("Cỡ hộp trên bàn")]
        [Tooltip("Scale của mỗi Stack (hộp + lớp lấp ló). Khoảng cách giữa các hộp tự giãn theo — xem PitchX/PitchY")]
        [SerializeField, Min(0.1f)] float stackScale = 1.25f;
        float StackScale { get { return stackScale; } }

        [Header("Fit bàn vào khoảng trống giữa HUD")]
        // Ô HUD che bàn — trên: CoinArea, LevelBox, Settings, Progress Bar; dưới: Box BG + Booster Button.
        // Rỗng = lề cố định kiểu cũ.
        [SerializeField] RectTransform[] hudBlockers;
        [SerializeField] float fitPadding = 0.15f;      // world unit, mỗi cạnh
        [SerializeField] float fitExtraBelow = 1.0f;    // chỗ cho lớp lấp ló + Tile Holder dưới hộp hàng cuối — núm chỉnh tay

        [Header("Booster")]
        // SO_BoosterAnim — thông số animation Nam châm + Xáo + Undo. Chưa gán thì dùng giá trị
        // mặc định khai trong class, bàn không sập.
        [SerializeField] BoosterAnimSettings animSettings;
        // Tấm nền xám bật suốt lúc booster diễn: chặn click uGUI và làm nền cho thẻ bay.
        // User tự dựng Panel; code chỉ bật/tắt (mờ vào/ra bằng hai LitMotionAnimation bên dưới). Muốn thẻ
        // bay NỔI TRÊN tấm nền thì Panel phải nằm dưới sorting 90 của thẻ bay — tức Canvas
        // riêng Screen Space-Camera (order 20..89) hoặc SpriteRenderer world-space; Canvas
        // Screen Space-Overlay luôn vẽ đè lên mọi sprite.
        [SerializeField] GameObject boosterBackdrop;
        // CanvasGroup alpha 0→1 / 1→0 trên boosterBackdrop — dựng bằng Tools ▸ WordStack ▸ Build Booster
        // Backdrop Animation, chỉnh và xem trước ngay trên component. Để trống thì nền bật/tắt khan.
        [SerializeField] LitMotionAnimation backdropFadeIn;
        [SerializeField] LitMotionAnimation backdropFadeOut;

        // DEBUG: tự gắn bộ blocker mẫu (y như phím B) lên MỌI màn ngay lúc nạp. Level JSON
        // chưa author blocker nên đây là cách duy nhất thấy chúng trong luồng chơi thật.
        // Tắt đi khi có content thật — bộ mẫu không qua solver, có thể làm màn không giải được.
        [SerializeField] bool autoDebugBlockers = true;
        BoosterAnimSettings animFallback;
        BoosterAnimSettings A
        {
            get
            {
                if (animSettings != null) return animSettings;
                if (animFallback == null) animFallback = ScriptableObject.CreateInstance<BoosterAnimSettings>();
                return animFallback;
            }
        }

        // Tint palette (GDD §9.1 cũ) đã bỏ 2026-08-17: sprite trạng thái trùng nhóm
        // (TileView.SetMatchState) thay vai trò gợi ý — bg luôn trắng. Ordinal cặp
        // (Option 1/2) là state sticky của view (pairOrdinals), KHÔNG dùng
        // BoxColorIndices — domain giữ nó cùng test phòng khi quay lại tint.

        [Header("Nhịp")]
        [SerializeField] float flyDur = 0.16f;
        [SerializeField] float clearDur = 0.26f;
        [SerializeField] float clearStagger = 0.04f;
        [Tooltip("Nhóm vừa mở được group lock: 4 thẻ gộp thành thẻ nhóm giữa hộp (nhịp như COLLAPSE), rồi thẻ đó bay vào icon trên lồng")]
        [SerializeField] float lockFlyDur = 0.3f;
        [SerializeField] float lockFlyGatherScale = 0.35f;   // thẻ nhóm co còn bao nhiêu lúc chạm icon
        [SerializeField] float cascadeGap = 0.35f;     // nhịp giữa hai bước cascade (§R6)

        [Header("Gộp 4 thẻ thành 1 (COLLAPSE)")]
        [SerializeField] float mergeGather = 0.24f;    // 4 thẻ bay chụm về ô đích mất bao lâu
        [SerializeField] float mergeShrink = 0.45f;    // tới nơi thì co còn bao nhiêu (1 = không co)
        [SerializeField] float mergeStagger = 0.03f;   // lệch nhau chút cho khỏi dính thành một khối
        [SerializeField] float mergeHold = 0.07f;      // khựng lại trước khi thẻ mới nở — nhịp "cộp"
        [SerializeField] float mergeBloom = 0.30f;     // thẻ mới nở ra mất bao lâu
        [SerializeField] float mergeSpin = 140f;       // thẻ mới xoay bao nhiêu độ lúc nở (0 = tắt)

        [Header("Thẻ nhóm to giữa hộp (feedback #5, #6)")]
        [SerializeField] float groupCardScale = 1.8f;  // 4 thẻ gộp thành thẻ to cỡ bấy nhiêu lần thẻ thường
        [SerializeField] float groupCardHold = 0.45f;  // đứng yên cho kịp đọc tên nhóm
        [SerializeField] float groupCardOut = 0.22f;   // CLEAR: thẻ to co về 0
        [SerializeField] float groupCardToSlot = 0.25f; // COLLAPSE: thẻ to thu về ô của thẻ nhóm

        [Header("Lộ hộp mới (Tile Holder)")]
        [SerializeField] float revealHolderDrop = 0.4f;    // Tile Holder tách ra trượt xuống bao nhiêu unit (world) trong lúc mờ
        [SerializeField] float revealHolderDur = 0.3f;
        [SerializeField] float revealTileDur = 0.32f;      // mỗi thẻ to lên + nhảy vào ô mất bao lâu
        [SerializeField] float revealTileStagger = 0.06f;  // thẻ sau nhảy trễ thẻ trước bấy nhiêu — "lần lượt"
        [SerializeField] float revealJump = 0.35f;         // đỉnh vòng cung khi nhảy (unit world, 0 = bay thẳng)
        [SerializeField] float revealHolderFadeIn = 0.15f; // holder của hộp kế tiếp hiện lại sau khi holder cũ đi hết


        Game g;
        // Nội dung màn hiện tại — do BoardInitializer (DI, Meta) đưa qua LevelCommands
        // (Addressables, address = LevelId trong catalog). Board không còn danh
        // sách level riêng; cache lại để phím R nạp lại được.
        int levelIndex;
        string levelJson;
        bool resultReported;               // mỗi màn chỉ báo kết quả cho tầng meta một lần
        bool firstInteractionRaised;       // Ready → Playing chỉ bắn một lần mỗi màn

        Camera cam;
        Transform root;
        bool fitDirty;                     // HUD có thể layout xong muộn một frame sau khi dựng màn
        Vector2Int fittedScreen;
        Rect fittedSafeArea;
        readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

        StackView[] stackViews;
        BoxView[] boxViews;
        readonly Dictionary<string, TileView> tiles = new Dictionary<string, TileView>();

        enum ZoneKind { Tile, Stack }
        struct Zone { public Rect Rect; public ZoneKind Kind; public int Stack; public string Uid; public bool Fixed; public bool Busy; }
        readonly List<Zone> zones = new List<Zone>();

        GhostView ghost;
        int dragFrom = -1;
        string dragUid;
        TileView hoverTile;
        bool locked;
        // Cascade cho đi tiếp ở hộp khác (spec 2026-10-07-input-during-cascade): locked chỉ còn cho
        // booster/Undo/Revive; Settle() thường chỉ khoá các hộp đang diễn + hộp chờ lượt.
        bool settling;                     // Settle() đang chạy — booster từ chối
        int landing;                       // thẻ đang bay về slot — cascade đợi hạ cánh hết mới SettleStep tiếp
        readonly HashSet<int> busyStacks = new HashSet<int>();   // stack đang diễn bước cascade hiện tại

        MotionHandle hoverPunch;           // cú giật hover đang chạy — TryComplete trước khi giật cú mới
        readonly Dictionary<int, MotionHandle> shakes = new Dictionary<int, MotionHandle>();   // stack → cú rung đang chạy

        // MỌI LSequence mà controller này khởi động (Magnet, Shuffle, Undo, RemoveTiles,
        // MergeTiles, LiftAwayBox, SpawnCollapsedTile...) — DestroyBoard() phải huỷ hết trong
        // này TRƯỚC khi xoá GameObject bên dưới, vì target còn sống lúc Cancel mới an toàn.
        readonly List<MotionHandle> running = new List<MotionHandle>();

        // ---- LitMotion (thay tween cũ 2026-09-17) — đọc Global Constraints của plan chuyển đổi.
        // Sequence: lỗi (target bị huỷ) thì huỷ cả chuỗi thay vì ghi lỗi mỗi frame.
        static readonly Action<MotionBuilder<double, NoOptions, DoubleMotionAdapter>> SeqCfg = b => b.WithCancelOnError();

        // Ease.InBack bản cũ kèm overshoot tuỳ biến — LitMotion.Ease không nhận overshoot.
        static float InBack(float t, float s) { return t * t * ((s + 1f) * t - s); }

        // Huỷ thẻ SAU khi chuỗi chạy xong, không huỷ trong callback: sequence vẫn ghi giá trị cho
        // motion con đã xong mỗi frame, target mất giữa chừng là cả chuỗi lỗi và thẻ khác khựng
        // giữa đường bay. Thẻ đã về scale 0 nên nằm thêm vài nhịp cũng không ai thấy.
        void DestroyAll(List<GameObject> gos)
        {
            foreach (var go in gos) if (go != null) Destroy(go);
        }

        // ---------------------------------------------------------------- boot

        void Awake()
        {
            ViewText.Font = OsFont("Segoe UI", "Arial");

            cam = Camera.main;
            if (cam == null) { Debug.LogError("Scene thiếu Main Camera."); enabled = false; return; }
            if (!RefsOk()) { enabled = false; return; }

            root = new GameObject("Board").transform;
            root.SetParent(transform, false);

            // KHÔNG tự nạp level nữa — Resources.LoadAll đã bỏ, AppFlow sở hữu vòng
            // đời màn chơi: catalog → AddressKey → BoardInitializer nạp JSON qua
            // Addressables rồi đưa xuống đây. Không có AppFlow trong scene thì bàn
            // đứng trống, đó là chủ ý. (Self-check toàn bộ level giờ chỉ còn chạy
            // ngoài Unity qua ./selfcheck.sh — nó đọc thẳng thư mục level trên đĩa.)
            LevelCommands.LoadRequested += OnLoadRequested;
            LevelCommands.MagnetRequested += OnMagnetRequested;
            LevelCommands.ShuffleRequested += OnShuffleRequested;
            LevelCommands.UndoRequested += OnUndoRequested;
            LevelCommands.ReviveRequested += OnReviveRequested;
            LevelCommands.MatchColorsChanged += OnMatchColorsChanged;
        }

        void OnDestroy()
        {
            LevelCommands.LoadRequested -= OnLoadRequested;
            LevelCommands.MagnetRequested -= OnMagnetRequested;
            LevelCommands.ShuffleRequested -= OnShuffleRequested;
            LevelCommands.UndoRequested -= OnUndoRequested;
            LevelCommands.ReviveRequested -= OnReviveRequested;
            LevelCommands.MatchColorsChanged -= OnMatchColorsChanged;
        }

        // Cheat bật/tắt tô màu thẻ cùng nhóm: vẽ lại nền mọi hộp, không nạp lại màn.
        void OnMatchColorsChanged(bool on)
        {
            if (g == null || boxViews == null) return;
            for (int s = 0; s < g.Stacks.Count; s++) RefreshTileVisuals(s);
        }

        void OnLoadRequested(int index, string json)
        {
            levelIndex = index;
            levelJson = json;
            Load();
        }

        // Booster Nam châm. Bàn nhận lệnh qua LevelCommands chứ không nghe Bus.Global —
        // assembly này cố ý không thấy EventBus (xem ghi chú trong LevelCommands).
        void OnMagnetRequested()
        {
            // Đang chạy cascade, popup meta đang mở, hoặc màn đã xong → bỏ qua. Không
            // hoàn lượt ở đây: nút chỉ sáng khi LevelSignals.MagnetAvailable bật, mà cờ
            // đó tắt trong đúng mấy trường hợp này.
            if (!BoosterGateOpen("Magnet")) return;
            RunMagnet();
        }

        // Hồi sinh (thua vì kẹt) = một phát nam châm miễn phí — LUÔN chạy, kể cả khi bàn
        // không kẹt thật (cheat "Lose kẹt → Revive" ép thua trên bàn còn Playing). Không qua
        // BoosterGateOpen: gate đó từ chối khi Status != Playing và khi popup meta đang mở —
        // đúng hai điều thường đúng lúc hồi sinh. Hút xong Settle() chấm lại trạng thái: còn
        // kẹt thì resultReported đã mở nên báo thua lần nữa (AppFlow lại hỏi hồi sinh), thoát
        // kẹt thì chơi tiếp, hút sạch bàn thì thắng.
        void OnReviveRequested()
        {
            LevelSignals.SetReviveAvailable(false);
            if (g == null || locked || settling)
            {
                Debug.Log("[Revive] bàn chưa nạp hoặc đang chạy cascade — bỏ qua.");
                return;
            }

            resultReported = false;   // bàn kẹt thật đã báo thua; mở lại để còn báo kết quả mới
            if (!RunMagnet()) Debug.Log("[Revive] không còn nhóm nào để hút — không hồi sinh được.");
        }

        bool RunMagnet()
        {
            // Chốt nhóm TRƯỚC rồi giữ lại Tile của nó: ApplyMagnet xoá thẻ khỏi Slots, mà
            // thẻ đang chôn không có view — animation phải dựng thẻ tạm từ đúng mặt này.
            string gid = g.FindMagnetTarget();
            var faces = new Dictionary<string, Tile>();
            if (gid != null)
                foreach (var st in g.Stacks) foreach (var bx in st.Boxes) foreach (var t in bx.Slots)
                    if (t != null && t.GroupId == gid) faces[t.Uid] = t;
            MagnetResult r = gid == null ? new MagnetResult { Ok = false } : g.ApplyMagnet(gid);
            if (!r.Ok)
            {
                Debug.Log("[Magnet] không có nhóm nào đủ 4 thẻ trên bàn để hút — bỏ qua.");
                return false;
            }
            Debug.Log("[Magnet] hút nhóm '" + r.GroupId + "' · " + r.Picks.Length + " thẻ"
                      + (r.NewTileUid != null ? " · sinh thẻ cha ở stack " + r.NewTileStack : ""));

            // Bàn vừa đổi vì booster → mất quyền undo. Ảnh chụp là TOÀN bàn, giữ lại thì
            // undo sau đó khôi phục về trước nước đi cũ và nuốt luôn kết quả người chơi
            // vừa mua bằng coin.
            g.ClearUndo();
            StartCoroutine(MagnetSequence(r, faces));
            return true;
        }

        // Cùng bộ chốt với nam châm. Không hoàn lượt ở đây — nút chỉ sáng khi
        // LevelSignals.ShuffleAvailable bật, mà cờ đó tắt trong đúng mấy trường hợp này.
        void OnShuffleRequested()
        {
            if (!BoosterGateOpen("Shuffle")) return;

            int topBefore = g.TopLayerTileCount();
            ShuffleResult r = g.ApplyShuffle();
            if (!r.Ok)
            {
                Debug.Log("[Shuffle] không xếp nổi — hết ô trống, vi phạm bất biến hoặc không đổi được gì, bàn giữ nguyên.");
                return;
            }
            Debug.Log("[Shuffle] " + r.PrimedGroups + " nhóm mồi · " + r.Moves.Length
                      + " thẻ đổi chỗ · tổng lớp trên " + topBefore + " → " + g.TopLayerTileCount());

            g.ClearUndo();   // xem ghi chú ở OnMagnetRequested
            StartCoroutine(ShuffleSequence(r));
        }

        // Booster Undo — trả bàn về trạng thái trước nước kéo thẻ gần nhất. Nước đã gây
        // CLEAR/COLLAPSE thì không lùi được (SettleStep vứt ảnh chụp), nên tới đây là ảnh
        // chụp luôn là một bàn không có cascade phía sau. Cùng bộ chốt với hai booster kia.
        void OnUndoRequested()
        {
            if (!BoosterGateOpen("Undo")) return;

            Game prev = g;
            Game restored = g.ApplyUndo();
            if (restored == null)
            {
                Debug.Log("[Undo] chưa có nước nào để lùi — bàn giữ nguyên.");
                return;
            }
            Debug.Log("[Undo] lùi về trước nước đi · nước " + g.Moves + " → " + restored.Moves
                      + " · nhóm đã gom " + g.Cleared + " → " + restored.Cleared);

            g = restored;
            StartCoroutine(UndoSequence(prev));
        }

        // Khoá HAI vế suốt lúc diễn, thiếu vế nào cũng lọt input:
        //   locked        → chặn kéo thẻ (board đọc raw Pointer, uGUI không chặn hộ)
        //   RaiseMoveCommitted → đẩy phase khỏi Playing → IsInputBlocked bật →
        //                   GameplayBlockInputOverlayView phủ kín, chặn nốt nút HUD và
        //                   các nút booster khác.
        // Mượn MoveCommitted chứ không thêm tín hiệu mới: nó chỉ mang movesUsed, mà
        // nam châm KHÔNG tăng Moves nên truyền g.Moves vào là số y nguyên, HUD không
        // trôi. Phase quay về Playing ở cuối Settle (EvaluationCompleted +
        // AnimationCompleted) — đó cũng là chỗ overlay hạ xuống.
        IEnumerator MagnetSequence(MagnetResult r, Dictionary<string, Tile> faces)
        {
            locked = true;
            // Tắt CẢ BA: Settle() chỉ chạy sau animation, nên cờ nào còn bật là còn nói dối
            // suốt chừng ấy giây — bấm trúng thì BoosterManager trừ lượt trước khi bàn kịp
            // từ chối, mà lượt đó mua bằng coin.
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);
            LevelSignals.SetUndoAvailable(false);

            // Bấm nam châm ngay khi vào màn (chưa chạm bàn lần nào) thì phase còn Ready,
            // mà NotifyPlayerActionCommittedAsync đòi phase == Playing — không đẩy Ready
            // sang Playing trước là MoveCommitted lẫn EvaluationCompleted đều bị VM nuốt:
            // overlay không lên và progress bar đứng im dù nhóm đã bị gom.
            if (!firstInteractionRaised)
            {
                firstInteractionRaised = true;
                LevelSignals.RaiseFirstInteraction();
            }

            LevelSignals.RaiseMoveCommitted(g.Moves);

            var picked = new List<string>();
            foreach (var p in r.Picks) picked.Add(p.Uid);
            yield return BreakFixed(picked);   // thẻ đóng đinh bị hút: tháo đinh trước khi phồng → bay
            yield return Backdrop(true);
            yield return MagnetAnimation(r, faces);
            yield return Backdrop(false);

            RebuildBoardViews();
            yield return Settle();   // dọn hộp rỗng, chạy cascade, chốt thắng/kẹt
        }

        // Nam châm: 4 thẻ phồng một nhịp → bay về điểm hội tụ (viewport, chỉnh trong
        // SO_BoosterAnim) → khựng → nổ về 0. Thẻ đang chôn không có view (StackView chỉ vẽ
        // lớp lấp ló) nên dựng thẻ tạm ngay giữa hộp che, nở ra rồi bay như ba thẻ kia.
        // Domain đã xoá 4 thẻ trước khi vào đây; Rebuild sau animation dọn phần còn lại.
        // Nhóm có cha (COLLAPSE) thì thẻ cha nở ra tại điểm gộp rồi bay về ô của nó.
        IEnumerator MagnetAnimation(MagnetResult r, Dictionary<string, Tile> faces)
        {
            var a = A;
            Vector3 center = cam.ViewportToWorldPoint(
                new Vector3(a.magnetGatherViewport.x, a.magnetGatherViewport.y, -cam.transform.position.z));
            center.z = 0f;

            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            float lastBurst = 0f;   // lúc thẻ CUỐI bắt đầu nổ — thẻ cha nở đúng nhịp đó
            for (int i = 0; i < r.Picks.Length; i++)
            {
                var p = r.Picks[i];
                TileView tv = null;
                bool temp = p.Box > 0 || !tiles.TryGetValue(p.Uid, out tv) || tv == null;
                if (temp)
                {
                    Tile face;
                    if (!faces.TryGetValue(p.Uid, out face) || p.Stack < 0 || p.Stack >= boxViews.Length) continue;
                    tv = Instantiate(tilePrefab, root, false);
                    tv.transform.position = boxViews[p.Stack].transform.position;
                    tv.transform.localScale = Vector3.zero;
                    tv.Bind(face, ArtOf(face));
                }
                else
                {
                    tiles.Remove(p.Uid);
                    tv.transform.SetParent(root, true);
                }
                tv.SetFlying(true);
                var tr = tv.transform;
                float at = i * a.magnetStagger;

                // LitMotion chốt giá trị đầu lúc TẠO motion, nên mỗi nhịp khai "đi từ đâu" = đích của
                // nhịp trước trên cùng thuộc tính.
                Vector3 scale = tr.localScale;
                if (temp)
                {
                    seq.Insert(at, LMotion.Create(Vector3.zero, Vector3.one, a.magnetRevealDur)
                                          .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
                    at += a.magnetRevealDur;
                    scale = Vector3.one;
                }
                var pop = Vector3.one * a.magnetPopScale;
                seq.Insert(at, LMotion.Create(scale, pop, a.magnetPopDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
                at += a.magnetPopDur;
                var gather = Vector3.one * a.magnetGatherScale;
                seq.Insert(at, LMotion.Create(tr.position, center, a.magnetFlyDur)
                                      .WithEase(a.magnetFlyEase).WithCancelOnError().BindToPosition(tr));
                seq.Insert(at, LMotion.Create(pop, gather, a.magnetFlyDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
                if (Mathf.Abs(a.magnetSpin) > 0.01f)
                    // RotateMode.FastBeyond360: từ góc hiện tại (đã chuẩn hoá [0,360)) tới đúng magnetSpin.
                    seq.Insert(at, LMotion.Create(tr.eulerAngles.z, a.magnetSpin, a.magnetFlyDur)
                                          .WithEase(a.magnetFlyEase).WithCancelOnError().BindToEulerAnglesZ(tr));
                at += a.magnetFlyDur + a.magnetHold;
                if (at > lastBurst) lastBurst = at;
                seq.Insert(at, LMotion.Create(gather, Vector3.zero, a.magnetBurstDur)
                                      .WithEase(a.magnetBurstEase).WithCancelOnError().BindToLocalScale(tr));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); yield break; }
            AppendParentFlight(seq, r, center, lastBurst);
            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(this);
            running.Add(h);
            yield return h.ToYieldInstruction();
            DestroyAll(doomed);
        }

        // COLLAPSE qua nam châm: thẻ cha nở ra TẠI ĐIỂM GỘP đúng lúc thẻ cuối nổ, khựng một nhịp,
        // rồi bay về ô mà domain đã đặt nó trong hộp r.NewTileStack, co về cỡ thường. Là thẻ
        // tạm dưới root — nó nằm yên ở ô đó tới khi Rebuild (DestroyBoard) huỷ và dựng thẻ thật
        // đúng chỗ, nên không có khung hình nào ô bị trống.
        void AppendParentFlight(MotionSequenceBuilder seq, MagnetResult r, Vector3 center, float at)
        {
            if (r.NewTileUid == null || r.NewTileStack < 0 || r.NewTileStack >= boxViews.Length) return;
            var box = g.TopBox(r.NewTileStack);
            int slot = box == null ? -1 : Array.FindIndex(box.Slots, t => t != null && t.Uid == r.NewTileUid);
            if (slot < 0) return;

            var a = A;
            var t = box.Slots[slot];
            var tv = Instantiate(tilePrefab, root, false);
            tv.transform.position = center;
            tv.transform.localScale = Vector3.zero;
            tv.Bind(t, ArtOf(t));
            var cc = GroupCountsIn(box);
            tv.SetMatchState(cc[t.GroupId], OrdinalOf(PairOrdinalsFor(r.NewTileStack, box, cc), t.GroupId));
            tv.SetFlying(true);

            var tr = tv.transform;
            Vector3 dest = boxViews[r.NewTileStack].Slot(slot).position;
            var gather = Vector3.one * a.magnetGatherScale;

            seq.Insert(at, LMotion.Create(Vector3.zero, gather, a.magnetParentBloomDur)
                                  .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
            if (Mathf.Abs(mergeSpin) > 0.01f)
            {
                tr.localEulerAngles = new Vector3(0f, 0f, mergeSpin);
                seq.Insert(at, LMotion.Create(tr.localRotation, Quaternion.identity, a.magnetParentBloomDur)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToLocalRotation(tr));
            }
            at += a.magnetParentBloomDur + a.magnetParentHold;
            seq.Insert(at, LMotion.Create(center, dest, a.magnetParentFlyDur)
                                  .WithEase(a.magnetParentFlyEase).WithCancelOnError().BindToPosition(tr));
            seq.Insert(at, LMotion.Create(gather, Vector3.one, a.magnetParentFlyDur)
                                  .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
        }

        // Khoá HAI vế suốt lúc diễn, y như nam châm — thiếu vế nào cũng lọt input:
        //   locked             → chặn kéo thẻ (board đọc raw Pointer, uGUI không chặn hộ)
        //   RaiseMoveCommitted → đẩy phase khỏi Playing → IsInputBlocked bật →
        //                        GameplayBlockInputOverlayView phủ kín, chặn nốt nút HUD.
        // Mượn MoveCommitted chứ không thêm tín hiệu mới: nó chỉ mang movesUsed, mà
        // Shuffle KHÔNG tăng Moves nên truyền g.Moves vào là số y nguyên, HUD không trôi.
        IEnumerator ShuffleSequence(ShuffleResult r)
        {
            locked = true;
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);
            LevelSignals.SetUndoAvailable(false);   // xem ghi chú ở MagnetSequence

            // Bấm ngay khi vào màn thì phase còn Ready, mà NotifyPlayerActionCommittedAsync
            // đòi phase == Playing — không đẩy Ready sang Playing trước là MoveCommitted lẫn
            // EvaluationCompleted đều bị ViewModel nuốt: overlay không lên và progress bar
            // đứng im dù bàn đã đổi.
            if (!firstInteractionRaised)
            {
                firstInteractionRaised = true;
                LevelSignals.RaiseFirstInteraction();
            }

            LevelSignals.RaiseMoveCommitted(g.Moves);

            yield return Backdrop(true);
            yield return ShuffleAnimation(r);   // tự RebuildBoardViews sau khi thẻ bay xong
            yield return Backdrop(false);

            yield return Settle();   // dọn hộp rỗng, chạy cascade, chốt thắng/kẹt
        }

        // Xáo (feedback #11 — bỏ xoáy hai pha cho đơn giản): thẻ nào đổi chỗ thì bay vòng cung thẳng từ ô
        // cũ tới ô mới, lần lượt; thẻ đứng yên (khoá / băng / đinh / không bị xáo) không nhúc nhích.
        // Move dính hộp chôn không có view thật ở đầu kia: thẻ chui xuống hộp dưới thì co mất tại chỗ,
        // thẻ từ hộp dưới lên thì pop ra ở ô mới sau khi rebuild.
        IEnumerator ShuffleAnimation(ShuffleResult r)
        {
            if (r.Moves.Length == 0) { RebuildBoardViews(); yield break; }
            var a = A;
            var seq = LSequence.Create();
            var popIn = new List<string>();
            int n = 0;
            foreach (var m in r.Moves)
            {
                bool fromTop = m.From.Box == 0, toTop = m.To.Box == 0;
                if (!fromTop) { if (toTop) popIn.Add(m.Uid); continue; }
                TileView tv;
                if (!tiles.TryGetValue(m.Uid, out tv) || tv == null) continue;
                tv.SetFlying(true);
                var tr = tv.transform;
                float at = n++ * a.shuffleStagger;
                if (toTop)
                {
                    Vector3 from = tr.position, to = boxViews[m.To.Stack].Slot(m.To.Slot).position;
                    seq.Insert(at, LMotion.Create(0f, 1f, a.shuffleFlyDur).WithEase(a.shuffleFlyEase).WithCancelOnError()
                                          .Bind(k => tr.position = Vector3.LerpUnclamped(from, to, k) + Vector3.up * (a.shuffleArc * Mathf.Sin(k * Mathf.PI))));
                }
                else
                    seq.Insert(at, LMotion.Create(tr.localScale, Vector3.zero, a.shuffleFlyDur * 0.6f)
                                          .WithEase(Ease.InBack).WithCancelOnError().BindToLocalScale(tr));
            }
            if (n > 0) yield return RunAndWait(seq, gameObject);
            else seq.Dispose();

            RebuildBoardViews();   // view khớp g tuyệt đối: thẻ vừa bay đã nằm đúng ô mới

            if (popIn.Count == 0) yield break;
            var pop = LSequence.Create();
            int p = 0;
            foreach (var uid in popIn)
            {
                TileView tv;
                if (!tiles.TryGetValue(uid, out tv) || tv == null) continue;
                var tr = tv.transform;
                var size = tr.localScale;
                tr.localScale = Vector3.zero;
                pop.Insert(p++ * a.shuffleStagger, LMotion.Create(Vector3.zero, size, a.shufflePopDur)
                                                       .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
            }
            if (p > 0) yield return RunAndWait(pop, gameObject);
            else pop.Dispose();
        }

        // Khoá HAI vế y như hai booster kia. Mượn MoveCommitted được vì g đã là bàn khôi
        // phục: g.Moves lúc này chính là số nước SAU khi lùi, đúng thứ HUD phải hiện.
        IEnumerator UndoSequence(Game prev)
        {
            locked = true;
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);
            LevelSignals.SetUndoAvailable(false);   // ảnh chụp dùng xong là hết

            // Chỉ với tới được qua DebugMove (kéo tay đã bật cờ này lúc bấm chuột), nhưng
            // thiếu thì MoveCommitted lẫn EvaluationCompleted đều bị ViewModel nuốt.
            if (!firstInteractionRaised)
            {
                firstInteractionRaised = true;
                LevelSignals.RaiseFirstInteraction();
            }

            LevelSignals.RaiseMoveCommitted(g.Moves);

            yield return Backdrop(true);
            yield return UndoAnimation(prev);
            yield return Backdrop(false);

            RebuildBoardViews();
            // Trạng thái khôi phục vốn đã đứng yên nên SettleStep trả None ngay — nhưng
            // đây là đường DUY NHẤT bàn đẩy lại cờ booster, hạ overlay và bắn
            // EvaluationCompleted. Bỏ nó thì phase kẹt ngoài Playing.
            yield return Settle();
        }

        // Undo chỉ lùi nước KHÔNG nổ nhóm (SettleStep vứt ảnh chụp khi CLEAR), nên diff giữa
        // bàn cũ (prev, view đang hiện) và bàn khôi phục (g) chỉ có hai thứ: đúng một thẻ
        // đổi ô, và có thể một hộp từng lùi ra nay đứng lại. Diễn theo thứ tự đó: hộp cũ
        // trượt từ trên xuống + hiện dần đè lên hộp vừa lộ (cùng lúc Tile Holder trồi lên lại
        // chỗ cũ — tua ngược RevealBox, thẻ không bay về holder), thẻ trong nó nở ra, rồi thẻ
        // vừa kéo bay ngược về ô cũ. Xong Rebuild để view khớp g tuyệt đối.
        IEnumerator UndoAnimation(Game prev)
        {
            var a = A;
            var before = TopPositions(prev);
            var after = TopPositions(g);

            string movedUid = null;
            SlotRef movedTo = default(SlotRef);
            foreach (var kv in after)
            {
                SlotRef was;
                if (!before.TryGetValue(kv.Key, out was) || was.Stack != kv.Value.Stack || was.Slot != kv.Value.Slot)
                {
                    movedUid = kv.Key; movedTo = kv.Value;
                    if (before.ContainsKey(kv.Key)) break;   // thẻ đổi ô là ứng viên chắc hơn thẻ mới lộ
                }
            }

            // Hộp cũ đứng lại: stack nào bàn khôi phục sâu hơn bàn đang hiện.
            for (int s = 0; s < g.Stacks.Count && s < prev.Stacks.Count; s++)
            {
                if (g.Stacks[s].Boxes.Count <= prev.Stacks[s].Boxes.Count) continue;

                // Thẻ của hộp vừa lộ biến mất ngay — hộp cũ sắp đè lên, không ai thấy chúng.
                foreach (var t in prev.Stacks[s].Boxes[0].Slots)
                {
                    TileView old;
                    if (t != null && tiles.TryGetValue(t.Uid, out old) && old != null) { tiles.Remove(t.Uid); Destroy(old.gameObject); }
                }

                var bv = boxViews[s];
                bv.ResetVisual();
                bv.SetOpen();
                stackViews[s].ShowDepth(g.Stacks[s].Boxes.Count - 1, TilesInSecondBox(g.Stacks[s]));
                bv.SetAlpha(0f);
                bv.transform.localPosition = new Vector3(a.undoBoxSlideFrom.x, a.undoBoxSlideFrom.y, 0f);
                var slide = LSequence.Create();
                slide.Insert(0f, LMotion.Create(bv.transform.localPosition, Vector3.zero, a.undoBoxSlideDur)
                                        .WithEase(a.undoBoxSlideEase).WithCancelOnError().BindToLocalPosition(bv.transform));
                // Không SetEase ở bản cũ → OutQuad (ease mặc định trong config cũ).
                slide.Insert(0f, LMotion.Create(0f, 1f, a.undoBoxSlideDur)
                                        .WithEase(Ease.OutQuad).WithCancelOnError().Bind(bv, (v, b) => b.SetAlpha(v)));
                // Tile Holder (đã mang tile nhỏ của hộp vừa bị đè) trồi lên lại — tua ngược cú tụt lúc lộ hộp.
                var holder = stackViews[s].NextHolder;
                if (holder != null && holder.gameObject.activeInHierarchy)
                    slide.Insert(0f, HolderDrop(holder, 1f, 0f, Ease.InCubic));
                running.RemoveAll(mh => !mh.IsActive());
                var slideH = slide.Run(SeqCfg).AddTo(bv.gameObject);
                running.Add(slideH);
                yield return slideH.ToYieldInstruction();

                // Thẻ của hộp cũ nở ra — trừ thẻ sắp bay về, nó đang đứng ở chỗ khác.
                var box = g.TopBox(s);
                var pop = LSequence.Create();
                int popped = 0;
                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var t = box.Slots[i];
                    if (t == null || t.Uid == movedUid) continue;
                    var tv = Instantiate(tilePrefab, bv.Slot(i), false);
                    tv.transform.localPosition = Vector3.zero;
                    tv.transform.localScale = Vector3.zero;
                    tv.Bind(t, ArtOf(t));
                    tiles[t.Uid] = tv;
                    pop.Insert(0f, LMotion.Create(Vector3.zero, Vector3.one, a.undoBoxTilePopDur)
                                          .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tv.transform));
                    popped++;
                }
                if (popped > 0)
                {
                    running.RemoveAll(mh => !mh.IsActive());
                    var popH = pop.Run(SeqCfg).AddTo(this);
                    running.Add(popH);
                    yield return popH.ToYieldInstruction();
                }
                else pop.Dispose();
            }

            TileView mv;
            if (movedUid != null && tiles.TryGetValue(movedUid, out mv) && mv != null
                && movedTo.Stack >= 0 && movedTo.Stack < boxViews.Length)
            {
                var from = mv.transform.position;
                mv.transform.SetParent(boxViews[movedTo.Stack].Slot(movedTo.Slot), false);
                mv.transform.position = from;
                mv.SetFlying(true);
                // Bản cũ: Append pop @0, Append bay @undoPopDur, Join co về 1 @undoPopDur.
                var mt = mv.transform;
                var popS = Vector3.one * a.undoPopScale;
                var seq = LSequence.Create();
                seq.Insert(0f, LMotion.Create(mt.localScale, popS, a.undoPopDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(mt));
                seq.Insert(a.undoPopDur, LMotion.Create(mt.localPosition, Vector3.zero, a.undoFlyDur)
                                                .WithEase(a.undoFlyEase).WithCancelOnError().BindToLocalPosition(mt));
                seq.Insert(a.undoPopDur, LMotion.Create(popS, Vector3.one, a.undoFlyDur)
                                                .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(mt));
                running.RemoveAll(mh => !mh.IsActive());
                var moveH = seq.Run(SeqCfg).AddTo(mv.gameObject);
                running.Add(moveH);
                yield return moveH.ToYieldInstruction();
                if (mv != null) mv.SetFlying(false);
            }
        }

        // uid → ô đang đứng, chỉ xét hộp trên cùng (thẻ chìm không có view).
        static Dictionary<string, SlotRef> TopPositions(Game game)
        {
            var d = new Dictionary<string, SlotRef>();
            for (int s = 0; s < game.Stacks.Count; s++)
            {
                var box = game.TopBox(s);
                if (box == null) continue;
                for (int i = 0; i < box.Slots.Length; i++)
                    if (box.Slots[i] != null) d[box.Slots[i].Uid] = new SlotRef { Stack = s, Box = 0, Slot = i };
            }
            return d;
        }

        // Bật/tắt tấm nền booster bằng hai LitMotionAnimation (spec 2026-10-02-booster-anim-preview Mục 3).
        // Thiếu component → SetActive khan. Chưa gán nền → không làm gì (bàn vẫn chạy). Tắt xong mới
        // Rebuild để nền không che cascade sau đó.
        //
        // LitMotionAnimation.Stop() trả giá trị về lúc trước Play: KHÔNG Stop fade-in khi xong (alpha sẽ
        // về 0); trước fade-out mới Stop nó và đặt lại alpha = 1 ngay cùng frame.
        IEnumerator Backdrop(bool on)
        {
            if (boosterBackdrop == null) yield break;
            var cg = boosterBackdrop.GetComponent<CanvasGroup>();
            var anim = on ? backdropFadeIn : backdropFadeOut;
            if (cg == null || anim == null)
            {
                if (on) boosterBackdrop.SetActive(true);
                if (cg != null) cg.alpha = on ? 1f : 0f;
                if (!on) boosterBackdrop.SetActive(false);
                yield break;
            }

            if (on)
            {
                cg.alpha = 0f;
                boosterBackdrop.SetActive(true);
            }
            else
            {
                if (backdropFadeIn != null) backdropFadeIn.Stop();
                cg.alpha = 1f;
            }
            anim.Stop();
            anim.Play();
            while (anim != null && anim.IsPlaying) yield return null;
            if (!on)
            {
                anim.Stop();
                boosterBackdrop.SetActive(false);
            }
        }

        // Chốt chung cho mọi booster. Log từng lý do từ chối — không có nó thì bấm xong
        // bàn đứng im và không phân biệt được "chưa nối dây" với "bàn từ chối".
        bool BoosterGateOpen(string name)
        {
            if (g == null) { Debug.Log("[" + name + "] chưa nạp màn nào."); return false; }
            if (locked || settling) { Debug.Log("[" + name + "] bàn đang chạy cascade."); return false; }
            if (LevelCommands.InputBlocked) { Debug.Log("[" + name + "] popup meta đang mở."); return false; }
            if (g.Status != GameStatus.Playing) { Debug.Log("[" + name + "] màn đã kết thúc: " + g.Status); return false; }
            if (ghost != null) { Debug.Log("[" + name + "] đang kéo thẻ."); return false; }
            return true;
        }

        // Nhịp 2 chưa có animation moi thẻ: dựng lại toàn bộ view từ trạng thái mới.
        // Nhịp 3 thay bằng chuỗi bay thật — MagnetResult đã mang sẵn vị trí nguồn của
        // từng thẻ (kể cả thẻ đang bị chôn) để làm đúng việc đó.
        void RebuildBoardViews()
        {
            DestroyBoard();
            BuildBoard();
            RefreshZones();
        }

        // Quét lại xem còn nhóm nào hút được không rồi đẩy sang tầng meta để xám/sáng
        // nút. Chỉ gọi khi bàn đã đứng yên, không gọi mỗi khung hình —
        // FindMagnetTarget duyệt cả bàn.
        void RefreshBoosterAvailability()
        {
            bool playing = g != null && g.Status == GameStatus.Playing;
            LevelSignals.SetMagnetAvailable(playing && g.FindMagnetTarget() != null);
            // Chạy thử trên bản sao: nút chỉ sáng khi bấm thật chắc chắn đổi được bàn — không
            // bao giờ ăn lượt mà bàn đứng yên (spec 2026-10-02-shuffle-redesign Mục 6).
            LevelSignals.SetShuffleAvailable(playing && g.ShuffleWouldChange());
            LevelSignals.SetUndoAvailable(playing && g.CanUndo);
        }

        bool RefsOk()
        {
            string missing = stackPrefab == null ? "stackPrefab"
                           : boxPrefab == null ? "boxPrefab"
                           : tilePrefab == null ? "tilePrefab"
                           : ghostPrefab == null ? "ghostPrefab" : null;
            if (missing == null) return true;
            Debug.LogError("BoardController trên '" + name + "' thiếu tham chiếu: " + missing +
                           " — kéo prefab vào field đó trong Inspector.");
            return false;
        }

        bool HasArt(string key) { return LoadArt(key) != null; }

        Sprite LoadArt(string key)
        {
            Sprite s;
            if (artCache.TryGetValue(key, out s)) return s;
            s = Resources.Load<Sprite>("Art/" + key);
            artCache[key] = s;
            return s;
        }

        Sprite ArtOf(Tile t) { return t != null && t.Art != null ? LoadArt(t.Art) : null; }

        static Font OsFont(params string[] names)
        {
            foreach (var n in names)
            {
                var f = Font.CreateDynamicFontFromOSFont(n, 64);
                if (f != null) return f;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Load()
        {
            StopAllCoroutines();
            DestroyBoard();
            locked = false;
            settling = false;
            landing = 0;
            busyStacks.Clear();
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);  // Settle() cuối Load() đặt lại giá trị thật
            LevelSignals.SetUndoAvailable(false);     // màn mới thì không có nước nào để lùi
            if (string.IsNullOrEmpty(levelJson))
            {
                Debug.LogError("Chưa có JSON level — BoardInitializer phải nạp qua Addressables trước.");
                return;
            }
            try
            {
                var lv = LevelData.Parse(levelJson);
                lv.Validate(HasArt);
                g = Game.Build(lv);
                // Bật chụp ảnh cho booster Undo. CHỈ ở đây: cờ mặc định tắt để Solver
                // (gọi MoveTile hàng vạn lần mỗi lần giải) không clone mỗi nút.
                g.UndoEnabled = true;
            }
            catch (Exception e)
            {
                g = null;
                Debug.LogError("Level không hợp lệ — " + e.Message);
                return;
            }
            BuildBoard();
            if (autoDebugBlockers) ApplyDebugBlockers();
            resultReported = false;
            firstInteractionRaised = false;
            LevelSignals.SetReviveAvailable(false);
            LevelSignals.RaiseStarted(levelIndex, g.TotalGroups);
            StartCoroutine(Settle(0f, allowMoves: true));   // hộp nạp sẵn nhóm đủ phải nổ ngay lúc load
        }

        // --------------------------------------------------------------- input

        void Update()
        {
            var p = Pointer.current;
            if (p == null || g == null) return;
            float dt = Time.deltaTime;
            var wp = cam.ScreenToWorldPoint(p.position.ReadValue());
            var pt = new Vector2(wp.x, wp.y);

            HandleKeys();

            // Hết màn thì đứng yên chờ AppFlow. Trước đây chạm màn hình là tự nạp
            // màn kế — giờ ResultState hiện popup và người chơi bấm Next/Retry ở đó.
            if (g.Status != GameStatus.Playing) return;

            if (locked) return;

            // Dùng hết nước (có thể ngay giữa cascade) — chờ EvaluationCompleted cuối chuỗi chốt thua.
            if (LevelSignals.OutOfMoves) return;

            // Popup meta đang mở (settings...) — board đọc raw Pointer nên phải tự
            // nhường, uGUI không chặn hộ. Ghost đang kéo (nếu có) đứng im tới khi mở lại.
            if (LevelCommands.InputBlocked) return;

            if (p.press.wasPressedThisFrame && !firstInteractionRaised)
            {
                firstInteractionRaised = true;
                LevelSignals.RaiseFirstInteraction();       // tầng meta: Ready → Playing
            }

            if (p.press.wasPressedThisFrame && ghost == null)
            {
                foreach (var z in zones)
                {
                    if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                    if (z.Fixed) ShakeTile(z.Uid);          // thẻ đóng đinh: rung, không nhấc
                    else BeginDrag(z.Stack, z.Uid, pt);
                    break;
                }
            }
            else if (ghost != null && p.press.isPressed)
            {
                ghost.Follow(pt, dt);
            }
            else if (ghost != null)
            {
                Drop(pt);
            }
            else if (!(p is Touchscreen))
            {
                // Cảm ứng không có hover: nhấc tay rồi Pointer vẫn giữ vị trí cuối, nên thẻ nằm
                // dưới chỗ nhấc tay sẽ phồng lên và đứng đó mãi.
                Hover(pt);
            }
        }

        // Phím tắt dev: R nạp lại JSON đang cache — không qua AppFlow nên KHÔNG đổi
        // LevelProgressData.CurrentLevel. N và 1-9 đã bỏ: board không còn danh sách
        // level để nhảy tới, muốn đổi màn thì đi qua AppFlow (hoặc cheat panel sau này).
        // DEBUG: bật Gizmos (Scene view, hoặc nút Gizmos trên Game view) lúc Play để thấy vùng
        // chạm thật: xanh = zone Stack (BoxSize), vàng = zone Tile (Shadow của slot). Vẽ từ chính
        // danh sách zones nên cái nhìn thấy là cái hit-test dùng, không phải bản tính lại.
        [SerializeField] bool drawZones = false;
        void OnDrawGizmos()
        {
            if (!drawZones || zones == null) return;
            foreach (var z in zones)
            {
                Gizmos.color = z.Kind == ZoneKind.Stack ? Color.cyan : Color.yellow;
                Gizmos.DrawWireCube(z.Rect.center, z.Rect.size);
            }
        }

        void HandleKeys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) Load();
            if (k.bKey.wasPressedThisFrame) ApplyDebugBlockers();   // DEBUG: xem ApplyDebugBlockers
        }

        // DEBUG (phím B, hoặc tự động lúc nạp khi bật autoDebugBlockers): gắn một bộ blocker
        // mẫu lên bàn ĐANG chơi để xem phần nhìn và phần chặn input. Không phải content:
        // tắt autoDebugBlockers rồi bấm R nạp lại là sạch. Chọn mục tiêu theo
        // thứ tự cố định để lần nào cũng ra như nhau.
        //   stack có thẻ đầu tiên  → thẻ đầu đóng băng 3 nước
        //   stack tiếp theo        → hộp khoá, cần thêm 1 nhóm nữa
        //   stack tiếp theo        → hộp khoá theo nhóm, nhóm lấy từ một thẻ ở stack khác
        void ApplyDebugBlockers()
        {
            if (g == null || g.Status != GameStatus.Playing) { Debug.Log("[Blocker] chưa có bàn để gắn."); return; }

            int icedStack = -1, lockedStack = -1, keyedStack = -1;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var box = g.TopBox(s);
                if (box == null) continue;
                int count = 0;
                foreach (var t in box.Slots) if (t != null) count++;
                if (count == 0) continue;
                if (icedStack < 0) { icedStack = s; continue; }
                if (lockedStack < 0) { lockedStack = s; continue; }
                if (keyedStack < 0) { keyedStack = s; break; }
            }

            if (icedStack >= 0)
                foreach (var t in g.TopBox(icedStack).Slots)
                    if (t != null) { t.Lock = new Lock { Kind = LockKind.Moves, Need = 3 }; break; }

            if (lockedStack >= 0)
                g.TopBox(lockedStack).Lock = new Lock { Kind = LockKind.Clears, Need = g.Cleared + 1 };

            if (keyedStack >= 0)
            {
                // Nhóm bị khoá không được có thẻ nào TRONG hay DƯỚI hộp nó khoá, nếu không là
                // khoá vĩnh viễn (spec luật 6). Lấy nhóm của thẻ đầu tiên thoả điều đó ở stack khác.
                string gid = null;
                for (int s = 0; s < g.Stacks.Count && gid == null; s++)
                {
                    if (s == keyedStack) continue;
                    var box = g.TopBox(s);
                    if (box == null) continue;
                    foreach (var t in box.Slots)
                    {
                        if (t == null || Game.IsFrozen(t)) continue;
                        bool inside = false;
                        foreach (var b in g.Stacks[keyedStack].Boxes)
                            foreach (var u in b.Slots)
                                if (u != null && g.InGroup(u.GroupId, t.GroupId)) inside = true;
                        if (!inside) { gid = t.GroupId; break; }
                    }
                }
                if (gid != null) g.TopBox(keyedStack).Lock = new Lock { Kind = LockKind.Group, GroupId = gid };
            }

            Debug.Log("[Blocker] gắn mẫu: băng ở stack " + icedStack + " · hộp khoá ở stack " + lockedStack +
                      " · hộp khoá theo nhóm ở stack " + keyedStack);
            RefreshZones();
            RefreshBlockerVisuals();
        }

        Tile FindTile(string uid)
        {
            foreach (var st in g.Stacks)
                foreach (var t in st.Boxes[0].Slots)
                    if (t != null && t.Uid == uid) return t;
            return null;
        }

        static int SlotIndexOf(Box box, string uid)
        {
            for (int i = 0; i < box.Slots.Length; i++)
                if (box.Slots[i] != null && box.Slots[i].Uid == uid) return i;
            return -1;
        }

        // Slot của top box stack `s` mà điểm thả rơi trúng; -1 khi rơi vào khe giữa các
        // slot hoặc mép hộp — lúc đó domain tự chọn slot trống đầu tiên.
        int SlotAt(int s, Vector2 pt)
        {
            var box = g.TopBox(s);
            if (box == null) return -1;
            for (int i = 0; i < box.Slots.Length; i++)
                if (SlotZone(s, i).Contains(pt)) return i;
            return -1;
        }

        // Vùng chạm của slot i trên stack s = AABB sprite Shadow trong Box.prefab (BoxView.SlotRect),
        // nên khớp hình thật kể cả scale 1.15 của Stack và 1.1 của Slot. Chưa có view hoặc slot
        // chưa nối Shadow thì lùi về hằng layout (ô vuông SlotSize) như trước.
        Rect SlotZone(int s, int i)
        {
            var bv = boxViews != null && s < boxViews.Length ? boxViews[s] : null;
            if (bv != null && bv.HasSlotRect(i)) return bv.SlotRect(i);
            return RectAt(StackWorldPos(g.Stacks[s]) + SlotOffset(i), Vector2.one * SlotSize);
        }

        int TargetStack(Vector2 pt)
        {
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Stack || !z.Rect.Contains(pt)) continue;
                if (z.Stack == dragFrom) return -1;
                return Game.FreeCount(g.TopBox(z.Stack)) > 0 ? z.Stack : -1;
            }
            return -1;
        }

        void BeginDrag(int stack, string uid, Vector2 pt)
        {
            var t = FindTile(uid);
            if (t == null) return;
            dragFrom = stack;
            dragUid = uid;

            ghost = Instantiate(ghostPrefab, transform, false);
            ghost.Begin(pt);
            var gt = Instantiate(tilePrefab, ghost.TileAnchor, false);
            gt.transform.localPosition = Vector3.zero;
            gt.Bind(t, ArtOf(t));
            gt.SetFlying(true);      // thẻ đang kéo = thẻ đang bay: nổi trên mọi hộp/thẻ trên bàn

            hoverPunch.TryComplete();                     // trả góc quay về 0 trước khi nhấc
            TileView tv;
            if (tiles.TryGetValue(uid, out tv) && tv != null)
                tv.transform.localScale = Vector3.zero;   // thẻ "được nhấc lên"
        }

        void Drop(Vector2 pt)
        {
            var dropPos = ghost.transform.position;
            Destroy(ghost.gameObject);
            ghost = null;
            int from = dragFrom, to = -1;
            string uid = dragUid;
            dragFrom = -1;
            dragUid = null;

            bool toBusy = false;
            foreach (var z in zones)
                if (z.Kind == ZoneKind.Stack && z.Rect.Contains(pt)) { to = z.Stack; toBusy = z.Busy; break; }

            // Thả ra ngoài, hoặc về chính stack cũ = huỷ thao tác (§R1).
            if (to < 0 || to == from) { SnapBack(uid, dropPos); return; }
            // Hộp đang diễn / chờ nổ: thẻ về chỗ cũ, không rung hộp (feedback #4).
            if (toBusy) { SnapBack(uid, dropPos); return; }
            if (!g.MoveTile(from, uid, to, SlotAt(to, pt)))
            {
                Shake(to);                                  // box đích đầy (§E1)
                SnapBack(uid, dropPos);
                return;
            }

            AfterMove(from, to, uid, dropPos);
        }

        // Phần sau khi domain đã nhận nước đi. Tách ra để test tự động (DebugMove) đi đúng
        // đường này chứ không phải một bản chép lại.
        void AfterMove(int from, int to, string uid, Vector3 fromWorld)
        {
            int slot = SlotIndexOf(g.TopBox(to), uid);
            TileView tv;
            if (slot >= 0 && tiles.TryGetValue(uid, out tv) && tv != null)
            {
                FlyTo(tv, boxViews[to].Slot(slot), fromWorld);
                hoverTile = tv;   // con trỏ còn đứng trên thẻ vừa thả: coi như đã hover sẵn để Hover() không phồng nó lên HoverScale
            }

            // HAI hộp đổi màu, không chỉ hộp đích: hộp nguồn mất thẻ → cặp có thể tan.
            RefreshTileVisuals(from);
            RefreshTileVisuals(to);
            RefreshZones();
            RefreshBlockerVisuals();   // băng đếm ở MỌI stack, không riêng from/to
            ReportResultIfFinished();
            LevelSignals.RaiseMoveCommitted(g.Moves);       // tầng meta: vào phase Evaluating
            // Đang cascade thì vòng đang chạy tự gặp nước này ở SettleStep kế — không mở vòng thứ hai.
            // Thẻ hạ cánh rồi mới nổ: Settle đợi landing về 0 (WaitLanding).
            if (!settling) StartCoroutine(Settle(0f, allowMoves: true));
        }

#if UNITY_EDITOR
        // Cửa sau cho test tự động: đi từ đúng chỗ Drop() đi tiếp, bỏ phần con trỏ/ghost.
        // Không thay được việc kéo tay — hit-test, ghost và feel vẫn phải người kiểm.
        public bool DebugMove(int from, int slot, int to)
        {
            if (g == null || locked || boxViews == null) return false;
            var box = g.TopBox(from);
            if (box == null || slot < 0 || slot >= box.Slots.Length || box.Slots[slot] == null) return false;
            string uid = box.Slots[slot].Uid;
            TileView tv;
            if (!tiles.TryGetValue(uid, out tv) || tv == null) return false;
            var fromWorld = tv.transform.position;
            if (!g.MoveTile(from, uid, to)) return false;
            AfterMove(from, to, uid, fromWorld);
            return true;
        }

        public bool DebugBusy { get { return locked; } }
        public string DebugStatus { get { return g == null ? "?" : g.Status + " " + g.Cleared + "/" + g.TotalGroups + " " + g.Moves + " moves"; } }
#endif

        void SnapBack(string uid, Vector3 from)
        {
            TileView tv;
            if (tiles.TryGetValue(uid, out tv) && tv != null)
                FlyTo(tv, tv.transform.parent, from);
        }

        // Thẻ bay từ chỗ thả về đúng slot — chính thẻ đó, không phải bản sao tạm. Đây là
        // thứ retained-mode mua được: danh tính GameObject sống xuyên qua nước đi.
        void FlyTo(TileView tv, Transform anchor, Vector3 fromWorld)
        {
            tv.transform.SetParent(anchor, false);
            tv.transform.position = fromWorld;
            tv.transform.localScale = Vector3.one;
            tv.SetFlying(true);
            landing++;
            LMotion.Create(tv.transform.localPosition, Vector3.zero, flyDur)
                   .WithEase(Ease.OutCubic)
                   .WithOnComplete(() => { landing = Mathf.Max(0, landing - 1); if (tv != null) tv.SetFlying(false); })
                   .WithOnCancel(() => landing = Mathf.Max(0, landing - 1))
                   .WithCancelOnError()
                   .BindToLocalPosition(tv.transform)
                   .AddTo(tv.gameObject);
        }

        void Hover(Vector2 pt)
        {
            TileView h = null;
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                if (!z.Fixed) tiles.TryGetValue(z.Uid, out h);   // thẻ đóng đinh không phồng
                break;
            }
            if (h == hoverTile) return;           // chỉ tween lúc VÀO/RA, không mỗi frame
            if (hoverTile != null && !IsLifted(hoverTile))
                LMotion.Create(hoverTile.transform.localScale, Vector3.one, 0.12f)
                       .WithEase(Ease.OutBack).WithCancelOnError()
                       .BindToLocalScale(hoverTile.transform).AddTo(hoverTile.gameObject);
            hoverTile = h;
            if (hoverTile != null && !IsLifted(hoverTile))
            {
                LMotion.Create(hoverTile.transform.localScale, Vector3.one * HoverScale, 0.12f)
                       .WithEase(Ease.OutBack).WithCancelOnError()
                       .BindToLocalScale(hoverTile.transform).AddTo(hoverTile.gameObject);
                // Giật một cái lúc con trỏ vào (CardVisual.PointerEnter). Complete cú trước để góc
                // quay không cộng dồn khi rê nhanh qua nhiều thẻ. DOPunchRotation(vibrato 20,
                // elasticity 1) — công thức punch LitMotion khác, Task 8 so bằng mắt.
                hoverPunch.TryComplete();
                hoverPunch = LMotion.Punch.Create(hoverTile.transform.localEulerAngles, Vector3.forward * HoverPunchAngle, 0.12f)
                                    .WithFrequency(20).WithDampingRatio(1f).WithCancelOnError()
                                    .BindToLocalEulerAngles(hoverTile.transform).AddTo(hoverTile.gameObject);
            }
        }

        // Thẻ đang bị "nhấc lên" (scale 0) vì đang kéo. Đừng đụng scale của nó.
        static bool IsLifted(TileView tv) { return tv.transform.localScale.x < 0.01f; }

        void Shake(int stack)
        {
            var bv = boxViews[stack];
            if (bv == null) return;
            MotionHandle prev;
            if (shakes.TryGetValue(stack, out prev)) prev.TryComplete();   // rung dồn: kết thúc cú trước đã
            // DOPunchPosition(…, vibrato 6, elasticity 0.6) cũ. DampingRatio chọn để biên độ
            // tắt còn ~5% lúc hết giờ (envelope exp(-damping*frequency/2π*t)) — điểm khởi đầu
            // cho Task 8 so bằng mắt, không phải số chốt cuối.
            shakes[stack] = LMotion.Punch.Create(bv.transform.localPosition, new Vector3(0.12f, 0f, 0f), 0.22f)
                                   .WithFrequency(6).WithDampingRatio(3.1f).WithCancelOnError()
                                   .BindToLocalPosition(bv.transform).AddTo(bv.gameObject);
        }

        // Bấm thẻ đóng đinh: rung ngang một cú, cùng công thức Shake của hộp nhưng nhỏ hơn.
        const float TileShakeAmp = 0.06f, TileShakeDur = 0.2f;
        MotionHandle tileShake;

        void ShakeTile(string uid)
        {
            TileView tv;
            if (!tiles.TryGetValue(uid, out tv) || tv == null) return;
            tileShake.TryComplete();   // rung dồn: kết thúc cú trước đã
            tileShake = LMotion.Punch.Create(tv.transform.localPosition, new Vector3(TileShakeAmp, 0f, 0f), TileShakeDur)
                               .WithFrequency(6).WithDampingRatio(3.1f).WithCancelOnError()
                               .BindToLocalPosition(tv.transform).AddTo(tv.gameObject);
        }

        // ------------------------------------------------------------ cascade
        // Domain mutate từng bước; view animate trên instance đang sống rồi mới cập nhật
        // sổ sách. Khoá input tới khi bàn đứng yên (§E11).

        // Thẻ đóng đinh trong các uid này tháo đinh song song, chờ xong, Stop (EndFixedBreak) rồi mới
        // cho gộp / bay (spec fixed-tile Mục 5). Không thẻ nào đóng đinh thì trả về ngay.
        IEnumerator BreakFixed(IEnumerable<string> uids)
        {
            var breaking = new List<TileView>();
            foreach (var uid in uids)
            {
                TileView tv;
                if (!tiles.TryGetValue(uid, out tv) || tv == null || !tv.IsFixed) continue;
                tv.PlayFixedBreak();
                breaking.Add(tv);
            }
            while (breaking.Exists(tv => tv != null && tv.IsBreakingFixed)) yield return null;
            foreach (var tv in breaking) if (tv != null) tv.EndFixedBreak();
        }

        // allowMoves = false (booster, Undo, Revive): khoá cả bàn tới hết chuỗi như trước.
        // allowMoves = true (nước đi thường, lúc nạp màn): chỉ khoá hộp đang diễn + hộp chờ lượt.
        IEnumerator Settle(float delay = 0f, bool allowMoves = false)
        {
            settling = true;
            if (!allowMoves) locked = true;
            // Tắt nút booster suốt cascade. Không tắt thì cờ giữ giá trị cũ, nút vẫn
            // sáng, người chơi bấm được → BoosterManager trừ lượt xong handler lại drop
            // vì locked = mất lượt đã mua bằng coin.
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);
            LevelSignals.SetUndoAvailable(false);
            bool hadCascade = false;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (;;)
            {
                yield return WaitLanding();   // không hộp nào nổ khi thẻ còn giữa đường bay vào nó

                // Chụp group lock đang đóng TRƯỚC khi domain mutate: lock Group suy từ bàn, thẻ
                // vừa bị xoá là IsOpen đổi ngay, không còn dấu vết "vừa mở bởi nhóm này".
                var wasLocked = ClosedGroupLocks();
                var ev = g.SettleStep(Rules.RemoveEmptyNonBottomBox);
                if (ev.Kind == SettleKind.None) break;
                hadCascade = true;
                // Domain vừa đi trước view một bước: khoá hộp này (+ hộp khoá sắp mở) NGAY, trước animation.
                var opened = NowOpen(wasLocked);
                MarkBusy(ev.Stack, opened);
                if (ev.Kind == SettleKind.Clear || ev.Kind == SettleKind.Collapse)
                    yield return BreakFixed(ev.DoomedUids);   // tháo đinh trước khi gộp

                if (ev.Kind == SettleKind.Clear)
                {
                    if (opened.Count > 0)
                        yield return ClearIntoLock(ev.Stack, ev.GroupId, ev.DoomedUids, opened);   // gộp giữa hộp → bay vào icon → lồng mở
                    else
                        // 4 thẻ chụm về tâm hộp thành thẻ to mang tên nhóm, rồi thẻ to biến mất (feedback #5).
                        yield return ClearIntoCard(ev.Stack, ev.GroupId, ev.DoomedUids);
                    RefreshTileVisuals(ev.Stack);
                }
                if (ev.Kind == SettleKind.Collapse)
                {
                    yield return MergeTiles(ev.Stack, ev.GroupId, ev.DoomedUids, ev.NewTileUid);
                    // COLLAPSE cũng xoá 4 thẻ của một nhóm nên có thể mở luôn group lock — nhưng
                    // 4 thẻ đó đã bay chụm vào ô gộp rồi (MergeTiles), không bay lại vào icon nữa.
                    if (opened.Count > 0) yield return OpenLocks(opened);
                    RefreshTileVisuals(ev.Stack);
                }
                if (ev.BoxRemoved)
                {
                    yield return LiftAwayBox(ev.Stack);
                    yield return RevealBox(ev.Stack);
                }

                ReleaseBusy();   // bước này diễn xong: nhả hộp, vẽ lại số đếm / hộp khoá theo từng bước
                ReportResultIfFinished();
                yield return new WaitForSeconds(cascadeGap);
            }
            RefreshZones();
            ReportResultIfFinished();
            CheckInvariant("settle");
            settling = false;
            locked = false;
            RefreshBoosterAvailability();
            RefreshBlockerVisuals();   // một lần gom có thể vừa mở hộp khoá hoặc hộp có ổ

            // Cascade đã tính xong. hadCascade = có animation vừa chạy → tầng meta
            // vào Animating, rồi RaiseAnimationCompleted đưa về Playing.
            // Thắng/thua thì phase đi thẳng Win/Lose, lời gọi dưới thành no-op.
            LevelSignals.RaiseEvaluationCompleted(
                isWin: g != null && g.Status == GameStatus.Won,
                isLose: g != null && g.Status == GameStatus.Stuck,
                hasPendingAnimation: hadCascade,
                movesUsed: g != null ? g.Moves : 0,
                groupsCleared: g != null ? g.Cleared : 0);

            LevelSignals.RaiseAnimationCompleted();
        }

        // Đợi thẻ của nước vừa đi hạ cánh. Trần thời gian: motion bị huỷ mà không gọi OnCancel thì
        // landing kẹt > 0 — thà nổ sớm một nhịp còn hơn treo cả cascade.
        IEnumerator WaitLanding()
        {
            float t = 0f;
            while (landing > 0 && t < flyDur * 4f) { t += Time.deltaTime; yield return null; }
            landing = 0;
        }

        void MarkBusy(int s, List<int> opened)
        {
            busyStacks.Add(s);
            foreach (int o in opened) busyStacks.Add(o);
            if (dragFrom >= 0 && busyStacks.Contains(dragFrom)) CancelDrag();   // lưới an toàn: hộp chờ lượt đã không cho nhấc
            RefreshZones();
        }

        void ReleaseBusy()
        {
            busyStacks.Clear();
            RefreshBlockerVisuals();
            RefreshZones();
        }

        // Bỏ thẻ đang kéo, trả nó về hình dạng cũ tại slot (BeginDrag đã thu scale về 0).
        void CancelDrag()
        {
            if (ghost != null) { Destroy(ghost.gameObject); ghost = null; }
            TileView tv;
            if (dragUid != null && tiles.TryGetValue(dragUid, out tv) && tv != null) tv.transform.localScale = Vector3.one;
            dragFrom = -1;
            dragUid = null;
        }

        // Stack có hộp trên cùng khoá theo nhóm và còn đóng. Gọi trước SettleStep (xem Settle).
        List<int> ClosedGroupLocks()
        {
            var r = new List<int>();
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var box = g.TopBox(s);
                if (box != null && box.Lock.Kind == LockKind.Group && !g.IsOpen(box.Lock)) r.Add(s);
            }
            return r;
        }

        // Trong số stack chụp ở trên, stack nào giờ đã mở và có view để diễn.
        List<int> NowOpen(List<int> stacks)
        {
            var r = new List<int>();
            foreach (int s in stacks)
            {
                var box = g.TopBox(s);
                if (box != null && g.IsOpen(box.Lock) && boxViews != null && s < boxViews.Length && boxViews[s] != null)
                    r.Add(s);
            }
            return r;
        }

        // Nhóm vừa gom xong chính là nhóm mở lồng. Nhìn như COLLAPSE: 4 thẻ bay chụm về TÂM hộp vừa
        // gom (không phải một ô), nén lại, thẻ nhóm nở ra ở đó — rồi thẻ ấy bay vào icon trên hộp
        // khoá đầu tiên (nhiều hộp cùng khoá một nhóm thì các hộp sau chỉ diễn phần lồng mở), co dần
        // trên đường bay, tới nơi thì huỷ; rồi lồng mở (BoxView.OpenGroupLock). Clear xoá hẳn nhóm
        // khỏi domain nên thẻ nhóm là thẻ tạm của view (SpawnGroupCard).
        IEnumerator ClearIntoLock(int s, string gid, string[] uids, List<int> opened)
        {
            var center = boxViews[s].transform.position;
            yield return GatherTiles(uids, center);

            var card = SpawnGroupCard(s, gid, center);
            if (card != null)
            {
                yield return ShowGroupCard(card);
                var go = card.gameObject;
                var tr = card.transform;
                var fly = LSequence.Create();
                fly.Insert(0f, LMotion.Create(tr.position, boxViews[opened[0]].GroupIconWorld, lockFlyDur)
                                      .WithEase(Ease.InCubic).WithCancelOnError().BindToPosition(tr));
                fly.Insert(0f, LMotion.Create(tr.localScale, Vector3.one * lockFlyGatherScale, lockFlyDur)
                                      .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                running.RemoveAll(mh => !mh.IsActive());
                var h = fly.Run(SeqCfg).AddTo(go);
                running.Add(h);
                yield return h.ToYieldInstruction();
                Destroy(go);   // chuỗi đã xong — huỷ target lúc này là an toàn (xem DestroyAll)
            }

            yield return OpenLocks(opened);
        }

        // Thẻ tạm đại diện nhóm gid: nền "đủ bộ" + tên nhóm (GroupDef.Text), không art. Làm con
        // của Slot(0) hộp s để ăn đúng scale thẻ trong hộp, đặt tại worldPos. Không vào sổ tiles —
        // bên gọi tự huỷ. null khi level không có def của nhóm (không diễn gộp, lồng vẫn mở).
        TileView SpawnGroupCard(int s, string gid, Vector3 worldPos)
        {
            GroupDef def;
            if (g.GroupDefs == null || gid == null || !g.GroupDefs.TryGetValue(gid, out def)) return null;
            var t = new Tile { Uid = "card:" + gid, CardId = gid, GroupId = gid, Text = def.Text, Art = def.Art };
            var tv = Instantiate(tilePrefab, boxViews[s].Slot(0), false);
            tv.transform.position = worldPos;
            tv.Bind(t, null);                              // thẻ to chỉ mang TÊN nhóm; hình nhóm để dành lúc pop (#6)
            tv.ShowGroupName(def.Text);
            tv.SetMatchState(Rules.BoxCapacity, 0);        // nền "đủ bộ"
            tv.SetFlying(true);   // nổi trên hộp và lồng (sorting 90 > lồng 14–16)
            return tv;
        }

        // Mở group lock trên từng hộp trong `opened` song song; cùng một animation nên chờ hộp
        // đầu là đủ. Dùng chung cho ClearIntoLock (sau khi thẻ bay vào icon) và nhánh Collapse
        // trong Settle (không có thẻ bay riêng — 4 thẻ đã gộp qua MergeTiles).
        IEnumerator OpenLocks(List<int> opened)
        {
            for (int k = 1; k < opened.Count; k++) StartCoroutine(boxViews[opened[k]].OpenGroupLock());
            yield return boxViews[opened[0]].OpenGroupLock();
        }

        // 4 thẻ co về 0 lệch nhau clearStagger rồi huỷ. Không còn thẻ nào có view thì kết thúc ngay.
        IEnumerator RemoveTiles(string[] uids)
        {
            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            for (int i = 0; i < uids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(uids[i], out tv) || tv == null) continue;
                tiles.Remove(uids[i]);
                seq.Insert(i * clearStagger, LMotion.Create(tv.transform.localScale, Vector3.zero, clearDur)
                                                    .WithEase(Ease.InBack).WithCancelOnError().BindToLocalScale(tv.transform));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); yield break; }
            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(this);
            running.Add(h);
            yield return h.ToYieldInstruction();
            DestroyAll(doomed);
        }

        // COLLAPSE nhìn phải ra "gộp", không phải "biến mất rồi mọc lại" — nên 4 thẻ BAY CHỤM về
        // đúng ô mà domain đã đặt thẻ mới, nén về 0 ở đó, rồi thẻ mới bung ra từ chính điểm ấy.
        // Domain đã mutate xong trước khi hàm này chạy, nên thẻ mới đã nằm sẵn trong Slots — tra
        // ra ô của nó chính là cách biết điểm hội tụ.
        IEnumerator MergeTiles(int s, string gid, string[] doomedUids, string newUid)
        {
            var box = g.TopBox(s);
            int dest = box == null ? -1 : Array.FindIndex(box.Slots, t => t != null && t.Uid == newUid);
            if (dest < 0)
            {
                // Không tra ra ô đích thì lùi về cách cũ — thà xấu còn hơn nuốt mất thẻ.
                yield return RemoveTiles(doomedUids);
                SpawnCollapsedTile(s, newUid);
                yield break;
            }

            // Feedback #5/#6: chụm về TÂM hộp thành thẻ to mang tên nhóm, thẻ to thu về ô của thẻ nhóm,
            // rồi thẻ nhóm thật pop ra ở đó với hình của nhóm (SpawnCollapsedTile → Bloom).
            var center = boxViews[s].transform.position;
            yield return GatherTiles(doomedUids, center);
            var card = SpawnGroupCard(s, gid, center);
            if (card != null)
            {
                yield return ShowGroupCard(card);
                var tr = card.transform;
                var seq = LSequence.Create();
                seq.Insert(0f, LMotion.Create(tr.position, boxViews[s].Slot(dest).position, groupCardToSlot)
                                      .WithEase(Ease.InCubic).WithCancelOnError().BindToPosition(tr));
                seq.Insert(0f, LMotion.Create(tr.localScale, Vector3.one * 0.4f, groupCardToSlot)
                                      .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                yield return RunAndWait(seq, card.gameObject);
                Destroy(card.gameObject);
            }
            SpawnCollapsedTile(s, newUid);
        }

        // CLEAR thường (feedback #5): 4 thẻ chụm về tâm hộp → thẻ to mang tên nhóm → co về 0 rồi huỷ.
        IEnumerator ClearIntoCard(int s, string gid, string[] uids)
        {
            var center = boxViews[s].transform.position;
            yield return GatherTiles(uids, center);
            var card = SpawnGroupCard(s, gid, center);
            if (card == null) yield break;
            yield return ShowGroupCard(card);
            var seq = LSequence.Create();
            seq.Insert(0f, LMotion.Create(card.transform.localScale, Vector3.zero, groupCardOut)
                                  .WithEase(Ease.InBack).WithCancelOnError().BindToLocalScale(card.transform));
            yield return RunAndWait(seq, card.gameObject);
            Destroy(card.gameObject);
        }

        // Thẻ nhóm to nở ra ở tâm hộp rồi đứng yên groupCardHold cho kịp đọc tên.
        IEnumerator ShowGroupCard(TileView card)
        {
            yield return Bloom(card, groupCardScale).ToYieldInstruction();
            if (groupCardHold > 0f) yield return new WaitForSeconds(groupCardHold);
        }

        IEnumerator RunAndWait(MotionSequenceBuilder seq, GameObject owner)
        {
            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(owner);
            running.Add(h);
            yield return h.ToYieldInstruction();
        }

        // Các thẻ bay chụm về destPos, co còn mergeShrink trên đường, tới nơi nén nốt về 0, rồi huỷ
        // SAU khi cả chuỗi xong (DestroyAll). Sổ tiles cập nhật ngay. Dùng chung cho COLLAPSE (đích =
        // ô thẻ mới), CLEAR thường và nhóm mở khoá (đích = tâm hộp). Không còn view nào để diễn thì
        // kết thúc ngay.
        IEnumerator GatherTiles(string[] uids, Vector3 destPos)
        {
            var shrink = Vector3.one * mergeShrink;
            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            for (int i = 0; i < uids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(uids[i], out tv) || tv == null) continue;
                tiles.Remove(uids[i]);
                tv.SetFlying(true);                          // bay chụm thì nổi lên trên hộp
                var tr = tv.transform;
                var from = tr.position;
                float at = i * mergeStagger;

                // InBack: nhích ra ngoài một chút rồi mới lao vào — cú lấy đà làm chuyển động
                // đọc ra là "bị hút vào" thay vì "trượt tới". Overshoot 0.6 như bản cũ;
                // LitMotion.Ease không nhận overshoot nên nội suy tay (motion Linear 0→1).
                seq.Insert(at, LMotion.Create(0f, 1f, mergeGather).WithCancelOnError()
                                      .Bind(k => tr.position = Vector3.LerpUnclamped(from, destPos, InBack(k, 0.6f))));
                seq.Insert(at, LMotion.Create(tr.localScale, shrink, mergeGather)
                                      .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                // Tới nơi thì nén nốt về 0: nhịp "cộp" ngăn giữa lúc các thẻ tắt và lúc thẻ mới bung.
                seq.Insert(at + mergeGather, LMotion.Create(shrink, Vector3.zero, Mathf.Max(mergeHold, 0.01f))
                                                    .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); yield break; }

            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(this);
            running.Add(h);
            yield return h.ToYieldInstruction();
            DestroyAll(doomed);
        }

        // Hộp rỗng bị xoá (GDD §9.3 "Xoá box"): nhấc lên + mờ dần, y như mở khoá hộp khoá theo số —
        // số liệu ở BoxView (Unlock Dur / Unlock Lift / Unlock Ease), chỉnh một chỗ ăn cả hai.
        IEnumerator LiftAwayBox(int s)
        {
            running.RemoveAll(mh => !mh.IsActive());
            var h = boxViews[s].LiftAway();
            running.Add(h);
            yield return h.ToYieldInstruction();
        }

        // ------------------------------------------------- thao tác tăng dần
        // Thay cho Rebuild() của bản runtime: mỗi cái đụng đúng phần đã đổi.

        // Tile Holder tụt + mờ: k = 0 đứng đúng chỗ hiện tại, hiện đủ; k = 1 tụt xuống revealHolderDrop, mờ hẳn.
        // Lộ hộp chạy 0 → 1 OutCubic; Undo chạy 1 → 0 InCubic — đúng đoạn phim cũ tua ngược.
        // Đặt giá trị đầu ngay để không nháy một frame trước khi sequence chạy.
        MotionHandle HolderDrop(Transform holder, float from, float to, Ease ease)
        {
            var srs = holder.GetComponentsInChildren<SpriteRenderer>();
            var a0 = srs.Select(r => r.color.a).ToArray();
            var p0 = holder.position;
            Action<float> set = k =>
            {
                holder.position = p0 + Vector3.down * (revealHolderDrop * k);
                for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = a0[i] * (1f - k); srs[i].color = c; }
            };
            set(from);
            return LMotion.Create(from, to, revealHolderDur).WithEase(ease).WithCancelOnError().Bind(set);
        }

        // Hộp dưới lộ lên. Tile Holder của Peek1 đang mang tile nhỏ của chính hộp này: một bản sao
        // tách ra trượt xuống + mờ đi, còn thẻ thật dựng sẵn ngay chỗ tile nhỏ, cỡ tile nhỏ, rồi lần
        // lượt to lên bằng thẻ thật và nhảy vòng cung vào ô. Holder thật đã mang tile nhỏ của hộp kế
        // tiếp (ShowDepth) nên ẩn đi, chờ bản sao đi hết mới hiện lại.
        IEnumerator RevealBox(int s)
        {
            var sv = stackViews[s];
            var holder = sv.NextHolder;
            // Chụp tile nhỏ TRƯỚC ShowDepth — nó bật/tắt marker theo hộp kế tiếp. Trái → phải.
            var minis = holder != null && holder.gameObject.activeInHierarchy
                ? sv.NextTileMarkers.Where(m => m != null && m.activeInHierarchy)
                    .Select(m => m.GetComponent<SpriteRenderer>()).OrderBy(r => r.transform.position.x)
                    .Select(r => (pos: r.transform.position, width: r.bounds.size.x)).ToList()
                : new List<(Vector3 pos, float width)>();

            GameObject leaving = null;
            if (minis.Count > 0)
            {
                // ponytail: scale đều — holder/stack không scale lệch trục.
                leaving = Instantiate(holder.gameObject, sv.transform);
                leaving.transform.SetPositionAndRotation(holder.position, holder.rotation);
                leaving.transform.localScale = holder.lossyScale / sv.transform.lossyScale.x;
                foreach (Transform c in leaving.transform) c.gameObject.SetActive(false);   // tile nhỏ đã thành thẻ thật
            }

            boxViews[s].ResetVisual();
            sv.ShowDepth(g.Stacks[s].Boxes.Count - 1, TilesInSecondBox(g.Stacks[s]));
            SpawnTiles(s);                                 // thẻ của hộp vừa lộ
            RefreshBlockerVisuals();                       // hộp vừa lộ có thể đang khoá
            if (leaving == null) yield break;

            var seq = LSequence.Create();

            seq.Insert(0f, HolderDrop(leaving.transform, 0f, 1f, Ease.OutCubic));

            // Holder thật (đã mang tile nhỏ của hộp kế tiếp) — Peek1 tắt thì khỏi diễn.
            if (holder.gameObject.activeInHierarchy)
            {
                var srs = holder.GetComponentsInChildren<SpriteRenderer>();
                var a0 = srs.Select(r => r.color.a).ToArray();
                Action<float> setA = k => { for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = a0[i] * k; srs[i].color = c; } };
                setA(0f);
                seq.Insert(revealHolderDur, LMotion.Create(0f, 1f, Mathf.Max(revealHolderFadeIn, 0.01f)).WithCancelOnError().Bind(setA));
            }

            // Ghép tile nhỏ ↔ thẻ thật theo thứ tự trái → phải. Lệch số (không nên có) thì thẻ dư nằm yên trong ô.
            var box = g.TopBox(s);
            var landing = box.Slots.Where(t => t != null && tiles.ContainsKey(t.Uid)).Select(t => tiles[t.Uid])
                             .OrderBy(tv => tv.transform.position.x).ToList();
            int n = Mathf.Min(minis.Count, landing.Count);
            for (int i = 0; i < n; i++)
            {
                var tv = landing[i];
                var tr = tv.transform;
                var to = tr.position;
                var from = minis[i].pos;
                var s1 = tr.localScale;
                var s0 = s1 * (minis[i].width / Mathf.Max(tv.Width, 0.0001f));
                tr.position = from;
                tr.localScale = s0;
                tv.SetFlying(true);                         // bay qua mép hộp thì nổi trên hộp
                float at = i * revealTileStagger;
                seq.Insert(at, LMotion.Create(0f, 1f, revealTileDur).WithEase(Ease.OutQuad).WithCancelOnError()
                                      .Bind(k =>
                                      {
                                          var p = Vector3.LerpUnclamped(from, to, k);
                                          p.y += revealJump * Mathf.Sin(k * Mathf.PI);
                                          tr.position = p;
                                      }));
                seq.Insert(at, LMotion.Create(s0, s1, revealTileDur).WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
            }

            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(this);
            running.Add(h);
            yield return h.ToYieldInstruction();

            for (int i = 0; i < n; i++) if (landing[i] != null) landing[i].SetFlying(false);
            if (leaving != null) Destroy(leaving);
        }

        void SpawnTiles(int s)
        {
            var box = g.TopBox(s);
            if (box == null) return;
            pairOrdinals.Remove(s);   // hộp mới (dựng bàn / lộ hộp dưới) → sticky làm lại từ đầu
            var counts = GroupCountsIn(box);
            var ordinals = PairOrdinalsFor(s, box, counts);
            for (int i = 0; i < box.Slots.Length; i++)
            {
                var t = box.Slots[i];
                if (t == null) continue;
                var tv = Instantiate(tilePrefab, boxViews[s].Slot(i), false);
                tv.transform.localPosition = Vector3.zero;
                tv.Bind(t, ArtOf(t));
                tv.SetMatchState(counts[t.GroupId], OrdinalOf(ordinals, t.GroupId));
                tiles[t.Uid] = tv;
            }
        }

        // Thẻ sinh ra từ collapse: dựng như SpawnTiles nhưng một thẻ, nở từ 0 ngay tại điểm 4 thẻ
        // vừa hội tụ. Xoay về 0 trong lúc nở để cú bung có hướng, đọc ra "vừa được đúc ra".
        void SpawnCollapsedTile(int s, string uid)
        {
            var box = g.TopBox(s);
            if (box == null) return;
            int i = Array.FindIndex(box.Slots, t => t != null && t.Uid == uid);
            if (i < 0) return;
            var t = box.Slots[i];
            var tv = Instantiate(tilePrefab, boxViews[s].Slot(i), false);
            tv.transform.localPosition = Vector3.zero;
            tv.Bind(t, ArtOf(t));
            var cc = GroupCountsIn(box);
            tv.SetMatchState(cc[t.GroupId], OrdinalOf(PairOrdinalsFor(s, box, cc), t.GroupId));
            Bloom(tv);
            tiles[t.Uid] = tv;
        }

        // Thẻ mới nở từ 0 (OutBack), kèm xoay mergeSpin về 0. Dùng cho thẻ collapse và thẻ nhóm mở khoá.
        MotionHandle Bloom(TileView tv, float scale = 1f)
        {
            var go = tv.gameObject;
            tv.transform.localScale = Vector3.zero;   // motion chỉ ghi từ frame sau — đừng để lộ thẻ cỡ 1
            var seq = LSequence.Create();
            seq.Insert(0f, LMotion.Create(Vector3.zero, Vector3.one * scale, mergeBloom)
                                  .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tv.transform));
            if (Mathf.Abs(mergeSpin) > 0.01f)
            {
                tv.transform.localEulerAngles = new Vector3(0f, 0f, mergeSpin);
                seq.Insert(0f, LMotion.Create(tv.transform.localRotation, Quaternion.identity, mergeBloom)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToLocalRotation(tv.transform));
            }
            running.RemoveAll(mh => !mh.IsActive());
            var h = seq.Run(SeqCfg).AddTo(go);
            running.Add(h);
            return h;
        }

        // Sprite nền theo số thẻ cùng nhóm — refresh sau nước đi (cả 2 hộp) và sau
        // mỗi bước cascade.
        void RefreshTileVisuals(int s)
        {
            var box = g.TopBox(s);
            if (box == null) return;
            var counts = GroupCountsIn(box);
            var ordinals = PairOrdinalsFor(s, box, counts);
            for (int i = 0; i < box.Slots.Length; i++)
            {
                var t = box.Slots[i];
                if (t == null) continue;
                TileView tv;
                if (tiles.TryGetValue(t.Uid, out tv) && tv != null)
                    tv.SetMatchState(counts[t.GroupId], OrdinalOf(ordinals, t.GroupId));
            }
        }

        // Option cặp STICKY: cặp giữ nguyên Option đã nhận cho đến khi tan — cặp đứng
        // trước biến mất thì cặp sau KHÔNG đổi sprite. State thuần view (domain không
        // biết): map gid→ordinal theo stack, reset khi hộp đổi (SpawnTiles) / bàn huỷ.
        readonly Dictionary<int, Dictionary<string, int>> pairOrdinals =
            new Dictionary<int, Dictionary<string, int>>();

        Dictionary<string, int> PairOrdinalsFor(int s, Box box, Dictionary<string, int> counts)
        {
            Dictionary<string, int> map;
            if (!pairOrdinals.TryGetValue(s, out map))
            {
                map = new Dictionary<string, int>();
                pairOrdinals[s] = map;
            }

            // Thả ordinal của nhóm không còn là cặp (tan, hoặc lớn thành bộ ba).
            List<string> stale = null;
            foreach (var kv in map)
            {
                int c;
                if (!counts.TryGetValue(kv.Key, out c) || c != 2)
                    (stale = stale ?? new List<string>()).Add(kv.Key);
            }
            if (stale != null) foreach (var gid in stale) map.Remove(gid);

            // Cấp ordinal trống thấp nhất cho cặp mới — hai cặp sinh cùng lúc thì
            // theo thứ tự xuất hiện trong hộp (duyệt slot 0→3).
            foreach (var t in box.Slots)
            {
                if (t == null) continue;
                if (counts[t.GroupId] != 2 || map.ContainsKey(t.GroupId)) continue;
                map[t.GroupId] = map.ContainsValue(0) ? 1 : 0;
            }
            return map;
        }

        static int OrdinalOf(Dictionary<string, int> ordinals, string gid)
        {
            int ord;
            return ordinals.TryGetValue(gid, out ord) ? ord : 0;
        }

        // gid → số thẻ của nhóm đó trong hộp. Đếm ở view vì đây là dẫn xuất hiển thị
        // thuần, không phải luật (khác BoxColorIndices — cấp màu có quy tắc riêng ở domain).
        static Dictionary<string, int> GroupCountsIn(Box box)
        {
            var counts = new Dictionary<string, int>();
            foreach (var t in box.Slots)
            {
                if (t == null) continue;
                int n;
                counts.TryGetValue(t.GroupId, out n);
                counts[t.GroupId] = n + 1;
            }
            return counts;
        }

        void RefreshZones()
        {
            zones.Clear();
            if (g == null) return;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var pos = StackWorldPos(g.Stacks[s]);
                var box = g.TopBox(s);
                // Hộp đang diễn bước cascade hoặc đang chờ lượt nổ: không zone thẻ (không hover/nhấc),
                // zone hộp gắn Busy để Drop trả thẻ về chỗ cũ.
                bool busy = busyStacks.Contains(s) || g.SettlePending(s, Rules.RemoveEmptyNonBottomBox);
                if (box != null && !busy)
                    for (int i = 0; i < box.Slots.Length; i++)
                    {
                        var t = box.Slots[i];
                        if (t == null) continue;
                        // Thẻ băng và thẻ trong hộp đóng: không hover, không nhấc (thẻ đóng đinh CÓ zone để bấm thì rung). Hover() và
                        // BeginDrag() đều duyệt cùng danh sách này nên bỏ ở đây là bỏ cả hai.
                        // Zone Stack bên dưới vẫn giữ: thả VÀO hộp đóng thì MoveTile từ chối và
                        // Drop() cho hộp rung, rõ hơn là im lặng nuốt thao tác.
                        if (!g.IsPullable(t, box)) continue;
                        zones.Add(new Zone
                        {
                            Rect = SlotZone(s, i),
                            Kind = ZoneKind.Tile, Stack = s, Uid = t.Uid, Fixed = Game.IsFixed(t)
                        });
                    }
                zones.Add(new Zone
                {
                    Rect = RectAt(pos, Vector2.one * BoxSize),
                    Kind = ZoneKind.Stack, Stack = s, Busy = busy
                });
            }
        }

        // Trạng thái blocker đổi ở bốn thời điểm: dựng bàn, sau mỗi nước đi (băng đếm),
        // khi hộp dưới lộ ra, và cuối cascade (một lần gom có thể mở hộp khoá theo số hoặc theo nhóm).
        // Quét cả bàn thay vì lần theo từng thay đổi: bàn tối đa vài chục ô, và bỏ sót một
        // chỗ thì hình nói dối về thứ người chơi bấm được.
        void RefreshBlockerVisuals()
        {
            if (g == null || boxViews == null) return;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                if (busyStacks.Contains(s)) continue;   // view của nó đang đi sau domain — vẽ lại khi nhả
                var box = g.TopBox(s);
                if (box == null) continue;

                if (boxViews[s] != null)
                {
                    if (g.IsOpen(box.Lock)) boxViews[s].SetOpen();
                    else if (box.Lock.Kind == LockKind.Group) boxViews[s].SetGroupLock(GroupArt(box.Lock.GroupId));
                    else boxViews[s].SetCountLock(Mathf.Max(box.Lock.Need - g.Cleared, 0));
                }

                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var t = box.Slots[i];
                    if (t == null) continue;
                    TileView tv;
                    if (!tiles.TryGetValue(t.Uid, out tv) || tv == null) continue;
                    bool frozen = Game.IsFrozen(t);
                    int iceLeft = frozen ? t.Lock.Need - t.Lock.Have : 0;
                    tv.SetIce(frozen, iceLeft, frozen ? t.Lock.Need : 0);
                    tv.SetFixed(Game.IsFixed(t));
                    tv.SetBlockerDebug(iceLeft);
                }
            }
        }


        // Hộp khoá theo nhóm hiện art của nhóm phải gom sạch (GroupDef.Art, cùng nguồn với thẻ collapse).
        Sprite GroupArt(string gid)
        {
            GroupDef d;
            return g.GroupDefs != null && g.GroupDefs.TryGetValue(gid, out d) && d.Art != null ? LoadArt(d.Art) : null;
        }

        // HUD prototype (HudView) đã bỏ — HUD thật là GamePlayUIRoot của tầng meta.
        // Chỉ còn lại phần báo kết quả, vốn sống nhờ các call site của RefreshHud cũ.
        // Chạy nhiều lần mỗi màn, nên chốt bằng cờ: tầng meta chỉ được nghe kết quả
        // đúng một lần, nếu không sẽ cộng coin lặp mỗi khung hình.
        void ReportResultIfFinished()
        {
            if (g == null) return;
            if (!resultReported && g.Status != GameStatus.Playing)
            {
                resultReported = true;
                // Chốt TRƯỚC Finished: AppFlow đọc cờ này khi nhận kết quả thua.
                LevelSignals.SetReviveAvailable(g.Status == GameStatus.Stuck && g.FindMagnetTarget() != null);
                LevelSignals.RaiseFinished(g.Status == GameStatus.Won, levelIndex, g.Moves);
            }
        }

        // --------------------------------------------------------------- dựng

        void BuildBoard()
        {
            int n = g.Stacks.Count;
            stackViews = new StackView[n];
            boxViews = new BoxView[n];
            for (int s = 0; s < n; s++)
            {
                var st = g.Stacks[s];
                var sv = Instantiate(stackPrefab, root, false);
                sv.transform.localScale = Vector3.one * stackScale;
                var wp = StackWorldPos(st);
                sv.transform.localPosition = new Vector3(wp.x, wp.y, 0f);
                stackViews[s] = sv;

                var bv = Instantiate(boxPrefab, sv.BoxAnchor, false);
                bv.transform.localPosition = Vector3.zero;
                boxViews[s] = bv;

                sv.ShowDepth(st.Boxes.Count - 1, TilesInSecondBox(st));
                SpawnTiles(s);
            }

            FitCamera();
            fitDirty = true;   // HUD có thể layout xong muộn một frame — fit lại ở LateUpdate
            RefreshZones();
            RefreshBlockerVisuals();
            ReportResultIfFinished();
            CheckInvariant("build");
        }

        void DestroyBoard()
        {
            // Huỷ MỌI LSequence đang chạy TRƯỚC KHI xoá GameObject bên dưới: Cancel một
            // sequence còn đọc SetTime trên state cuối của mỗi motion con, nên target phải
            // còn sống lúc Cancel — xoá GameObject trước rồi mới Cancel là bind ném
            // MissingReferenceException, làm khựng luôn UpdateRunner của LitMotion cả frame.
            foreach (var h in running) h.TryCancel();
            running.Clear();

            if (ghost != null) { Destroy(ghost.gameObject); ghost = null; }
            dragFrom = -1;
            dragUid = null;
            hoverTile = null;
            if (root != null)
                foreach (Transform c in root) Destroy(c.gameObject);
            tiles.Clear();
            zones.Clear();
            pairOrdinals.Clear();
            stackViews = null;
            boxViews = null;
        }

        // Khung nhìn CỐ ĐỊNH theo lưới 3x3: mọi màn cùng cỡ khung, màn ít hộp không bị
        // zoom to. Pos trong JSON giờ là tọa độ tuyệt đối trong lưới 0..2.
        const int GridCols = 3, GridRows = 3;

        void FitCamera()
        {
            fitDirty = false;
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            fittedSafeArea = Screen.safeArea;

            // Union khung 3x3 với pos thực tế — level lỡ đặt ngoài lưới vẫn không bị cắt.
            float minX = Mathf.Min(0f, (float)g.Stacks.Min(s => s.X));
            float maxX = Mathf.Max(GridCols - 1f, (float)g.Stacks.Max(s => s.X));
            float minY = Mathf.Min(0f, (float)g.Stacks.Min(s => s.Y));
            float maxY = Mathf.Max(GridRows - 1f, (float)g.Stacks.Max(s => s.Y));

            if (hudBlockers == null || hudBlockers.Length == 0)
            {
                float cx = (minX + maxX) / 2f * PitchX;
                float cy = -(minY + maxY) / 2f * PitchY + 0.5f;   // camera lên 0.5 → bàn hiện thấp xuống 0.5 (root phải ở gốc vì hit-test so world với local)
                float halfW = (maxX - minX) / 2f * PitchX + BoxSize / 2f + 0.4f;
                float halfH = (maxY - minY) / 2f * PitchY + BoxSize / 2f + 1.5f;   // chừa HUD trên + gợi ý dưới
                cam.transform.position = new Vector3(cx, cy, -10f);
                cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(cam.aspect, 0.01f));
                return;
            }

            var board = Rect.MinMaxRect(
                minX * PitchX - BoxSize / 2f - fitPadding,
                -maxY * PitchY - BoxSize / 2f - fitExtraBelow - fitPadding,
                maxX * PitchX + BoxSize / 2f + fitPadding,
                -minY * PitchY + BoxSize / 2f + fitPadding);

            Canvas.ForceUpdateCanvases();
            var screen = new Vector2(Screen.width, Screen.height);
            var free = BoardFit.FreeArea(Screen.safeArea, screen, BlockerRects());
            BoardFit.FitOrtho(board, free, screen, out var pos, out var size);
            // Chỉ camera di chuyển — root của bàn phải ở gốc vì hit-test so world với local.
            cam.transform.position = new Vector3(pos.x, pos.y, -10f);
            cam.orthographicSize = size;
        }

        // Góc mỗi blocker đổi sang pixel màn hình. Canvas Overlay: camera null.
        IEnumerable<Rect> BlockerRects()
        {
            var corners = new Vector3[4];
            foreach (var rt in hudBlockers)
            {
                if (rt == null) continue;
                var canvas = rt.GetComponentInParent<Canvas>(true);
                Camera uiCam = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : canvas.rootCanvas.worldCamera;
                rt.GetWorldCorners(corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);
                yield return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            }
        }

        // Đổi độ phân giải / safe area (xoay, Device Simulator, dropdown Cheat) → fit lại.
        void LateUpdate()
        {
            if (g == null || cam == null || g.Stacks.Count == 0) return;
            if (fitDirty || Screen.width != fittedScreen.x || Screen.height != fittedScreen.y || Screen.safeArea != fittedSafeArea)
                FitCamera();
        }

        void OnValidate() { fitDirty = true; }   // chỉnh padding trong Inspector thấy ngay

        // Ruột hộp nằm dưới không bao giờ đổi khi đang nằm dưới (nước đi chỉ đụng top box),
        // nên chỉ cần tính ở đúng 2 chỗ gọi ShowDepth: dựng bàn + lộ hộp mới.
        static int TilesInSecondBox(Stack st)
        {
            return st.Boxes.Count > 1 ? Rules.BoxCapacity - Game.FreeCount(st.Boxes[1]) : 0;
        }

        Vector2 StackWorldPos(Stack st)
        {
            // data y đi XUỐNG (cùng chiều đọc slot), Unity y đi LÊN → đảo dấu.
            return new Vector2((float)st.X * PitchX, -(float)st.Y * PitchY);
        }

        Vector2 SlotOffset(int slot)
        {
            int c = slot % 2, r = slot / 2;
            float step = SlotSize + SlotGap;
            return new Vector2((c - 0.5f) * step, (0.5f - r) * step);
        }

        static Rect RectAt(Vector2 center, Vector2 size) { return new Rect(center - size / 2f, size); }

        // --------------------------------------------------------- invariant
        // Thay cho sự an toàn mà rebuild cho không: "màn hình là hàm thuần của state".
        // Lệch → báo NGAY tại nước đi gây ra, thay vì lộ ra sau 20 nước.

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void CheckInvariant(string where)
        {
            if (g == null || boxViews == null) return;
            var expect = new HashSet<string>();
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var box = g.TopBox(s);
                if (box == null) continue;
                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var t = box.Slots[i];
                    if (t == null) continue;
                    expect.Add(t.Uid);
                    TileView tv;
                    if (!tiles.TryGetValue(t.Uid, out tv) || tv == null)
                        Debug.LogError("View lệch (" + where + "): thiếu thẻ " + t.Uid +
                                       " ở stack " + s + " slot " + i);
                    else if (tv.transform.parent != boxViews[s].Slot(i))
                        Debug.LogError("View lệch (" + where + "): thẻ " + t.Uid +
                                       " không nằm ở stack " + s + " slot " + i);
                }
            }
            foreach (var uid in tiles.Keys)
                if (!expect.Contains(uid))
                    Debug.LogError("View lệch (" + where + "): thẻ ma " + uid + " còn sống");
        }
    }
}
