// ============================================================================
// THROWAWAY PROTOTYPE — view MonoBehaviour. KHÔNG mang sang production.
// Tự bootstrap khi bấm Play (mọi scene), vẽ hoàn toàn bằng code.
//
// Hành vi tham chiếu: demo/wordstack-clear-demo.html. Lệch chỗ nào là bug chỗ đó.
// Luật nằm hết ở PrototypeDomain.cs — file này chỉ vẽ, bắt input, và tạo nhịp cascade.
//
// Game feel kiểu Balatro (tham chiếu D:\Balatro-Feel CardVisual.cs, tự viết lerp thay
// DOTween): ghost lerp đuổi cursor + xoay Z theo độ trễ chuyển động + tilt lắc sin/cos
// + scale pop + shadow; thẻ bay về chỗ cũ khi thả hụt, hộp rung khi từ chối, hover phồng nhẹ.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace WordStack.Prototype
{
    public class PrototypeView : MonoBehaviour
    {
        // ---- Layout (world units) ----
        const float BoxSize = 1.6f;
        const float BoxBorder = 0.08f;
        const float BoxPad = 0.09f;
        const float SlotGap = 0.08f;
        const float PeekStep = 0.10f;          // mỗi lớp hộp ẩn tụt xuống bao nhiêu
        const int PeekMax = 3;
        static float SlotSize { get { return (BoxSize - 2 * BoxPad - SlotGap) / 2f; } }
        static float PitchX { get { return BoxSize + 0.28f; } }
        static float PitchY { get { return BoxSize + 0.62f; } }   // chừa chỗ cho lớp lấp ló

        // ---- Feel ----
        const float FollowSpeed = 25f;
        const float RotAmount = 70f;
        const float RotSpeed = 20f;
        const float AutoTilt = 7f;
        const float TiltSpeed = 12f;
        const float DragScale = 1.15f;
        const float HoverScale = 1.07f;
        const float FlyDur = 0.16f;
        const float ClearDur = 0.26f;
        const float ClearStagger = 0.04f;
        // Bề rộng trung bình một ký tự = CharW × size. Đo thật trong Play mode bằng
        // Renderer.bounds trên TextMesh; lấy giá trị LỚN nhất đo được để chữ không bao giờ tràn.
        const float CharW = 0.085f;
        const float CascadeGap = 0.35f;        // nhịp giữa hai bước cascade (§R6)

        // ---- Bảng màu (GDD §9.1) ----
        static readonly Color Bg = Hex(0x3A2E5F);
        static readonly Color HeaderCol = Hex(0x6A5BA5);
        static readonly Color BoxBg = Hex(0x6B5CA8);
        static readonly Color BoxEdge = Hex(0xA5A5A5);
        static readonly Color BoxEdgeBottom = Hex(0x8B8B8B);
        static readonly Color PeekBg = Hex(0x544593);
        static readonly Color TileBg = Hex(0xDEDEDE);
        static readonly Color Ink = Hex(0x111111);
        // Palette gợi ý nhóm — domain trả index vào mảng này (§7.1)
        static readonly Color[] Palette =
        {
            Hex(0xF4B740), Hex(0x5BC98C), Hex(0xEF7C8E),
            Hex(0x4FA8E8), Hex(0xB48CE8), Hex(0xE88C4F),
        };

        Game g;
        List<string> levelJsons = new List<string>();
        int levelIndex;

        Transform root;
        Sprite white;
        Font labelFont;
        Camera cam;
        readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

        enum ZoneKind { Tile, Stack }
        struct Zone { public Rect Rect; public ZoneKind Kind; public int Stack; public string Uid; }
        readonly List<Zone> zones = new List<Zone>();
        readonly Dictionary<string, GameObject> tileGo = new Dictionary<string, GameObject>();
        readonly Dictionary<int, GameObject> boxGo = new Dictionary<int, GameObject>();

        GameObject ghost;
        Transform ghostTilt;
        Vector3 moveDelta, rotDelta;
        int dragFrom = -1;
        string dragUid;
        GameObject hoverGo;
        int highlightStack = -1;
        bool locked;

        // The "bay" tam: snap-back khi tha hut, hoac bay vao slot dich. DOTween lo chuyen
        // dong; list nay chi de don sach khi doi level. Destroy huy luon tween nho SetLink.
        readonly List<GameObject> flies = new List<GameObject>();

        // Bản chờ xoá: giữ để đối chiếu hành vi tới khi bàn dựng-từ-prefab nghiệm thu xong.
        // Nhường sân ngay khi scene đã có BoardController — nếu không thì Main.unity sẽ có
        // hai bàn chồng lên nhau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // Include: mặc định FindAnyObjectByType BỎ QUA object inactive, nên tắt
            // GameObject "Game" để dừng bàn mới lại khiến bản prototype này tự dựng
            // bàn cũ thay vào — đúng thứ vừa bị tắt.
            if (FindAnyObjectByType<BoardController>(FindObjectsInactive.Include) != null) return;
            new GameObject("WordStackPrototype").AddComponent<PrototypeView>();
        }

        // ---------------------------------------------------------------- boot

        void Awake()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            labelFont = OsFont("Segoe UI", "Arial");

            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.backgroundColor = Bg;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);

            foreach (var ta in Resources.LoadAll<TextAsset>("Levels").OrderBy(t => t.name, StringComparer.Ordinal))
                levelJsons.Add(ta.text);

#if UNITY_EDITOR
            // Cùng bộ assert với ./selfcheck.sh — chạy luôn lúc Play để bắt drift sớm.
            try { SelfCheck.Run(Debug.Log, levelJsons, HasArt); }
            catch (Exception e) { Debug.LogError("SelfCheck FAIL: " + e.Message); }
#endif
            Load(0);
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

        static Font OsFont(params string[] names)
        {
            foreach (var n in names)
            {
                var f = Font.CreateDynamicFontFromOSFont(n, 64);
                if (f != null) return f;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Load(int i)
        {
            StopAllCoroutines();
            ClearTransients();
            locked = false;
            if (levelJsons.Count == 0)
            {
                Fatal("Không thấy level nào trong Assets/Prototype/Resources/Levels/");
                return;
            }
            levelIndex = ((i % levelJsons.Count) + levelJsons.Count) % levelJsons.Count;
            try
            {
                var lv = LevelData.Parse(levelJsons[levelIndex]);
                lv.Validate(HasArt);
                g = Game.Build(lv);
            }
            catch (Exception e)
            {
                g = null;
                Fatal("Level không hợp lệ — " + e.Message);
                return;
            }
            Rebuild();
            StartCoroutine(Settle());          // hộp nạp sẵn nhóm đủ phải nổ ngay lúc load
        }

        void Fatal(string msg)
        {
            Debug.LogError(msg);
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("Board").transform;
            root.SetParent(transform, false);
            zones.Clear();
            Label(root, Vector2.zero, msg, 1.2f, Color.white, 10, 12f);
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
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

            if (g.Status != GameStatus.Playing)
            {
                if (p.press.wasPressedThisFrame)
                    Load(g.Status == GameStatus.Won ? levelIndex + 1 : levelIndex);
                return;
            }

            if (locked) return;

            if (p.press.wasPressedThisFrame && ghost == null)
            {
                foreach (var z in zones)
                {
                    if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                    dragFrom = z.Stack;
                    dragUid = z.Uid;
                    BuildGhost(FindTile(z.Uid), pt);
                    GameObject go;
                    if (tileGo.TryGetValue(z.Uid, out go) && go != null)
                        go.transform.localScale = Vector3.zero;   // thẻ "được nhấc lên"
                    break;
                }
            }
            else if (ghost != null && p.press.isPressed)
            {
                DragGhost(pt, dt);
                Highlight(TargetStack(pt));
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

        void HandleKeys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) { Load(levelIndex); return; }
            if (k.nKey.wasPressedThisFrame) { Load(levelIndex + 1); return; }
            for (int i = 0; i < Math.Min(9, levelJsons.Count); i++)
                if (k[Key.Digit1 + i].wasPressedThisFrame) { Load(i); return; }
        }

        Tile FindTile(string uid)
        {
            foreach (var st in g.Stacks)
                foreach (var t in st.Boxes[0].Slots)
                    if (t != null && t.Uid == uid) return t;
            return null;
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

        void Drop(Vector2 pt)
        {
            var dropPos = ghost.transform.position;
            Destroy(ghost);
            ghost = null;
            Highlight(-1);
            int from = dragFrom, to = -1;
            string uid = dragUid;
            dragFrom = -1;
            dragUid = null;
            var tile = FindTile(uid);

            foreach (var z in zones)
                if (z.Kind == ZoneKind.Stack && z.Rect.Contains(pt)) { to = z.Stack; break; }

            // Thả ra ngoài, hoặc về chính stack cũ = huỷ thao tác (§R1).
            if (to < 0 || to == from)
            {
                SnapBack(tile, dropPos, from, uid);
                return;
            }
            if (!g.MoveTile(from, uid, to))
            {
                Shake(to);                                  // box đích đầy (§E1)
                SnapBack(tile, dropPos, from, uid);
                return;
            }
            Rebuild();
            StartCoroutine(Settle());
        }

        void SnapBack(Tile tile, Vector3 from, int stack, string uid)
        {
            GameObject go;
            if (tileGo.TryGetValue(uid, out go) && go != null && tile != null)
            {
                go.transform.localScale = Vector3.zero;
                SpawnFly(tile, from, go.transform.position, go);
            }
        }

        // ------------------------------------------------------- feel helpers

        void DragGhost(Vector2 pt, float dt)
        {
            var target = new Vector3(pt.x, pt.y, 0);
            ghost.transform.position = Vector3.Lerp(ghost.transform.position, target, FollowSpeed * dt);
            var movement = ghost.transform.position - target;
            moveDelta = Vector3.Lerp(moveDelta, movement, 25f * dt);
            rotDelta = Vector3.Lerp(rotDelta, moveDelta * RotAmount, RotSpeed * dt);
            ghost.transform.eulerAngles = new Vector3(0, 0, Mathf.Clamp(rotDelta.x, -40f, 40f));

            float sine = Mathf.Sin(Time.time * 2f) * AutoTilt;
            float cosine = Mathf.Cos(Time.time * 2f) * AutoTilt;
            var e = ghostTilt.localEulerAngles;
            ghostTilt.localEulerAngles = new Vector3(
                Mathf.LerpAngle(e.x, sine, TiltSpeed * dt),
                Mathf.LerpAngle(e.y, cosine, TiltSpeed * dt), 0);

        }

        void BuildGhost(Tile t, Vector2 pt)
        {
            ghost = new GameObject("Ghost");
            ghost.transform.SetParent(transform, false);
            ghost.transform.position = new Vector3(pt.x, pt.y, 0);
            ghostTilt = new GameObject("Tilt").transform;
            ghostTilt.SetParent(ghost.transform, false);
            Quad(ghostTilt, new Vector2(0.05f, -0.06f), Vector2.one * (SlotSize + 0.06f),
                 new Color(0, 0, 0, 0.35f), 98);
            BuildTile(ghostTilt, t, Vector2.zero, TileBg, 100);
            ghost.transform.DOScale(DragScale, 0.12f).SetEase(Ease.OutQuad).SetLink(ghost);
            moveDelta = Vector3.zero;
            rotDelta = Vector3.zero;
        }

        void Hover(Vector2 pt)
        {
            GameObject h = null;
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                tileGo.TryGetValue(z.Uid, out h);
                break;
            }
            if (h == hoverGo) return;             // chi tween luc VAO/RA, khong moi frame
            if (hoverGo != null && !IsLifted(hoverGo))
                hoverGo.transform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad).SetLink(hoverGo);
            hoverGo = h;
            if (hoverGo != null && !IsLifted(hoverGo))
                hoverGo.transform.DOScale(HoverScale, 0.12f).SetEase(Ease.OutQuad).SetLink(hoverGo);
        }

        // The dang bi "nhac len" (scale 0): dang keo, hoac dang co ban bay thay the no.
        // Dung dung scale cua no, khong thi no hien lai giua chung.
        static bool IsLifted(GameObject go) { return go.transform.localScale.x < 0.01f; }

        void Highlight(int stack)
        {
            if (stack == highlightStack) return;
            highlightStack = stack;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                GameObject go;
                if (!boxGo.TryGetValue(s, out go) || go == null) continue;
                var edge = go.transform.GetChild(0).GetComponent<SpriteRenderer>();
                edge.color = s == stack ? Color.white
                          : (g.TopBox(s).IsBottom ? BoxEdgeBottom : BoxEdge);
            }
        }

        void Shake(int stack)
        {
            GameObject go;
            if (!boxGo.TryGetValue(stack, out go) || go == null) return;
            go.transform.DOComplete();            // rung don: ket thuc cu truoc da
            go.transform.DOPunchPosition(new Vector3(0.12f, 0, 0), 0.22f, 6, 0.6f).SetLink(go);
        }

        void SpawnFly(Tile t, Vector3 from, Vector3 to, GameObject reveal)
        {
            var go = new GameObject("Fly");
            go.transform.SetParent(transform, false);
            go.transform.position = from;
            BuildTile(go.transform, t, Vector2.zero, TileBg, 90);
            flies.Add(go);
            go.transform.DOMove(to, FlyDur).SetEase(Ease.OutCubic).SetLink(go)
              .OnComplete(() =>
              {
                  if (reveal != null) reveal.transform.localScale = Vector3.one;
                  flies.Remove(go);
                  Destroy(go);
              });
        }

        void ClearFlies()
        {
            foreach (var go in flies) if (go != null) Destroy(go);
            flies.Clear();
        }

        void ClearTransients()
        {
            ClearFlies();
            if (ghost != null) { Destroy(ghost); ghost = null; }
            hoverGo = null;
            highlightStack = -1;
            dragFrom = -1;
            dragUid = null;
        }

        // ------------------------------------------------------------ cascade
        // Domain mutate từng bước; view animate trên GameObject của lần Rebuild TRƯỚC
        // đó rồi mới dựng lại. Khoá input tới khi bàn đứng yên (§E11).

        IEnumerator Settle()
        {
            locked = true;
            for (;;)
            {
                var ev = g.SettleStep(Rules.RemoveEmptyNonBottomBox);
                if (ev.Kind == SettleKind.None) break;

                if (ev.Kind == SettleKind.Clear)
                {
                    // 4 the co ve 0, lech nhau ClearStagger (GDD 9.3 "CLEAR")
                    var seq = DOTween.Sequence();
                    int n = 0;
                    for (int i = 0; i < ev.DoomedUids.Length; i++)
                    {
                        GameObject go;
                        if (!tileGo.TryGetValue(ev.DoomedUids[i], out go) || go == null) continue;
                        seq.Insert(i * ClearStagger,
                                   go.transform.DOScale(0f, ClearDur).SetEase(Ease.InBack).SetLink(go));
                        n++;
                    }
                    if (n > 0) yield return seq.WaitForCompletion();
                    else seq.Kill();
                }
                else                                            // RemoveBox
                {
                    GameObject go;
                    if (boxGo.TryGetValue(ev.Stack, out go) && go != null)
                    {
                        // hộp co lại + mờ dần (GDD §9.3 "Xoá box").
                        // Dùng DOTween.ToAlpha (core) chứ không phải sr.DOFade — DOFade nằm
                        // trong module Sprite tuỳ chọn, tránh phụ thuộc vào việc Setup có bật nó.
                        var seq = DOTween.Sequence().SetLink(go);
                        seq.Join(go.transform.DOScale(0.9f, ClearDur).SetEase(Ease.InQuad));
                        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        {
                            var r = sr;
                            seq.Join(DOTween.ToAlpha(() => r.color, c => r.color = c, 0f, ClearDur));
                        }
                        yield return seq.WaitForCompletion();
                    }
                }

                Rebuild();
                yield return new WaitForSeconds(CascadeGap);
            }
            Rebuild();
            locked = false;
        }

        // ------------------------------------------------------------- drawing

        Vector2 StackWorldPos(Stack st)
        {
            // data y đi XUỐNG (cùng chiều đọc slot), Unity y đi LÊN → đảo dấu.
            return new Vector2((float)st.X * PitchX, -(float)st.Y * PitchY);
        }

        Vector2 SlotWorldPos(Vector2 boxPos, int slot)
        {
            int c = slot % 2, r = slot / 2;
            float step = SlotSize + SlotGap;
            return boxPos + new Vector2((c - 0.5f) * step, (0.5f - r) * step);
        }

        void Rebuild()
        {
            ClearTransientsKeepGhost();
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("Board").transform;
            root.SetParent(transform, false);
            zones.Clear();
            tileGo.Clear();
            boxGo.Clear();
            if (g == null) return;

            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var st = g.Stacks[s];
                var pos = StackWorldPos(st);

                // lớp hộp ẩn: TỤT XUỐNG để hở mép dưới, lớp sâu vẽ trước nên nằm sau
                int hidden = st.Boxes.Count - 1;
                for (int d = Mathf.Min(hidden, PeekMax); d >= 1; d--)
                {
                    var pp = pos + new Vector2(0, -PeekStep * d);
                    Quad(root, pp, Vector2.one * (BoxSize + BoxBorder * 2), BoxEdge, -10 - d);
                    Quad(root, pp, Vector2.one * BoxSize, PeekBg, -9 - d);
                }
                if (hidden > PeekMax)
                    Label(root, pos + new Vector2(0, -BoxSize / 2 - PeekStep * PeekMax - 0.18f),
                          "+" + hidden, 1.0f, new Color(1, 1, 1, 0.7f), 5, 1.2f);

                var box = g.TopBox(s);
                var bo = new GameObject("Box" + s);
                bo.transform.SetParent(root, false);
                bo.transform.position = new Vector3(pos.x, pos.y, 0);
                boxGo[s] = bo;
                // child 0 = viền (Highlight đổi màu chính nó)
                Quad(bo.transform, Vector2.zero, Vector2.one * (BoxSize + BoxBorder * 2),
                     box.IsBottom ? BoxEdgeBottom : BoxEdge, 0);
                Quad(bo.transform, Vector2.zero, Vector2.one * BoxSize, BoxBg, 1);

                var colors = Game.BoxColorIndices(box);
                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var sp = SlotWorldPos(Vector2.zero, i);
                    Quad(bo.transform, sp, Vector2.one * SlotSize, new Color(0, 0, 0, 0.14f), 2);
                    var t = box.Slots[i];
                    if (t == null) continue;
                    int ci;
                    var bg = colors.TryGetValue(t.Uid, out ci) ? Palette[ci] : TileBg;
                    tileGo[t.Uid] = BuildTile(bo.transform, t, sp, bg, 3);
                    zones.Add(new Zone
                    {
                        Rect = RectAt(pos + sp, Vector2.one * SlotSize),
                        Kind = ZoneKind.Tile, Stack = s, Uid = t.Uid
                    });
                }
                zones.Add(new Zone
                {
                    Rect = RectAt(pos, Vector2.one * BoxSize),
                    Kind = ZoneKind.Stack, Stack = s
                });
            }

            DrawHud();
            FitCamera();
        }

        void ClearTransientsKeepGhost()
        {
            ClearFlies();
            hoverGo = null;
            highlightStack = -1;
        }

        void DrawHud()
        {
            float minY = g.Stacks.Min(s => (float)s.Y), maxY = g.Stacks.Max(s => (float)s.Y);
            float minX = g.Stacks.Min(s => (float)s.X), maxX = g.Stacks.Max(s => (float)s.X);
            float top = -minY * PitchY + BoxSize / 2f;
            float bottom = -maxY * PitchY - BoxSize / 2f;
            float cx = (minX + maxX) / 2f * PitchX;
            float width = (maxX - minX) * PitchX + BoxSize;

            Quad(root, new Vector2(cx, top + 0.58f), new Vector2(width, 0.55f), HeaderCol, 5);
            Label(root, new Vector2(cx, top + 0.58f),
                  g.Title + "   ·   " + g.Cleared + "/" + g.TotalGroups + " groups   ·   " + g.Moves + " moves",
                  1.00f, Color.white, 6, width - 0.36f);
            Label(root, new Vector2(cx, bottom - 0.45f),
                  "Drag a tile onto another stack   ·   R restart   ·   N next level",
                  0.85f, new Color(1, 1, 1, 0.55f), 6, width);

            if (g.Status == GameStatus.Won)
            {
                Quad(root, new Vector2(cx, 0), new Vector2(60, 60), new Color(0.08f, 0.05f, 0.16f, 0.82f), 50);
                Label(root, new Vector2(cx, 0.45f), "Complete!", 3.0f, Color.white, 51, 6f);
                Label(root, new Vector2(cx, -0.6f), "click / N for next level", 1.2f,
                      new Color(1, 1, 1, 0.8f), 51, 12f);
            }
            else if (g.Status == GameStatus.Stuck)
            {
                Quad(root, new Vector2(cx, bottom - 1.15f), new Vector2(width * 0.9f, 0.7f),
                     new Color(0.11f, 0.09f, 0.2f, 0.95f), 50);
                Label(root, new Vector2(cx, bottom - 1.15f), "No moves left — click or press R to restart",
                      1.0f, Color.white, 51, width * 0.85f);
            }
        }

        void FitCamera()
        {
            float minX = g.Stacks.Min(s => (float)s.X), maxX = g.Stacks.Max(s => (float)s.X);
            float minY = g.Stacks.Min(s => (float)s.Y), maxY = g.Stacks.Max(s => (float)s.Y);
            float cx = (minX + maxX) / 2f * PitchX;
            float cy = -(minY + maxY) / 2f * PitchY;
            float halfW = (maxX - minX) / 2f * PitchX + BoxSize / 2f + 0.4f;
            float halfH = (maxY - minY) / 2f * PitchY + BoxSize / 2f + 1.5f;   // chừa HUD trên + gợi ý dưới
            cam.transform.position = new Vector3(cx, cy, -10);
            cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(cam.aspect, 0.01f));
        }

        // Ba trường hợp như demo: chỉ ảnh / chỉ chữ / cả hai.
        GameObject BuildTile(Transform parent, Tile t, Vector2 pos, Color bg, int order)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            Quad(go.transform, Vector2.zero, Vector2.one * SlotSize, bg, order);
            if (t == null) return go;

            var art = t.Art != null ? LoadArt(t.Art) : null;
            bool both = art != null && t.Text != null;

            if (art != null)
            {
                float target = SlotSize * (both ? 0.46f : 0.72f);
                var sg = new GameObject("Art");
                sg.transform.SetParent(go.transform, false);
                sg.transform.localPosition = new Vector3(0, both ? SlotSize * 0.14f : 0f, 0);
                var sr = sg.AddComponent<SpriteRenderer>();
                sr.sprite = art;
                sr.sortingOrder = order + 1;
                var b = art.bounds.size;
                float k = target / Mathf.Max(Mathf.Max(b.x, b.y), 0.0001f);
                sg.transform.localScale = new Vector3(k, k, 1);
            }
            if (t.Text != null)
            {
                float y = both ? -SlotSize * 0.26f : 0f;
                float size = both ? 0.85f : 1.10f;
                Label(go.transform, new Vector2(0, y), t.Text, size, Ink, order + 1, SlotSize * 0.92f);
            }
            return go;
        }

        void Quad(Transform parent, Vector2 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject("Quad");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = white;
            sr.color = color;
            sr.sortingOrder = order;
        }

        // maxWidth: co chữ theo bề rộng cho phép + xuống dòng ở khoảng trắng (§E10).
        void Label(Transform parent, Vector2 pos, string text, float size, Color color,
                   int order, float maxWidth)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var tm = go.AddComponent<TextMesh>();

            // CharW đo thật trong Play mode bằng Renderer.bounds (xem hằng ở đầu class).
            // Chỉ CO lại, không bao giờ phóng to.
            // KHÔNG tự ngắt dòng: ngắt ở mọi khoảng trắng biến câu HUD thành cột dọc một
            // từ mỗi dòng. Chuỗi dài thì co chữ, không xé câu.
            float fit = maxWidth / Mathf.Max(text.Length * CharW, 0.0001f);
            float chosen = Mathf.Min(size, fit);

            tm.text = text;
            tm.font = labelFont;
            tm.fontSize = 64;
            tm.characterSize = 0.021f * chosen;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().material = tm.font.material;
            go.GetComponent<MeshRenderer>().sortingOrder = order;
        }

        static Rect RectAt(Vector2 center, Vector2 size) { return new Rect(center - size / 2f, size); }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
