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
using DG.Tweening;
using WordStack.Contracts;

namespace WordStack.Board
{
    public class BoardController : MonoBehaviour
    {
        // ---- Layout (world units) — hằng bố cục gắn với thuật toán đặt lưới, ở lại code.
        // Kích thước bên trong hộp (viền, slot) author trong Box.prefab; đổi bên đó phải đổi
        // BoxSize/BoxPad/SlotGap theo, vì hit-test tính từ mấy hằng này.
        const float BoxSize = 1.6f;
        const float BoxPad = 0.09f;
        const float SlotGap = 0.08f;
        static float SlotSize { get { return (BoxSize - 2f * BoxPad - SlotGap) / 2f; } }
        static float PitchX { get { return BoxSize + 0.28f; } }
        static float PitchY { get { return BoxSize + 0.62f; } }   // chừa chỗ cho lớp lấp ló

        // Sorting order KHÔNG set từ code nữa — author hết trong prefab
        // (Tile.prefab bg 10 / art 11, Ghost.prefab, Stack.prefab...).

        // Feel hover (tham chiếu D:\Balatro-Feel CardVisual.cs): phồng + giật một cái.
        const float HoverScale = 1.07f;
        const float HoverPunchAngle = 5f;
        const int HoverPunchId = 2;            // để Kill cú punch trước, không đụng tween scale


        [Header("Prefabs")]
        [SerializeField] StackView stackPrefab;
        [SerializeField] BoxView boxPrefab;
        [SerializeField] TileView tilePrefab;
        [SerializeField] GhostView ghostPrefab;

        // Tint palette (GDD §9.1 cũ) đã bỏ 2026-08-17: sprite trạng thái trùng nhóm
        // (TileView.SetMatchState) thay vai trò gợi ý — bg luôn trắng. Ordinal cặp
        // (Option 1/2) là state sticky của view (pairOrdinals), KHÔNG dùng
        // BoxColorIndices — domain giữ nó cùng test phòng khi quay lại tint.

        [Header("Nhịp")]
        [SerializeField] float flyDur = 0.16f;
        [SerializeField] float clearDur = 0.26f;
        [SerializeField] float clearStagger = 0.04f;
        [SerializeField] float cascadeGap = 0.35f;     // nhịp giữa hai bước cascade (§R6)

        [Header("Gộp 4 thẻ thành 1 (COLLAPSE)")]
        [SerializeField] float mergeGather = 0.24f;    // 4 thẻ bay chụm về ô đích mất bao lâu
        [SerializeField] float mergeShrink = 0.45f;    // tới nơi thì co còn bao nhiêu (1 = không co)
        [SerializeField] float mergeStagger = 0.03f;   // lệch nhau chút cho khỏi dính thành một khối
        [SerializeField] float mergeHold = 0.07f;      // khựng lại trước khi thẻ mới nở — nhịp "cộp"
        [SerializeField] float mergeBloom = 0.30f;     // thẻ mới nở ra mất bao lâu
        [SerializeField] float mergeSpin = 140f;       // thẻ mới xoay bao nhiêu độ lúc nở (0 = tắt)

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
        readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

        StackView[] stackViews;
        BoxView[] boxViews;
        readonly Dictionary<string, TileView> tiles = new Dictionary<string, TileView>();

        enum ZoneKind { Tile, Stack }
        struct Zone { public Rect Rect; public ZoneKind Kind; public int Stack; public string Uid; }
        readonly List<Zone> zones = new List<Zone>();

        GhostView ghost;
        int dragFrom = -1;
        string dragUid;
        TileView hoverTile;
        bool locked;

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
        }

        void OnDestroy()
        {
            LevelCommands.LoadRequested -= OnLoadRequested;
        }

        void OnLoadRequested(int index, string json)
        {
            levelIndex = index;
            levelJson = json;
            Load();
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
            }
            catch (Exception e)
            {
                g = null;
                Debug.LogError("Level không hợp lệ — " + e.Message);
                return;
            }
            BuildBoard();
            resultReported = false;
            firstInteractionRaised = false;
            LevelSignals.RaiseStarted(levelIndex, g.TotalGroups);   // tầng meta trừ tim ở đây
            StartCoroutine(Settle());          // hộp nạp sẵn nhóm đủ phải nổ ngay lúc load
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
                    BeginDrag(z.Stack, z.Uid, pt);
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
            else
            {
                Hover(pt);
            }
        }

        // Phím tắt dev: R nạp lại JSON đang cache — không qua AppFlow nên KHÔNG đổi
        // LevelProgressData.CurrentLevel. N và 1-9 đã bỏ: board không còn danh sách
        // level để nhảy tới, muốn đổi màn thì đi qua AppFlow (hoặc cheat panel sau này).
        void HandleKeys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) Load();
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
            var pos = StackWorldPos(g.Stacks[s]);
            for (int i = 0; i < box.Slots.Length; i++)
                if (RectAt(pos + SlotOffset(i), Vector2.one * SlotSize).Contains(pt)) return i;
            return -1;
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

            DOTween.Kill(HoverPunchId, true);             // trả góc quay về 0 trước khi nhấc
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

            foreach (var z in zones)
                if (z.Kind == ZoneKind.Stack && z.Rect.Contains(pt)) { to = z.Stack; break; }

            // Thả ra ngoài, hoặc về chính stack cũ = huỷ thao tác (§R1).
            if (to < 0 || to == from) { SnapBack(uid, dropPos); return; }
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
                FlyTo(tv, boxViews[to].Slot(slot), fromWorld);

            // HAI hộp đổi màu, không chỉ hộp đích: hộp nguồn mất thẻ → cặp có thể tan.
            RefreshTileVisuals(from);
            RefreshTileVisuals(to);
            RefreshZones();
            ReportResultIfFinished();
            LevelSignals.RaiseMoveCommitted(g.Moves);       // tầng meta: vào phase Evaluating
            StartCoroutine(Settle(flyDur));                 // để thẻ hạ cánh rồi mới cascade
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
            tv.transform.DOLocalMove(Vector3.zero, flyDur).SetEase(Ease.OutCubic)
              .SetLink(tv.gameObject)
              .OnComplete(() => { if (tv != null) tv.SetFlying(false); });
        }

        void Hover(Vector2 pt)
        {
            TileView h = null;
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                tiles.TryGetValue(z.Uid, out h);
                break;
            }
            if (h == hoverTile) return;           // chỉ tween lúc VÀO/RA, không mỗi frame
            if (hoverTile != null && !IsLifted(hoverTile))
                hoverTile.transform.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetLink(hoverTile.gameObject);
            hoverTile = h;
            if (hoverTile != null && !IsLifted(hoverTile))
            {
                hoverTile.transform.DOScale(HoverScale, 0.12f).SetEase(Ease.OutBack).SetLink(hoverTile.gameObject);
                // Giật một cái lúc con trỏ vào (CardVisual.PointerEnter). Kill(complete)
                // cú trước để góc quay không cộng dồn khi rê nhanh qua nhiều thẻ.
                DOTween.Kill(HoverPunchId, true);
                hoverTile.transform.DOPunchRotation(Vector3.forward * HoverPunchAngle, 0.12f, 20, 1f)
                         .SetId(HoverPunchId).SetLink(hoverTile.gameObject);
            }
        }

        // Thẻ đang bị "nhấc lên" (scale 0) vì đang kéo. Đừng đụng scale của nó.
        static bool IsLifted(TileView tv) { return tv.transform.localScale.x < 0.01f; }

        void Shake(int stack)
        {
            var bv = boxViews[stack];
            if (bv == null) return;
            bv.transform.DOComplete();            // rung dồn: kết thúc cú trước đã
            bv.transform.DOPunchPosition(new Vector3(0.12f, 0, 0), 0.22f, 6, 0.6f).SetLink(bv.gameObject);
        }

        // ------------------------------------------------------------ cascade
        // Domain mutate từng bước; view animate trên instance đang sống rồi mới cập nhật
        // sổ sách. Khoá input tới khi bàn đứng yên (§E11).

        IEnumerator Settle(float delay = 0f)
        {
            locked = true;
            bool hadCascade = false;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (;;)
            {
                var ev = g.SettleStep(Rules.RemoveEmptyNonBottomBox);
                if (ev.Kind == SettleKind.None) break;
                hadCascade = true;

                if (ev.Kind == SettleKind.Clear)
                {
                    var seq = RemoveTiles(ev.DoomedUids);   // 4 thẻ co về 0, lệch nhau clearStagger
                    if (seq != null) yield return seq.WaitForCompletion();
                    RefreshTileVisuals(ev.Stack);
                }
                if (ev.Kind == SettleKind.Collapse)
                {
                    yield return MergeTiles(ev.Stack, ev.DoomedUids, ev.NewTileUid);
                    RefreshTileVisuals(ev.Stack);
                }
                if (ev.BoxRemoved)
                {
                    yield return FadeBox(ev.Stack).WaitForCompletion();
                    RevealBox(ev.Stack);
                }

                RefreshZones();
                ReportResultIfFinished();
                yield return new WaitForSeconds(cascadeGap);
            }
            RefreshZones();
            ReportResultIfFinished();
            CheckInvariant("settle");
            locked = false;

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

        Sequence RemoveTiles(string[] uids)
        {
            var seq = DOTween.Sequence();
            int n = 0;
            for (int i = 0; i < uids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(uids[i], out tv) || tv == null) continue;
                tiles.Remove(uids[i]);
                var go = tv.gameObject;
                seq.Insert(i * clearStagger,
                           tv.transform.DOScale(0f, clearDur).SetEase(Ease.InBack).SetLink(go)
                             .OnComplete(() => Destroy(go)));
                n++;
            }
            if (n > 0) return seq;
            seq.Kill();
            return null;
        }

        // COLLAPSE nhìn phải ra "gộp", không phải "biến mất rồi mọc lại" — nên 4 thẻ BAY CHỤM về
        // đúng ô mà domain đã đặt thẻ mới, nén về 0 ở đó, rồi thẻ mới bung ra từ chính điểm ấy.
        // Domain đã mutate xong trước khi hàm này chạy, nên thẻ mới đã nằm sẵn trong Slots — tra
        // ra ô của nó chính là cách biết điểm hội tụ.
        IEnumerator MergeTiles(int s, string[] doomedUids, string newUid)
        {
            var box = g.TopBox(s);
            int dest = box == null ? -1 : Array.FindIndex(box.Slots, t => t != null && t.Uid == newUid);
            if (dest < 0)
            {
                // Không tra ra ô đích thì lùi về cách cũ — thà xấu còn hơn nuốt mất thẻ.
                var fallback = RemoveTiles(doomedUids);
                if (fallback != null) yield return fallback.WaitForCompletion();
                SpawnCollapsedTile(s, newUid);
                yield break;
            }

            var destPos = boxViews[s].Slot(dest).position;
            var seq = DOTween.Sequence();
            int n = 0;
            for (int i = 0; i < doomedUids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(doomedUids[i], out tv) || tv == null) continue;
                tiles.Remove(doomedUids[i]);
                var go = tv.gameObject;
                tv.SetFlying(true);                          // bay chụm về ô đích thì nổi lên trên hộp
                float at = i * mergeStagger;

                // InBack: nhích ra ngoài một chút rồi mới lao vào — cú lấy đà làm chuyển động
                // đọc ra là "bị hút vào" thay vì "trượt tới".
                seq.Insert(at, tv.transform.DOMove(destPos, mergeGather)
                                 .SetEase(Ease.InBack, 0.6f).SetLink(go));
                seq.Insert(at, tv.transform.DOScale(mergeShrink, mergeGather)
                                 .SetEase(Ease.InQuad).SetLink(go));
                // Tới nơi thì nén nốt về 0: nhịp "cộp" ngăn giữa lúc 4 thẻ tắt và lúc thẻ mới bung.
                seq.Insert(at + mergeGather,
                           tv.transform.DOScale(0f, Mathf.Max(mergeHold, 0.01f))
                             .SetEase(Ease.InQuad).SetLink(go)
                             .OnComplete(() => Destroy(go)));
                n++;
            }
            if (n == 0) { seq.Kill(); SpawnCollapsedTile(s, newUid); yield break; }

            yield return seq.WaitForCompletion();
            SpawnCollapsedTile(s, newUid);
        }

        // Hộp co lại + mờ dần (GDD §9.3 "Xoá box"). Dùng DOTween.To trên BoxView.SetAlpha
        // chứ không phải sr.DOFade — DOFade nằm trong module Sprite tuỳ chọn của DOTween.
        Sequence FadeBox(int s)
        {
            var bv = boxViews[s];
            var seq = DOTween.Sequence().SetLink(bv.gameObject);
            seq.Join(bv.transform.DOScale(0.9f, clearDur).SetEase(Ease.InQuad));
            seq.Join(DOTween.To(() => 1f, bv.SetAlpha, 0f, clearDur));
            return seq;
        }

        // ------------------------------------------------- thao tác tăng dần
        // Thay cho Rebuild() của bản runtime: mỗi cái đụng đúng phần đã đổi.

        void RevealBox(int s)
        {
            boxViews[s].ResetVisual();
            stackViews[s].ShowDepth(g.Stacks[s].Boxes.Count - 1, TilesInSecondBox(g.Stacks[s]));
            SpawnTiles(s);                                 // thẻ của hộp vừa lộ
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
            tv.transform.localScale = Vector3.zero;

            var go = tv.gameObject;
            var seq = DOTween.Sequence().SetLink(go);
            seq.Join(tv.transform.DOScale(1f, mergeBloom).SetEase(Ease.OutBack));
            if (Mathf.Abs(mergeSpin) > 0.01f)
            {
                tv.transform.localEulerAngles = new Vector3(0f, 0f, mergeSpin);
                seq.Join(tv.transform.DOLocalRotate(Vector3.zero, mergeBloom).SetEase(Ease.OutCubic));
            }
            tiles[t.Uid] = tv;
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
                if (box != null)
                    for (int i = 0; i < box.Slots.Length; i++)
                    {
                        var t = box.Slots[i];
                        if (t == null) continue;
                        zones.Add(new Zone
                        {
                            Rect = RectAt(pos + SlotOffset(i), Vector2.one * SlotSize),
                            Kind = ZoneKind.Tile, Stack = s, Uid = t.Uid
                        });
                    }
                zones.Add(new Zone
                {
                    Rect = RectAt(pos, Vector2.one * BoxSize),
                    Kind = ZoneKind.Stack, Stack = s
                });
            }
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
            RefreshZones();
            ReportResultIfFinished();
            CheckInvariant("build");
        }

        void DestroyBoard()
        {
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
            // Union khung 3x3 với pos thực tế — level lỡ đặt ngoài lưới vẫn không bị cắt.
            float minX = Mathf.Min(0f, (float)g.Stacks.Min(s => s.X));
            float maxX = Mathf.Max(GridCols - 1f, (float)g.Stacks.Max(s => s.X));
            float minY = Mathf.Min(0f, (float)g.Stacks.Min(s => s.Y));
            float maxY = Mathf.Max(GridRows - 1f, (float)g.Stacks.Max(s => s.Y));
            float cx = (minX + maxX) / 2f * PitchX;
            float cy = -(minY + maxY) / 2f * PitchY;
            float halfW = (maxX - minX) / 2f * PitchX + BoxSize / 2f + 0.4f;
            float halfH = (maxY - minY) / 2f * PitchY + BoxSize / 2f + 1.5f;   // chừa HUD trên + gợi ý dưới
            cam.transform.position = new Vector3(cx, cy, -10f);
            cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(cam.aspect, 0.01f));
        }

        // Ruột hộp nằm dưới không bao giờ đổi khi đang nằm dưới (nước đi chỉ đụng top box),
        // nên chỉ cần tính ở đúng 2 chỗ gọi ShowDepth: dựng bàn + lộ hộp mới.
        static int TilesInSecondBox(Stack st)
        {
            return st.Boxes.Count > 1 ? Rules.BoxCapacity - Game.FreeCount(st.Boxes[1]) : 0;
        }

        static Vector2 StackWorldPos(Stack st)
        {
            // data y đi XUỐNG (cùng chiều đọc slot), Unity y đi LÊN → đảo dấu.
            return new Vector2((float)st.X * PitchX, -(float)st.Y * PitchY);
        }

        static Vector2 SlotOffset(int slot)
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
