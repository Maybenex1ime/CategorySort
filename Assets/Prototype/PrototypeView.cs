// ============================================================================
// THROWAWAY PROTOTYPE — view MonoBehaviour. KHÔNG mang sang production.
// Tự bootstrap khi bấm Play (mọi scene). Art: emoji font hệ thống + tên item.
// Kéo-thả: chuột (Input System — project để activeInputHandler=InputSystem only).
// Layout Rev 3: 4 cột slot quanh CỘT GIỮA collector; deck góc phải trên;
// hàng khay dưới đáy = 5 slot trống thường (cùng luật với slot lưới trống).
//
// Game feel kiểu Balatro (tham chiếu D:\Balatro-Feel CardVisual.cs, tự viết
// lerp thay DOTween): ghost lerp đuổi cursor + xoay Z theo độ trễ chuyển động
// + tilt lắc sin/cos + scale pop + shadow; thẻ bay vào collector, snap-back
// khi thả hụt, punch collector khi ăn thẻ, hover phồng nhẹ.
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CategorySort.Prototype
{
    public class PrototypeView : MonoBehaviour
    {
        // Layout (world units)
        const float CardW = 1f, CardH = 1.4f;
        const float CellW = 1.5f, CellH = 2.05f;
        const int GridCols = 4; // 4 cột slot (2 trái + 2 phải), cột giữa dành cho collector
        const int GridRows = 3;

        // Feel (chuyển từ tham số CardVisual.cs sang world-unit)
        const float FollowSpeed = 25f;   // ghost đuổi cursor
        const float RotAmount = 70f;     // độ trễ ngang (world unit) → độ xoay Z
        const float RotSpeed = 20f;
        const float AutoTilt = 7f;       // biên độ lắc sin/cos khi cầm thẻ
        const float TiltSpeed = 12f;
        const float DragScale = 1.15f;
        const float HoverScale = 1.07f;
        const float FlyDur = 0.16f;      // thẻ bay vào collector / snap-back

        static readonly Color[] CatColor =
        {
            new Color(0.85f, 0.25f, 0.25f), // Trái cây
            new Color(0.45f, 0.65f, 0.30f), // Động vật
            new Color(0.25f, 0.45f, 0.85f), // Xe cộ
            new Color(0.90f, 0.60f, 0.15f), // Mặt cười
        };
        static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
        static readonly Color Wood = new Color(0.42f, 0.30f, 0.20f);

        Game g;
        Transform root;
        Sprite white;
        Font emojiFont, labelFont;
        Camera cam;

        enum ZoneKind { SlotTop, EmptySlot, CollectorCell }
        struct Zone { public Rect Rect; public ZoneKind Kind; public int Index; }
        readonly List<Zone> zones = new List<Zone>();
        readonly Dictionary<int, GameObject> slotCards = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> cellCards = new Dictionary<int, GameObject>();

        GameObject ghost;
        Transform ghostTilt;
        Vector3 moveDelta, rotDelta;
        int dragSlot = -1;
        GameObject hoverGo;

        class FlyAnim
        {
            public GameObject Go;
            public Vector3 From, To;
            public float T;
            public bool Shrink;
            public GameObject Reveal;    // thẻ thật đang ẩn, hiện lại khi bay xong
            public GameObject PunchOnEnd;
        }
        readonly List<FlyAnim> flies = new List<FlyAnim>();
        GameObject punchGo;
        float punchT = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => new GameObject("CategorySortPrototype").AddComponent<PrototypeView>();

        void Awake()
        {
#if UNITY_EDITOR
            try { SelfCheck.Run(Debug.Log); } catch (Exception e) { Debug.LogError("SelfCheck FAIL: " + e.Message); }
#endif
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            emojiFont = OsFont("Segoe UI Emoji", "Apple Color Emoji", "Noto Color Emoji");
            labelFont = OsFont("Segoe UI", "Arial");

            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.orthographic = true;
            cam.backgroundColor = Wood;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);

            NewGame();
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

        void NewGame()
        {
            g = Game.Level1();
            Rebuild();
        }

        // ---------------------------------------------------------------- input

        void Update()
        {
            var m = Mouse.current;
            if (m == null) return;
            float dt = Time.deltaTime;
            var wp = cam.ScreenToWorldPoint(m.position.ReadValue());
            var p = new Vector2(wp.x, wp.y);

            if (m.leftButton.wasPressedThisFrame)
            {
                if (g.Status != GameStatus.Playing) { NewGame(); return; }
                foreach (var z in zones)
                {
                    if (z.Kind != ZoneKind.SlotTop || !z.Rect.Contains(p)) continue;
                    dragSlot = z.Index;
                    BuildGhost(g.Top(z.Index), p);
                    if (slotCards.TryGetValue(z.Index, out var src) && src != null)
                        src.transform.localScale = Vector3.zero; // thẻ "được nhấc lên"
                    break;
                }
            }
            else if (ghost != null && m.leftButton.isPressed)
            {
                // SmoothFollow + FollowRotation + CardTilt (công thức CardVisual.cs)
                var target = new Vector3(p.x, p.y, 0);
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

                float s = Mathf.Lerp(ghost.transform.localScale.x, DragScale, 10f * dt);
                ghost.transform.localScale = new Vector3(s, s, 1);
            }
            // ghost còn mà nút không còn giữ (kể cả release cùng frame với press,
            // hoặc mất focus giữa lúc kéo) → xử lý thả + dọn ghost.
            else if (ghost != null)
            {
                var dropPos = ghost.transform.position;
                Destroy(ghost);
                ghost = null;
                int src = dragSlot;
                dragSlot = -1;
                var entry = g.Top(src);
                bool success = false;
                int hitCell = -1, hitSlot = -1;
                foreach (var z in zones)
                {
                    if (!z.Rect.Contains(p)) continue;
                    // Thả trúng zone nào là chốt thao tác ở đó — kể cả gom sai
                    // category (Collect trả false nhưng vẫn trừ move phạt).
                    if (z.Kind == ZoneKind.CollectorCell) { success = g.Collect(src, z.Index); hitCell = z.Index; break; }
                    if (z.Kind == ZoneKind.EmptySlot) { success = g.MoveToSlot(src, z.Index); hitSlot = z.Index; break; }
                }
                Rebuild();
                if (g.Status == GameStatus.Playing && entry != null)
                {
                    if (success && hitCell >= 0)
                        SpawnFly(entry, dropPos, CellPos(hitCell), shrink: true,
                            punch: cellCards.TryGetValue(hitCell, out var cgo) ? cgo : null);
                    else if (success && hitSlot >= 0)
                        SpawnFly(entry, dropPos, SlotPos(hitSlot), reveal: HideCard(hitSlot));
                    else
                        SpawnFly(entry, dropPos, SlotPos(src), reveal: HideCard(src)); // snap-back
                }
            }
            else
            {
                // Hover: thẻ dưới cursor phồng nhẹ (scaleOnHover của CardVisual.cs)
                GameObject h = null;
                foreach (var z in zones)
                    if (z.Kind == ZoneKind.SlotTop && z.Rect.Contains(p) && slotCards.TryGetValue(z.Index, out var go))
                    { h = go; break; }
                if (h != hoverGo && hoverGo != null && !IsRevealPending(hoverGo))
                    hoverGo.transform.localScale = Vector3.one;
                hoverGo = h;
                if (hoverGo != null && !IsRevealPending(hoverGo))
                {
                    float s = Mathf.Lerp(hoverGo.transform.localScale.x, HoverScale, 12f * dt);
                    hoverGo.transform.localScale = new Vector3(s, s, 1);
                }
            }

            AnimateTransients(dt);
        }

        // ------------------------------------------------------- feel helpers

        void BuildGhost(Entry e, Vector2 p)
        {
            ghost = new GameObject("Ghost");
            ghost.transform.SetParent(transform, false);
            ghost.transform.position = new Vector3(p.x, p.y, 0);
            ghostTilt = new GameObject("Tilt").transform;
            ghostTilt.SetParent(ghost.transform, false);
            Quad(ghostTilt, new Vector2(0.14f, -0.18f), new Vector2(CardW + 0.1f, CardH + 0.1f),
                new Color(0, 0, 0, 0.35f), 98); // shadow tách lớp
            BuildCard(ghostTilt, e, p, 100, 1f);
            moveDelta = Vector3.zero;
            rotDelta = Vector3.zero;
        }

        GameObject HideCard(int slot)
        {
            if (!slotCards.TryGetValue(slot, out var go) || go == null) return null;
            go.transform.localScale = Vector3.zero;
            return go;
        }

        void SpawnFly(Entry e, Vector3 from, Vector2 to, bool shrink = false,
            GameObject reveal = null, GameObject punch = null)
        {
            var go = BuildCard(transform, e, from, 90, 1f);
            flies.Add(new FlyAnim { Go = go, From = from, To = new Vector3(to.x, to.y, 0), Shrink = shrink, Reveal = reveal, PunchOnEnd = punch });
        }

        bool IsRevealPending(GameObject go)
        {
            foreach (var f in flies)
                if (f.Reveal == go) return true;
            return false;
        }

        void AnimateTransients(float dt)
        {
            for (int i = flies.Count - 1; i >= 0; i--)
            {
                var f = flies[i];
                f.T += dt / FlyDur;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(f.T), 3f); // ease-out cubic
                if (f.Go != null)
                {
                    f.Go.transform.position = Vector3.Lerp(f.From, f.To, e);
                    if (f.Shrink)
                    {
                        float s = Mathf.Lerp(1f, 0.45f, e);
                        f.Go.transform.localScale = new Vector3(s, s, 1);
                    }
                }
                if (f.T < 1f) continue;
                if (f.Go != null) Destroy(f.Go);
                if (f.Reveal != null) f.Reveal.transform.localScale = Vector3.one;
                if (f.PunchOnEnd != null) { punchGo = f.PunchOnEnd; punchT = 0f; }
                flies.RemoveAt(i);
            }

            if (punchGo != null)
            {
                punchT += dt / 0.18f;
                if (punchT >= 1f)
                {
                    punchGo.transform.localScale = Vector3.one;
                    punchGo = null;
                }
                else
                {
                    float s = 1f + 0.25f * Mathf.Sin(punchT * Mathf.PI); // punch lên rồi hạ
                    punchGo.transform.localScale = new Vector3(s, s, 1);
                }
            }
        }

        void ClearTransients()
        {
            foreach (var f in flies)
                if (f.Go != null) Destroy(f.Go);
            flies.Clear();
            punchGo = null;
            hoverGo = null;
        }

        // ------------------------------------------------------------- drawing

        // Slot lưới i (0..11): 4 cột quanh cột giữa. Slot khay (12..16): hàng dưới đáy.
        Vector2 SlotPos(int i)
        {
            if (i < Knobs.BoardSlots)
            {
                int c = i % GridCols, r = i / GridCols;
                float x = (c < GridCols / 2 ? c - GridCols / 2 : c - GridCols / 2 + 1) * CellW;
                return new Vector2(x, ((GridRows - 1) / 2f - r) * CellH);
            }
            int s = i - Knobs.BoardSlots;
            return new Vector2((s - (Knobs.TraySlots - 1) / 2f) * 1.25f, -(GridRows * CellH) / 2 - 1.15f);
        }

        Vector2 CellPos(int c) => new Vector2(0, ((GridRows - 1) / 2f - c) * CellH);

        void Rebuild()
        {
            ClearTransients();
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("Board").transform;
            root.SetParent(transform, false);
            zones.Clear();
            slotCards.Clear();
            cellCards.Clear();

            float boardH = GridRows * CellH;

            // Nền hàng khay
            var trayCenter = new Vector2(0, -boardH / 2 - 1.15f);
            Quad(root, trayCenter, new Vector2(Knobs.TraySlots * 1.25f + 0.2f, 1.7f), new Color(0, 0, 0, 0.30f), -6);

            // Slots (lưới + khay, cùng luật)
            for (int i = 0; i < g.Slots.Count; i++)
            {
                var pos = SlotPos(i);
                var top = g.Top(i);
                if (top == null)
                {
                    Quad(root, pos, new Vector2(CardW, CardH), new Color(1, 1, 1, 0.13f), -4);
                    zones.Add(new Zone { Rect = RectAt(pos, new Vector2(CardW, CardH)), Kind = ZoneKind.EmptySlot, Index = i });
                    continue;
                }
                int buried = g.Slots[i].Count - 1;
                for (int b = Mathf.Min(buried, 3); b >= 1; b--)
                    Quad(root, pos + new Vector2(0, -0.13f * b), new Vector2(CardW, CardH),
                        new Color(0.55f, 0.5f, 0.45f), -b);
                if (buried > 0)
                    Label(root, pos + new Vector2(0, -CardH / 2 - 0.22f), "+" + buried, 0.55f,
                        new Color(1, 1, 1, 0.7f), 5);
                slotCards[i] = BuildCard(root, top, pos, 1, 1f);
                zones.Add(new Zone { Rect = RectAt(pos, new Vector2(CardW, CardH)), Kind = ZoneKind.SlotTop, Index = i });
            }

            // Cột giữa: các ô collector
            for (int c = 0; c < g.Cells.Length; c++)
            {
                var pos = CellPos(c);
                if (g.Cells[c] == null)
                {
                    Quad(root, pos, new Vector2(CardW + 0.1f, CardH + 0.1f), new Color(0, 0, 0, 0.25f), 0);
                    continue;
                }
                cellCards[c] = BuildCard(root, g.Cells[c], pos, 1, 1f);
                zones.Add(new Zone { Rect = RectAt(pos, new Vector2(CardW, CardH)), Kind = ZoneKind.CollectorCell, Index = c });
            }

            // Deck collector — góc phải trên
            var deckPos = new Vector2((GridCols / 2 + 1) * CellW, boardH / 2 + 1.0f);
            if (g.Deck.Count > 0)
            {
                Quad(root, deckPos + new Vector2(0.07f, -0.07f), new Vector2(CardW * 0.8f, CardH * 0.8f), new Color(0.75f, 0.6f, 0.2f), 0);
                Quad(root, deckPos, new Vector2(CardW * 0.8f, CardH * 0.8f), Gold, 1);
                Label(root, deckPos, "?", 1.0f, new Color(0.35f, 0.22f, 0f), 2);
                Label(root, deckPos + new Vector2(0, -CardH * 0.8f / 2 - 0.25f), "Deck ×" + g.Deck.Count, 0.5f, Color.white, 2);
            }

            // HUD
            Label(root, new Vector2(0, boardH / 2 + 1.0f), "Nước còn: " + g.MovesLeft, 1.1f, Color.white, 5);
            Label(root, new Vector2(0, trayCenter.y - 1.2f), "Kéo thẻ vào ô gom cùng loại (cột giữa), hoặc sang slot trống bất kỳ", 0.5f,
                new Color(1, 1, 1, 0.55f), 5);

            if (g.Status != GameStatus.Playing)
            {
                Quad(root, Vector2.zero, new Vector2(40, 40), new Color(0, 0, 0, 0.65f), 50);
                string msg = g.Status == GameStatus.Won
                    ? "THẮNG!"
                    : g.LoseReason == LoseReason.OutOfMoves ? "THUA — hết nước đi" : "THUA — kẹt bàn";
                Label(root, new Vector2(0, 0.5f), msg, 1.6f, Color.white, 51);
                Label(root, new Vector2(0, -0.7f), "Bấm chuột để chơi lại", 0.7f, new Color(1, 1, 1, 0.8f), 51);
            }

            // Camera fit
            float halfH = boardH / 2 + 3.2f;
            float halfW = (GridCols / 2 + 1.5f) * CellW + 0.5f;
            cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
        }

        GameObject BuildCard(Transform parent, Entry e, Vector2 pos, int order, float alpha)
        {
            var go = new GameObject("Card");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var bg = e.IsCollector ? Gold : Color.white;
            bg.a = alpha;
            var frame = CatColor[(int)e.Cat];
            Quad(go.transform, Vector2.zero, new Vector2(CardW + 0.1f, CardH + 0.1f), frame, order);
            Quad(go.transform, Vector2.zero, new Vector2(CardW, CardH), bg, order + 1);

            if (e.IsCollector)
            {
                Label(go.transform, new Vector2(0, 0.38f), Art.Emoji[(int)e.Cat][0], 0.8f, Color.black, order + 2, emojiFont);
                Label(go.transform, new Vector2(0, -0.05f), e.Progress + "/" + e.Quota, 0.85f, new Color(0.25f, 0.15f, 0f), order + 2);
                Label(go.transform, new Vector2(0, -0.45f), Art.CatName[(int)e.Cat], 0.42f, new Color(0.25f, 0.15f, 0f), order + 2);
            }
            else
            {
                Label(go.transform, new Vector2(0, 0.12f), Art.Emoji[(int)e.Cat][e.Variant], 1.05f, Color.black, order + 2, emojiFont);
                Label(go.transform, new Vector2(0, -0.48f), Art.ItemName[(int)e.Cat][e.Variant], 0.42f, new Color(0.2f, 0.2f, 0.2f), order + 2);
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

        void Label(Transform parent, Vector2 pos, string text, float size, Color color, int order, Font font = null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font != null ? font : labelFont;
            tm.fontSize = 64;
            tm.characterSize = 0.021f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = tm.font.material;
            mr.sortingOrder = order;
        }

        static Rect RectAt(Vector2 center, Vector2 size) => new Rect(center - size / 2, size);
    }
}
