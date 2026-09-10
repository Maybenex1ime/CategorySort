# Blocker nhịp 2 — Tầng hình: chỗ trống cho art, logic đầy đủ

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bàn chơi trong Unity biết bàn có blocker: không cho chạm thứ không chạm được, và gọi đúng chỗ để cập nhật hiển thị. Art do người dùng tự làm sau, nhịp này chỉ để sẵn chỗ trống.

**Architecture:** Quyết định "chạm được hay không" đã sống trong `Game.IsPullable(Tile, Box)` từ nhịp 1, nên tầng hình không tự suy luật — `RefreshZones` chỉ hỏi nó. `TileView` và `BoxView` nhận thêm một hàm nhận trạng thái blocker; mọi tham chiếu art là `[SerializeField]` để trống, và hàm không làm gì khi chưa gán. `BoardController` gọi làm mới toàn bàn ở bốn chỗ trạng thái blocker có thể đổi.

**Tech Stack:** C# / Unity 6000.x · DOTween · Roslyn qua Unity Hub cho hai script kiểm.

**Spec:** `docs/superpowers/specs/2026-09-10-blocker-locks-design.md` (Mục 8, nhịp 2)

## Global Constraints

- Nhịp này **không tạo và không sửa art, prefab, hay scene**. Người dùng tự làm phần nhìn. Mọi thứ mới là `[SerializeField]` để trống, code phải chạy đúng khi tất cả đều null.
- `Assets/_Game/Board/Views/` **được** import `UnityEngine` (khác `Domain/`). Không đưa luật mới vào đây — luật blocker đã xong ở nhịp 1, tầng hình chỉ hỏi.
- Không thêm luật vào `Domain/`. Nếu thấy cần một phép kiểm mới, dùng lại `IsPullable` / `IsOpen` / `IsFrozen`.
- `Rules.BoxCapacity = 4`, `Rules.GroupSize = 4`. Không hardcode.
- Thẻ băng và thẻ trong hộp đóng: **không hover, không nhấc**. Thả **vào** hộp đóng thì vẫn cho thả và để hộp rung báo từ chối — đó là phản hồi rõ hơn im lặng.
- `TileView` giữ nguyên nguyên tắc "kích thước và vị trí author trong prefab, code không đụng scale".
- Chỉ `SetFlying` được đụng `sortingOrder`. Đừng thêm chỗ thứ hai.

---

## Vòng phản hồi

Tầng hình **không có test tự động** trong repo này: `selfcheck.sh` chỉ compile `Domain/`, và NUnit không chạy được từ dòng lệnh. Nên nhịp này dựa vào ba mức:

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `./compilecheck.sh` | compile `game` / `editor` / `meta` | `game.dll OK` `editor.dll OK` `meta.dll OK` |
| `./selfcheck.sh Temp/selfcheck-levels` | luật bàn không hồi quy | `SelfCheck OK - 2 level, luật khớp demo/check.mjs` |
| Mở Unity, bấm phím `B` rồi chơi | phần nhìn và input thật | xem Task 3 |

Thư mục `Temp/selfcheck-levels` dựng ở nhịp 1; thiếu thì tạo lại:

```bash
mkdir -p Temp/selfcheck-levels
cp Assets/_Game/Content/Levels/lv-001.json Temp/selfcheck-levels/a-lv-001.json
cp Assets/_Game/Content/Levels/lv-001.json Temp/selfcheck-levels/b-lv-001.json
```

Commit: nếu classifier chặn `git add`/`git commit` ở Bash thì chạy qua PowerShell với `git -C D:\CategorySort ...`. Message tiếng Anh, mệnh lệnh, kết thúc bằng `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

---

### Task 1: Chỗ trống cho hình blocker trên thẻ và hộp

**Files:**
- Modify: `Assets/_Game/Board/Views/TileView.cs`
- Modify: `Assets/_Game/Board/Views/BoxView.cs`

**Interfaces:**
- Produces:
  - `void TileView.SetIce(bool frozen, int movesLeft)`
  - `void BoxView.SetLock(bool closed, string label)`
  - Cả hai no-op khi field art chưa gán.

- [ ] **Step 1: Thêm `SetIce` vào `TileView`**

Thêm field vào cụm `[Header]` mới, đặt sau nhóm sprite nền hiện có:

```csharp
        // Blocker — CHƯA GÁN, người dùng tự làm phần nhìn (nhịp 2). Tất cả nullable:
        // chưa kéo gì vào thì SetIce là no-op và bàn chạy y như không có blocker.
        [Header("Băng (blocker) — để trống, gắn art sau")]
        [SerializeField] GameObject iceRoot;     // lớp phủ băng, bật/tắt cả cụm
        [SerializeField] TextMesh iceCountText;  // số nước còn lại
```

Thêm hàm, đặt ngay sau `SetMatchState`:

```csharp
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
```

- [ ] **Step 2: Thêm `SetLock` vào `BoxView`**

Thêm field ngay sau `slotAnchors`:

```csharp
        // Blocker — CHƯA GÁN, xem ghi chú cùng loại trong TileView.
        [Header("Khoá (blocker) — để trống, gắn art sau")]
        [SerializeField] GameObject lockRoot;     // ổ khoá phủ lên hộp
        [SerializeField] TextMesh lockLabelText;  // số nhóm còn cần, hoặc id chìa
```

Thêm hàm, đặt sau `Slot(int i)`:

```csharp
        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain).
        // label do bên gọi dựng — hộp khoá là số nhóm còn cần, hộp có ổ là id chìa.
        public void SetLock(bool closed, string label)
        {
            if (lockRoot != null) lockRoot.SetActive(closed);
            if (lockLabelText != null)
            {
                lockLabelText.gameObject.SetActive(closed && !string.IsNullOrEmpty(label));
                if (closed && !string.IsNullOrEmpty(label))
                    ViewText.Apply(lockLabelText, label, 1f, 0.9f);
            }
        }
```

Lưu ý: `ResetVisual()` hiện chỉ trả scale và alpha. **Không** đụng `lockRoot` ở đó — hộp vừa lộ ra có thể vẫn đang khoá, và `RefreshBlockerVisuals` ở Task 2 mới là chỗ quyết định.

- [ ] **Step 3: Compile**

Run: `./compilecheck.sh 2>&1 | grep "dll OK\|error CS"`
Expected: ba dòng `game.dll OK`, `editor.dll OK`, `meta.dll OK`

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Board/Views/TileView.cs Assets/_Game/Board/Views/BoxView.cs
git commit -m "feat(blocker): empty ice and lock slots on the tile and box views"
```

---

### Task 2: Bàn chặn thứ không chạm được và làm mới trạng thái blocker

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (`RefreshZones`, thêm `RefreshBlockerVisuals`, gọi ở `BuildBoard` / `AfterMove` / `RevealBox` / cuối `Settle`)
- Test: `Assets/_Game/Board/Domain/SelfCheck.cs` (thêm vào mục 8f)

**Interfaces:**
- Consumes: `Game.IsPullable(Tile, Box)`, `Game.IsOpen(Lock)`, `Game.IsFrozen(Tile)`, `Lock.Need/Have/KeyId/Kind`
- Produces: `void BoardController.RefreshBlockerVisuals()`

- [ ] **Step 1: Viết assert cho luật input, thêm vào cuối mục 8f**

Chèn ngay trước dòng đóng `}` của khối 8f trong `SelfCheck.cs` (sau assert `"Xáo: ô trống toàn nằm trong hộp đóng thì không xáo được"`):

```csharp
                // Vùng bấm được của bàn chơi hỏi đúng hàm này (BoardController.RefreshZones),
                // nên khoá nó ở đây là khoá luôn cả hover lẫn nhấc thẻ.
                var g5 = load(true);
                var openBox = g5.TopBox(0);
                Ok(g5.IsPullable(openBox.Slots[0], openBox), "thẻ thường ở hộp mở thì nhấc được");
                openBox.Slots[0].Lock = new Lock { Kind = LockKind.Moves, Need = 3 };
                Ok(!g5.IsPullable(openBox.Slots[0], openBox), "thẻ băng thì không nhấc được");
                Ok(g5.IsPullable(openBox.Slots[1], openBox), "thẻ bên cạnh vẫn nhấc được");
                g5.Stacks[1].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                var closedBox = g5.TopBox(1);
                foreach (var tt in closedBox.Slots)
                    if (tt != null) Ok(!g5.IsPullable(tt, closedBox), "mọi thẻ trong hộp đóng đều không nhấc được");
```

- [ ] **Step 2: Chạy để thấy nó xanh sẵn**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

Đây là assert **ghi lại hợp đồng**, không phải assert đỏ trước: `IsPullable` đã tồn tại từ nhịp 1. Mục đích là để lần sau ai sửa nó thì biết tầng input đang dựa vào.

- [ ] **Step 3: `RefreshZones` bỏ thẻ không chạm được**

Trong `BoardController.RefreshZones`, thay:

```csharp
                        var t = box.Slots[i];
                        if (t == null) continue;
                        zones.Add(new Zone
```

bằng:

```csharp
                        var t = box.Slots[i];
                        if (t == null) continue;
                        // Thẻ băng và thẻ trong hộp đóng: không hover, không nhấc. Hover() và
                        // BeginDrag() đều duyệt cùng danh sách này nên bỏ ở đây là bỏ cả hai.
                        // Zone Stack bên dưới vẫn giữ: thả VÀO hộp đóng thì MoveTile từ chối và
                        // Drop() cho hộp rung, rõ hơn là im lặng nuốt thao tác.
                        if (!g.IsPullable(t, box)) continue;
                        zones.Add(new Zone
```

- [ ] **Step 4: Thêm `RefreshBlockerVisuals`**

Đặt ngay sau `RefreshZones` trong `BoardController`:

```csharp
        // Trạng thái blocker đổi ở bốn thời điểm: dựng bàn, sau mỗi nước đi (băng đếm),
        // khi hộp dưới lộ ra, và cuối cascade (một lần gom có thể mở hộp khoá hoặc hộp có ổ).
        // Quét cả bàn thay vì lần theo từng thay đổi: bàn tối đa vài chục ô, và bỏ sót một
        // chỗ thì hình nói dối về thứ người chơi bấm được.
        void RefreshBlockerVisuals()
        {
            if (g == null || boxViews == null) return;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var box = g.TopBox(s);
                if (box == null) continue;

                bool closed = !g.IsOpen(box.Lock);
                if (boxViews[s] != null) boxViews[s].SetLock(closed, LockLabel(box.Lock));

                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var t = box.Slots[i];
                    if (t == null) continue;
                    TileView tv;
                    if (!tiles.TryGetValue(t.Uid, out tv) || tv == null) continue;
                    bool frozen = Game.IsFrozen(t);
                    tv.SetIce(frozen, frozen ? t.Lock.Need - t.Lock.Have : 0);
                }
            }
        }

        // Hộp khoá hiện số nhóm CÒN CẦN; hộp có ổ hiện id chìa. Hộp đã mở không có nhãn.
        string LockLabel(Lock l)
        {
            if (g == null) return null;
            if (l.Kind == LockKind.Clears) return Mathf.Max(l.Need - g.Cleared, 0).ToString();
            if (l.Kind == LockKind.Key) return l.KeyId;
            return null;
        }
```

- [ ] **Step 5: Gọi ở bốn chỗ**

Trong `BuildBoard`, sau `RefreshZones();`:

```csharp
            FitCamera();
            RefreshZones();
            RefreshBlockerVisuals();
            ReportResultIfFinished();
            CheckInvariant("build");
```

Trong `AfterMove`, sau `RefreshZones();`:

```csharp
            RefreshTileVisuals(from);
            RefreshTileVisuals(to);
            RefreshZones();
            RefreshBlockerVisuals();   // băng đếm ở MỌI stack, không riêng from/to
            ReportResultIfFinished();
```

Trong `RevealBox`, ở cuối:

```csharp
            SpawnTiles(s);                                 // thẻ của hộp vừa lộ
            RefreshBlockerVisuals();                       // hộp vừa lộ có thể đang khoá
```

Trong `Settle`, ngay sau `RefreshBoosterAvailability();`:

```csharp
            locked = false;
            RefreshBoosterAvailability();
            RefreshBlockerVisuals();   // một lần gom có thể vừa mở hộp khoá hoặc hộp có ổ
```

- [ ] **Step 6: Compile và chạy tự kiểm**

Run: `./compilecheck.sh 2>&1 | grep "dll OK\|error CS"`
Expected: ba dòng OK.

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): board refuses to pick what is blocked and refreshes blocker state"
```

---

### Task 3: Phím debug gắn blocker mẫu lên bàn đang chơi

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (`HandleKeys`, thêm `ApplyDebugBlockers`)

**Interfaces:**
- Consumes: `Lock`, `LockKind`, `IsFrozen`, `RefreshZones`, `RefreshBlockerVisuals`
- Produces: phím `B` gắn một bộ blocker mẫu lên bàn hiện tại.

Vì sao là phím debug chứ không phải một màn mẫu: màn mới phải đi qua catalog và Addressables mới chơi được, mà nhịp này không đụng content ship. Phím `B` cho người dùng thấy ngay trên bất kỳ màn nào, và biến mất khi bấm `R` nạp lại.

- [ ] **Step 1: Thêm `ApplyDebugBlockers`**

Đặt ngay sau `HandleKeys` trong `BoardController`:

```csharp
        // DEBUG (phím B): gắn một bộ blocker mẫu lên bàn ĐANG chơi để xem phần nhìn và
        // phần chặn input. Không phải content: bấm R nạp lại là sạch. Chọn mục tiêu theo
        // thứ tự cố định để lần nào cũng ra như nhau.
        //   stack có thẻ đầu tiên  → thẻ đầu đóng băng 3 nước
        //   stack tiếp theo        → hộp khoá, cần thêm 1 nhóm nữa
        //   stack tiếp theo        → hộp có ổ, chìa gắn lên một thẻ ở stack khác
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
                g.TopBox(keyedStack).Lock = new Lock { Kind = LockKind.Key, KeyId = "dbg" };
                // Chìa phải nằm NGOÀI hộp nó mở, nếu không là khoá vĩnh viễn (spec luật 6).
                for (int s = 0; s < g.Stacks.Count; s++)
                {
                    if (s == keyedStack) continue;
                    var box = g.TopBox(s);
                    if (box == null) continue;
                    bool done = false;
                    foreach (var t in box.Slots)
                        if (t != null && !Game.IsFrozen(t)) { t.KeyId = "dbg"; done = true; break; }
                    if (done) break;
                }
            }

            Debug.Log("[Blocker] gắn mẫu: băng ở stack " + icedStack + " · hộp khoá ở stack " + lockedStack +
                      " · hộp có ổ ở stack " + keyedStack + " (bấm R để nạp lại bàn sạch)");
            RefreshZones();
            RefreshBlockerVisuals();
        }
```

- [ ] **Step 2: Nối phím `B`**

Trong `HandleKeys`:

```csharp
        void HandleKeys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) Load();
            if (k.bKey.wasPressedThisFrame) ApplyDebugBlockers();   // DEBUG: xem ApplyDebugBlockers
        }
```

- [ ] **Step 3: Compile và chạy tự kiểm**

Run: `./compilecheck.sh 2>&1 | grep "dll OK\|error CS"`
Expected: ba dòng OK.

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs
git commit -m "feat(blocker): debug key B drops a sample blocker set on the live board"
```

- [ ] **Step 5: Kiểm bằng tay trong Unity** (người dùng làm)

Mở Unity, chạy một màn, bấm `B`, rồi kiểm bốn điều. Chưa gắn art nên chưa thấy gì trên màn hình — kiểm bằng hành vi và Console:

1. Console in dòng `[Blocker] gắn mẫu: ...` với ba số stack.
2. Thẻ đóng băng: rê chuột lên không phóng to, bấm giữ không nhấc lên được.
3. Hộp khoá và hộp có ổ: mọi thẻ bên trong không nhấc được; kéo một thẻ khác thả vào thì hộp rung và thẻ bay về chỗ cũ.
4. Đi ba nước bất kỳ rồi thử lại thẻ băng: giờ nhấc được.

Bấm `R` để về bàn sạch.

---

## Ngoài nhịp này

- Art thật cho lớp băng và ổ khoá, và gắn vào `Tile.prefab` / `Box.prefab` — người dùng làm.
- Hiệu ứng lúc băng vỡ và lúc khoá mở. Nhịp này chỉ có trạng thái tĩnh.
- Màn ship có blocker, đi qua catalog và Addressables.
- Công cụ dựng màn `demo/wordstack.html` hiểu `blockers` — nhịp 3.
- `demo/check.mjs` (bản luật JS) chưa biết blocker — nhịp 3.
