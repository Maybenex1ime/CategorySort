# Board Auto-fit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The board fills the free screen area between the top HUD and the booster bar on every aspect ratio, and re-fits when the screen or safe area changes.

**Architecture:** A pure static `BoardFit` (free-area + ortho math) in `Board/Views`, unit-tested. `BoardController.FitCamera` measures HUD blocker rects in screen pixels, builds the fixed 3×3 board frame in world units, and moves only the camera. `LateUpdate` re-fits on screen/safe-area change. Empty `hudBlockers` keeps the old formula.

**Tech Stack:** Unity 6000.3.8f1, URP 2D, uGUI Canvas (Screen Space Overlay, 1080×1920, match width), NUnit EditMode.

Spec: `docs/superpowers/specs/2026-10-06-board-autofit-design.md`.

## Global Constraints

- Work only in `D:\CategorySort`. Never touch the mukbang project.
- Do not edit `.prefab`, `.unity`, `.asset` on disk; wiring `hudBlockers` is the user's job.
- Board root stays at the origin; only the camera moves.
- `git add` explicit paths only. Commit trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Verify: `./compilecheck.sh` then `bash .git/sdd/testcheck.sh`.

---

### Task 1: BoardFit math

**Files:**
- Create: `Assets/_Game/Board/Views/BoardFit.cs`
- Create: `Assets/_Game/Board/Tests/BoardFitTests.cs`

**Interfaces:**
- Produces: `WordStack.Board.BoardFit.FitOrtho(Rect board, Rect freePx, Vector2 screenPx, out Vector2 camPos, out float orthoSize)` and `Rect FreeArea(Rect safeArea, Vector2 screenPx, IEnumerable<Rect> blockersPx)`.

- [ ] **Step 1: Write the failing test**

```csharp
// BoardFit: khung bàn phải nằm trọn vùng trống giữa HUD, tâm trùng tâm vùng trống.
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class BoardFitTests
    {
        static readonly Rect Board = Rect.MinMaxRect(-1f, -8f, 5f, 1f);   // 6 × 9 world
        const float Eps = 0.5f;   // pixel

        // Khung world → pixel theo camera ortho vừa fit.
        static Rect ToPx(Rect world, Vector2 cam, float size, Vector2 screen)
        {
            float pxPerWorld = screen.y / (2f * size);
            Vector2 min = (world.min - cam) * pxPerWorld + screen / 2f;
            Vector2 max = (world.max - cam) * pxPerWorld + screen / 2f;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void BoardFitsInsideFreeAreaAndIsCentred(float w, float h)
        {
            var screen = new Vector2(w, h);
            var free = Rect.MinMaxRect(0f, h * 0.12f, w, h * 0.85f);
            BoardFit.FitOrtho(Board, free, screen, out var cam, out var size);
            var px = ToPx(Board, cam, size, screen);

            Assert.GreaterOrEqual(px.xMin, free.xMin - Eps);
            Assert.LessOrEqual(px.xMax, free.xMax + Eps);
            Assert.GreaterOrEqual(px.yMin, free.yMin - Eps);
            Assert.LessOrEqual(px.yMax, free.yMax + Eps);
            Assert.AreEqual(free.center.x, px.center.x, Eps);
            Assert.AreEqual(free.center.y, px.center.y, Eps);
            Assert.IsTrue(Mathf.Abs(px.width - free.width) < Eps || Mathf.Abs(px.height - free.height) < Eps,
                "phải chạm một cặp cạnh — không thu nhỏ thừa");
        }

        [Test]
        public void TallScreenIsWidthBound()
        {
            var screen = new Vector2(1080f, 2400f);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var cam, out var size);
            Assert.AreEqual(1080f, ToPx(Board, cam, size, screen).width, Eps);
        }

        [Test]
        public void WideScreenIsHeightBound()
        {
            var screen = new Vector2(1536f, 2048f);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var cam, out var size);
            Assert.AreEqual(2048f, ToPx(Board, cam, size, screen).height, Eps);
        }

        [Test]
        public void EmptyFreeAreaFallsBackToWholeScreen()
        {
            var screen = new Vector2(1080f, 1920f);
            BoardFit.FitOrtho(Board, Rect.MinMaxRect(0f, 900f, 1080f, 800f), screen, out var camA, out var sizeA);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var camB, out var sizeB);
            Assert.AreEqual(sizeB, sizeA, 1e-4f);
            Assert.AreEqual(camB.y, camA.y, 1e-4f);
        }

        [Test]
        public void FreeAreaStopsAtTheNearestBlockers()
        {
            var screen = new Vector2(1080f, 1920f);
            var free = BoardFit.FreeArea(new Rect(Vector2.zero, screen), screen, new[]
            {
                Rect.MinMaxRect(0f, 1700f, 1080f, 1920f),   // dải HUD trên
                Rect.MinMaxRect(400f, 1650f, 700f, 1760f),  // LevelBox thò xuống thấp hơn
                Rect.MinMaxRect(0f, 0f, 1080f, 250f),       // thanh booster
            });
            Assert.AreEqual(1650f, free.yMax);
            Assert.AreEqual(250f, free.yMin);
            Assert.AreEqual(0f, free.xMin);
            Assert.AreEqual(1080f, free.xMax);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected: board-tests FAIL, `BoardFit` not found.

- [ ] **Step 3: Implement**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace WordStack.Board
{
    // Toán fit camera ortho: khung bàn (world) nằm trọn vùng trống (pixel) giữa HUD, tâm bàn
    // rơi đúng tâm vùng trống. Tách khỏi BoardController để test được không cần scene.
    public static class BoardFit
    {
        public static void FitOrtho(Rect board, Rect freePx, Vector2 screenPx, out Vector2 camPos, out float orthoSize)
        {
            float h = Mathf.Max(screenPx.y, 1f);
            if (freePx.width <= 0f || freePx.height <= 0f)
                freePx = new Rect(0f, 0f, Mathf.Max(screenPx.x, 1f), h);

            orthoSize = Mathf.Max(board.height * h / (2f * freePx.height), board.width * h / (2f * freePx.width));
            float worldPerPx = 2f * orthoSize / h;
            camPos = board.center - (freePx.center - screenPx / 2f) * worldPerPx;
        }

        // Blocker có tâm ở nửa trên đẩy mép trên xuống, nửa dưới đẩy mép dưới lên.
        public static Rect FreeArea(Rect safeArea, Vector2 screenPx, IEnumerable<Rect> blockersPx)
        {
            float top = safeArea.yMax, bottom = safeArea.yMin;
            foreach (var b in blockersPx)
            {
                if (b.center.y >= screenPx.y / 2f) top = Mathf.Min(top, b.yMin);
                else bottom = Mathf.Max(bottom, b.yMax);
            }
            return Rect.MinMaxRect(safeArea.xMin, bottom, safeArea.xMax, top);
        }
    }
}
```

- [ ] **Step 4: Verify**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected OK. Unity Test Runner `BoardFitTests` — Expected: 7 pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Views/BoardFit.cs Assets/_Game/Board/Tests/BoardFitTests.cs
git commit -m "Board: BoardFit math to fit the board between HUD blockers"
```

### Task 2: FitCamera uses HUD blockers and re-fits

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (serialized fields near `[Header("Booster")]`; build call ~line 1861; `FitCamera` ~line 1894; new `LateUpdate`, `OnValidate`, `BlockerRects`)

**Interfaces:**
- Consumes: `BoardFit.FitOrtho`, `BoardFit.FreeArea` (Task 1).

- [ ] **Step 1: Fields**

```csharp
[Header("Fit bàn vào khoảng trống giữa HUD")]
// Ô HUD che bàn — trên: CoinArea, LevelBox, Settings, Progress Bar; dưới: Box BG + Booster Button.
// Rỗng = lề cố định kiểu cũ.
[SerializeField] RectTransform[] hudBlockers;
[SerializeField] float fitPadding = 0.15f;      // world unit, mỗi cạnh
[SerializeField] float fitExtraBelow = 1.0f;    // chỗ cho lớp lấp ló + Tile Holder dưới hộp hàng cuối — núm chỉnh tay

bool fitDirty;
Vector2Int fittedScreen;
Rect fittedSafeArea;
```

- [ ] **Step 2: Build call**

After the existing `FitCamera();` in the build method add `fitDirty = true;   // HUD có thể layout xong muộn một frame — fit lại ở LateUpdate`.

- [ ] **Step 3: Replace FitCamera, add re-fit**

```csharp
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
    if (g == null || cam == null) return;
    if (fitDirty || Screen.width != fittedScreen.x || Screen.height != fittedScreen.y || Screen.safeArea != fittedSafeArea)
        FitCamera();
}

void OnValidate() { fitDirty = true; }   // chỉnh padding trong Inspector thấy ngay
```

- [ ] **Step 4: Verify**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected OK. Selfcheck unchanged (Domain untouched).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs
git commit -m "Board: fit the camera between HUD blockers and re-fit on screen change"
```

### Task 3 (user, in Unity): wiring

1. `Main.unity` → `BoardController.hudBlockers`: CoinArea, LevelBox, Settings Button, Progress Bar, Box BG, Booster Button (1..3).
2. Recommended: re-anchor Progress Bar in `GamePlayUIRoot .prefab` to `(0.5, 1)`.
3. Check 9:16, 20:9, 4:3 with the Cheat screen-size dropdown; tune `fitExtraBelow` / `fitPadding`.
