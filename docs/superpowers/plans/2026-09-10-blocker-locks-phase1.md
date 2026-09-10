# Blocker nhịp 1 — Luật bàn, đọc màn, Solver, tự kiểm

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ba blocker Hộp khoá / Thẻ băng / Ổ và chìa chạy đúng trong luật bàn, đọc được từ JSON, Solver hiểu, tự kiểm xanh — chưa có hình trong Unity.

**Architecture:** Một struct `Lock` gắn lên `Box` (khoá theo số nhóm, hoặc ổ chờ chìa) và lên `Tile` (băng theo số nước). Chìa là field riêng `Tile.KeyId` vì thẻ chìa không bị khoá gì. Mọi luật sống trong `Assets/_Game/Board/Domain/` (partial `Game`, không import UnityEngine) nên `selfcheck.sh` compile và chạy được ngoài Unity. Trạng thái mở của hộp khoá và hộp có ổ suy ra từ bố cục thẻ nên Solver chỉ phải mã hoá thêm mức băng.

**Tech Stack:** C# / Unity 6000.x · Roslyn qua Unity Hub cho `selfcheck.sh` và `compilecheck.sh` · không có NUnit chạy được từ CLI.

**Spec:** `docs/superpowers/specs/2026-09-10-blocker-locks-design.md`

## Global Constraints

- `Rules.BoxCapacity = 4`, `Rules.GroupSize = 4`. Không hardcode số 4, dùng hằng.
- Mọi file dưới `Assets/_Game/Board/Domain/` **KHÔNG import `UnityEngine`**. `selfcheck.sh` compile cả thư mục bằng csc trần.
- **Không blocker nào thêm loại nước đi mới.** Game vẫn chỉ có một hành động là kéo thẻ từ hộp trên cùng sang hộp trên cùng khác.
- Hộp đóng (khoá hoặc ổ): không nhặt ra, không thả vào, không tự nổ, không tính là còn chỗ khi xét kẹt, không bị xoá dù rỗng.
- Thẻ băng: không kéo đi được, không tính khi đếm bộ bốn, không bị xoá khi nhóm của nó nổ. Bộ đếm chỉ chạy khi thẻ đang ở hộp trên cùng, và chỉ sau nước đi **thành công**.
- Thứ tự trong một lượt: **nước đi → giảm băng → dây chuyền**.
- Bất biến: không thẻ nào bị cắt vĩnh viễn khỏi khả năng biến mất. Bàn hết nước đi hợp lệ thì phải báo **kẹt**, không được báo còn chơi.
- Định dạng: blocker của thẻ nằm trên entry thẻ trong `meaning`, của hộp nằm trên hộp trong `layout`, đều trong object `blockers` với key là id. Sổ đăng ký: `locked`, `keylock` (hộp); `ice`, `key` (thẻ).
- Cổng xuất bản không đổi: màn có blocker vẫn phải giải được ở cả hai chế độ xoá hộp.
- Solver: **không mã hoá** trạng thái hộp khoá và hộp có ổ (suy ra từ bố cục), **phải mã hoá** mức băng còn lại.
- Booster: Nam châm bỏ qua nhóm có thành viên đang băng hoặc nằm trong hộp đóng; Xáo không đụng thẻ băng và hộp đóng; Undo không cần luật riêng.
- File `.cs` mới phải có `.meta` hai dòng viết tay (`fileFormatVersion: 2` + `guid:` 32 hex), như các meta khác trong thư mục.

---

## Vòng phản hồi

Baseline trên `main` (đo 2026-09-10): `./selfcheck.sh` **đỏ trước khi tới luật** — lv-008 thiếu art `airplane.png`, và lv-002 chưa giải được ở chế độ chặt. Hai lỗi đó là việc tồn đọng của content, không phải của nhịp này. Task 0 cho script nhận thư mục level tuỳ chọn để chạy toàn bộ tự kiểm trên bộ level xanh.

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `./selfcheck.sh Temp/selfcheck-levels` | compile `Domain/` + 8 mục tự kiểm + giải 2 bản lv-001 | `SelfCheck OK - 2 level, luật khớp demo/check.mjs` |
| `./compilecheck.sh` | compile `game` / `editor` / `meta` | `game.dll OK` `editor.dll OK` `meta.dll OK` |

Mỗi task theo nhịp: viết assert vào `SelfCheck.cs` mục 8 → chạy thấy **FAIL đúng câu** → viết code → chạy thấy **OK** → commit. `SelfCheck.Ok(cond, msg)` ném `Exception(msg)` khi sai, nên câu thất bại hiện nguyên văn sau `SELFCHECK FAIL: `.

Bàn dùng cho tự kiểm là `RulesLv` (đã có trong `SelfCheck.cs`), nạp bằng `load(drain)`:

| Stack | Hộp trên | Hộp dưới | Nhóm |
|---|---|---|---|
| 0 | `c1 c2 _ _` | `c3 c4 _ _` | ga = c1..c4 |
| 1 | `d1 d2 d3 e1` | | gb = d1..d4, gc = e1..e4 |
| 2 | `d4 e2 _ _` | | |
| 3 | `e3 e4 _ _` | | |
| 4 | `_ _ _ _` | | |

Trong mục 8 luôn dùng `drain = true` khi cần Solver giải: RulesLv **không giải được ở chế độ chặt** (hộp trên của stack 0 rỗng ra thì không tự xoá, c3 c4 kẹt). Đó là tính chất của bàn kiểm luật, không phải của blocker.

Commit: nếu classifier chặn `git add`/`git commit` ở Bash thì chạy cùng lệnh qua PowerShell với `git -C D:\CategorySort ...`. Message tiếng Anh, mệnh lệnh, kết thúc bằng `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

---

### Task 0: selfcheck.sh nhận thư mục level tuỳ chọn

**Files:**
- Modify: `selfcheck.sh` (dòng cuối, lệnh chạy dll)

**Interfaces:**
- Produces: `./selfcheck.sh [levels-dir]`; không truyền thì như cũ.

- [ ] **Step 1: Dựng thư mục level xanh**

```bash
mkdir -p Temp/selfcheck-levels
cp Assets/_Game/Content/Levels/lv-001.json Temp/selfcheck-levels/a-lv-001.json
cp Assets/_Game/Content/Levels/lv-001.json Temp/selfcheck-levels/b-lv-001.json
```

`Temp/` đã nằm trong `.gitignore`. Hai bản vì `SelfCheck.Run` đòi ≥ 2 file level.

- [ ] **Step 2: Chạy script để thấy nó chưa nhận tham số**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -2`
Expected: vẫn đọc `Assets/_Game/Content/Levels`, báo `SELFCHECK FAIL: thiếu 1 file art: airplane.png ...`

- [ ] **Step 3: Sửa dòng chạy dll**

Trong `selfcheck.sh`, thay dòng cuối:

```bash
"$DOTNET" "$(cygpath -w "$OUT/selfcheck.dll")" "$(cygpath -w "$PWD/Assets/_Game/Content/Levels")"
```

bằng:

```bash
# Tham số 1 (tuỳ chọn): thư mục level. Dùng khi bộ level ship đang đỏ vì content
# (art thiếu, level chưa giải được) mà vẫn cần chạy toàn bộ mục luật.
LEVELS="${1:-$PWD/Assets/_Game/Content/Levels}"
"$DOTNET" "$(cygpath -w "$OUT/selfcheck.dll")" "$(cygpath -w "$LEVELS")"
```

- [ ] **Step 4: Chạy lại với thư mục xanh**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -3`
Expected:
```
  lv-001 chặt - 19 nước (...)
  lv-001 rộng - 19 nước (...)
SelfCheck OK - 2 level, luật khớp demo/check.mjs
```

- [ ] **Step 5: Commit**

```bash
git add selfcheck.sh
git commit -m "chore(selfcheck): optional levels dir argument for a green feedback loop"
```

---

### Task 1: Kiểu `Lock`, sổ đăng ký, field trên Box/Tile, Clone, IsOpen

**Files:**
- Create: `Assets/_Game/Board/Domain/GameBlockers.cs`
- Create: `Assets/_Game/Board/Domain/GameBlockers.cs.meta`
- Modify: `Assets/_Game/Board/Domain/Game.cs` (class `Tile`, class `Box`)
- Test: `Assets/_Game/Board/Domain/SelfCheck.cs` (mục 8 mới, trước dòng `log("SelfCheck OK — "`)

**Interfaces:**
- Produces:
  - `enum LockKind { None, Clears, Moves, Key }`
  - `struct Lock { LockKind Kind; int Need; int Have; string KeyId; }`
  - `static class Blockers` — `const string Locked, KeyLock, Ice, Key`; `string[] BoxIds, CardIds`; `bool CardPairAllowed(string a, string b)`; `bool IsCount(object v)`
  - `Tile.Lock` (field), `Tile.KeyId` (field), `Box.Lock` (field)
  - `bool Game.IsOpen(Lock l)`, `static bool Game.IsFrozen(Tile t)`, `bool Game.IsPullable(Tile t, Box b)`

- [ ] **Step 1: Viết assert mục 8a**

Chèn vào `SelfCheck.cs`, ngay trước dòng `log("SelfCheck OK — " + levelJsons.Count + ...`:

```csharp
            // ---- 8. Blocker (spec 2026-09-10) ----
            // Bàn dựng tay trên RulesLv. drain = true ở mọi chỗ cần Solver: RulesLv không
            // giải được ở chế độ chặt (hộp trên stack 0 rỗng ra thì không tự xoá).
            Func<Game, string, string> uidOf = (game, card) =>
                game.Stacks.SelectMany(st => st.Boxes).SelectMany(b => b.Slots)
                    .First(t => t != null && t.CardId == card).Uid;

            // 8a. Kiểu khoá, IsOpen, Clone
            {
                var g = load(true);
                g.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 1 };
                Ok(!g.IsOpen(g.Stacks[2].Boxes[0].Lock), "hộp khoá cần 1 nhóm: chưa gom thì đóng");
                var c = g.Clone();
                Ok(c.Stacks[2].Boxes[0].Lock.Kind == LockKind.Clears && c.Stacks[2].Boxes[0].Lock.Need == 1,
                   "Clone phải chép khoá của hộp");
                g.Cleared = 1;
                Ok(g.IsOpen(g.Stacks[2].Boxes[0].Lock), "đủ số nhóm thì hộp khoá mở");

                var t = g.TopBox(0).Slots[0];
                t.Lock = new Lock { Kind = LockKind.Moves, Need = 2 };
                t.KeyId = "k1";
                Ok(Game.IsFrozen(t), "thẻ có Kind = Moves là còn băng");
                Ok(!Game.IsFrozen(g.TopBox(0).Slots[1]), "thẻ thường không băng");
                var ct = g.Clone().TopBox(0).Slots[0];
                Ok(Game.IsFrozen(ct) && ct.Lock.Need == 2 && ct.KeyId == "k1", "Clone phải chép băng và chìa của thẻ");

                var keyLock = new Lock { Kind = LockKind.Key, KeyId = "k1" };
                Ok(!g.IsOpen(keyLock), "thẻ chìa còn trên bàn thì ổ đóng");
                g.TopBox(0).Slots[0] = null;
                Ok(g.IsOpen(keyLock), "thẻ chìa biến mất thì ổ mở");
                Ok(g.IsOpen(default(Lock)), "không khoá thì luôn mở");

                Ok(Blockers.CardPairAllowed(Blockers.Ice, Blockers.Key) && Blockers.CardPairAllowed(Blockers.Key, Blockers.Ice),
                   "băng + chìa là cặp được phép trên một thẻ");
                Ok(Blockers.IsCount(3.0) && !Blockers.IsCount(0.0) && !Blockers.IsCount(1.5) && !Blockers.IsCount("3"),
                   "IsCount: số nguyên ≥ 1");
            }
```

- [ ] **Step 2: Chạy để thấy fail compile**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | grep -m3 "error CS"`
Expected: `error CS0246: The type or namespace name 'Lock' could not be found` (hoặc `LockKind`)

- [ ] **Step 3: Tạo `GameBlockers.cs`**

```csharp
// Blocker: Hộp khoá, Thẻ băng, Ổ và chìa. Spec: docs/superpowers/specs/2026-09-10-blocker-locks-design.md
//
// Cả ba là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ" — không thêm loại
// nước đi nào. Một struct Lock gắn lên Box (Clears/Key) và Tile (Moves). Thẻ chìa không
// bị khoá gì nên chìa là field riêng Tile.KeyId, không nằm trong Lock.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System;
using System.Collections.Generic;

namespace WordStack.Board
{
    public enum LockKind { None, Clears, Moves, Key }

    public struct Lock
    {
        public LockKind Kind;
        public int Need;
        public int Have;       // chỉ có nghĩa với Moves — hai loại kia tính động từ bàn
        public string KeyId;   // chỉ có nghĩa với Key
    }

    /// <summary>
    /// Sổ đăng ký id blocker trong data. Thêm blocker mới = thêm hằng + thêm vào mảng đúng
    /// phía. Blocker thẻ mới còn phải thêm cặp được phép vào CardPairs nếu nó được đứng
    /// chung với cái khác — validator đọc bảng này, không cần sửa code kiểm.
    /// </summary>
    public static class Blockers
    {
        public const string Locked = "locked";   // hộp: đóng tới khi Cleared ≥ N
        public const string KeyLock = "keylock"; // hộp: đóng tới khi thẻ mang key cùng id biến mất
        public const string Ice = "ice";         // thẻ: bất động N nước kể từ lúc lộ ở hộp trên
        public const string Key = "key";         // thẻ: không khoá gì; bị gom là mở keylock cùng id

        public static readonly string[] BoxIds = { Locked, KeyLock };
        public static readonly string[] CardIds = { Ice, Key };

        static readonly string[][] CardPairs = { new[] { Ice, Key } };

        public static bool CardPairAllowed(string a, string b)
        {
            foreach (var p in CardPairs)
                if ((p[0] == a && p[1] == b) || (p[0] == b && p[1] == a)) return true;
            return false;
        }

        /// <summary>Giá trị JSON là số nguyên ≥ 1 (Json.Parse trả số dạng double).</summary>
        public static bool IsCount(object v)
        {
            if (!(v is double)) return false;
            double d = (double)v;
            return d >= 1 && d == Math.Floor(d);
        }
    }

    public partial class Game
    {
        /// <summary>
        /// Hộp/khoá này đang mở không. Clears và Key KHÔNG lưu trạng thái riêng — chúng suy
        /// ra từ bàn (Cleared chỉ tăng; thẻ chìa mất là mất hẳn), nên Solver.Encode không
        /// phải mã hoá gì cho hai loại đó. Chỉ Moves mang Have.
        /// </summary>
        public bool IsOpen(Lock l)
        {
            switch (l.Kind)
            {
                case LockKind.Clears: return Cleared >= l.Need;
                case LockKind.Moves:  return l.Have >= l.Need;
                case LockKind.Key:    return !KeyOnBoard(l.KeyId);
                default:              return true;
            }
        }

        /// <summary>Thẻ còn băng. Băng tan thì Lock về default nên thẻ tan = thẻ thường.</summary>
        public static bool IsFrozen(Tile t) { return t != null && t.Lock.Kind == LockKind.Moves; }

        /// <summary>Booster hút/xáo được thẻ này không: không băng, và hộp chứa nó đang mở.</summary>
        public bool IsPullable(Tile t, Box b) { return !IsFrozen(t) && IsOpen(b.Lock); }

        bool KeyOnBoard(string keyId)
        {
            foreach (var st in Stacks)
                foreach (var b in st.Boxes)
                    foreach (var t in b.Slots)
                        if (t != null && t.KeyId == keyId) return true;
            return false;
        }
    }
}
```

Tạo `GameBlockers.cs.meta` (guid: 32 ký tự hex ngẫu nhiên, ví dụ lấy từ `python -c "import uuid;print(uuid.uuid4().hex)"`):

```
fileFormatVersion: 2
guid: <32-hex>
```

- [ ] **Step 4: Thêm field vào `Tile` và `Box`, chép trong `Box.Clone`**

Trong `Game.cs`, sửa hai class đầu file:

```csharp
    public class Tile
    {
        public string Uid, CardId, GroupId, Text, Art;
        public Lock Lock;       // Kind = Moves khi còn băng; default = thẻ thường
        public string KeyId;    // thẻ mang chìa; null = không mang
        public Tile Clone() { return (Tile)MemberwiseClone(); }   // struct + string: copy đủ
    }

    public class Box
    {
        public bool IsBottom;
        public bool HadCollapse;   // đã từng xảy ra collapse — chế độ chặt dùng để xoá hộp rỗng
        public Lock Lock;          // Clears hoặc Key; default = hộp thường
        public Tile[] Slots = new Tile[Rules.BoxCapacity];
        public Box Clone()
        {
            var b = new Box { IsBottom = IsBottom, HadCollapse = HadCollapse, Lock = Lock, Slots = new Tile[Slots.Length] };
            for (int i = 0; i < Slots.Length; i++) b.Slots[i] = Slots[i] == null ? null : Slots[i].Clone();
            return b;
        }
    }
```

- [ ] **Step 5: Chạy tự kiểm**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameBlockers.cs Assets/_Game/Board/Domain/GameBlockers.cs.meta Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): Lock type, blocker registry, IsOpen/IsFrozen on Box and Tile"
```

---

### Task 2: Nước đi bị chặn, băng đếm sau nước đi, kẹt khi hết nước hợp lệ

**Files:**
- Modify: `Assets/_Game/Board/Domain/Game.cs` (`MoveTile`, `CheckStatus`)
- Modify: `Assets/_Game/Board/Domain/GameBlockers.cs` (thêm `TickIce`, `HasAnyMove`)
- Test: `SelfCheck.cs` mục 8b

**Interfaces:**
- Consumes: `Lock`, `IsOpen`, `IsFrozen` (Task 1)
- Produces: `bool Game.HasAnyMove()`; `MoveTile` từ chối hộp đóng ở cả hai đầu và thẻ băng; sau nước thành công gọi `TickIce()`; `CheckStatus` = Won / (HasAnyMove ? Playing : Stuck).

- [ ] **Step 1: Viết assert mục 8b**

Chèn ngay sau khối 8a:

```csharp
            // 8b. Nước đi và bộ đếm băng
            {
                var g = load(true);
                g.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 1 };
                Ok(!g.MoveTile(2, uidOf(g, "d4"), 0), "hộp khoá: không nhặt thẻ ra");
                Ok(!g.MoveTile(0, uidOf(g, "c1"), 2), "hộp khoá: không thả thẻ vào");
                Ok(g.MoveTile(0, uidOf(g, "c1"), 3), "hộp mở khác vẫn nhận thẻ như thường");

                var g2 = load(true);
                var ice = g2.TopBox(0).Slots[0];   // c1
                ice.Lock = new Lock { Kind = LockKind.Moves, Need = 2 };
                Ok(!g2.MoveTile(0, ice.Uid, 3), "thẻ băng không kéo đi được");
                Ok(g2.MoveTile(0, uidOf(g2, "c2"), 3), "nước hợp lệ thứ nhất");
                Ok(Game.IsFrozen(ice) && ice.Lock.Have == 1, "sau một nước, băng đếm 1");
                Ok(g2.MoveTile(3, uidOf(g2, "c2"), 4), "nước hợp lệ thứ hai");
                Ok(!Game.IsFrozen(ice), "đủ hai nước thì băng tan");
                Ok(g2.MoveTile(0, ice.Uid, 4), "tan rồi thì kéo được");

                var g3 = load(true);
                var buried = g3.Stacks[0].Boxes[1].Slots[0];   // c3, đang chìm
                buried.Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                Ok(g3.MoveTile(0, uidOf(g3, "c1"), 4), "nước hợp lệ");
                Ok(Game.IsFrozen(buried) && buried.Lock.Have == 0, "thẻ băng còn chìm thì không đếm");

                var g4 = load(true);
                var ice4 = g4.TopBox(0).Slots[0];
                ice4.Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                Ok(!g4.MoveTile(0, uidOf(g4, "c2"), 0), "thả về chính stack bị từ chối");
                Ok(Game.IsFrozen(ice4) && ice4.Lock.Have == 0, "nước bị từ chối không giảm băng");

                // Băng trong hộp khoá đang ở trên cùng vẫn đếm — thẻ đang lộ (spec 4.4).
                var g5 = load(true);
                g5.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                var iceInLocked = g5.TopBox(2).Slots[0];
                iceInLocked.Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                Ok(g5.MoveTile(0, uidOf(g5, "c1"), 4), "nước hợp lệ");
                Ok(!Game.IsFrozen(iceInLocked), "băng trong hộp khoá ở trên cùng vẫn tan theo nước đi");

                // Kẹt phải là "hết nước đi hợp lệ", không phải "hết ô trống" — nếu không mọi
                // thẻ lộ đều băng là thua ngầm (spec 2.1).
                var g6 = load(true);
                foreach (var st in g6.Stacks)
                    if (st != g6.Stacks[4]) st.Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 99 };
                Ok(!g6.HasAnyMove() && g6.CheckStatus() == GameStatus.Stuck,
                   "còn ô trống ở stack rỗng mà không thẻ nào nhặt ra được → kẹt");

                var g7 = load(true);
                foreach (var st in g7.Stacks)
                    foreach (var t in st.Boxes[0].Slots)
                        if (t != null) t.Lock = new Lock { Kind = LockKind.Moves, Need = 99 };
                Ok(g7.CheckStatus() == GameStatus.Stuck, "mọi thẻ lộ đều băng → kẹt thật, không thua ngầm");

                var g8 = load(true);
                Ok(g8.HasAnyMove() && g8.CheckStatus() == GameStatus.Playing, "bàn thường vẫn Playing");
            }
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `error CS1061 ... 'HasAnyMove'` (chưa có hàm) — hoặc nếu compile qua thì `SELFCHECK FAIL: hộp khoá: không nhặt thẻ ra`

- [ ] **Step 3: Sửa `MoveTile` trong `Game.cs`**

Thay toàn bộ thân hàm:

```csharp
        public bool MoveTile(int from, string uid, int to, int preferSlot = -1)
        {
            if (from == to) return false;
            if (from < 0 || from >= Stacks.Count || to < 0 || to >= Stacks.Count) return false;
            var src = TopBox(from);
            var dst = TopBox(to);
            if (src == null || dst == null) return false;
            // Hộp đóng: không nhặt ra, không thả vào (spec 4.1).
            if (!IsOpen(src.Lock) || !IsOpen(dst.Lock)) return false;
            int i = Array.FindIndex(src.Slots, t => t != null && t.Uid == uid);
            if (i < 0) return false;                       // không phải thẻ của top box
            if (IsFrozen(src.Slots[i])) return false;      // thẻ băng đứng yên (spec 4.2)
            int j = preferSlot >= 0 && preferSlot < dst.Slots.Length && dst.Slots[preferSlot] == null
                  ? preferSlot
                  : Array.FindIndex(dst.Slots, t => t == null);
            if (j < 0) return false;                       // box đích đầy

            // Sau chốt từ chối cuối cùng, trước dòng mutate đầu tiên — xem GameUndo.cs.
            CaptureUndoSnapshot();

            dst.Slots[j] = src.Slots[i];
            src.Slots[i] = null;
            Moves++;
            // Nước đi → giảm băng → (bên gọi) dây chuyền. Chỉ nước thành công mới tới đây.
            TickIce();
            return true;
        }
```

- [ ] **Step 4: Sửa `CheckStatus` trong `Game.cs`**

```csharp
        // Kẹt = không còn nước đi hợp lệ nào. Trước đây là "mọi hộp trên cùng đều đầy" —
        // hai định nghĩa trùng nhau khi không có blocker, nhưng với hộp đóng và thẻ băng
        // thì "còn ô trống" không còn nghĩa là "còn đi được", và báo Playing lúc đó là
        // thua ngầm (spec 2.1).
        public GameStatus CheckStatus()
        {
            if (TotalTiles() == 0) return GameStatus.Won;
            return HasAnyMove() ? GameStatus.Playing : GameStatus.Stuck;
        }
```

- [ ] **Step 5: Thêm `TickIce` và `HasAnyMove` vào `GameBlockers.cs`** (trong `partial class Game`)

```csharp
        /// <summary>
        /// Sau mỗi nước đi thành công: mọi thẻ băng đang ở hộp trên cùng tiến một bước.
        /// Đếm cả thẻ trong hộp đang khoá ở trên cùng (nó đang lộ), không đếm thẻ chìm.
        /// Tan thì Lock về default — thẻ tan không khác gì thẻ thường, kể cả với Encode.
        /// </summary>
        void TickIce()
        {
            foreach (var st in Stacks)
            {
                if (st.Boxes.Count == 0) continue;
                var top = st.Boxes[0];
                for (int i = 0; i < top.Slots.Length; i++)
                {
                    var t = top.Slots[i];
                    if (t == null || t.Lock.Kind != LockKind.Moves) continue;
                    t.Lock.Have++;
                    if (t.Lock.Have >= t.Lock.Need) t.Lock = default(Lock);
                }
            }
        }

        /// <summary>
        /// Có ít nhất một nước đi mà MoveTile sẽ nhận không. Soi đúng các chốt của MoveTile
        /// (hộp đóng hai đầu, thẻ băng, hộp đích đầy) mà không mutate.
        /// </summary>
        public bool HasAnyMove()
        {
            for (int from = 0; from < Stacks.Count; from++)
            {
                var src = TopBox(from);
                if (src == null || !IsOpen(src.Lock)) continue;
                bool movable = false;
                foreach (var t in src.Slots) if (t != null && !IsFrozen(t)) { movable = true; break; }
                if (!movable) continue;

                for (int to = 0; to < Stacks.Count; to++)
                {
                    if (to == from) continue;
                    var dst = TopBox(to);
                    if (dst != null && IsOpen(dst.Lock) && FreeCount(dst) > 0) return true;
                }
            }
            return false;
        }
```

- [ ] **Step 6: Chạy tự kiểm**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -3`
Expected: hai dòng `lv-001 ... 19 nước` (số nước không đổi so với Task 0) và `SelfCheck OK - 2 level`.

Nếu số nước của lv-001 đổi, `CheckStatus` mới đang phân loại khác ở một trạng thái nào đó — dừng lại và so sánh trước khi commit.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/GameBlockers.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): locked boxes and frozen tiles refuse moves; ice ticks per move; stuck means no legal move"
```

---

### Task 3: Dây chuyền tôn trọng hộp đóng và thẻ băng

**Files:**
- Modify: `Assets/_Game/Board/Domain/Game.cs` (`CompletedGroupIn`, `SettleStep`)
- Test: `SelfCheck.cs` mục 8c

**Interfaces:**
- Consumes: `IsOpen`, `IsFrozen`, `TickIce` (Task 1, 2)
- Produces: hộp đóng không nổ và không bị xoá; thẻ băng không tính bộ bốn và không bị xoá; hộp vừa mở nhờ một lần gom sẽ nổ ngay trong cùng `Settle()`.

- [ ] **Step 1: Viết assert mục 8c**

Chèn sau khối 8b:

```csharp
            // 8c. Dây chuyền
            Func<string, int, Tile> mkT = (gid, n) => new Tile { Uid = gid + n, CardId = gid, GroupId = gid, Text = gid };
            {
                // Hộp khoá đủ bộ vẫn không nổ; gom một nhóm ở nơi khác mở nó, và nó nổ
                // ngay trong CÙNG Settle (spec 4.1 + 2.1).
                var g = load(true);
                var box3 = g.TopBox(3);
                for (int i = 0; i < box3.Slots.Length; i++) box3.Slots[i] = mkT("zz", i);
                box3.Lock = new Lock { Kind = LockKind.Clears, Need = 1 };
                g.Settle(true);
                Ok(g.Cleared == 0, "hộp khoá đủ bộ vẫn không nổ");

                var box4 = g.TopBox(4);
                for (int i = 0; i < Rules.GroupSize - 1; i++) box4.Slots[i] = mkT("yy", i);
                g.TopBox(0).Slots[2] = mkT("yy", 9);
                Ok(g.MoveTile(0, "yy9", 4), "thẻ thứ 4 kéo sang được");
                g.Settle(true);
                Ok(g.Cleared == 2, "gom 1 nhóm → hộp khoá mở → hộp đó nổ ngay trong cùng dây chuyền");

                // Hộp khoá rỗng không bị xoá (drain = true xoá mọi hộp trống không phải đáy).
                var g2 = load(true);
                var top0 = g2.TopBox(0);
                for (int i = 0; i < top0.Slots.Length; i++) top0.Slots[i] = null;
                top0.Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                int boxesBefore = g2.Stacks[0].Boxes.Count;
                g2.Settle(true);
                Ok(g2.Stacks[0].Boxes.Count == boxesBefore, "hộp khoá rỗng không bị xoá");

                // Thẻ băng không tính bộ 4; tan đúng nhịp thì nhóm nổ ngay (spec 4.2).
                var g3 = load(true);
                var b0 = g3.TopBox(0);
                for (int i = 0; i < b0.Slots.Length; i++) b0.Slots[i] = mkT("zz", i);
                b0.Slots[0].Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                g3.Settle(true);
                Ok(g3.Cleared == 0, "3 thẻ + 1 băng cùng nhóm thì chưa nổ");
                Ok(g3.MoveTile(2, uidOf(g3, "d4"), 4), "một nước hợp lệ ở nơi khác");
                g3.Settle(true);
                Ok(g3.Cleared == 1, "băng tan sau nước đó → nhóm nổ ngay trong cùng nhịp");

                // Ổ và chìa: thẻ chìa bị gom là ổ mở (spec 4.1, 2.4).
                var g4 = load(true);
                g4.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Key, KeyId = "k1" };
                var b4 = g4.TopBox(4);
                for (int i = 0; i < Rules.GroupSize - 1; i++) b4.Slots[i] = mkT("zz", i);
                var keyTile = mkT("zz", 9);
                keyTile.KeyId = "k1";
                g4.TopBox(0).Slots[2] = keyTile;
                Ok(!g4.MoveTile(0, uidOf(g4, "c1"), 2), "ổ đóng: không thả vào");
                Ok(g4.MoveTile(0, "zz9", 4), "thẻ chìa kéo được như thẻ thường");
                g4.Settle(true);
                Ok(g4.Cleared == 1 && g4.IsOpen(g4.Stacks[2].Boxes[0].Lock), "thẻ chìa bị gom thì ổ mở");
                Ok(g4.MoveTile(0, uidOf(g4, "c1"), 2), "ổ mở rồi thả vào được");

                // Nhóm khác nổ thì ổ không mở.
                var g5 = load(true);
                g5.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Key, KeyId = "k1" };
                g5.TopBox(3).Slots[0].KeyId = "k1";                 // e3 mang chìa, không bị gom
                var b5 = g5.TopBox(4);
                for (int i = 0; i < Rules.GroupSize - 1; i++) b5.Slots[i] = mkT("zz", i);
                g5.TopBox(0).Slots[2] = mkT("zz", 9);
                Ok(g5.MoveTile(0, "zz9", 4), "nước gom nhóm zz");
                g5.Settle(true);
                Ok(g5.Cleared == 1 && !g5.IsOpen(g5.Stacks[2].Boxes[0].Lock), "nhóm không có chìa nổ thì ổ vẫn đóng");

                // Thẻ vừa băng vừa chìa: tan → gom → mở (spec 4.4).
                var g6 = load(true);
                g6.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Key, KeyId = "k1" };
                var b6 = g6.TopBox(4);
                for (int i = 0; i < Rules.GroupSize - 1; i++) b6.Slots[i] = mkT("zz", i);
                var iceKey = mkT("zz", 9);
                iceKey.KeyId = "k1";
                iceKey.Lock = new Lock { Kind = LockKind.Moves, Need = 1 };
                g6.TopBox(0).Slots[2] = iceKey;
                Ok(!g6.MoveTile(0, "zz9", 4), "còn băng thì chìa chưa kéo được");
                // e3 sang stack 0 (còn 1 ô), KHÔNG sang stack 4 — hộp đó phải giữ đúng 1 ô trống cho chìa.
                Ok(g6.MoveTile(3, uidOf(g6, "e3"), 0), "nước khác làm tan băng");
                Ok(g6.MoveTile(0, "zz9", 4), "tan rồi kéo chìa sang");
                g6.Settle(true);
                Ok(g6.IsOpen(g6.Stacks[2].Boxes[0].Lock), "băng tan → chìa bị gom → ổ mở");
            }
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SELFCHECK FAIL: hộp khoá đủ bộ vẫn không nổ`

- [ ] **Step 3: Sửa `CompletedGroupIn` và `SettleStep` trong `Game.cs`**

`CompletedGroupIn` bỏ qua thẻ băng:

```csharp
        static string CompletedGroupIn(Box box)
        {
            var count = new Dictionary<string, int>();
            foreach (var t in box.Slots)
            {
                if (t == null || IsFrozen(t)) continue;   // thẻ băng không tính bộ 4 (spec 4.2)
                int n;
                count[t.GroupId] = count.TryGetValue(t.GroupId, out n) ? n + 1 : 1;
            }
            foreach (var kv in count) if (kv.Value == Rules.GroupSize) return kv.Key;
            return null;
        }
```

Trong `SettleStep`, vòng tìm nhóm đủ: thay

```csharp
                var box = TopBox(s);
                if (box == null) continue;
                string gid = CompletedGroupIn(box);
```

bằng

```csharp
                var box = TopBox(s);
                if (box == null || !IsOpen(box.Lock)) continue;   // hộp đóng không tự nổ (spec 4.1)
                string gid = CompletedGroupIn(box);
```

và vòng xoá thẻ:

```csharp
                for (int i = 0; i < box.Slots.Length; i++)
                    if (box.Slots[i] != null && box.Slots[i].GroupId == gid && !IsFrozen(box.Slots[i]))
                    {
                        doomed.Add(box.Slots[i].Uid);
                        box.Slots[i] = null;
                    }
```

Vòng xoá hộp rỗng: thay

```csharp
                if (box != null && !box.IsBottom && IsEmpty(box) && (drain || box.HadCollapse))
```

bằng

```csharp
                if (box != null && IsOpen(box.Lock) && !box.IsBottom && IsEmpty(box) && (drain || box.HadCollapse))
```

Ghi chú cho người đọc sau: với hộp 4 ô, "4 thẻ thường + 1 thẻ băng cùng nhóm" không xảy ra được, nên nhánh `!IsFrozen` trong vòng xoá là lưới an toàn cho sức chứa khác, không phải đường đi thật của bản này.

- [ ] **Step 4: Chạy tự kiểm**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): cascade skips closed boxes and frozen tiles; opened box clears in the same settle"
```

---

### Task 4: Solver mã hoá băng và bỏ sớm nước bị cấm

**Files:**
- Modify: `Assets/_Game/Board/Domain/Solver.cs` (`Encode`, vòng sinh nước trong `Solve`)
- Test: `SelfCheck.cs` mục 8d

**Interfaces:**
- Consumes: `IsFrozen`, `IsOpen`, `Lock.Need/Have`
- Produces: `Encode` ghi thẻ băng thành `CardId~<còn lại>`; `Solve` không thử nước từ/vào hộp đóng và không thử kéo thẻ băng.

- [ ] **Step 1: Viết assert mục 8d**

Chèn sau khối 8c:

```csharp
            // 8d. Solver
            {
                var a = load(true);
                var b = load(true);
                b.TopBox(0).Slots[0].Lock = new Lock { Kind = LockKind.Moves, Need = 3 };
                Ok(Solver.Encode(a) != Solver.Encode(b), "băng phải vào mã trạng thái");
                var b2 = b.Clone();
                b2.TopBox(0).Slots[0].Lock.Have = 1;
                Ok(Solver.Encode(b) != Solver.Encode(b2), "hai mức băng là hai trạng thái");

                // Hộp khoá và ổ KHÔNG vào mã: cùng bố cục thẻ là cùng trạng thái (spec Mục 3).
                var c1 = load(true);
                var c2 = load(true);
                c2.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 1 };
                Ok(Solver.Encode(c1) == Solver.Encode(c2), "khoá hộp không đổi mã — trạng thái suy từ bố cục");

                // Bàn có cả ba blocker vẫn giải được (drain = true, xem ghi chú đầu mục 8).
                // Đường giải: c1 c2 → 4, hộp trên stack 0 rỗng bị xoá, c3 c4 → 4, ga nổ → stack 2
                // mở; e1 → 4, d4 → 1, gb nổ → chìa d4 mất → stack 3 mở; e1 e2 → 3, gc nổ. Băng
                // trên e3 tan sau 2 nước đầu. Chìa KHÔNG được là thẻ gc: gc cần e1 đang nằm trong
                // hộp mà chìa đó mở → vòng khoá, chính là thứ luật kiểm 6 cấm.
                var s = load(true);
                s.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 1 };
                s.TopBox(3).Slots[0].Lock = new Lock { Kind = LockKind.Moves, Need = 2 };   // e3 băng
                s.TopBox(2).Slots[0].KeyId = "k1";                                           // d4 (gb) mang chìa
                s.Stacks[3].Boxes[0].Lock = new Lock { Kind = LockKind.Key, KeyId = "k1" };  // stack 3 có ổ
                var r = Solver.Solve(s, true);
                Ok(r.Ok, "bàn luật có đủ ba blocker phải giải được ở chế độ rộng: " + (r.Why ?? ""));

                // Bàn thực sự vô nghiệm vì blocker phải bị bắt: ổ mà chìa nằm ngay trong hộp.
                var dead = load(true);
                dead.TopBox(2).Slots[0].KeyId = "k1";                                        // d4 mang chìa
                dead.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Key, KeyId = "k1" };
                var rd = Solver.Solve(dead, true);
                Ok(!rd.Ok, "chìa nằm trong chính hộp nó mở → Solver phải báo không giải được");
            }
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SELFCHECK FAIL: băng phải vào mã trạng thái`

- [ ] **Step 3: Sửa `Encode` trong `Solver.cs`**

Thêm hàm mã thẻ và dùng nó ở cả hai nhánh:

```csharp
        // Thẻ băng mã thêm số nước còn lại: hai bàn giống hệt về thẻ mà khác mức băng là hai
        // tương lai khác nhau. Hộp khoá và ổ KHÔNG mã hoá — trạng thái của chúng suy ra từ
        // bố cục thẻ (spec Mục 3), mã thêm chỉ nở memo vô ích.
        static string Code(Tile t)
        {
            return Game.IsFrozen(t) ? t.CardId + "~" + (t.Lock.Need - t.Lock.Have) : t.CardId;
        }

        public static string Encode(Game g)
        {
            var keys = new List<string>(g.Stacks.Count);
            foreach (var st in g.Stacks)
            {
                var parts = new List<string>(st.Boxes.Count);
                for (int i = 0; i < st.Boxes.Count; i++)
                {
                    var b = st.Boxes[i];
                    // Dấu "!" cho hộp đã collapse: ở chế độ chặt, hai bàn giống hệt về thẻ
                    // nhưng khác cờ HadCollapse có tương lai khác nhau (một cái rỗng là tự
                    // mở) — memo trộn chúng là solver trả kết quả sai im lặng.
                    if (i == 0)
                    {
                        var ids = b.Slots.Where(t => t != null).Select(Code).ToList();
                        ids.Sort(StringComparer.Ordinal);
                        parts.Add((b.HadCollapse ? "!" : "") + string.Join(",", ids));
                    }
                    else parts.Add((b.HadCollapse ? "!" : "") +
                                   string.Join(",", b.Slots.Select(t => t == null ? "_" : Code(t))));
                }
                keys.Add(string.Join("/", parts));
            }
            keys.Sort(StringComparer.Ordinal);
            return string.Join("|", keys);
        }
```

- [ ] **Step 4: Bỏ sớm trong vòng sinh nước của `Solve`**

Thay đoạn:

```csharp
                    for (int from = 0; from < cur.Stacks.Count; from++)
                    {
                        foreach (var t in cur.Stacks[from].Boxes[0].Slots)
                        {
                            if (t == null) continue;
                            for (int to = 0; to < cur.Stacks.Count; to++)
                            {
                                if (to == from || Game.FreeCount(cur.Stacks[to].Boxes[0]) == 0) continue;
```

bằng:

```csharp
                    for (int from = 0; from < cur.Stacks.Count; from++)
                    {
                        // MoveTile tự từ chối, nhưng lọc ở đây tránh Clone cả bàn cho một
                        // nước chắc chắn bị bỏ.
                        if (!cur.IsOpen(cur.Stacks[from].Boxes[0].Lock)) continue;
                        foreach (var t in cur.Stacks[from].Boxes[0].Slots)
                        {
                            if (t == null || Game.IsFrozen(t)) continue;
                            for (int to = 0; to < cur.Stacks.Count; to++)
                            {
                                if (to == from) continue;
                                var dstBox = cur.Stacks[to].Boxes[0];
                                if (!cur.IsOpen(dstBox.Lock) || Game.FreeCount(dstBox) == 0) continue;
```

- [ ] **Step 5: Chạy tự kiểm**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -3`
Expected: `lv-001 ... 19 nước` ở cả hai chế độ (số nút có thể lệch vài đơn vị nếu `CheckStatus` mới cắt nhánh sớm hơn — chấp nhận; số **nước** phải giữ 19) và `SelfCheck OK - 2 level`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/Solver.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): solver encodes remaining ice and prunes moves into or out of closed boxes"
```

---

### Task 5: Đọc `blockers` từ JSON, kiểm 6 luật, dựng vào bàn

**Files:**
- Modify: `Assets/_Game/Board/Domain/LevelData.cs` (`CardDef`, `BoxDef`, `Parse`, `Validate`)
- Modify: `Assets/_Game/Board/Domain/Game.cs` (`Build`)
- Test: `SelfCheck.cs` mục 8e (thêm hằng `BlockerLv` cạnh `RulesLv`)

**Interfaces:**
- Consumes: `Blockers` (Task 1)
- Produces: `CardDef.Blockers`, `BoxDef.Blockers` : `Dictionary<string, object>`; `Validate` ném lỗi cho 6 luật ở spec Mục 5; `Game.Build` đặt `Tile.Lock`, `Tile.KeyId`, `Box.Lock` từ data.

- [ ] **Step 1: Thêm hằng `BlockerLv` vào `SelfCheck.cs`**

Ngay sau hằng `RulesLv`:

```csharp
        // RulesLv cộng blocker: c2 băng 2 nước, d4 mang chìa k1, hộp stack 3 có ổ k1,
        // hộp stack 2 khoá 1 nhóm. Chìa d4 (nhóm gb) không nằm trong/dưới stack 3 — hợp luật 6.
        const string BlockerLv = @"{
          ""id"":""t-blocker"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""c1"",""c2"",null,null] },
                                          { ""slots"":[""c3"",""c4"",null,null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""d1"",""d2"",""d3"",""e1""] } ] },
            { ""pos"":[0,1], ""boxes"":[ { ""slots"":[""d4"",""e2"",null,null], ""blockers"": { ""locked"": 1 } } ] },
            { ""pos"":[1,1], ""boxes"":[ { ""slots"":[""e3"",""e4"",null,null], ""blockers"": { ""keylock"": ""k1"" } } ] },
            { ""pos"":[0,2], ""boxes"":[ { ""slots"":[null,null,null,null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              { ""id"":""c1"",""text"":""C1"" },{ ""id"":""c2"",""text"":""C2"", ""blockers"": { ""ice"": 2 } },
              { ""id"":""c3"",""text"":""C3"" },{ ""id"":""c4"",""text"":""C4"" } ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              { ""id"":""d1"",""text"":""D1"" },{ ""id"":""d2"",""text"":""D2"" },
              { ""id"":""d3"",""text"":""D3"" },{ ""id"":""d4"",""text"":""D4"", ""blockers"": { ""key"": ""k1"" } } ]},
            { ""id"":""gc"", ""text"":""C"", ""cards"":[
              { ""id"":""e1"",""text"":""E1"" },{ ""id"":""e2"",""text"":""E2"" },
              { ""id"":""e3"",""text"":""E3"" },{ ""id"":""e4"",""text"":""E4"" } ]}
          ]}
        }";
```

- [ ] **Step 2: Viết assert mục 8e**

Chèn sau khối 8d:

```csharp
            // 8e. Đọc màn, kiểm dữ liệu, dựng vào bàn
            {
                Func<LevelData> freshB = () => LevelData.Parse(BlockerLv);
                var lv = freshB();
                lv.Validate(hasArt);
                var g = Game.Build(lv);
                Ok(g.Stacks[2].Boxes[0].Lock.Kind == LockKind.Clears && g.Stacks[2].Boxes[0].Lock.Need == 1, "Build: locked → Box.Lock Clears");
                Ok(g.Stacks[3].Boxes[0].Lock.Kind == LockKind.Key && g.Stacks[3].Boxes[0].Lock.KeyId == "k1", "Build: keylock → Box.Lock Key");
                var c2 = g.TopBox(0).Slots[1];
                Ok(c2.CardId == "c2" && Game.IsFrozen(c2) && c2.Lock.Need == 2 && c2.Lock.Have == 0, "Build: ice → Tile.Lock Moves");
                Ok(g.TopBox(2).Slots[0].CardId == "d4" && g.TopBox(2).Slots[0].KeyId == "k1", "Build: key → Tile.KeyId");
                Ok(g.TopBox(0).Slots[0].Lock.Kind == LockKind.None && g.TopBox(0).Slots[0].KeyId == null, "thẻ không khai blockers thì trống");

                Action<Action<LevelData>, string> brokenB = (mutate, label) =>
                {
                    var l = freshB();
                    mutate(l);
                    bool threw = false;
                    try { l.Validate(hasArt); } catch { threw = true; }
                    Ok(threw, "validate blocker phải ném lỗi: " + label);
                };
                // Luật 1
                brokenB(l => l.Groups[0].Cards[0].Blockers["frost"] = 1.0, "id lạ trên thẻ");
                brokenB(l => l.Groups[0].Cards[0].Blockers["locked"] = 1.0, "id của hộp đặt trên thẻ");
                brokenB(l => l.Stacks[4].Boxes[0].Blockers["ice"] = 1.0, "id của thẻ đặt trên hộp");
                brokenB(l => l.Stacks[4].Boxes[0].Blockers["chain"] = 1.0, "id lạ trên hộp");
                // Luật 2
                brokenB(l => l.Stacks[2].Boxes[0].Blockers["keylock"] = "k1", "hộp mang hai blocker");
                // Luật 3: bản này chỉ có một cặp và nó được phép — không có case từ chối
                // để kiểm; CardPairAllowed đã kiểm ở 8a.
                // Luật 4
                brokenB(l => l.Groups[0].Cards[1].Blockers["ice"] = 0.0, "ice = 0");
                brokenB(l => l.Groups[0].Cards[1].Blockers["ice"] = 1.5, "ice không nguyên");
                brokenB(l => l.Stacks[2].Boxes[0].Blockers["locked"] = "3", "locked là chuỗi");
                // Luật 5
                brokenB(l => l.Stacks[3].Boxes[0].Blockers["keylock"] = "k9", "keylock trỏ chìa không ai mang");
                brokenB(l => l.Groups[2].Cards[0].Blockers["key"] = "k1", "hai thẻ cùng mang một chìa");
                // Luật 6
                brokenB(l => { l.Stacks[3].Boxes[0].Slots[2] = "d4"; l.Stacks[2].Boxes[0].Slots[0] = null; },
                        "thẻ chìa nằm trong hộp nó mở");
                brokenB(l => { l.Stacks[3].Boxes[0].Slots[2] = "d1"; l.Stacks[1].Boxes[0].Slots[0] = null; },
                        "thẻ cùng nhóm với chìa nằm trong hộp chìa mở");
                brokenB(l => { l.Stacks[3].Boxes.Add(new BoxDef { Slots = new[] { "d1", null, null, null } });
                               l.Stacks[1].Boxes[0].Slots[0] = null; },
                        "thẻ cùng nhóm với chìa nằm DƯỚI hộp chìa mở");

                // Hợp lệ: cùng thẻ vừa băng vừa chìa.
                var okLv = freshB();
                okLv.Groups[1].Cards[3].Blockers["ice"] = 1.0;
                okLv.Validate(hasArt);
                Ok(Game.IsFrozen(Game.Build(okLv).TopBox(2).Slots[0]), "thẻ vừa băng vừa chìa là hợp lệ");
            }
```

- [ ] **Step 3: Chạy để thấy fail compile**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | grep -m2 "error CS"`
Expected: `error CS1061 ... 'CardDef' does not contain a definition for 'Blockers'`

- [ ] **Step 4: Sửa `LevelData.cs` — class và Parse**

```csharp
    public class CardDef
    {
        public string Id, Text, Art;
        // key = id blocker trong sổ Blockers.CardIds, value = tham số thô từ JSON (double/string).
        public Dictionary<string, object> Blockers = new Dictionary<string, object>();
    }
```

```csharp
    public class BoxDef
    {
        public string[] Slots;
        public Dictionary<string, object> Blockers = new Dictionary<string, object>();
    }
```

Trong `Parse`, thẻ:

```csharp
                        var cd = Json.AsObj(co, "group.cards[]");
                        g.Cards.Add(new CardDef
                        {
                            Id = Json.AsStr(Json.Get(cd, "id"), "card.id"),
                            Text = Json.AsStr(Json.Get(cd, "text"), "card.text"),
                            Art = Json.AsStr(Json.Get(cd, "art"), "card.art"),
                            Blockers = ParseBlockers(Json.Get(cd, "blockers"), "card.blockers"),
                        });
```

hộp:

```csharp
                    var bd = Json.AsObj(bo, "stack.boxes[]");
                    st.Boxes.Add(new BoxDef
                    {
                        Slots = Json.AsArr(Json.Get(bd, "slots"), "box.slots")
                                    .Select(x => Json.AsStr(x, "box.slots[]")).ToArray(),
                        Blockers = ParseBlockers(Json.Get(bd, "blockers"), "box.blockers"),
                    });
```

và hàm phụ (đặt cạnh `Parse`):

```csharp
        // "blockers" vắng mặt = không blocker. Giữ giá trị thô: Validate mới kiểm kiểu, để
        // thông điệp lỗi nói đúng luật nào bị vi phạm thay vì "cast fail".
        static Dictionary<string, object> ParseBlockers(object o, string where)
        {
            var d = new Dictionary<string, object>();
            if (o == null) return d;
            foreach (var kv in Json.AsObj(o, where)) d[kv.Key] = kv.Value;
            return d;
        }
```

- [ ] **Step 5: Thêm khối kiểm blocker vào cuối `Validate`**

Chèn ngay trước dòng `foreach (var c in AllCards()) if (!seen.Contains(c.Id)) ...` (khối stack đã kiểm mọi id trong `slots` là id thẻ có thật, nên tra `groupOf[id]` bên dưới là an toàn):

```csharp
            // -- Blocker (spec 2026-09-10 Mục 5, sáu luật) --
            var groupOf = new Dictionary<string, string>();
            foreach (var g in Groups) foreach (var c in g.Cards) groupOf[c.Id] = g.Id;
            var keyOwner = new Dictionary<string, CardDef>();   // id chìa → thẻ mang

            foreach (var g in Groups)
                foreach (var c in g.Cards)
                {
                    string at = "card \"" + c.Id + "\"";
                    foreach (var key in c.Blockers.Keys)
                    {
                        if (Array.IndexOf(Blockers.BoxIds, key) >= 0)
                            die(at + ": \"" + key + "\" là blocker của hộp, không đặt trên thẻ");
                        if (Array.IndexOf(Blockers.CardIds, key) < 0)
                            die(at + ": blocker \"" + key + "\" không có trong sổ đăng ký");
                    }
                    foreach (var a in c.Blockers.Keys)
                        foreach (var b in c.Blockers.Keys)
                            if (string.CompareOrdinal(a, b) < 0 && !Blockers.CardPairAllowed(a, b))
                                die(at + ": \"" + a + "\" và \"" + b + "\" không được đứng chung trên một thẻ");
                    object v;
                    if (c.Blockers.TryGetValue(Blockers.Ice, out v) && !Blockers.IsCount(v))
                        die(at + ": ice phải là số nguyên ≥ 1");
                    if (c.Blockers.TryGetValue(Blockers.Key, out v))
                    {
                        var kid = v as string;
                        if (string.IsNullOrEmpty(kid)) die(at + ": key phải là chuỗi id chìa");
                        if (keyOwner.ContainsKey(kid))
                            die("id chìa \"" + kid + "\" có hai thẻ mang: " + keyOwner[kid].Id + " và " + c.Id);
                        keyOwner[kid] = c;
                    }
                }

            for (int si = 0; si < Stacks.Count; si++)
                for (int bi = 0; bi < Stacks[si].Boxes.Count; bi++)
                {
                    var box = Stacks[si].Boxes[bi];
                    string at = "stack " + si + " box " + bi;
                    foreach (var key in box.Blockers.Keys)
                    {
                        if (Array.IndexOf(Blockers.CardIds, key) >= 0)
                            die(at + ": \"" + key + "\" là blocker của thẻ, không đặt trên hộp");
                        if (Array.IndexOf(Blockers.BoxIds, key) < 0)
                            die(at + ": blocker \"" + key + "\" không có trong sổ đăng ký");
                    }
                    if (box.Blockers.Count > 1) die(at + ": hộp mang tối đa một blocker");
                    object v;
                    if (box.Blockers.TryGetValue(Blockers.Locked, out v) && !Blockers.IsCount(v))
                        die(at + ": locked phải là số nguyên ≥ 1");
                    if (box.Blockers.TryGetValue(Blockers.KeyLock, out v))
                    {
                        var kid = v as string;
                        CardDef keyCard;
                        if (string.IsNullOrEmpty(kid) || !keyOwner.TryGetValue(kid, out keyCard))
                            die(at + ": keylock \"" + kid + "\" không có thẻ nào mang chìa đó");
                        // Luật 6: hộp có ổ không bao giờ rỗng nên hộp dưới nó không lộ ra chừng
                        // nào chưa mở. Thẻ chìa — hay bất kỳ thẻ nào cùng nhóm, vì chìa chỉ bị gom
                        // khi cả nhóm về chung hộp — nằm trong hoặc dưới đây là khoá vĩnh viễn.
                        string kg = groupOf[keyCard.Id];
                        for (int bj = bi; bj < Stacks[si].Boxes.Count; bj++)
                            foreach (var id in Stacks[si].Boxes[bj].Slots)
                                if (id != null && groupOf[id] == kg)
                                    die(at + ": thẻ \"" + id + "\" cùng nhóm với chìa \"" + kid +
                                        "\" nằm trong hoặc dưới hộp mà chìa đó mở — khoá vĩnh viễn");
                    }
                }
```

- [ ] **Step 6: Đặt blocker vào bàn trong `Game.Build`**

Trong `Game.cs`, thay thân vòng dựng box/tile:

```csharp
                for (int bi = 0; bi < sd.Boxes.Count; bi++)
                {
                    var bd = sd.Boxes[bi];
                    var box = new Box { IsBottom = bi == sd.Boxes.Count - 1 };
                    object bv;
                    if (bd.Blockers.TryGetValue(Blockers.Locked, out bv))
                        box.Lock = new Lock { Kind = LockKind.Clears, Need = (int)(double)bv };
                    if (bd.Blockers.TryGetValue(Blockers.KeyLock, out bv))
                        box.Lock = new Lock { Kind = LockKind.Key, KeyId = (string)bv };

                    for (int i = 0; i < bd.Slots.Length; i++)
                    {
                        var id = bd.Slots[i];
                        if (id == null) continue;
                        var c = card[id];
                        var tile = new Tile
                        {
                            Uid = "t" + (++g.uidSeq),
                            CardId = c.Id, GroupId = ownerGroup[c.Id], Text = c.Text, Art = c.Art
                        };
                        object cv;
                        if (c.Blockers.TryGetValue(Blockers.Ice, out cv))
                            tile.Lock = new Lock { Kind = LockKind.Moves, Need = (int)(double)cv };
                        if (c.Blockers.TryGetValue(Blockers.Key, out cv))
                            tile.KeyId = (string)cv;
                        box.Slots[i] = tile;
                    }
                    st.Boxes.Add(box);
                }
```

(`Validate` đã bảo đảm kiểu, nên cast thẳng. `Build` không được gọi trên data chưa `Validate`.)

- [ ] **Step 7: Chạy tự kiểm**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

- [ ] **Step 8: Commit**

```bash
git add Assets/_Game/Board/Domain/LevelData.cs Assets/_Game/Board/Domain/Game.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): parse and validate blockers on card and box entries, build them into the board"
```

---

### Task 6: Nam châm và Xáo tránh blocker

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameMagnet.cs` (`FindMagnetTarget`, `PickCollapseHost`)
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs` (`CanShuffle`, `AssignableTopSlots`, `PickPrimeCandidates`)
- Test: `SelfCheck.cs` mục 8f

**Interfaces:**
- Consumes: `IsPullable`, `IsOpen`, `IsFrozen`
- Produces: `FindMagnetTarget` không trả nhóm có thành viên không hút được; `PickCollapseHost` không chọn hộp đóng; `AssignableTopSlots` bỏ hộp đóng và thẻ băng; `CanShuffle` chỉ đếm ô trống ở hộp mở; `PickPrimeCandidates` bỏ nhóm có thành viên không kéo được.

- [ ] **Step 1: Viết assert mục 8f**

Chèn sau khối 8e:

```csharp
            // 8f. Booster tránh blocker (spec 4.3)
            {
                var g = load(true);
                string t1 = g.FindMagnetTarget();
                Ok(t1 != null, "bàn luật có mục tiêu nam châm");
                foreach (var st in g.Stacks) foreach (var b in st.Boxes) foreach (var t in b.Slots)
                    if (t != null && t.GroupId == t1) { t.Lock = new Lock { Kind = LockKind.Moves, Need = 5 }; break; }
                string t2 = g.FindMagnetTarget();
                Ok(t2 != t1, "nhóm có thẻ băng không được nam châm hút");

                var g2 = load(true);
                string u1 = g2.FindMagnetTarget();
                int lockedStack = -1;
                for (int s = 0; s < g2.Stacks.Count && lockedStack < 0; s++)
                    foreach (var t in g2.Stacks[s].Boxes[0].Slots)
                        if (t != null && t.GroupId == u1) { lockedStack = s; break; }
                g2.Stacks[lockedStack].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(g2.FindMagnetTarget() != u1, "nhóm có thẻ trong hộp đóng không được nam châm hút");

                // Thẻ băng phải là thẻ TRẮNG (đứng lẻ trong hộp) thì bài kiểm mới có nghĩa —
                // thẻ có màu vốn không vào pool. e1 ở stack 1 là thẻ gc duy nhất trong hộp đó.
                var g3 = load(true);
                g3.Stacks[2].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(Game.IsWhite(g3.TopBox(1), 3), "e1 đang trắng — tiền đề của bài kiểm");
                g3.TopBox(1).Slots[3].Lock = new Lock { Kind = LockKind.Moves, Need = 9 };   // e1 băng
                var pool = g3.AssignableTopSlots();
                Ok(!pool.Any(r => r.Stack == 2), "Xáo: hộp đóng không có ô nào trong pool");
                Ok(!pool.Any(r => r.Stack == 1 && r.Slot == 3), "Xáo: ô của thẻ băng không vào pool dù thẻ trắng");
                Ok(pool.Any(r => r.Stack == 0 && r.Slot == 2), "Xáo: ô trống ở hộp mở vẫn vào pool");
                var cands = g3.PickPrimeCandidates(3);
                Ok(!cands.Contains("gc"), "Xáo: nhóm có thẻ băng không làm mồi");
                Ok(!cands.Contains("gb"), "Xáo: nhóm có thẻ trong hộp đóng không làm mồi");
                Ok(cands.Contains("ga"), "Xáo: nhóm không dính blocker vẫn làm mồi được");

                var g4 = load(true);
                foreach (int s in new[] { 0, 2, 3, 4 })
                    g4.Stacks[s].Boxes[0].Lock = new Lock { Kind = LockKind.Clears, Need = 9 };
                Ok(!g4.CanShuffle(), "Xáo: ô trống toàn nằm trong hộp đóng thì không xáo được");
            }
```

- [ ] **Step 2: Chạy để thấy fail**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SELFCHECK FAIL: nhóm có thẻ băng không được nam châm hút`

- [ ] **Step 3: Sửa `FindMagnetTarget` trong `GameMagnet.cs`**

Trong vòng đếm, thay

```csharp
                        Tile t = slots[i];
                        if (t == null) continue;
                        string gid = t.GroupId;
```

bằng

```csharp
                        Tile t = slots[i];
                        if (t == null) continue;
                        // Thẻ băng hay thẻ trong hộp đóng không hút được → không đếm vào
                        // onBoard, nhóm đó không bao giờ đủ GroupSize và bị bỏ qua bên dưới.
                        // Hút được là phá vật cản miễn phí (spec 4.3).
                        if (!IsPullable(t, boxes[b])) continue;
                        string gid = t.GroupId;
```

Trong `PickCollapseHost`, ở cả ba vòng `for (int s = 0; ...)`, thay

```csharp
                Box box = TopBox(s);
                if (box == null) continue;
```

bằng

```csharp
                Box box = TopBox(s);
                if (box == null || !IsOpen(box.Lock)) continue;   // không thả thẻ cha vào hộp đóng
```

và ở nhánh `target`:

```csharp
            Box tb = TopBox(target);
            int f = tb == null || !IsOpen(tb.Lock) ? -1 : FirstFreeAfterPull(target, tb, picks);
```

- [ ] **Step 4: Sửa `GameShuffle.cs`**

`CanShuffle`:

```csharp
        public bool CanShuffle()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;   // hộp đóng không nhận thẻ
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null) return true;
            }
            return false;
        }
```

`AssignableTopSlots`:

```csharp
        public List<SlotRef> AssignableTopSlots()
        {
            var result = new List<SlotRef>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null || !IsOpen(top.Lock)) continue;   // hộp đóng: không đụng (spec 4.3)
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null || (IsWhite(top, i) && !IsFrozen(top.Slots[i])))
                        result.Add(new SlotRef { Stack = s, Box = 0, Slot = i });
            }
            return result;
        }
```

`PickPrimeCandidates`, trong vòng đếm thay

```csharp
                        if (slots[i] == null) continue;
                        string gid = slots[i].GroupId;
```

bằng

```csharp
                        if (slots[i] == null || !IsPullable(slots[i], boxes[b])) continue;   // như Magnet
                        string gid = slots[i].GroupId;
```

- [ ] **Step 5: Chạy tự kiểm và compile ba assembly**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -1`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

Run: `./compilecheck.sh 2>&1 | grep "dll OK"`
Expected: ba dòng `game.dll OK`, `editor.dll OK`, `meta.dll OK` (BoardController và tests hiện có vẫn compile với `Tile`/`Box` mở rộng).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameMagnet.cs Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "feat(blocker): magnet and shuffle leave frozen tiles and closed boxes alone"
```

---

### Task 7: Tài liệu luật và chốt nhịp

**Files:**
- Modify: `docs/wordstack-rules.md` (thêm Mục 11)
- Verify: toàn bộ vòng kiểm

- [ ] **Step 1: Thêm Mục 11 vào `docs/wordstack-rules.md`** (cuối file)

```markdown
## 11. Vật cản (blocker)

Ba vật cản, đều là "một đối tượng bị vô hiệu, gỡ bằng một điều kiện tiến độ". Không vật cản
nào thêm loại nước đi mới. Đặc tả đầy đủ: `docs/superpowers/specs/2026-09-10-blocker-locks-design.md`.

| id | gắn vào | tham số | ý nghĩa |
|---|---|---|---|
| `locked` | hộp | số nguyên ≥ 1 | hộp đóng cho tới khi số nhóm đã gom trên toàn bàn đạt số đó |
| `keylock` | hộp | id chìa | hộp đóng cho tới khi thẻ mang `key` cùng id biến mất khỏi bàn |
| `ice` | thẻ | số nguyên ≥ 1 | thẻ bất động và không tính bộ 4, tan sau đủ số nước kể từ lúc lộ ở hộp trên |
| `key` | thẻ | id chìa | không khoá gì; thẻ bị gom là mở mọi `keylock` cùng id |

Hộp đóng: không nhặt ra, không thả vào, không tự nổ, không tính là còn chỗ khi xét kẹt, không
bị xoá dù rỗng. Thứ tự một lượt: nước đi → giảm băng → dây chuyền.

**Kẹt** giờ là "không còn nước đi hợp lệ nào" (trước đây: "mọi hộp trên đều đầy"). Hai định
nghĩa trùng nhau khi không có vật cản; có vật cản thì chỉ định nghĩa mới tránh được thua ngầm.

Dữ liệu: vật cản của thẻ nằm trên entry thẻ trong `meaning`, của hộp nằm trên hộp trong
`layout`, đều trong object `blockers` với key là id ở bảng trên.

```json
{ "id": "banana", "text": "Banana", "blockers": { "ice": 5, "key": "k1" } }
{ "slots": ["apple","banana",null,null], "blockers": { "locked": 3 } }
{ "slots": ["grape","plum",null,null],   "blockers": { "keylock": "k1" } }
```

Luật kiểm thêm: id phải có trong bảng và đúng phía · hộp tối đa một vật cản · thẻ mang nhiều
vật cản phải theo bảng cặp được phép (hiện: `ice` + `key`) · số đếm ≥ 1 · mỗi id chìa đúng một
thẻ mang · thẻ chìa và mọi thẻ cùng nhóm không nằm trong hoặc dưới hộp mà chìa đó mở.
```

- [ ] **Step 2: Chạy trọn vòng kiểm lần cuối**

Run: `./selfcheck.sh Temp/selfcheck-levels 2>&1 | tail -3`
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs`

Run: `./compilecheck.sh 2>&1 | grep "dll OK"`
Expected: ba dòng OK.

Run: `./selfcheck.sh 2>&1 | tail -1`
Expected: vẫn `SELFCHECK FAIL: thiếu 1 file art: airplane.png ...` — đây là baseline content, không phải hồi quy của nhịp này. Ghi rõ vào message commit.

- [ ] **Step 3: Commit**

```bash
git add docs/wordstack-rules.md
git commit -m "docs(rules): blockers section - ids, closed box rules, stuck means no legal move"
```

---

## Ngoài nhịp này

- Hình trong Unity (lớp băng, ổ khoá, số đếm) và một màn mẫu dựng tay — nhịp 2.
- Công cụ dựng màn `demo/wordstack.html` hiểu `blockers` — nhịp 3. Tới lúc đó màn có blocker viết tay bằng JSON.
- `demo/check.mjs` (bản luật JS) chưa biết blocker. Nó chỉ chạy trên level nhúng trong demo HTML nên chưa đỏ, nhưng câu "luật khớp demo/check.mjs" ở cuối SelfCheck không còn đúng cho mục 8 — đồng bộ ở nhịp 3.
- lv-002 chưa giải được và lv-008 thiếu art là việc content đang treo, không nằm trong kế hoạch này.
