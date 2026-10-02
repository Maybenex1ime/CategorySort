# FixedTile Blocker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm blocker thẻ `fixed`: thẻ không nhặt được nhưng vẫn tính bộ 4; lúc bị ăn thì tháo đinh (vặn + nhấc, rồi mờ cùng tấm back) trước khi gộp.

**Architecture:** Luật sống ở `Assets/_Game/Board/Domain` (không import UnityEngine, kiểm bằng `SelfCheck` chạy ngoài Unity): `LockKind.Fixed` trên `Tile.Lock`. View: `TileView` bật cụm `Fixed Tile` có sẵn trong prefab và chạy một `LitMotionAnimation` do tool Editor dựng; `BoardController` rung thẻ khi bấm và chèn bước tháo đinh trước clear/collapse/Magnet.

**Tech Stack:** Unity 6000.3.8f1 (URP 2D), C#, LitMotion 2.0.2 + LitMotion.Animation 2.0.2, NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-09-29-fixed-tile-design.md`. Lệch spec (đã chốt với user sau khi duyệt spec): **bỏ phần JS** (`demo/wordstack.html`, `tool-check.mjs`) — bản JS đã lệch C# từ trước (còn `keylock`), Task 1 sửa spec cho khớp.

## Global Constraints

- `Assets/_Game/Board/Domain/` **không import UnityEngine** (`selfcheck.sh` compile cả thư mục bằng csc thuần).
- Id dữ liệu: `"fixed": true` trên card (`meaning.groups[].cards[].blockers`). Chỉ ở hộp chỉ số 0 của stack; không đứng chung `ice`.
- Thẻ Fixed **tính** vào bộ 4 (chỉ thẻ băng bị loại). Magnet hút được nhưng nhóm có thẻ Fixed xếp **sau mọi nhóm không có** (tiêu chí đứng đầu). Shuffle để thẻ Fixed đứng yên.
- Animation tháo đinh: 4 đinh cùng lúc quay 360° quanh z (z+ = ngược chiều kim đồng hồ) + nhấc +0.3 Y trong **0.6 s** (InOutSine / OutCubic), xong cả cụm `Fixed Tile` (4 đinh + tấm back) mờ 1→0 trong **0.2 s** bắt đầu lúc 0.6 s (OutQuad).
- Rung khi bấm thẻ Fixed: punch vị trí ngang **0.06** trong **0.2 s**, Frequency 6, DampingRatio 3.1.
- `LitMotionAnimation` giữ (Preserve) motion đã xong và **vẫn ghi giá trị cuối mỗi frame** → sau khi chờ `IsPlaying` về false phải gọi `Stop()`.
- **Không sửa `.prefab` / `.unity` trên đĩa khi Unity đang mở** — Tile.prefab chỉ do tool ghi, user chạy tool (Task 5).
- Không stage file của user: `Assets/_Game/Art/Fonts/*.asset`, `Assets/_Game/Content/SO_LevelCatalog.asset`, `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`, `Assets/Scenes/Main.unity`, `Assets/_Game/Board/Views/BoxView.cs`. Luôn `git add <đường dẫn cụ thể>`; không `git add -A` / `.`; không `git stash`.
- Commit kết thúc bằng dòng: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Lệnh chạy từ gốc repo `D:\CategorySort` bằng Git Bash. File nguồn dùng CRLF.

**Lệnh kiểm dùng chung:**
- Compile: `./compilecheck.sh` → `game.dll OK`, `editor.dll OK`, `meta.dll OK`.
- Compile test: `bash .git/sdd/testcheck.sh` (sau compilecheck) → `board-tests.dll OK`, `meta-tests.dll OK`.
- Luật (bộ level thật đang đỏ vì content có sẵn — chạy trên bản sao lv-009 + lv-010):
  ```bash
  D="$TEMP/claude/lv-ok" && rm -rf "$D" && mkdir -p "$D" && cp Assets/_Game/Content/Levels/lv-009.json Assets/_Game/Content/Levels/lv-010.json "$D/" && ./selfcheck.sh "$(cygpath -w "$D")"
  ```
  → dòng cuối `SelfCheck OK - 2 level, ...`; lỗi thì `SELFCHECK FAIL: <lý do>`.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Board/Domain/GameBlockers.cs` (sửa) | `LockKind.Fixed`, id `fixed`, `IsFixed`, `HasAnyMove` |
| `Assets/_Game/Board/Domain/Game.cs` (sửa) | `MoveTile` từ chối thẻ Fixed; `Build` đọc `fixed` |
| `Assets/_Game/Board/Domain/LevelData.cs` (sửa) | Validator: `fixed` phải `true`, chỉ ở box 0 |
| `Assets/_Game/Board/Domain/Solver.cs` (sửa) | Không sinh nước cho thẻ Fixed |
| `Assets/_Game/Board/Domain/GameMagnet.cs` (sửa) | Nhóm có thẻ Fixed xếp sau |
| `Assets/_Game/Board/Domain/GameShuffle.cs` (sửa) | Thẻ Fixed không vào pool, không làm mồi |
| `Assets/_Game/Board/Domain/SelfCheck.cs` (sửa) | Assert 8a, 8e, 8g, 8h |
| `Assets/_Game/Board/Views/TileView.cs` (sửa) | `fixedRoot`, `fixedBreakAnim`, API tháo đinh |
| `Assets/_Game/Board/Editor/FixedTileAnimationBuilder.cs` (mới) | Tool dựng animation tháo đinh vào Tile.prefab |
| `Assets/_Game/Board/Tests/FixedTileAnimationBuilderTests.cs` (mới) | Test tool trên bản mở tạm của prefab |
| `Assets/_Game/Board/Views/BoardController.cs` (sửa) | Rung khi bấm, hover bỏ qua, hiện đinh, tháo đinh trước clear/collapse/Magnet |
| `docs/wordstack-rules.md` (sửa) | §11 thêm `fixed` |
| `docs/superpowers/specs/2026-09-29-fixed-tile-design.md` (sửa) | Bỏ phần JS |

---

### Task 1: Luật cơ bản của thẻ đóng đinh

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameBlockers.cs`
- Modify: `Assets/_Game/Board/Domain/Game.cs` (class `Tile` dòng ~14, `Build` dòng ~103, `MoveTile` dòng ~141)
- Modify: `Assets/_Game/Board/Domain/LevelData.cs` (Validate, dòng ~251-290)
- Modify: `Assets/_Game/Board/Domain/Solver.cs` (dòng ~101)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (8a, 8e, thêm 8g)
- Modify: `docs/wordstack-rules.md` (§11), `docs/superpowers/specs/2026-09-29-fixed-tile-design.md`

**Interfaces:**
- Produces: `LockKind.Fixed`; `Blockers.Fixed = "fixed"`; `public static bool Game.IsFixed(Tile t)`. Task 2, 3, 4 dùng `Game.IsFixed`.

- [ ] **Step 1: Viết assert trước (RED)**

Trong `SelfCheck.cs`, khối **8a**, ngay sau dòng
`Ok(!Blockers.CardPairAllowed(Blockers.Ice, Blockers.Ice), "bảng cặp rỗng: không cặp nào được phép");` thêm:

```csharp
                Ok(!Blockers.CardPairAllowed(Blockers.Ice, Blockers.Fixed), "băng và đinh không đứng chung một thẻ");
```

Khối **8e**, ngay sau dòng
`Ok(Game.IsFrozen(Game.Build(okLv).TopBox(2).Slots[0]), "thẻ băng thuộc nhóm bị khoá là hợp lệ");` thêm:

```csharp

                // Thẻ đóng đinh (spec 2026-09-29-fixed-tile Mục 3). BlockerLv: c1 ở hộp trên stack 0,
                // c3 ở hộp chôn stack 0, c2 đang băng.
                var fxLv = freshB();
                fxLv.Groups[0].Cards[0].Blockers["fixed"] = true;
                fxLv.Validate(hasArt);
                var fc1 = Game.Build(fxLv).TopBox(0).Slots[0];
                Ok(fc1.CardId == "c1" && Game.IsFixed(fc1), "Build: fixed → Tile.Lock Fixed");
                brokenB(l => l.Groups[0].Cards[0].Blockers["fixed"] = 1.0, "fixed không phải true");
                brokenB(l => l.Groups[0].Cards[0].Blockers["fixed"] = false, "fixed = false");
                brokenB(l => l.Groups[0].Cards[2].Blockers["fixed"] = true, "thẻ đóng đinh nằm ở hộp chôn");
                brokenB(l => l.Groups[0].Cards[1].Blockers["fixed"] = true, "fixed + ice trên cùng một thẻ");
```

Thêm khối **8g** ngay TRƯỚC khối `// 8f. Booster tránh blocker (spec 4.3)`:

```csharp
            // 8g. Thẻ đóng đinh: nước đi, gom nhóm, kẹt (spec 2026-09-29-fixed-tile Mục 4)
            {
                var fx = new Lock { Kind = LockKind.Fixed };
                var g = load(true);
                var c1 = g.TopBox(0).Slots[0];
                c1.Lock = fx;
                Ok(Game.IsFixed(c1) && !Game.IsFrozen(c1), "Kind = Fixed là đóng đinh, không phải băng");
                Ok(Game.IsFixed(g.Clone().TopBox(0).Slots[0]), "Clone phải chép đinh của thẻ");
                Ok(!g.MoveTile(0, c1.Uid, 4), "thẻ đóng đinh không kéo đi được");
                Ok(g.Moves == 0, "nước bị từ chối không tính");
                string c2 = uidOf(g, "c2");
                Ok(g.MoveTile(0, c2, 4), "thẻ bên cạnh vẫn kéo đi được");
                Ok(g.MoveTile(4, c2, 0), "thả thẻ vào hộp có thẻ đóng đinh được");

                // Thẻ đóng đinh tính vào bộ 4 (khác thẻ băng) và bị xoá cùng nhóm.
                var g2 = load(true);
                var box4 = g2.TopBox(4);
                for (int i = 0; i < Rules.GroupSize - 1; i++) box4.Slots[i] = mkT("yy", i);
                box4.Slots[0].Lock = fx;
                g2.TopBox(0).Slots[2] = mkT("yy", 9);
                Ok(g2.MoveTile(0, "yy9", 4), "thẻ thứ 4 vào hộp có thẻ đóng đinh");
                g2.Settle(true);
                Ok(g2.Cleared == 1 && Game.IsEmpty(g2.TopBox(4)), "thẻ đóng đinh tính vào bộ 4, bị xoá cùng nhóm");

                // Kẹt: thẻ đóng đinh không phải thẻ đi được.
                var g3 = load(true);
                foreach (var st in g3.Stacks) foreach (var t in st.Boxes[0].Slots) if (t != null) t.Lock = fx;
                Ok(!g3.HasAnyMove(), "lộ ra toàn thẻ đóng đinh thì không còn nước");
                g3.TopBox(1).Slots[0].Lock = default(Lock);
                Ok(g3.HasAnyMove(), "gỡ một đinh là có nước lại");
            }

```

(`load`, `uidOf`, `mkT`, `freshB`, `brokenB` đã có sẵn trong `Run`; `mkT` khai ở đầu mục 8c, cùng scope method.)

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck ở Global Constraints.
Expected: compile lỗi `CS0117`/`CS0103` — `LockKind` không có `Fixed`, `Blockers` không có `Fixed`, `Game` không có `IsFixed`.

- [ ] **Step 3: `GameBlockers.cs`**

Dòng header đầu file đổi thành:

```csharp
// Blocker: Hộp khoá, Thẻ băng, Hộp khoá theo nhóm, Thẻ đóng đinh. Spec: docs/superpowers/specs/2026-09-10-blocker-locks-design.md
// + docs/superpowers/specs/2026-09-29-fixed-tile-design.md (thẻ đóng đinh).
```

Enum:

```csharp
    public enum LockKind { None, Clears, Moves, Group, Fixed }
```

Trong `Blockers`: sau dòng `public const string Ice = "ice"; ...` thêm

```csharp
        public const string Fixed = "fixed";         // thẻ: không nhặt được, vẫn tính bộ 4; chỉ ở hộp trên cùng lúc đầu màn
```

và đổi hai dòng:

```csharp
        public static readonly string[] CardIds = { Ice, Fixed };

        // ponytail: bảng cặp rỗng — ice và fixed không đứng chung một thẻ. Thêm cặp khi có luật cho phép.
```

(dòng thứ hai thay cho comment `// ponytail: thẻ mới có một blocker (băng) nên bảng cặp rỗng; ...`).

Sau `public static bool IsFrozen(Tile t) ...` thêm:

```csharp

        /// <summary>Thẻ đóng đinh: người chơi không nhặt được, nhưng vẫn tính bộ 4 và Magnet vẫn hút được.</summary>
        public static bool IsFixed(Tile t) { return t != null && t.Lock.Kind == LockKind.Fixed; }
```

Trong `HasAnyMove`, dòng
`foreach (var t in src.Slots) if (t != null && !IsFrozen(t)) { movable = true; break; }` đổi thành:

```csharp
                foreach (var t in src.Slots) if (t != null && !IsFrozen(t) && !IsFixed(t)) { movable = true; break; }
```

và comment doc của `HasAnyMove` đổi `(hộp đóng hai đầu, thẻ băng, hộp đích đầy)` thành `(hộp đóng hai đầu, thẻ băng / đóng đinh, hộp đích đầy)`.

- [ ] **Step 4: `Game.cs`**

Class `Tile`:

```csharp
        public Lock Lock;       // Kind = Moves khi còn băng, Fixed khi đóng đinh; default = thẻ thường
```

`MoveTile`, dòng `if (IsFrozen(src.Slots[i])) return false;      // thẻ băng đứng yên (spec 4.2)` đổi thành:

```csharp
            if (IsFrozen(src.Slots[i]) || IsFixed(src.Slots[i])) return false;   // thẻ băng / đóng đinh đứng yên
```

`Build`, ngay sau hai dòng

```csharp
                        if (c.Blockers.TryGetValue(Blockers.Ice, out cv))
                            tile.Lock = new Lock { Kind = LockKind.Moves, Need = (int)(double)cv };
```

thêm:

```csharp
                        if (c.Blockers.ContainsKey(Blockers.Fixed))
                            tile.Lock = new Lock { Kind = LockKind.Fixed };   // validator đã chặn đứng chung ice
```

- [ ] **Step 5: `LevelData.cs` (Validate)**

Ngay TRƯỚC vòng `foreach (var g in Groups)` / `foreach (var c in g.Cards)` kiểm blocker thẻ (vòng có `string at = "card \"" + c.Id + "\"";`) thêm:

```csharp
            var fixedCards = new HashSet<string>();   // card đóng đinh — chỉ được nằm ở box 0 (kiểm ở vòng stack)
```

Trong vòng card đó, ngay sau

```csharp
                    if (c.Blockers.TryGetValue(Blockers.Ice, out v) && !Blockers.IsCount(v))
                        die(at + ": ice phải là số nguyên >= 1");
```

thêm:

```csharp
                    if (c.Blockers.TryGetValue(Blockers.Fixed, out v))
                    {
                        if (!(v is bool) || !(bool)v) die(at + ": fixed phải là true");
                        fixedCards.Add(c.Id);
                    }
```

Trong vòng stack/box, ngay sau dòng `string at = "stack " + si + " box " + bi;` thêm:

```csharp
                    if (bi > 0)
                        foreach (var id in box.Slots)
                            if (id != null && fixedCards.Contains(id))
                                die(at + ": thẻ \"" + id + "\" đóng đinh chỉ được nằm ở hộp trên cùng (box 0)");
```

- [ ] **Step 6: `Solver.cs`**

Dòng `if (t == null || Game.IsFrozen(t)) continue;` (trong vòng sinh nước) đổi thành:

```csharp
                            if (t == null || Game.IsFrozen(t) || Game.IsFixed(t)) continue;
```

- [ ] **Step 7: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck ở Global Constraints. Expected: `SelfCheck OK - 2 level, ...`.
Run: `./compilecheck.sh` → 3 dll OK.

- [ ] **Step 8: Tài liệu luật + spec**

`docs/wordstack-rules.md` §11:

Câu mở đầu
`Ba vật cản, đều là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ". Không vật cản`
`nào thêm loại nước đi mới. Đặc tả đầy đủ: \`docs/superpowers/specs/2026-09-10-blocker-locks-design.md\`.`
đổi thành:

```markdown
Bốn vật cản. Ba cái đầu là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ"; `fixed` không
gỡ được — thẻ chỉ rời bàn khi nhóm của nó được gom. Không vật cản nào thêm loại nước đi mới. Đặc tả
đầy đủ: `docs/superpowers/specs/2026-09-10-blocker-locks-design.md`, `docs/superpowers/specs/2026-09-29-fixed-tile-design.md`.
```

Bảng: thêm dòng cuối

```markdown
| `fixed` | thẻ | `true` | thẻ không nhặt được nhưng vẫn tính bộ 4; chỉ nằm ở hộp trên cùng lúc đầu màn; lúc bị ăn tháo đinh rồi mới gộp |
```

Khối JSON ví dụ: thêm dòng

```json
{ "id": "kiwi",   "text": "Kiwi",   "blockers": { "fixed": true } }
```

Đoạn "Luật kiểm thêm": trước ` · \`grouplock\` trỏ một id nhóm có` chèn ` · \`fixed\` phải là \`true\` và thẻ mang nó chỉ nằm ở hộp trên cùng`.

Câu `Nam châm bỏ qua nhóm có thành viên đang băng hoặc nằm trong hộp đóng; Xáo không đụng hai thứ`
`đó;` đổi thành:

```markdown
Nam châm bỏ qua nhóm có thành viên đang băng hoặc nằm trong hộp đóng, hút được thẻ đóng đinh nhưng
xếp nhóm có nó sau mọi nhóm khác; Xáo không đụng ba thứ đó;
```

Cuối đoạn "Công cụ dựng màn" thêm câu: `Tool JS chưa hỗ trợ \`fixed\` — màn có thẻ đóng đinh sửa tay JSON.`

`docs/superpowers/specs/2026-09-29-fixed-tile-design.md`:
- Mục 4, đoạn `**Bản JS phải khớp:** ...` (2 dòng) thay bằng:
  `**Bản JS không cập nhật:** \`demo/wordstack.html\` / \`tool-check.mjs\` đã lệch C# từ trước (còn \`keylock\`), màn có thẻ Fixed sửa tay JSON. \`SelfCheck\` thêm assert cho từng gạch đầu dòng ở trên.`
- Mục 7, dòng `- \`SelfCheck\` (luật, chạy ngoài Unity) + \`demo/check.mjs\` / \`demo/tool-check.mjs\`.` đổi thành `- \`SelfCheck\` (luật, chạy ngoài Unity).`

- [ ] **Step 9: Commit**

```bash
git add Assets/_Game/Board/Domain/GameBlockers.cs Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/LevelData.cs Assets/_Game/Board/Domain/Solver.cs Assets/_Game/Board/Domain/SelfCheck.cs docs/wordstack-rules.md docs/superpowers/specs/2026-09-29-fixed-tile-design.md
git commit -m "FixedTile: nailed tiles cannot be picked but still complete a group

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Thẻ đóng đinh với Magnet và Shuffle

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameMagnet.cs` (`FindMagnetTarget`, `IsBetterTarget`)
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`PickPrimeCandidates` dòng ~190, `AssignableTopSlots` dòng ~228)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (thêm 8h)

**Interfaces:**
- Consumes: `Game.IsFixed(Tile)`, `LockKind.Fixed` (Task 1).
- Produces: không API mới.

- [ ] **Step 1: Viết assert trước (RED)**

Thêm khối **8h** ngay TRƯỚC dòng `log("SelfCheck OK — " + ...`:

```csharp
            // 8h. Thẻ đóng đinh với booster (spec 2026-09-29-fixed-tile Mục 4)
            {
                var fx = new Lock { Kind = LockKind.Fixed };
                var g = load(true);
                string m1 = g.FindMagnetTarget();
                Ok(m1 != null, "bàn luật có mục tiêu nam châm");
                Tile nailed = null; Box nailedBox = null;
                foreach (var st in g.Stacks)
                    foreach (var t in st.Boxes[0].Slots)
                        if (nailed == null && t != null && t.GroupId == m1) { nailed = t; nailedBox = st.Boxes[0]; }
                nailed.Lock = fx;
                Ok(g.IsPullable(nailed, nailedBox), "thẻ đóng đinh vẫn hút được (và vẫn có vùng chạm để rung)");
                string m2 = g.FindMagnetTarget();
                Ok(m2 != null && m2 != m1, "nhóm có thẻ đóng đinh xếp sau nhóm không có");
                foreach (var gid in new[] { "ga", "gb", "gc" })
                {
                    if (gid == m1) continue;
                    var other = g.Stacks.SelectMany(st => st.Boxes).SelectMany(b => b.Slots)
                                 .First(t => t != null && t.GroupId == gid);
                    other.Lock = new Lock { Kind = LockKind.Moves, Need = 9 };   // băng → nhóm đó hết hợp lệ
                }
                Ok(g.FindMagnetTarget() == m1, "không còn nhóm nào khác thì vẫn hút nhóm có thẻ đóng đinh");
                var mr = g.ApplyMagnet(m1);
                Ok(mr.Ok && mr.Picks.Any(p => p.Uid == nailed.Uid), "Magnet hút cả thẻ đóng đinh");

                // Xáo: e1 ở stack 1 là thẻ gc duy nhất trong hộp đó (thẻ trắng) — không đóng đinh thì nó vào pool.
                var g2 = load(true);
                Ok(Game.IsWhite(g2.TopBox(1), 3), "e1 đang trắng — tiền đề của bài kiểm");
                var e1 = g2.TopBox(1).Slots[3];
                e1.Lock = fx;
                Ok(!g2.AssignableTopSlots().Any(r => r.Stack == 1 && r.Slot == 3), "Xáo: ô của thẻ đóng đinh không vào pool dù thẻ trắng");
                Ok(!g2.PickPrimeCandidates(3).Contains("gc"), "Xáo: nhóm có thẻ đóng đinh không làm mồi");
                g2.ApplyShuffle();
                Ok(g2.TopBox(1).Slots[3] == e1, "Xáo: thẻ đóng đinh đứng yên đúng ô");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck. Expected: `SELFCHECK FAIL: nhóm có thẻ đóng đinh xếp sau nhóm không có`.

- [ ] **Step 3: `GameMagnet.cs`**

Comment doc của `FindMagnetTarget`, dòng `/// Thứ tự chốt: nhiều thẻ ở hộp trên cùng nhất → nhóm gốc (không đẻ thẻ cha)` đổi thành:

```csharp
        /// Thứ tự chốt: nhóm không có thẻ đóng đinh → nhiều thẻ ở hộp trên cùng nhất → nhóm gốc (không đẻ thẻ cha)
```

Trong `FindMagnetTarget`, sau dòng `var order = new List<string>();   // ...` thêm:

```csharp
            var fixedGroups = new HashSet<string>();   // nhóm có thẻ đóng đinh — hút được nhưng không ưu tiên
```

Ngay sau dòng `string gid = t.GroupId;` (trong vòng quét slot) thêm:

```csharp
                        if (IsFixed(t)) fixedGroups.Add(gid);
```

Lời gọi `IsBetterTarget(gid, best, onTop, deepest, firstAt)` đổi thành `IsBetterTarget(gid, best, onTop, deepest, firstAt, fixedGroups)`.

`IsBetterTarget`:

```csharp
        bool IsBetterTarget(string a, string b, Dictionary<string, int> onTop,
                            Dictionary<string, int> deepest, Dictionary<string, int> firstAt,
                            HashSet<string> fixedGroups)
        {
            // Nhóm có thẻ đóng đinh đứng sau mọi nhóm không có (spec fixed-tile Mục 2).
            bool fa = fixedGroups.Contains(a), fb = fixedGroups.Contains(b);
            if (fa != fb) return fb;

            if (onTop[a] != onTop[b]) return onTop[a] > onTop[b];
```

(phần còn lại của hàm giữ nguyên).

- [ ] **Step 4: `GameShuffle.cs`**

`PickPrimeCandidates`, dòng
`if (slots[i] == null || !IsPullable(slots[i], boxes[b])) continue;   // như Magnet` đổi thành:

```csharp
                        // Như Magnet, thêm: thẻ đóng đinh không kéo được nên nhóm có nó không làm mồi.
                        if (slots[i] == null || !IsPullable(slots[i], boxes[b]) || IsFixed(slots[i])) continue;
```

`AssignableTopSlots`, dòng
`if (top.Slots[i] == null || (IsWhite(top, i) && !IsFrozen(top.Slots[i])))` đổi thành:

```csharp
                    if (top.Slots[i] == null || (IsWhite(top, i) && !IsFrozen(top.Slots[i]) && !IsFixed(top.Slots[i])))
```

(donor của `TryPrimeGroup` chỉ lấy từ pool, `SwapDonorIntoHand` chỉ lấy từ box ≥ 1 nơi không có thẻ Fixed — không cần sửa thêm.)

- [ ] **Step 5: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`. Run: `./compilecheck.sh` → 3 dll OK.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameMagnet.cs Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "FixedTile: Magnet ranks nailed groups last, Shuffle leaves nailed tiles alone

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: `TileView` tháo đinh + tool dựng animation

**Files:**
- Modify: `Assets/_Game/Board/Views/TileView.cs` (sau field `revealAnim` / hàm `EndReveal`)
- Create: `Assets/_Game/Board/Editor/FixedTileAnimationBuilder.cs`
- Create: `Assets/_Game/Board/Tests/FixedTileAnimationBuilderTests.cs`

**Interfaces:**
- Consumes: `AnimationBuildKit` (`Common`, `Timing`, `Write(anim, undoName, params parts)`, `FindDeep`) trong `Assets/_Game/Board/Editor/AnimationBuildKit.cs`; `FigmaMotion.SpriteGroupAlphaAnimation`.
- Produces (Task 4 dùng): `TileView.SetFixed(bool)`, `bool TileView.IsFixed`, `TileView.PlayFixedBreak()`, `bool TileView.IsBreakingFixed`, `TileView.EndFixedBreak()`; serialized fields `fixedRoot`, `fixedBreakAnim`; tool `FixedTileAnimationBuilder.Build(TileView)`.

- [ ] **Step 1: Viết test trước (RED)**

`Assets/_Game/Board/Tests/FixedTileAnimationBuilderTests.cs`:

```csharp
// Tool tháo đinh — chạy trên bản mở tạm của Tile.prefab thật, KHÔNG lưu.
using System.Linq;
using FigmaMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class FixedTileAnimationBuilderTests
    {
        [Test]
        public void Build_SpinsAndLiftsFourNailsThenFadesAndWires()
        {
            var tile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Tile.prefab");
            try
            {
                var tv = tile.GetComponent<TileView>();
                FixedTileAnimationBuilder.Build(tv);

                var so = new SerializedObject(tv);
                var root = (GameObject)so.FindProperty("fixedRoot").objectReferenceValue;
                var anim = (LitMotionAnimation)so.FindProperty("fixedBreakAnim").objectReferenceValue;
                Assert.AreEqual("Fixed Tile", root.name);
                Assert.AreSame(root, anim.gameObject, "anim nằm trên chính Fixed Tile");
                Assert.AreEqual(9, anim.Components.Count, "4 × (vặn + nhấc) + 1 mờ");
                Assert.AreEqual(4, anim.Components.OfType<TransformRotationAnimation>().Count());
                Assert.AreEqual(4, anim.Components.OfType<TransformPositionAnimation>().Count());
                Assert.IsTrue(anim.Components[8] is SpriteGroupAlphaAnimation, "mờ cả cụm ở cuối");
                Assert.IsFalse(root.activeSelf, "prefab giữ Fixed Tile tắt — chỉ bật khi thẻ bị đóng đinh");
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }
    }
}
```

- [ ] **Step 2: Compile test — phải đỏ**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `board-tests` FAIL, `CS0103`/`CS0246` — `FixedTileAnimationBuilder` không tồn tại.

- [ ] **Step 3: `TileView.cs`**

Ngay sau hàm `public void EndReveal() { ... }` thêm:

```csharp

        [Header("Đóng đinh (blocker)")]
        [Tooltip("Cụm Fixed Tile: tấm back + 4 đinh. Bật khi thẻ bị đóng đinh")]
        [SerializeField] GameObject fixedRoot;
        [Tooltip("Tháo đinh lúc thẻ bị ăn: đinh vặn + nhấc, rồi mờ cùng tấm back. Dựng bằng Tools ▸ WordStack ▸ Build Fixed Tile Break Animation")]
        [SerializeField] LitMotionAnimation fixedBreakAnim;

        // Thẻ đóng đinh: không nhặt được, vẫn tính bộ 4 (luật ở Domain). Ở đây chỉ hiện đinh + tháo đinh.
        public void SetFixed(bool on) { if (fixedRoot != null) fixedRoot.SetActive(on); }
        public bool IsFixed => fixedRoot != null && fixedRoot.activeSelf;

        public void PlayFixedBreak()
        {
            if (fixedBreakAnim == null) return;
            fixedBreakAnim.Stop();
            fixedBreakAnim.Play();
        }

        public bool IsBreakingFixed => fixedBreakAnim != null && fixedBreakAnim.IsPlaying;

        /// <summary>Gọi khi tháo đinh xong: Stop (motion đã xong vẫn ghi giá trị mỗi frame, xem EndReveal),
        /// OnStop trả đinh/alpha về như author rồi tắt cả cụm — thẻ còn lại như thẻ thường để gộp.</summary>
        public void EndFixedBreak()
        {
            if (fixedBreakAnim != null) fixedBreakAnim.Stop();
            SetFixed(false);
        }
```

- [ ] **Step 4: `FixedTileAnimationBuilder.cs`**

`Assets/_Game/Board/Editor/FixedTileAnimationBuilder.cs`:

```csharp
// Dựng animation tháo đinh của thẻ đóng đinh (spec docs/superpowers/specs/2026-09-29-fixed-tile-design.md Mục 6):
//   4 đinh (con "Blocker - Fixed Tile…" của Fixed Tile) cùng lúc quay 1 vòng ngược chiều kim đồng hồ + nhấc lên,
//   xong cả cụm Fixed Tile (4 đinh + tấm back) mờ về 0. Nối vào TileView.fixedRoot / fixedBreakAnim.
// Tools ▸ WordStack ▸ Build Fixed Tile Break Animation. Sửa thẳng asset Tile.prefab (LoadPrefabContents →
// SaveAsPrefabAsset): ĐÓNG Prefab Mode trước. Chạy lại = dựng lại từ đầu, ghi đè số đã chỉnh trong Inspector.
// Hoàn tác bằng git.
using System.Collections.Generic;
using FigmaMotion;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;
using static WordStack.Board.Editor.AnimationBuildKit;

namespace WordStack.Board.Editor
{
    public static class FixedTileAnimationBuilder
    {
        const string TilePath = "Assets/Prefabs/Tile.prefab";
        const string UndoName = "Build Fixed Tile Break Animation";
        const string NailPrefix = "Blocker - Fixed Tile";

        const float SpinDur = 0.6f, Lift = 0.3f, FadeDur = 0.2f;

        [MenuItem("Tools/WordStack/Build Fixed Tile Break Animation")]
        static void Menu()
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == TilePath)
            { Debug.LogError("[FixedTile] Đóng Prefab Mode của " + TilePath + " trước khi chạy tool."); return; }

            var tile = PrefabUtility.LoadPrefabContents(TilePath);
            try
            {
                Build(tile.GetComponent<TileView>());
                PrefabUtility.SaveAsPrefabAsset(tile, TilePath);
                Debug.Log("[FixedTile] " + TilePath + ": đã dựng tháo đinh.");
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }

        /// <summary>Dựng fixedBreakAnim trên Fixed Tile của thẻ đang mở (LoadPrefabContents hoặc Prefab Mode).</summary>
        public static void Build(TileView tv)
        {
            var root = FindDeep(tv.transform, "Fixed Tile");
            if (root == null) throw new System.InvalidOperationException("Không thấy con \"Fixed Tile\" trong Tile.prefab.");
            var nails = new List<Transform>();
            foreach (Transform c in root) if (c.name.StartsWith(NailPrefix)) nails.Add(c);
            if (nails.Count != 4)
                throw new System.InvalidOperationException("Fixed Tile phải có đúng 4 đinh \"" + NailPrefix + "…\", đang có " + nails.Count + ".");

            var anim = root.GetComponent<LitMotionAnimation>();
            if (anim == null) anim = root.gameObject.AddComponent<LitMotionAnimation>();

            var parts = new List<(LitMotionAnimationComponent, System.Action<SerializedProperty>)>();
            foreach (var n in nails)
            {
                var nail = n;
                parts.Add((new TransformRotationAnimation(), c =>
                {
                    Common(c, nail.name + " · vặn", nail, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, 0f, 360f);   // z+ = ngược chiều kim đồng hồ
                    Timing(s, SpinDur, 0f, Ease.InOutSine);
                }));
                parts.Add((new TransformPositionAnimation(), c =>
                {
                    Common(c, nail.name + " · nhấc", nail, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, Lift, 0f);
                    Timing(s, SpinDur, 0f, Ease.OutCubic);
                }));
            }
            parts.Add((new SpriteGroupAlphaAnimation(), c =>
            {
                Common(c, "Fixed Tile · mờ (đinh + back)", root, false);
                var s = c.FindPropertyRelative("settings");
                s.FindPropertyRelative("startValue").floatValue = 1f;
                s.FindPropertyRelative("endValue").floatValue = 0f;
                Timing(s, FadeDur, SpinDur, Ease.OutQuad);
            }));
            Write(anim, UndoName, parts.ToArray());

            var so = new SerializedObject(tv);
            so.FindProperty("fixedRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("fixedBreakAnim").objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
```

Nếu Unity không tự sinh `.meta` cho 2 file mới: ép import bằng `AssetDatabase.ImportAsset("<path>")` (Unity MCP `Unity_RunCommand`, nếu còn kết nối) hoặc chờ user focus Unity; commit `.meta` cùng file.

- [ ] **Step 5: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → 3 dll OK + `board-tests.dll OK`, `meta-tests.dll OK`.
Test EditMode `FixedTileAnimationBuilderTests` chạy trong Unity ở Task 5.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Views/TileView.cs Assets/_Game/Board/Editor/FixedTileAnimationBuilder.cs Assets/_Game/Board/Editor/FixedTileAnimationBuilder.cs.meta Assets/_Game/Board/Tests/FixedTileAnimationBuilderTests.cs Assets/_Game/Board/Tests/FixedTileAnimationBuilderTests.cs.meta
git commit -m "FixedTile: tile shows its nails and a tool builds the unscrew animation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `BoardController` — rung khi bấm, hiện đinh, tháo đinh trước khi gộp

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs`

**Interfaces:**
- Consumes: `Game.IsFixed(Tile)` (Task 1); `TileView.SetFixed`, `IsFixed`, `PlayFixedBreak`, `IsBreakingFixed`, `EndFixedBreak` (Task 3); `MagnetPick.Uid`; `SettleEvent.DoomedUids`.
- Produces: không API mới.

- [ ] **Step 1: Zone mang cờ đóng đinh**

`struct Zone` (dòng ~122):

```csharp
        struct Zone { public Rect Rect; public ZoneKind Kind; public int Stack; public string Uid; public bool Fixed; }
```

`RefreshZones`: comment trong vòng slot đổi dòng đầu `// Thẻ băng và thẻ trong hộp đóng: không hover, không nhấc. Hover() và` thành
`// Thẻ băng và thẻ trong hộp đóng: không hover, không nhấc (thẻ đóng đinh CÓ zone để bấm thì rung). Hover() và`, và khởi tạo zone:

```csharp
                        zones.Add(new Zone
                        {
                            Rect = SlotZone(s, i),
                            Kind = ZoneKind.Tile, Stack = s, Uid = t.Uid, Fixed = Game.IsFixed(t)
                        });
```

- [ ] **Step 2: Bấm thẻ đóng đinh = rung; hover bỏ qua**

Trong xử lý input (`if (p.press.wasPressedThisFrame && ghost == null)`), vòng zone đổi thành:

```csharp
                foreach (var z in zones)
                {
                    if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                    if (z.Fixed) ShakeTile(z.Uid);          // thẻ đóng đinh: rung, không nhấc
                    else BeginDrag(z.Stack, z.Uid, pt);
                    break;
                }
```

`Hover`, vòng zone đổi thành:

```csharp
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                if (!z.Fixed) tiles.TryGetValue(z.Uid, out h);   // thẻ đóng đinh không phồng
                break;
            }
```

Ngay sau hàm `Shake(int stack)` thêm:

```csharp

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
```

- [ ] **Step 3: Hiện đinh**

`RefreshBlockerVisuals`, ngay sau `tv.SetIce(frozen, iceLeft, frozen ? t.Lock.Need : 0);` thêm:

```csharp
                    tv.SetFixed(Game.IsFixed(t));
```

- [ ] **Step 4: Tháo đinh trước clear / collapse / Magnet**

Thêm coroutine (ngay trước `IEnumerator Settle(float delay = 0f)`):

```csharp
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

```

Trong `Settle`, ngay sau `hadCascade = true;` thêm:

```csharp
                if (ev.Kind == SettleKind.Clear || ev.Kind == SettleKind.Collapse)
                    yield return BreakFixed(ev.DoomedUids);   // tháo đinh trước khi gộp
```

Trong `MagnetSequence`, ngay TRƯỚC `yield return Backdrop(true);` thêm:

```csharp
            var picked = new List<string>();
            foreach (var p in r.Picks) picked.Add(p.Uid);
            yield return BreakFixed(picked);   // thẻ đóng đinh bị hút: tháo đinh trước khi phồng → bay
```

(`System.Collections.Generic` đã được dùng trong file — `List`, `Dictionary`.)

- [ ] **Step 5: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK. Run lệnh selfcheck → OK (không đổi luật).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs
git commit -m "FixedTile: tapping a nailed tile shakes it; nails come off before merge or Magnet

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Chạy tool, test EditMode, kiểm trong Play mode (user làm trong Unity)

**Files:**
- Modify (tool ghi): `Assets/Prefabs/Tile.prefab`

- [ ] **Step 1: Chạy tool**

Đóng Prefab Mode của Tile.prefab → **Tools ▸ WordStack ▸ Build Fixed Tile Break Animation**.
Expected Console: `[FixedTile] Assets/Prefabs/Tile.prefab: đã dựng tháo đinh.`
Kiểm: `Tile.prefab` → `TileView` có `Fixed Root` = `Fixed Tile`, `Fixed Break Anim` đã nối; `Fixed Tile` vẫn tắt.

- [ ] **Step 2: Test EditMode**

Test Runner ▸ EditMode ▸ Run All → PASS (có `FixedTileAnimationBuilderTests`).

- [ ] **Step 3: Level thử**

Thêm `"blockers": { "fixed": true }` vào một card nằm ở hộp trên cùng của một màn (sửa tay JSON; nạp màn qua Cheat panel ô LEVEL). Validator báo lỗi nếu card đó nằm ở hộp chôn.

- [ ] **Step 4: Kiểm trong Play mode** (spec Mục 7)

1. Thẻ Fixed hiện đinh + back; bấm vào thì rung, không có Ghost, không phồng khi hover.
2. Gom 3 thẻ cùng nhóm vào hộp chứa nó → 4 đinh vặn + nhấc cùng lúc → mờ cùng back → gộp như thường.
3. Nhóm có cha: collapse sau khi tháo đinh, thẻ cha là thẻ thường.
4. Magnet: chỉ chọn nhóm có Fixed khi không còn nhóm nào khác; tháo đinh trước khi bay.
5. Shuffle: thẻ Fixed đứng yên.
6. Hộp `locked` chứa thẻ Fixed: mở khoá xong thẻ vẫn bị đóng đinh.
7. Retry giữa lúc tháo đinh: bàn dựng lại sạch, Console không lỗi.

Chỉnh số trong Inspector nếu cần (chạy lại tool là ghi đè).

- [ ] **Step 5: Commit**

```bash
git add Assets/Prefabs/Tile.prefab
git commit -m "Tile.prefab carries the fixed-tile unscrew animation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
