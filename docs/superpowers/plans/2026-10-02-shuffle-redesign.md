# Shuffle Redesign + Dead Board Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Shuffle dựng được Nhóm mồi trong nhiều bàn hơn (mốc băng/đóng đinh/cụm sẵn, dời cụm đôi, xé cụm lấp hộp rỗng), không bao giờ ăn lượt mà bàn đứng yên, và bàn chết (còn nước nhưng hết đường nổ nhóm) báo kẹt để hồi sinh bằng nam châm.

**Architecture:** Luật sống ở `Assets/_Game/Board/Domain` (không import UnityEngine): `Game.IsDeadBoard()` trong `GameBlockers.cs`, gọi từ `CheckStatus()`; phần Shuffle trong `GameShuffle.cs` (chọn ứng viên, hộp chủ, lấp hộp rỗng, bất biến, chạy thử). View `BoardController` chỉ đổi điều kiện sáng nút (chạy thử trên bản sao) và để thẻ băng/đóng đinh đứng ngoài xoáy.

**Tech Stack:** Unity 6000.3.8f1, C#, NUnit EditMode, SelfCheck chạy ngoài Unity bằng Roslyn (`selfcheck.sh`).

**Spec:** `docs/superpowers/specs/2026-10-02-shuffle-redesign-design.md`. Kiểm mới nằm trong `SelfCheck` (mục 8i–8m) để mỗi task có vòng đỏ/xanh chạy được ngoài Unity; 16 test NUnit cũ trong `BoardShuffleTests` giữ nguyên và vẫn phải xanh.

**Đã kiểm trước khi viết plan:** toàn bộ code trong plan đã chạy trên bản sao thư mục `Domain/` — sau MỖI task, `SelfCheck` (lv-009 + lv-010) xanh; 16 test cũ của `BoardShuffleTests` chạy qua harness ngoài Unity đều xanh.

## Global Constraints

- `Assets/_Game/Board/Domain/` **không import UnityEngine** (`selfcheck.sh` compile cả thư mục bằng csc thuần).
- ApplyShuffle phải **xác định**: cùng bàn → cùng kết quả (nút sáng theo chạy thử trên `Clone()`).
- Không phát clear miễn phí: không hộp nào trên toàn bàn đủ 4 thẻ cùng nhóm sau khi xáo; tổng thẻ lớp trên không đổi.
- `IsDeadBoard()` **không bao giờ** được trả true cho bàn còn nổ được nhóm — Solver cắt nhánh theo `Status == Stuck`.
- Không stage file của user: `Assets/_Game/Art/Fonts/*.asset`, `Assets/_Game/Content/SO_LevelCatalog.asset`, `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`, `Assets/Scenes/Main.unity`, `Assets/Prefabs/Box.prefab`, `Assets/Prefabs/Stack.prefab`, `Assets/Prefabs/ProjectScope.prefab`. Luôn `git add <đường dẫn cụ thể>`; không `git add -A` / `.`; không `git stash`.
- Commit kết thúc bằng dòng: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Lệnh chạy từ gốc repo `D:\CategorySort` bằng Git Bash, nhánh `feat/shuffle-redesign`. File nguồn dùng CRLF, UTF-8.

**Lệnh kiểm dùng chung:**
- Compile: `./compilecheck.sh` → `game.dll OK`, `editor.dll OK`, `meta.dll OK`.
- Compile test: `bash .git/sdd/testcheck.sh` (sau compilecheck) → `board-tests.dll OK`, `meta-tests.dll OK`.
- Luật (bộ level thật đang đỏ vì thiếu art — chạy trên bản sao lv-009 + lv-010, ~2 phút):
  ```bash
  D="$TEMP/claude/lv-ok" && rm -rf "$D" && mkdir -p "$D" && cp Assets/_Game/Content/Levels/lv-009.json Assets/_Game/Content/Levels/lv-010.json "$D/" && ./selfcheck.sh "$(cygpath -w "$D")"
  ```
  → dòng cuối `SelfCheck OK - 2 level, ...`; lỗi thì `SELFCHECK FAIL: <lý do>`. Lỗi compile của csc cũng in ra trước đó.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Board/Domain/GameBlockers.cs` (sửa) | `IsDeadBoard()` |
| `Assets/_Game/Board/Domain/Game.cs` (sửa) | `CheckStatus()` gọi `IsDeadBoard()` |
| `Assets/_Game/Board/Domain/GameShuffle.cs` (sửa) | Ứng viên mồi, hộp chủ, lấp hộp rỗng, bất biến nới, chạy thử |
| `Assets/_Game/Board/Domain/SelfCheck.cs` (sửa) | Helper level gọn + mục 8i–8m; sửa 2 câu kiểm 8g/8h |
| `Assets/_Game/Board/Views/BoardController.cs` (sửa) | Nút Shuffle theo chạy thử; thẻ băng/đinh đứng ngoài xoáy |
| `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs` (sửa) | Chú thích `IsUsable` |
| `docs/wordstack-rules.md` (sửa) | Mục 6 kẹt + bàn chết; Mục 11 Xáo + việc chờ tool |

---

### Task 1: Bàn chết

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameBlockers.cs` (đầu file `using`; thêm method trước `/// <summary>Nhóm gid còn thẻ nào trên bàn không.`)
- Modify: `Assets/_Game/Board/Domain/Game.cs` (`CheckStatus`, dòng ~252-260)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (helper sau `static void Ok`; mục 8i trước dòng `log("SelfCheck OK — "`)

**Interfaces:**
- Produces: `public bool Game.IsDeadBoard()`; helper tĩnh trong `SelfCheck`: `BuildQ(string json)`, `Meaning(params string[] groups)`, `CardOf(Game, string cardId)`, `InBox(Box, string gid)` — các task sau dùng.

- [ ] **Step 1: Thêm helper và mục 8i vào SelfCheck**

Trong `Assets/_Game/Board/Domain/SelfCheck.cs`, ngay SAU dòng

```csharp
        static void Ok(bool cond, string msg) { if (!cond) throw new Exception(msg); }
```

thêm:

```csharp

        // Level viết gọn cho các mục 8i–8m: nháy đơn thay nháy kép, nhóm "ga:a" = nhóm ga có
        // 4 thẻ a1..a4. Không Validate — thẻ khai báo mà không đặt lên bàn là cố ý.
        static Game BuildQ(string json) { return Game.Build(LevelData.Parse(json.Replace('\'', '"'))); }

        static string Meaning(params string[] groups)
        {
            var parts = groups.Select(x =>
            {
                string[] p = x.Split(':');
                var cards = Enumerable.Range(1, 4).Select(i => "{'id':'" + p[1] + i + "','text':'" + p[1] + i + "'}");
                return "{'id':'" + p[0] + "','text':'" + p[0] + "','cards':[" + string.Join(",", cards.ToArray()) + "]}";
            });
            return "'meaning':{'groups':[" + string.Join(",", parts.ToArray()) + "]}}";
        }

        static Tile CardOf(Game g, string cardId)
        {
            return g.Stacks.SelectMany(st => st.Boxes).SelectMany(b => b.Slots).First(t => t != null && t.CardId == cardId);
        }

        static int InBox(Box b, string gid) { return b.Slots.Count(t => t != null && t.GroupId == gid); }
```

Ngay TRƯỚC dòng `            log("SelfCheck OK — " + levelJsons.Count + ...` (cuối `Run`), thêm:

```csharp
            // 8i. Bàn chết (spec 2026-10-02-shuffle-redesign Mục 8). Stack 0 có hộp dưới, stack
            // 1 và 2 chỉ có hộp đáy. 2 ô trống, không nhóm nào đủ 4 ở lớp trên.
            {
                const string DeadLv = @"{'id':'t-dead','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','b1','c1',null]},{'slots':['a2','a3','a4','d4']}]},
                  {'pos':[1,0],'boxes':[{'slots':['d1','d2','e1',null]}]},
                  {'pos':[2,0],'boxes':[{'slots':['f1','f2','e2','e3']}]}]},";
                Func<Game> dead = () => BuildQ(DeadLv + Meaning("ga:a", "gb:b", "gc:c", "gd:d", "ge:e", "gf:f"));
                var fixedLock = new Lock { Kind = LockKind.Fixed };

                var g = dead();
                Ok(g.HasAnyMove(), "Chết: tiền đề — bàn vẫn còn nước đi");
                Ok(g.IsDeadBoard() && g.CheckStatus() == GameStatus.Stuck,
                   "Chết: 2 ô trống, không nhóm nào đủ 4 ở lớp trên → kẹt dù còn nước");

                var g1 = dead();
                g1.TopBox(0).Slots[3] = new Tile { Uid = "e4", CardId = "e4", GroupId = "ge" };
                Ok(!g1.IsDeadBoard() && g1.CheckStatus() == GameStatus.Playing, "Chết: ge đủ 4 ở lớp trên → còn sống");

                var g2 = dead();
                g2.TopBox(0).Slots[3] = new Tile { Uid = "e4", CardId = "e4", GroupId = "ge", Lock = fixedLock };
                g2.TopBox(1).Slots[2].Lock = fixedLock;
                Ok(g2.IsDeadBoard(), "Chết: thẻ đóng đinh của ge ở hai hộp → không gom được");

                var g3 = dead();
                g3.TopBox(0).Slots[3] = new Tile { Uid = "e4", CardId = "e4", GroupId = "ge", Lock = new Lock { Kind = LockKind.Moves, Need = 3 } };
                g3.TopBox(1).Slots[2].Lock = new Lock { Kind = LockKind.Moves, Need = 3 };
                Ok(!g3.IsDeadBoard(), "Chết: thẻ băng không chặn gom — còn nước là băng còn tan");

                var g4 = dead();
                g4.TopBox(1).Slots[1] = null; g4.TopBox(1).Slots[2] = null;
                Ok(!g4.IsDeadBoard(), "Chết: đủ 4 ô trống → stack 0 rút rỗng được → còn sống");

                var g5 = dead();
                g5.TopBox(1).Slots[1] = null; g5.TopBox(1).Slots[2] = null;
                g5.TopBox(1).Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(g5.HasAnyMove() && g5.CheckStatus() == GameStatus.Stuck, "Chết: ô trống trong hộp khoá không tính");

                var g6 = dead();
                g6.TopBox(1).Slots[1] = null; g6.TopBox(1).Slots[2] = null;
                g6.TopBox(0).Slots[0].Lock = fixedLock;
                Ok(g6.IsDeadBoard(), "Chết: hộp duy nhất rút được lại có thẻ đóng đinh → chết");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck ở Global Constraints.
Expected: lỗi compile `'Game' does not contain a definition for 'IsDeadBoard'`.

- [ ] **Step 3: Thêm `IsDeadBoard` vào `GameBlockers.cs`**

Đổi dòng `using System;` đầu file thành:

```csharp
using System;
using System.Collections.Generic;
```

Ngay TRƯỚC dòng `        /// <summary>Nhóm gid còn thẻ nào trên bàn không. Tính cả thẻ nhóm con: nhóm cha chỉ` thêm:

```csharp
        /// <summary>
        /// Bàn chết: còn nước đi nhưng không nhóm nào nổ được nữa, đi bao nhiêu cũng vậy.
        /// Chỉ xét hộp trên đang mở — hộp khoá chỉ mở khi có nhóm được gom, mà bàn chết thì
        /// không còn nhóm nào gom được, nên bỏ hẳn chúng ra là chính xác chứ không phải
        /// xấp xỉ. Spec 2026-10-02-shuffle-redesign Mục 8.
        ///
        /// Chết khi CẢ HAI cùng đúng:
        ///   D1 — không hộp nào làm rỗng được, nên không lộ được thẻ chôn. Nước đi chỉ dời
        ///        ô trống chứ không đổi TỔNG ô trống, nên rút hết n thẻ khỏi hộp X cần n ô
        ///        trống ở các hộp mở khác ngay bây giờ. Hộp đáy rỗng không bị xoá, hộp có
        ///        thẻ đóng đinh không rỗng được — cả hai loại không tính.
        ///   D2 — không nhóm nào gom được tại chỗ: đủ 4 thẻ trong hộp mở VÀ mọi thẻ đóng đinh
        ///        của nó cùng một hộp. Thẻ băng không chặn vì còn nước đi là băng còn tan.
        ///
        /// Chỉ đếm, không tìm kiếm: bỏ sót vài bàn chết hiếm (solver lo), nhưng KHÔNG BAO GIỜ
        /// gọi nhầm bàn sống là chết — Solver cắt nhánh theo đúng kết quả này.
        /// </summary>
        public bool IsDeadBoard()
        {
            var open = new List<Box>();
            int free = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                var top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;
                open.Add(top);
                free += FreeCount(top);
            }

            foreach (var b in open)
            {
                if (b.IsBottom) continue;
                int tiles = 0;
                bool nailed = false;
                foreach (var t in b.Slots)
                {
                    if (t == null) continue;
                    tiles++;
                    if (IsFixed(t)) nailed = true;
                }
                if (!nailed && tiles <= free - FreeCount(b)) return false;
            }

            var count = new Dictionary<string, int>();
            var nailBox = new Dictionary<string, Box>();
            var split = new HashSet<string>();
            foreach (var b in open)
                foreach (var t in b.Slots)
                {
                    if (t == null) continue;
                    int n;
                    count.TryGetValue(t.GroupId, out n);
                    count[t.GroupId] = n + 1;
                    if (!IsFixed(t)) continue;
                    Box first;
                    if (!nailBox.TryGetValue(t.GroupId, out first)) nailBox[t.GroupId] = b;
                    else if (first != b) split.Add(t.GroupId);
                }
            foreach (var kv in count)
                if (kv.Value >= Rules.GroupSize && !split.Contains(kv.Key)) return false;
            return true;
        }

```

- [ ] **Step 4: `CheckStatus` gọi `IsDeadBoard`**

Trong `Assets/_Game/Board/Domain/Game.cs`, tìm:

```csharp
        // thua ngầm (spec 2.1).
        public GameStatus CheckStatus()
        {
            if (TotalTiles() == 0) return GameStatus.Won;
            return HasAnyMove() ? GameStatus.Playing : GameStatus.Stuck;
        }
```

thay bằng:

```csharp
        // thua ngầm (spec 2.1). Còn nước mà bàn đã chết (IsDeadBoard) cũng là kẹt — đi mãi
        // không nổ được nhóm nào nữa (spec 2026-10-02-shuffle-redesign Mục 8).
        public GameStatus CheckStatus()
        {
            if (TotalTiles() == 0) return GameStatus.Won;
            return HasAnyMove() && !IsDeadBoard() ? GameStatus.Playing : GameStatus.Stuck;
        }
```

- [ ] **Step 5: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck. Expected: `SelfCheck OK - 2 level, ...` (solver lv-009/lv-010 vẫn giải được — bàn chết chỉ cắt nhánh không thắng được).
Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameBlockers.cs Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "Board: dead board counts as stuck

A board with legal moves left but no way to ever clear a group again
(no box can be emptied, no group gatherable in the open top boxes) is
now Stuck, so the player gets the stuck lose popup and Magnet revive
instead of moving tiles around until out of moves.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Chọn nhóm mồi

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`PickPrimeCandidates` dòng ~166-213; `ApplyShuffle` dòng ~553)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (2 câu kiểm ở 8g/8h; mục 8j)

**Interfaces:**
- Consumes: helper SelfCheck của Task 1.
- Produces: `PickPrimeCandidates(int max)` nhận nhóm có tối đa 1 thẻ bất động ở lớp trên, loại nhóm có thẻ trong hộp khoá, xếp nhóm băng sau. Trong `SelfCheck.Run` có local `const string AnchorLv` và `Func<Lock, Game> anchored` (bàn có a1 ở stack 0 mang lock truyền vào) — Task 4 và 5 dùng lại.

- [ ] **Step 1: Sửa 2 câu kiểm cũ + thêm mục 8j**

Trong `SelfCheck.cs` (mục 8g), tìm:

```csharp
                Ok(!cands.Contains("gc"), "Xáo: nhóm có thẻ băng không làm mồi");
```

thay bằng (gc vẫn bị loại, nhưng giờ vì e2 nằm trong hộp đóng):

```csharp
                Ok(!cands.Contains("gc"), "Xáo: gc có e2 trong hộp đóng nên không làm mồi, dù chỉ một thẻ băng");
```

Trong mục 8h, tìm:

```csharp
                Ok(!g2.PickPrimeCandidates(3).Contains("gc"), "Xáo: nhóm có thẻ đóng đinh không làm mồi");
```

thay bằng:

```csharp
                Ok(g2.PickPrimeCandidates(9).Contains("gc"), "Xáo: nhóm có MỘT thẻ đóng đinh vẫn làm mồi — thẻ đinh là mốc hộp chủ");
```

Ngay TRƯỚC dòng `            log("SelfCheck OK — "`, thêm:

```csharp
            // 8j. Xáo — chọn nhóm mồi (spec 2026-10-02-shuffle-redesign Mục 2). a1 ở stack 0 là
            // thẻ dùng làm mốc, a2 a3 trắng, a4 chôn dưới stack 0.
            const string AnchorLv = @"{'id':'t-anchor','title':'t','layout':{'stacks':[
              {'pos':[0,0],'boxes':[{'slots':['a1','b1',null,null]},{'slots':['a4','c3',null,null]}]},
              {'pos':[1,0],'boxes':[{'slots':['a2','c1',null,null]}]},
              {'pos':[2,0],'boxes':[{'slots':['c2','a3',null,null]}]}]},";
            Func<Lock, Game> anchored = l =>
            {
                var ga = BuildQ(AnchorLv + Meaning("ga:a", "gb:b", "gc:c"));
                ga.TopBox(0).Slots[0].Lock = l;
                return ga;
            };
            {
                var ice = new Lock { Kind = LockKind.Moves, Need = 2 };
                Ok(anchored(new Lock { Kind = LockKind.Fixed }).PickPrimeCandidates(9).Contains("ga"),
                   "Mồi: một thẻ đóng đinh ở lớp trên → vẫn làm mồi");
                Ok(anchored(ice).PickPrimeCandidates(9).Contains("ga"), "Mồi: một thẻ băng ở lớp trên → vẫn làm mồi");

                var two = anchored(ice);
                two.Stacks[0].Boxes[1].Slots[0].Lock = ice;   // a4 cũng băng
                Ok(!two.PickPrimeCandidates(9).Contains("ga"), "Mồi: hai thẻ bất động → bỏ nhóm");

                var buried = anchored(default(Lock));
                buried.Stacks[0].Boxes[1].Slots[0].Lock = ice;
                Ok(!buried.PickPrimeCandidates(9).Contains("ga"), "Mồi: thẻ băng bị chôn không làm mốc được → bỏ nhóm");

                var locked = anchored(default(Lock));
                locked.TopBox(1).Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(!locked.PickPrimeCandidates(9).Contains("ga"), "Mồi: có thẻ trong hộp khoá → bỏ nhóm");

                // ga nhiều thẻ lớp trên hơn gb, nhưng a1 băng → gb lên trước.
                const string IceOrderLv = @"{'id':'t-ice','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','a2','b1',null]},{'slots':['b2','b3','b4',null]}]},
                  {'pos':[1,0],'boxes':[{'slots':['a3','a4',null,null]}]}]},";
                var o1 = BuildQ(IceOrderLv + Meaning("ga:a", "gb:b"));
                CardOf(o1, "a1").Lock = new Lock { Kind = LockKind.Moves, Need = 3 };
                var p1 = o1.PickPrimeCandidates(9);
                Ok(p1.Count == 2 && p1[0] == "gb" && p1[1] == "ga", "Mồi: nhóm có băng xếp sau nhóm không băng");

                var o2 = BuildQ(IceOrderLv + Meaning("ga:a", "gb:b"));
                CardOf(o2, "a1").Lock = new Lock { Kind = LockKind.Moves, Need = 3 };
                CardOf(o2, "b1").Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                var p2 = o2.PickPrimeCandidates(9);
                Ok(p2.Count == 2 && p2[0] == "gb", "Mồi: giữa các nhóm băng, băng còn ít nước tan hơn lên trước");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck. Expected: `SELFCHECK FAIL: Xáo: nhóm có MỘT thẻ đóng đinh vẫn làm mồi — thẻ đinh là mốc hộp chủ`.

- [ ] **Step 3: Viết lại `PickPrimeCandidates`**

Trong `GameShuffle.cs`, thay TOÀN BỘ khối từ dòng `        /// <summary>` ngay trên `        /// Các nhóm đáng dựng mồi, nhiều nhất <paramref name="max"/> nhóm.` tới hết method `PickPrimeCandidates` (dòng `        }` ngay trước `        static int SlotKey(`) bằng:

```csharp
        /// <summary>
        /// Mọi nhóm đáng dựng mồi theo thứ tự thử, nhiều nhất <paramref name="max"/> nhóm.
        ///
        /// Nhận nhóm có ĐỦ 4 thẻ đang tồn tại trên bàn (nhóm cha còn nhóm con chưa collapse
        /// thì thiếu thẻ — cùng ràng buộc với Magnet), KHÔNG thẻ nào nằm trong hộp khoá, và
        /// tối đa MỘT thẻ bất động (băng hoặc đóng đinh). Thẻ bất động đó phải ở lớp trên:
        /// nó không dời được nên hộp chứa nó chính là hộp chủ (spec 2026-10-02 Mục 1).
        ///
        /// Thứ tự: nhóm không băng trước; giữa các nhóm băng thì băng còn ít nước tan trước
        /// (mồi có băng chỉ nổ khi băng tan); rồi nhiều thẻ sẵn ở lớp trên trước (ít phải kéo
        /// donor); hoà thì theo group id cho kết quả xác định, test lại được.
        /// </summary>
        public List<string> PickPrimeCandidates(int max)
        {
            var onBoard = new Dictionary<string, int>();
            var onTop = new Dictionary<string, int>();
            var pinned = new Dictionary<string, int>();
            var iceLeft = new Dictionary<string, int>();
            var bad = new HashSet<string>();
            var order = new List<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        Tile t = slots[i];
                        if (t == null) continue;
                        string gid = t.GroupId;
                        if (!onBoard.ContainsKey(gid))
                        {
                            order.Add(gid);
                            onBoard[gid] = 0; onTop[gid] = 0; pinned[gid] = 0; iceLeft[gid] = 0;
                        }
                        onBoard[gid]++;
                        if (b == 0) onTop[gid]++;
                        if (!IsOpen(boxes[b].Lock)) bad.Add(gid);   // hộp khoá: bỏ cả nhóm
                        if (!IsFrozen(t) && !IsFixed(t)) continue;
                        pinned[gid]++;
                        if (b != 0) bad.Add(gid);                   // băng bị chôn không làm mốc được
                        if (IsFrozen(t)) iceLeft[gid] = t.Lock.Need - t.Lock.Have;
                    }
                }
            }

            var eligible = new List<string>();
            for (int k = 0; k < order.Count; k++)
            {
                string gid = order[k];
                if (onBoard[gid] == Rules.GroupSize && !bad.Contains(gid) && pinned[gid] <= 1) eligible.Add(gid);
            }

            eligible.Sort(delegate (string a, string b)
            {
                if (iceLeft[a] != iceLeft[b]) return iceLeft[a] - iceLeft[b];   // 0 = không băng → lên đầu
                if (onTop[a] != onTop[b]) return onTop[b] - onTop[a];
                return string.CompareOrdinal(a, b);
            });

            if (eligible.Count > max) eligible.RemoveRange(max, eligible.Count - max);
            return eligible;
        }

```

Trong `ApplyShuffle`, tìm:

```csharp
            List<string> candidates = PickPrimeCandidates(3);
```

thay bằng:

```csharp
            // Lấy HẾT ứng viên: ứng viên đầu dựng hỏng thì ứng viên sau vẫn có cơ hội, vòng
            // dựng tự dừng ở 3 mồi.
            List<string> candidates = PickPrimeCandidates(int.MaxValue);
```

- [ ] **Step 4: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`. Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "Shuffle: frozen and fixed tiles no longer rule a group out as bait

A group with one frozen or fixed tile in the top layer can now be bait
(that tile will anchor the host box), a group with a tile in a locked
box is skipped, ice groups are tried last, and every candidate is tried
until three baits are built.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Lấp hộp trên rỗng + nới bất biến

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`EnsureEveryTopBoxOccupied` dòng ~303-352; `ApplyShuffle`; `ValidateShuffle`)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (mục 8l)

**Interfaces:**
- Produces: `EnsureEveryTopBoxOccupied(List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand, HashSet<string> movers = null)`; `ValidateShuffle(..., HashSet<string> movers)`; trong `ApplyShuffle` có biến `movers` (Task 4 truyền nó vào `TryPrimeGroup`).

- [ ] **Step 1: Thêm mục 8l vào SelfCheck**

Ngay TRƯỚC dòng `            log("SelfCheck OK — "`, thêm:

```csharp
            // 8l. Xáo — lấp hộp trên rỗng (spec 2026-10-02-shuffle-redesign Mục 4). Stack 2 trên
            // rỗng, tay cạn: phải xé cụm đôi ở hộp nhiều thẻ nhất, không đụng cụm ba.
            {
                const string FillLv = @"{'id':'t-fill','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','a2','b1','b2']}]},
                  {'pos':[1,0],'boxes':[{'slots':['c1','c2','c3',null]}]},
                  {'pos':[2,0],'boxes':[{'slots':[null,null,null,null]},{'slots':['d1',null,null,null]}]}]},";
                var g = BuildQ(FillLv + Meaning("ga:a", "gb:b", "gc:c", "gd:d"));
                Tile a1 = g.TopBox(0).Slots[0];
                var movers = new HashSet<string>();
                Ok(g.EnsureEveryTopBoxOccupied(g.AssignableTopSlots(), new HashSet<int>(), new List<Tile>(), movers),
                   "Lấp hộp: tay cạn vẫn lấp được nhờ xé cụm");
                Ok(g.TopBox(2).Slots.Contains(a1) && movers.Contains(a1.Uid), "Lấp hộp: xé cụm đôi ở hộp nhiều thẻ nhất, ghi vào movers");
                Ok(InBox(g.TopBox(1), "gc") == 3, "Lấp hộp: cụm ba không bị xé khi còn cụm đôi");

                var gl = BuildQ(FillLv + Meaning("ga:a", "gb:b", "gc:c", "gd:d"));
                gl.TopBox(2).Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(gl.EnsureEveryTopBoxOccupied(gl.AssignableTopSlots(), new HashSet<int>(), new List<Tile>()),
                   "Lấp hộp: hộp khoá rỗng được bỏ qua");
                Ok(gl.TopBox(2).Slots.All(t => t == null), "Lấp hộp: hộp khoá vẫn rỗng");

                // Hộp khoá rỗng từng làm mọi lần xáo thất bại.
                const string LockedEmptyLv = @"{'id':'t-lockedempty','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','b1',null,null]},{'slots':['a4','c1','c2','b2']}]},
                  {'pos':[1,0],'boxes':[{'slots':['a2','b3',null,null]}]},
                  {'pos':[2,0],'boxes':[{'slots':['a3',null,null,null]}]},
                  {'pos':[3,0],'boxes':[{'slots':[null,null,null,null]}]}]},";
                var ge = BuildQ(LockedEmptyLv + Meaning("ga:a", "gb:b", "gc:c"));
                ge.TopBox(3).Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(ge.ApplyShuffle().Ok, "Lấp hộp: hộp khoá rỗng không làm xáo thất bại");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck. Expected: lỗi compile `No overload for method 'EnsureEveryTopBoxOccupied' takes 4 arguments`.

- [ ] **Step 3: Viết lại `EnsureEveryTopBoxOccupied` + hai hàm tìm donor**

Trong `GameShuffle.cs`, thay TOÀN BỘ khối từ dòng `        /// <summary>` ngay trên `        /// Mỗi top box phải giữ ≥1 thẻ. Hộp top rỗng bị SettleStep xoá, hộp dưới lộ ra,` tới hết method `EnsureEveryTopBoxOccupied` (dòng `        }` ngay trước `        /// <summary>` của `        /// Gom cụm phần còn lại trong tay`) bằng:

```csharp
        /// <summary>
        /// Mỗi top box ĐANG MỞ phải giữ ≥1 thẻ. Hộp top rỗng bị SettleStep xoá, hộp dưới lộ
        /// ra, tổng thẻ lớp trên tăng — vỡ bất biến 1. Hộp khoá rỗng thì bỏ qua: nó không
        /// nhận thẻ và không bị xoá, đó là lỗi level chứ không phải việc của Shuffle.
        ///
        /// Tay cạn thì mượn: trước hết thẻ trắng chưa reserved; hết thì XÉ một cụm người chơi
        /// đã gom (spec 2026-10-02 Mục 3) — thẻ bị xé ghi vào <paramref name="movers"/>.
        ///
        /// Chạy TRƯỚC ClusterHand chứ không phải sau: gom cụm reserve hết ô trống, chạy
        /// sau thì không còn thẻ nào mượn được và cả lượt shuffle bị rollback oan.
        /// </summary>
        public bool EnsureEveryTopBoxOccupied(List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand,
                                              HashSet<string> movers = null)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                if (box == null) return false;
                if (!IsOpen(box.Lock) || BoxTileCount(box) > 0) continue;

                int slot = FirstOpen(pool, reserved, s);
                if (slot < 0) return false;

                if (hand.Count > 0)
                {
                    box.Slots[slot] = hand[0];
                    hand.RemoveAt(0);
                    reserved.Add(SlotKey(s, slot));
                    continue;
                }

                SlotRef donor;
                bool split = false;
                if (!FindLooseDonor(pool, reserved, s, out donor))
                {
                    if (!FindSplitDonor(reserved, s, out donor)) return false;
                    split = true;
                }

                Box db = TopBox(donor.Stack);
                Tile moved = db.Slots[donor.Slot];
                box.Slots[slot] = moved;
                db.Slots[donor.Slot] = null;
                reserved.Add(SlotKey(s, slot));
                if (split && movers != null) movers.Add(moved.Uid);
            }
            return true;
        }

        // Thẻ trắng CHƯA reserved ở hộp đang có ≥2 thẻ — ô đã reserved là Nhóm mồi, đụng
        // vào là phá thứ vừa dựng.
        bool FindLooseDonor(List<SlotRef> pool, HashSet<int> reserved, int target, out SlotRef donor)
        {
            donor = default(SlotRef);
            for (int d = 0; d < Stacks.Count; d++)
            {
                if (d == target) continue;
                Box db = TopBox(d);
                if (db == null || BoxTileCount(db) < 2) continue;
                for (int i = 0; i < db.Slots.Length; i++)
                {
                    if (db.Slots[i] == null || reserved.Contains(SlotKey(d, i)) || !InPool(pool, d, i)) continue;
                    donor = new SlotRef { Stack = d, Box = 0, Slot = i };
                    return true;
                }
            }
            return false;
        }

        // Thẻ trong một cụm người chơi đã gom, để xé lấp hộp rỗng. Không lấy thẻ băng, đóng
        // đinh, hộp khoá, hay ô reserved (mồi). Ưu tiên: xé đôi trước xé ba (cụm 3 gần nổ
        // hơn) → hộp nhiều thẻ nhất → stack nhỏ nhất → ô nhỏ nhất.
        bool FindSplitDonor(HashSet<int> reserved, int target, out SlotRef donor)
        {
            donor = default(SlotRef);
            bool found = false;
            int bestPair = 0, bestTiles = 0;
            for (int d = 0; d < Stacks.Count; d++)
            {
                Box db = TopBox(d);
                if (d == target || db == null || !IsOpen(db.Lock)) continue;
                int tiles = BoxTileCount(db);
                if (tiles < 2) continue;
                for (int i = 0; i < db.Slots.Length; i++)
                {
                    Tile t = db.Slots[i];
                    if (t == null || reserved.Contains(SlotKey(d, i)) || IsFrozen(t) || IsFixed(t)) continue;
                    int size = CountGroupInBox(db, t.GroupId);
                    if (size < 2) continue;
                    int pair = size == 2 ? 1 : 0;
                    if (found && (pair < bestPair || (pair == bestPair && tiles <= bestTiles))) continue;
                    found = true; bestPair = pair; bestTiles = tiles;
                    donor = new SlotRef { Stack = d, Box = 0, Slot = i };
                }
            }
            return found;
        }

```

- [ ] **Step 4: `ApplyShuffle` giữ `movers`, `ValidateShuffle` nới bất biến**

Trong `ApplyShuffle`, tìm:

```csharp
            var hand = new List<Tile>();
            DrainAll(pool, hand);
```

thay bằng:

```csharp
            var hand = new List<Tile>();
            var movers = new HashSet<string>();   // thẻ có màu được dời hợp lệ — xem ValidateShuffle
            DrainAll(pool, hand);
```

Tìm:

```csharp
            bool seeded = EnsureEveryTopBoxOccupied(pool, reserved, hand);
```

thay bằng:

```csharp
            bool seeded = EnsureEveryTopBoxOccupied(pool, reserved, hand, movers);
```

Tìm:

```csharp
            if (!seeded || hand.Count > 0 || !ValidateShuffle(topBefore, before, whiteBefore))
```

thay bằng:

```csharp
            if (!seeded || hand.Count > 0 || !ValidateShuffle(topBefore, before, whiteBefore, movers))
```

Trong `ValidateShuffle`, tìm:

```csharp
        // Bốn bất biến của spec Mục 5.
        bool ValidateShuffle(int topBefore, Dictionary<string, SlotRef> before, HashSet<string> whiteBefore)
```

thay bằng:

```csharp
        // Bốn bất biến của spec 2026-08-26 Mục 5, nới theo spec 2026-10-02-shuffle-redesign
        // Mục 5: thẻ có màu trong movers (cặp dời, thẻ dời giữa hai cụm đôi, thẻ xé) được đổi chỗ.
        bool ValidateShuffle(int topBefore, Dictionary<string, SlotRef> before, HashSet<string> whiteBefore,
                             HashSet<string> movers)
```

Tìm:

```csharp
                if (top == null || BoxTileCount(top) == 0) return false;
```

thay bằng:

```csharp
                if (top == null || (IsOpen(top.Lock) && BoxTileCount(top) == 0)) return false;   // hộp khoá được rỗng
```

Tìm:

```csharp
                if (whiteBefore.Contains(kv.Key)) continue;   // vốn trắng, được phép đổi chỗ
```

thay bằng:

```csharp
                if (whiteBefore.Contains(kv.Key)) continue;   // vốn trắng, được phép đổi chỗ
                if (movers.Contains(kv.Key)) continue;        // dời hợp lệ (spec 2026-10-02 Mục 5)
```

- [ ] **Step 5: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`. Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK (`BoardShuffleTests` gọi `EnsureEveryTopBoxOccupied` 3 tham số vẫn compile nhờ tham số mặc định).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "Shuffle: fill an emptied top box by splitting a pair, skip locked boxes

When no loose tile is left to refill an emptied top box, take a tile
from a player-built pair (pairs before triples, fullest box first)
instead of failing the whole shuffle. Locked boxes no longer count in
the empty-box check, so an empty locked box stops failing every press.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Hộp chủ — mốc, hai cụm đôi, dời cụm đôi

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`TryPrimeGroup` dòng ~252-301 + helper mới; `ApplyShuffle`)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (mục 8k)

**Interfaces:**
- Consumes: `movers` trong `ApplyShuffle` (Task 3); `anchored`, `AnchorLv` (Task 2); helper SelfCheck (Task 1).
- Produces: `TryPrimeGroup(string gid, List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand, HashSet<string> movers = null)`.

- [ ] **Step 1: Thêm mục 8k vào SelfCheck**

Ngay TRƯỚC dòng `            log("SelfCheck OK — "`, thêm:

```csharp
            // 8k. Xáo — hộp chủ (spec 2026-10-02-shuffle-redesign Mục 3).
            {
                foreach (var kind in new[] { LockKind.Fixed, LockKind.Moves })
                {
                    var g = anchored(new Lock { Kind = kind, Need = 2 });
                    Tile a1 = g.TopBox(0).Slots[0];
                    var r = g.ApplyShuffle();
                    Ok(r.Ok, "Hộp chủ (" + kind + "): xáo thành công");
                    Ok(g.TopBox(0).Slots[0] == a1, "Hộp chủ (" + kind + "): mốc đứng yên đúng ô");
                    Ok(InBox(g.TopBox(0), "ga") == 3 && Game.FreeCount(g.TopBox(0)) >= 1,
                       "Hộp chủ (" + kind + "): hộp chứa mốc thành hộp chủ, 3 thẻ ga + ô trống");
                    Ok(g.CountPrimedGroups() >= 1, "Hộp chủ (" + kind + "): có nhóm mồi");
                    Ok(!r.Moves.Any(m => m.Uid == a1.Uid), "Hộp chủ (" + kind + "): mốc không nằm trong Moves");
                }

                // Cụm đôi a1 a2 người chơi đã gom ở stack 0; a3 trắng; a4 chôn dưới stack 1.
                const string PairLv = @"{'id':'t-pair','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','a2','b1',null]}]},
                  {'pos':[1,0],'boxes':[{'slots':['a3','c1',null,null]},{'slots':['a4','c2',null,null]}]},
                  {'pos':[2,0],'boxes':[{'slots':['c3',null,null,null]}]}]},";
                var gp = BuildQ(PairLv + Meaning("ga:a", "gb:b", "gc:c"));
                Tile p1 = gp.TopBox(0).Slots[0], p2 = gp.TopBox(0).Slots[1];
                Ok(gp.ApplyShuffle().Ok, "Hộp chủ cụm đôi: xáo thành công");
                Ok(gp.TopBox(0).Slots[0] == p1 && gp.TopBox(0).Slots[1] == p2, "Hộp chủ cụm đôi: cụm đôi đứng yên");
                Ok(InBox(gp.TopBox(0), "ga") == 3 && Game.FreeCount(gp.TopBox(0)) == 1,
                   "Hộp chủ cụm đôi: hộp của cụm đôi thành hộp chủ");

                // Hai cụm đôi ga ở hai hộp.
                const string TwoPairsLv = @"{'id':'t-two','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','a2',null,null]}]},
                  {'pos':[1,0],'boxes':[{'slots':['a3','a4','b1','b2']}]}]},";
                var gt = BuildQ(TwoPairsLv + Meaning("ga:a", "gb:b"));
                int topT = gt.TopLayerTileCount();
                var rt = gt.ApplyShuffle();
                Ok(rt.Ok && rt.Moves.Length == 1, "Hai cụm đôi: đúng một thẻ đổi chỗ");
                Ok(InBox(gt.TopBox(0), "ga") == 3 && Game.FreeCount(gt.TopBox(0)) == 1, "Hai cụm đôi: stack 0 thành cụm 3 + ô trống");
                Ok(InBox(gt.TopBox(1), "ga") == 1 && InBox(gt.TopBox(1), "gb") == 2,
                   "Hai cụm đôi: thẻ ga ở lại là thẻ thứ 4, cụm gb không bị đụng");
                Ok(gt.TopLayerTileCount() == topT && !gt.AnyBoxHasFullGroup(), "Hai cụm đôi: giữ bất biến");

                // Không hộp nào trống đủ 4 ô. Stack 0 chỉ vướng cụm đôi gb và ngồi trên hộp có a3 a4.
                const string MergeLv = @"{'id':'t-merge','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['b1','b2','a2',null]},{'slots':['a3','a4','h1','h2']}]},
                  {'pos':[1,0],'boxes':[{'slots':['d1','d2','f1',null]}]},
                  {'pos':[2,0],'boxes':[{'slots':['a1','e1','e2','g1']}]}]},";
                var gm = BuildQ(MergeLv + Meaning("ga:a", "gb:b", "gd:d", "ge:e", "gf:f", "gg:g", "gh:h"));
                Tile b1 = CardOf(gm, "b1"), b2 = CardOf(gm, "b2");
                int topM = gm.TopLayerTileCount();
                Ok(gm.ApplyShuffle().Ok, "Dời cụm đôi: xáo thành công");
                Ok(InBox(gm.TopBox(0), "ga") == 3 && Game.FreeCount(gm.TopBox(0)) == 1, "Dời cụm đôi: stack 0 được giải phóng làm hộp chủ");
                Ok(gm.TopBox(1).Slots.Contains(b1) && gm.TopBox(1).Slots.Contains(b2), "Dời cụm đôi: cặp gb dời nguyên cặp sang stack 1");
                Ok(InBox(gm.TopBox(1), "gd") == 2, "Dời cụm đôi: cụm gd ở hộp nhận còn nguyên");
                Ok(gm.CountPrimedGroups() >= 1, "Dời cụm đôi: có nhóm mồi");
                Ok(gm.TopLayerTileCount() == topM && !gm.AnyBoxHasFullGroup(), "Dời cụm đôi: giữ bất biến");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck. Expected: `SELFCHECK FAIL: Hộp chủ (Fixed): hộp chứa mốc thành hộp chủ, 3 thẻ ga + ô trống`.

- [ ] **Step 3: Viết lại `TryPrimeGroup` + helper**

Trong `GameShuffle.cs`, thay TOÀN BỘ khối từ dòng `        /// <summary>` ngay trên `        /// Dựng Nhóm mồi 3+1: 3 thẻ vào hộp chủ (hộp đó còn ĐÚNG 1 ô trống được giữ chỗ),` tới hết method `TryPrimeGroup` (dòng `        }` ngay trước `        /// <summary>` của `        /// Mỗi top box ĐANG MỞ phải giữ ≥1 thẻ.` — đã viết ở Task 3) bằng:

```csharp
        /// <summary>
        /// Dựng Nhóm mồi 3+1: 3 thẻ trong hộp chủ (hộp đó còn ĐÚNG 1 ô trống được giữ chỗ),
        /// thẻ thứ 4 ở top box khác. Người chơi kéo một nước là nổ.
        ///
        /// Hộp chủ chọn theo thứ tự (spec 2026-10-02 Mục 2):
        ///   1. Có MỐC — thẻ gid Shuffle không dời được (băng, đóng đinh, cụm người chơi đã
        ///      gom) nằm gọn trong MỘT hộp → hộp đó là hộp chủ, mốc tính vào 3 thẻ.
        ///   2. Hai cụm đôi gid ở hai hộp → TryPrimeFromTwoPairs.
        ///   3. Hộp có đủ 4 ô mở, ưu tiên hộp có layer 2 nhiều thẻ nhất: nổ xong hộp chủ bị
        ///      xoá, hộp dưới lộ ra, ngồi trên hộp đầy thì lượt sau có nhiều nguyên liệu nhất.
        ///   4. Không hộp nào đủ 4 ô → dời một cụm đôi để giải phóng hộp (PlanPairMerge).
        /// Mốc rải theo hình khác (vd băng ở một hộp + đôi ở hộp khác) → bỏ nhóm này.
        ///
        /// <paramref name="movers"/> nhận uid thẻ có màu bị dời hợp lệ — ValidateShuffle
        /// miễn kiểm "thẻ có màu đứng yên" cho đúng những thẻ đó.
        ///
        /// Trả false khi không xếp nổi — bên gọi phải coi đó là bình thường, không phải lỗi.
        /// </summary>
        public bool TryPrimeGroup(string gid, List<SlotRef> pool, HashSet<int> reserved, List<Tile> hand,
                                  HashSet<string> movers = null)
        {
            // Sau DrainAll thẻ trắng đã vào tay, nên thẻ gid còn ở lớp trên đều là mốc.
            var anchorStacks = new List<int>();
            int anchors = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                int n = top == null ? 0 : CountGroupInBox(top, gid);
                if (n == 0) continue;
                anchorStacks.Add(s);
                anchors += n;
            }
            if (anchorStacks.Count == 2)
                return TryPrimeFromTwoPairs(gid, anchorStacks[0], anchorStacks[1], pool, reserved, movers);
            if (anchorStacks.Count > 2) return false;

            int host, mergeTo = -1;
            if (anchorStacks.Count == 1)
            {
                host = anchorStacks[0];
                if (OpenCount(pool, reserved, host) < Rules.GroupSize - anchors) return false;
            }
            else
            {
                host = -1;
                for (int s = 0; s < Stacks.Count; s++)
                {
                    if (OpenCount(pool, reserved, s) < Rules.GroupSize) continue;
                    if (host < 0 || Layer2TileCount(s) > Layer2TileCount(host)) host = s;
                }
                if (host < 0 && !PlanPairMerge(pool, reserved, out host, out mergeTo)) return false;
            }

            int carrier = -1;
            for (int s = 0; s < Stacks.Count && carrier < 0; s++)
                if (s != host && OpenAfterMerge(pool, reserved, s, mergeTo) > 0) carrier = s;
            if (carrier < 0) return false;

            // Gom thẻ TRƯỚC khi dời cặp: gom thất bại thì lớp trên chưa bị đụng gì.
            var need = new List<Tile>();
            for (int k = 0; k < Rules.GroupSize - anchors; k++)
            {
                Tile t = TakeFromHand(hand, gid);
                if (t == null) t = SwapDonorIntoHand(gid, hand);
                if (t == null) { hand.AddRange(need); return false; }
                need.Add(t);
            }

            if (mergeTo >= 0) MergePair(host, mergeTo, pool, reserved, movers);
            ReserveGroup(host, gid, reserved);   // mốc: pha sau không được xé

            for (int k = 0; k < need.Count - 1; k++)
            {
                int slot = FirstOpen(pool, reserved, host);
                TopBox(host).Slots[slot] = need[k];
                reserved.Add(SlotKey(host, slot));
            }

            // Ô CHỪA TRỐNG — reserve để pha sau không lấp mất chỗ thả thẻ thứ 4.
            reserved.Add(SlotKey(host, FirstOpen(pool, reserved, host)));

            int cslot = FirstOpen(pool, reserved, carrier);
            TopBox(carrier).Slots[cslot] = need[need.Count - 1];
            reserved.Add(SlotKey(carrier, cslot));
            return true;
        }

        // Hai cụm đôi gid ở hai hộp, không thẻ nào băng/đóng đinh: dời một thẻ sang hộp kia
        // thành cụm 3, thẻ ở lại chính là thẻ thứ 4. Hộp nhận cần 2 ô mở (1 cho thẻ dời,
        // 1 chừa trống); hộp nhiều ô mở hơn làm hộp chủ, hoà lấy stack nhỏ hơn.
        bool TryPrimeFromTwoPairs(string gid, int a, int b, List<SlotRef> pool, HashSet<int> reserved,
                                  HashSet<string> movers)
        {
            if (!IsMovablePair(TopBox(a), gid) || !IsMovablePair(TopBox(b), gid)) return false;
            int host = OpenCount(pool, reserved, b) > OpenCount(pool, reserved, a) ? b : a;
            int from = host == a ? b : a;
            if (OpenCount(pool, reserved, host) < 2) return false;

            Box src = TopBox(from);
            int i = 0;
            while (src.Slots[i] == null || src.Slots[i].GroupId != gid) i++;
            Tile moved = src.Slots[i];
            src.Slots[i] = null;   // ô này không thuộc pool nên pha sau không lấp — đúng ý

            int slot = FirstOpen(pool, reserved, host);
            TopBox(host).Slots[slot] = moved;
            if (movers != null) movers.Add(moved.Uid);
            ReserveGroup(host, gid, reserved);
            ReserveGroup(from, gid, reserved);
            reserved.Add(SlotKey(host, FirstOpen(pool, reserved, host)));   // ô chừa trống
            return true;
        }

        static bool IsMovablePair(Box box, string gid)
        {
            int n = 0;
            foreach (Tile t in box.Slots)
            {
                if (t == null || t.GroupId != gid) continue;
                if (IsFrozen(t) || IsFixed(t)) return false;
                n++;
            }
            return n == 2;
        }

        void ReserveGroup(int stack, string gid, HashSet<int> reserved)
        {
            Box top = TopBox(stack);
            for (int i = 0; i < top.Slots.Length; i++)
                if (top.Slots[i] != null && top.Slots[i].GroupId == gid) reserved.Add(SlotKey(stack, i));
        }

        /// <summary>
        /// Không hộp nào đủ 4 ô mở: tìm hộp X mà thứ duy nhất chiếm chỗ là MỘT cụm đôi, và
        /// hộp D nhận được cặp đó (≥2 ô mở, chưa có thẻ nhóm đó — 2+2 cùng nhóm là tự nổ).
        /// Dời cặp đi thì X trống 4 ô, làm hộp chủ được. Chỉ LẬP kế hoạch, chưa dời — gom
        /// thẻ còn có thể thất bại; MergePair chạy sau khi đã chắc chắn.
        ///
        /// X: layer 2 nhiều thẻ nhất (cùng lý do với hộp chủ thường), hoà lấy stack nhỏ.
        /// D: stack nhỏ nhất thoả điều kiện VÀ sau khi nhận cặp vẫn còn hộp mang thẻ thứ 4.
        /// </summary>
        bool PlanPairMerge(List<SlotRef> pool, HashSet<int> reserved, out int host, out int dest)
        {
            host = -1; dest = -1;
            for (int x = 0; x < Stacks.Count; x++)
            {
                string pair = LonePairGroup(pool, reserved, x);
                if (pair == null) continue;
                if (host >= 0 && Layer2TileCount(x) <= Layer2TileCount(host)) continue;
                for (int d = 0; d < Stacks.Count; d++)
                {
                    if (d == x || OpenCount(pool, reserved, d) < 2) continue;
                    if (CountGroupInBox(TopBox(d), pair) > 0) continue;
                    bool carrier = false;
                    for (int s = 0; s < Stacks.Count && !carrier; s++)
                        if (s != x && OpenAfterMerge(pool, reserved, s, d) > 0) carrier = true;
                    if (!carrier) continue;
                    host = x; dest = d;
                    break;
                }
            }
            return host >= 0;
        }

        // Nhóm của cụm đôi nếu hộp chỉ còn đúng cụm đôi đó (2 thẻ cùng nhóm, không băng/đinh,
        // chưa reserved) + 2 ô mở. Ngược lại null.
        string LonePairGroup(List<SlotRef> pool, HashSet<int> reserved, int x)
        {
            Box top = TopBox(x);
            if (top == null || OpenCount(pool, reserved, x) != Rules.GroupSize - 2) return null;
            string gid = null;
            int n = 0;
            for (int i = 0; i < top.Slots.Length; i++)
            {
                Tile t = top.Slots[i];
                if (t == null) continue;
                if (reserved.Contains(SlotKey(x, i)) || IsFrozen(t) || IsFixed(t)) return null;
                if (gid != null && t.GroupId != gid) return null;
                gid = t.GroupId;
                n++;
            }
            return n == 2 ? gid : null;
        }

        int OpenAfterMerge(List<SlotRef> pool, HashSet<int> reserved, int stack, int mergeTo)
        {
            return OpenCount(pool, reserved, stack) - (stack == mergeTo ? 2 : 0);
        }

        // Dời cụm đôi của hộp x sang hộp d, vẫn là một cặp. Ô cũ của cặp vào pool để x thành
        // hộp chủ 4 ô mở.
        void MergePair(int x, int d, List<SlotRef> pool, HashSet<int> reserved, HashSet<string> movers)
        {
            Box src = TopBox(x), dst = TopBox(d);
            for (int i = 0; i < src.Slots.Length; i++)
            {
                Tile t = src.Slots[i];
                if (t == null) continue;
                int slot = FirstOpen(pool, reserved, d);
                dst.Slots[slot] = t;
                reserved.Add(SlotKey(d, slot));
                if (movers != null) movers.Add(t.Uid);
                src.Slots[i] = null;
                pool.Add(new SlotRef { Stack = x, Box = 0, Slot = i });
            }
        }

```

Trong `ApplyShuffle`, tìm:

```csharp
                if (TryPrimeGroup(candidates[k], pool, reserved, hand)) primed++;
```

thay bằng:

```csharp
                if (TryPrimeGroup(candidates[k], pool, reserved, hand, movers)) primed++;
```

- [ ] **Step 4: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`. Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "Shuffle: build bait around anchors, two pairs, or a moved pair

Tiles Shuffle cannot move (frozen, fixed, or a pair the player built)
now anchor the host box. Two pairs of the same group become a triple
plus the fourth tile with one move. When no box has four open slots,
a box holding a single pair is freed by moving that pair, intact, to
another box.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Không đổi được gì thì nút xám

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`ApplyShuffle` cuối method; thêm `ShuffleWouldChange` sau `CanShuffle`)
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` (mục 8m)
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (`RefreshBoosterAvailability` dòng ~753-759)
- Modify: `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs` (chú thích `IsUsable` dòng ~25-28)

**Interfaces:**
- Consumes: `anchored` (Task 2).
- Produces: `public bool Game.ShuffleWouldChange()`; `ApplyShuffle` trả `Ok = false` khi `Moves` rỗng.

- [ ] **Step 1: Thêm mục 8m vào SelfCheck**

Ngay TRƯỚC dòng `            log("SelfCheck OK — "`, thêm:

```csharp
            // 8m. Xáo — không đổi được gì thì nút xám (spec 2026-10-02-shuffle-redesign Mục 6).
            // Không thẻ trắng, không nhóm nào đủ 4 trên bàn.
            {
                const string NoopLv = @"{'id':'t-noop','title':'t','layout':{'stacks':[
                  {'pos':[0,0],'boxes':[{'slots':['a1','a2','b1','b2']}]},
                  {'pos':[1,0],'boxes':[{'slots':['c1','c2',null,null]}]}]},";
                var g = BuildQ(NoopLv + Meaning("ga:a", "gb:b", "gc:c"));
                string enc = Solver.Encode(g);
                Ok(g.CanShuffle(), "Xáo rỗng: tiền đề — còn ô trống");
                Ok(!g.ShuffleWouldChange(), "Xáo rỗng: chạy thử không đổi được gì → nút xám");
                Ok(Solver.Encode(g) == enc, "Xáo rỗng: chạy thử không đụng bàn thật");
                var r = g.ApplyShuffle();
                Ok(!r.Ok && r.Moves.Length == 0 && Solver.Encode(g) == enc, "Xáo rỗng: ApplyShuffle báo thất bại, bàn y nguyên");
                Ok(anchored(new Lock { Kind = LockKind.Fixed }).ShuffleWouldChange(), "Xáo rỗng: bàn có mồi dựng được thì nút sáng");
            }

```

- [ ] **Step 2: Chạy selfcheck — phải đỏ**

Run: lệnh selfcheck. Expected: lỗi compile `'Game' does not contain a definition for 'ShuffleWouldChange'`.

- [ ] **Step 3: `Moves` rỗng là thất bại + `ShuffleWouldChange`**

Trong `ApplyShuffle`, tìm:

```csharp
            return new ShuffleResult
            {
                Ok = true,
                Moves = DiffPositions(before),
                PrimedGroups = CountPrimedGroups(),
            };
```

thay bằng:

```csharp
            // Không thẻ nào đổi chỗ = bấm mà bàn y nguyên. Coi là thất bại để nút xám thay vì
            // ăn lượt người chơi mua bằng coin (spec 2026-10-02-shuffle-redesign Mục 6). Bàn
            // chưa đổi gì nên không cần khôi phục.
            ShuffleMove[] moves = DiffPositions(before);
            if (moves.Length == 0) return fail;

            return new ShuffleResult
            {
                Ok = true,
                Moves = moves,
                PrimedGroups = CountPrimedGroups(),
            };
```

Ngay SAU method `CanShuffle()` (sau dòng `        }` đóng nó, trước `        /// <summary>` của `CountPrimedGroups`), thêm:

```csharp
        /// <summary>
        /// Bấm Shuffle lúc này có làm bàn đổi không — chạy thử trên bản sao. ApplyShuffle xác
        /// định (cùng bàn → cùng kết quả), nên nút sáng theo hàm này thì bấm thật luôn thành
        /// công: không bao giờ ăn lượt mà bàn đứng yên (spec 2026-10-02 Mục 6).
        /// </summary>
        public bool ShuffleWouldChange()
        {
            return CanShuffle() && Clone().ApplyShuffle().Ok;
        }

```

- [ ] **Step 4: Chạy selfcheck — phải xanh**

Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`.

- [ ] **Step 5: Nút Shuffle sáng theo chạy thử**

Trong `Assets/_Game/Board/Views/BoardController.cs`, `RefreshBoosterAvailability`, tìm:

```csharp
            LevelSignals.SetShuffleAvailable(playing && g.CanShuffle());
```

thay bằng:

```csharp
            // Chạy thử trên bản sao: nút chỉ sáng khi bấm thật chắc chắn đổi được bàn — không
            // bao giờ ăn lượt mà bàn đứng yên (spec 2026-10-02-shuffle-redesign Mục 6).
            LevelSignals.SetShuffleAvailable(playing && g.ShuffleWouldChange());
```

Trong `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs`, tìm:

```csharp
        /// Lớp trên còn ô trống không. Hết ô trống thì không dựng nổi Nhóm mồi, mà lượt
        /// này người chơi mua bằng coin — để bấm hụt rồi mất lượt là mất tiền thật.
```

thay bằng:

```csharp
        /// Bàn chạy thử Shuffle trên bản sao thấy đổi được (Game.ShuffleWouldChange). Lượt
        /// này người chơi mua bằng coin — để bấm hụt rồi mất lượt là mất tiền thật.
```

- [ ] **Step 6: Compile**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs Assets/_Game/Board/Views/BoardController.cs Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs
git commit -m "Shuffle: grey the button unless a dry run changes the board

A shuffle that moves no tile now counts as a failure, and the button
is lit only when ApplyShuffle on a clone of the board succeeds. Since
ApplyShuffle is deterministic, a lit button always changes the board,
so a bought charge is never spent on a no-op.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Thẻ băng / đóng đinh đứng ngoài xoáy

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (`ShuffleAnimation` + `Vortex`, dòng ~486-512)

**Interfaces:**
- Consumes: `Game.IsFrozen(Tile)`, `Game.IsFixed(Tile)` (đã có, an toàn với null).

- [ ] **Step 1: `ShuffleAnimation` lấy danh sách thẻ đứng yên**

Tìm:

```csharp
        IEnumerator ShuffleAnimation(ShuffleResult r)
        {
            if (r.Moves.Length == 0) { RebuildBoardViews(); yield break; }
            yield return Vortex(true, A.shuffleInDur);
            RebuildBoardViews();
            yield return Vortex(false, A.shuffleOutDur);
        }
```

thay bằng:

```csharp
        IEnumerator ShuffleAnimation(ShuffleResult r)
        {
            if (r.Moves.Length == 0) { RebuildBoardViews(); yield break; }
            HashSet<string> stay = PinnedTopTiles();
            yield return Vortex(true, A.shuffleInDur, stay);
            RebuildBoardViews();
            yield return Vortex(false, A.shuffleOutDur, stay);
        }

        // Thẻ băng / đóng đinh ở lớp trên: Shuffle không dời chúng nên chúng đứng yên, không
        // bay vào xoáy (spec 2026-10-02-shuffle-redesign Mục 7). Đọc sau ApplyShuffle cũng
        // đúng — chúng chưa hề đổi chỗ.
        HashSet<string> PinnedTopTiles()
        {
            var stay = new HashSet<string>();
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                Box top = g.TopBox(s);
                if (top == null) continue;
                foreach (Tile t in top.Slots)
                    if (Game.IsFrozen(t) || Game.IsFixed(t)) stay.Add(t.Uid);
            }
            return stay;
        }
```

- [ ] **Step 2: `Vortex` bỏ qua thẻ đứng yên**

Tìm:

```csharp
        IEnumerator Vortex(bool inward, float dur)
```

thay bằng:

```csharp
        IEnumerator Vortex(bool inward, float dur, HashSet<string> stay)
```

Trong cùng method, tìm:

```csharp
            foreach (var tv in tiles.Values)
            {
                if (tv == null) continue;
                tv.SetFlying(true);                    // bay trên hộp, như MergeTiles
```

thay bằng:

```csharp
            foreach (var kv in tiles)
            {
                TileView tv = kv.Value;
                if (tv == null || stay.Contains(kv.Key)) continue;
                tv.SetFlying(true);                    // bay trên hộp, như MergeTiles
```

(`tiles` là `Dictionary<string, TileView>` key = uid thẻ. Vòng `SetFlying(false)` cuối method giữ nguyên — gọi trên thẻ chưa bay là vô hại.)

- [ ] **Step 3: Compile**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs
git commit -m "Shuffle: frozen and fixed tiles stay put during the vortex

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Cập nhật luật chơi + kiểm cuối

**Files:**
- Modify: `docs/wordstack-rules.md` (Mục 6; Mục 11 ba chỗ)

- [ ] **Step 1: Mục 6 — kẹt mới**

Tìm:

```markdown
- **Kẹt**: bàn đã đứng yên và **mọi hộp trên cùng đều đầy** (không còn slot trống nào ở bất kỳ
  hộp trên cùng nào) → không nước đi nào hợp lệ nữa.
- **Không có màn Thua.** Kẹt chỉ hiện một toast gợi ý bấm Restart.
```

thay bằng:

```markdown
- **Kẹt**: bàn đã đứng yên và một trong hai:
  - **Hết nước đi hợp lệ** — không thẻ nhặt được nào có hộp mở khác còn chỗ (xem Mục 11).
  - **Bàn chết** — còn nước nhưng không nhóm nào nổ được nữa. Chỉ xét hộp trên đang mở (hộp khoá chỉ
    mở khi có nhóm được gom): không hộp nào rút rỗng được (hộp rút rỗng được = không phải hộp đáy,
    không có thẻ đóng đinh, và ô trống ở các hộp mở khác ≥ số thẻ của nó) **và** không nhóm nào có đủ
    4 thẻ trong các hộp mở với mọi thẻ đóng đinh của nhóm nằm cùng một hộp.
- Kẹt → popup thua kẹt; hồi sinh = một phát nam châm miễn phí (khi còn nhóm hút được).
```

- [ ] **Step 2: Mục 11 — Xáo với băng/đinh, việc chờ tool**

Tìm:

```markdown
xếp nhóm có nó sau mọi nhóm khác; Xáo không đụng ba thứ đó; Undo không cần luật riêng vì ảnh chụp là toàn bàn. Undo chỉ lùi được nước **không**
```

thay bằng:

```markdown
xếp nhóm có nó sau mọi nhóm khác; Xáo không dời ba thứ đó, nhưng nhóm có **một** thẻ băng hoặc đóng đinh ở lớp trên vẫn làm mồi — thẻ đó là mốc hộp chủ, nhóm băng xếp sau (spec `2026-10-02-shuffle-redesign`); Undo không cần luật riêng vì ảnh chụp là toàn bàn. Undo chỉ lùi được nước **không**
```

Tìm:

```markdown
**Việc chờ cho tool xếp level / solver** (ghi 2026-10-02, từ phân tích booster Shuffle). Hai kiểu
```

thay bằng:

```markdown
**Việc chờ cho tool xếp level / solver** (ghi 2026-10-02, từ phân tích booster Shuffle). Ba kiểu
```

Tìm:

```markdown
  nhận thẻ thả vào và không bị xoá khi rỗng, nên mãi rỗng; Shuffle hiện tại thất bại mỗi lần bấm.
- Màn mà ngay lúc mở, **mọi thẻ lớp trên đều không nhặt được** (băng, đóng đinh, hoặc nằm trong
  hộp khoá). Bàn kẹt ngay nước đầu; solver đã bắt được nhưng nên báo lý do rõ ràng.
```

thay bằng:

```markdown
  nhận thẻ thả vào và không bị xoá khi rỗng, nên mãi rỗng; Shuffle bỏ qua hộp đó khi xét hộp rỗng.
- Màn mà ngay lúc mở, **mọi thẻ lớp trên đều không nhặt được** (băng, đóng đinh, hoặc nằm trong
  hộp khoá). Bàn kẹt ngay nước đầu; solver đã bắt được nhưng nên báo lý do rõ ràng.
- **Bàn chết mà luật kẹt đếm sót** (Mục 6 chỉ đếm, không tìm kiếm) — vd đủ 4 thẻ một nhóm ở lớp trên
  nhưng kẹt trong các hộp đầy không xoay xở được. Người chơi vẫn đi được mà không bao giờ nổ thêm
  nhóm nào; solver nên báo các màn có thể rơi vào thế này.
```

- [ ] **Step 3: Kiểm cuối toàn bộ**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.
Run: lệnh selfcheck → `SelfCheck OK - 2 level, ...`.
Run: `git status --short` → chỉ còn các file của user trong danh sách không-stage (Global Constraints).

- [ ] **Step 4: Commit**

```bash
git add docs/wordstack-rules.md
git commit -m "Rules: stuck now includes dead boards; Shuffle with frozen/fixed tiles

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Kiểm trong Unity (user làm)

- [ ] **Step 1: EditMode** — Test Runner → EditMode → chạy `WordStack.Board.Tests` → `BoardShuffleTests` (16 test), `BoardRulesTests`, `BoardMagnetTests` xanh.
- [ ] **Step 2: Play mode — bàn chết** — dựng tay một bàn như ví dụ spec Mục 8 (hoặc chơi tới thế đó): sau nước làm bàn chết, popup thua kẹt hiện ngay; hồi sinh hút một nhóm.
- [ ] **Step 3: Play mode — Shuffle** — lv-001 (có thẻ đóng đinh): bấm Shuffle → thẻ đinh đứng yên, không bay vào xoáy; nhóm của nó thành 3 + ô trống. Bàn mà mọi thẻ lớp trên đã thành cụm và không nhóm nào đủ 4 trên bàn → nút Shuffle xám.
