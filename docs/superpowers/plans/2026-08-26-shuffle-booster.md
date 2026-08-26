# Shuffle Booster Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Booster Shuffle xáo lại nội dung thẻ để mở đường cho người chơi mà không phát clear miễn phí — mỗi nhóm nó dựng ra tốn đúng 1 nước đi để nổ.

**Architecture:** Luật sống trong `Assets/_Game/Board/Domain/GameShuffle.cs` (partial `Game`, không import UnityEngine) nên `selfcheck.sh` compile được ngoài Unity. Bàn nhận lệnh qua `LevelCommands` chứ không nghe `Bus.Global`, vì `WordStack.Board` chỉ tham chiếu `WordStack.Contracts`. `MetaSession` bắc cầu từ `BoosterActivatedEvent`. Toàn bộ hạ tầng này đã dựng sẵn cho booster Magnet — Shuffle đi lại đúng đường đó.

**Tech Stack:** C# / Unity 6000.3.8f1 · Reflex DI · R3 · NUnit (EditMode) · Roslyn qua Unity Hub cho hai script kiểm.

**Spec:** `docs/superpowers/specs/2026-08-26-shuffle-booster-design.md`

## Global Constraints

- `Rules.BoxCapacity = 4`, `Rules.GroupSize = 4`. Không hardcode số 4 — dùng hằng.
- Mọi file dưới `Assets/_Game/Board/Domain/` **KHÔNG được import `UnityEngine`**. `selfcheck.sh` compile cả thư mục bằng csc trần.
- `Assets/_Game/Contracts/` **không tham chiếu assembly nào**. Giữ C# thuần.
- `WordStack.Board.asmdef` chỉ tham chiếu `WordStack.Contracts` + `Unity.InputSystem`. Không thêm.
- `BoosterId.Shuffle = 6`. **Tuyệt đối không dùng 0** — `None = 0` là sentinel của ba chốt `if (evt.Id == BoosterId.None) return;` trong `BoosterManager`.
- Shuffle **không tăng `Game.Moves`**.
- `ApplyShuffle` **không được đụng danh sách `Boxes`**. `CheckStatus()` đọc `st.Boxes[0]` không kiểm rỗng.
- Bốn bất biến (spec Mục 5) phải đúng sau mỗi lần chạy: tổng lớp trên không đổi · mỗi top box ≥1 thẻ · không hộp nào trên **toàn bàn** có 4 thẻ cùng nhóm · thẻ có màu ở lớp trên nguyên vị trí và nội dung.

## Vòng phản hồi

Repo **không chạy được NUnit từ CLI**. Ba mức kiểm:

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `./selfcheck.sh` | compile `Domain/` + regression 7 level | `SelfCheck OK - 7 level, luật khớp demo/check.mjs` |
| `./compilecheck.sh` | compile `game` / `editor` / `meta` | `game.dll OK` `editor.dll OK` `meta.dll OK` |
| Unity ▸ Window ▸ General ▸ Test Runner ▸ EditMode | file NUnit | tất cả xanh |

Cả hai script đều loại trừ `*/Tests/*`, nên file NUnit chỉ được compile khi mở Unity.

**Đường chạy nhanh ngoài Unity** (dùng cho Task 1–4, đã verify được với Magnet): viết một `Main` tạm ngoài repo rồi compile chung với `Domain/`.

```bash
# Lưu harness vào $TMP/ShuffleHarness.cs với: public static class ShuffleHarness { public static int Main() {...} }
cd "$(git rev-parse --show-toplevel)"
UV="$(tr -d '\r' < ProjectSettings/ProjectVersion.txt | sed -n 's/^m_EditorVersion: //p')"
DATA="/c/Program Files/Unity/Hub/Editor/${UV}/Editor/Data"
DOTNET="$DATA/NetCoreRuntime/dotnet.exe"; CSC="$DATA/DotNetSdkRoslyn/csc.dll"
REF="$(ls -d "$DATA"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | head -1)"
OUT="$PWD/Temp/shuffle-harness"; mkdir -p "$OUT"
{ echo "-nologo"; echo "-nostdlib"; echo "-langversion:latest"; echo "-target:exe"
  echo "-main:ShuffleHarness"; echo "-out:\"$(cygpath -w "$OUT/h.dll")\""
  for f in "$REF"/System.*.dll "$REF"/netstandard.dll "$REF"/mscorlib.dll; do
    case "$f" in *Native*.dll) continue ;; esac; [ -f "$f" ] && echo "-r:\"$(cygpath -w "$f")\""; done
  find "$PWD/Assets/_Game/Board/Domain" -name '*.cs' | while read -r f; do echo "\"$(cygpath -w "$f")\""; done
  echo "\"$(cygpath -w "$TMP/ShuffleHarness.cs")\""
} > "$OUT/csc.rsp"
printf '{ "runtimeOptions": { "tfm": "net6.0", "framework": { "name": "Microsoft.NETCore.App", "version": "%s" } } }' \
  "$(basename "$REF")" > "$OUT/h.runtimeconfig.json"
"$DOTNET" "$CSC" "@$(cygpath -w "$OUT/csc.rsp")" && "$DOTNET" "$(cygpath -w "$OUT/h.dll")"
```

Nếu gặp `Application Control policy has blocked this file`: `rm -rf Temp/shuffle-harness` rồi chạy lại. Là trục trặc Windows, không phải lỗi code.

## Cấu trúc file

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Board/Domain/GameShuffle.cs` (mới) | Toàn bộ luật Shuffle: pool, chọn nhóm, xếp chỗ, kiểm bất biến |
| `Assets/_Game/Board/Tests/BoardShuffleTests.cs` (mới) | Test NUnit cho luật trên |
| `Assets/_Game/Contracts/LevelCommands.cs` | +`ShuffleRequested` / `RequestShuffle()` |
| `Assets/_Game/Contracts/LevelEvents.cs` | +`ShuffleAvailable` + event đổi cờ |
| `Assets/_Game/Board/Views/BoardController.cs` | Đăng ký lệnh, chuỗi diễn, chặn input, đẩy cờ availability |
| `Assets/_Game/MetaSession.cs` | +nhánh `BoosterId.Shuffle` trong cầu nối sẵn có |
| `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs` (mới) | Trừ lượt + gương cờ availability sang R3 |
| `Assets/_Game/Gameplay/Boosters/Views/ShuffleBoosterButtonView.cs` (mới) | Nút, xám khi bàn không xáo được |
| `Assets/BoosterModule/BoosterId.cs` | +`Shuffle = 6` |
| `Assets/_Game/Currency/Transactions/ItemIds.cs` | +`booster.shuffle` |
| `Assets/_Game/Currency/UI/TransactionIds.cs` | +`t_booster_shuffle` + 2 nhánh switch |
| `Assets/_Game/Currency/Services/Impl/TransactionItemDispatcher.cs` | +case cấp lượt |
| `Assets/_Game/AppFlow/Installers/AppFlowInstaller.cs` | +đăng ký ViewModel |

---

### Task 1: Nền domain — pool, thống kê, bộ kiểm bất biến

Không có bước này thì ba task sau không tự kiểm được kết quả của mình.

**Files:**
- Create: `Assets/_Game/Board/Domain/GameShuffle.cs`
- Create: `Assets/_Game/Board/Tests/BoardShuffleTests.cs`

**Interfaces:**
- Consumes: `Game.Stacks`, `Game.TopBox(int)`, `Game.GroupDefs`, `Box.Slots`, `Tile.GroupId`, `Rules.GroupSize`, `Rules.BoxCapacity` — đã có sẵn.
- Produces:
  - `public static bool IsWhite(Box box, int slot)` — thẻ ở ô đó có đang đứng lẻ trong hộp không
  - `public int TopLayerTileCount()` — tổng thẻ ở lớp trên cùng
  - `public bool AnyBoxHasFullGroup()` — có hộp nào (mọi layer) đủ 4 thẻ cùng nhóm không
  - `public bool CanShuffle()` — lớp trên còn ≥1 ô trống

- [ ] **Step 1: Viết test thất bại**

Tạo `Assets/_Game/Board/Tests/BoardShuffleTests.cs`:

```csharp
// Luật booster Shuffle. Level viết thẳng trong file như BoardRulesTests: test luật thì
// không được phụ thuộc file level ship. Chỉ Parse + Build, KHÔNG gọi Validate — Validate
// đòi level có cả thẻ chỉ-ảnh lẫn thẻ chỉ-chữ, không liên quan gì tới Shuffle.
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class BoardShuffleTests
    {
        static Game Build(string json) { return Game.Build(LevelData.Parse(json)); }

        // Stack 0 top: 2 thẻ ga (thành cụm → có màu) + 1 thẻ gb (đứng lẻ → trắng) + 1 ô trống.
        // Stack 1 top: 2 thẻ gb. Stack 2: 1 thẻ ga trên, 3 thẻ dưới.
        const string Lv = @"{
          ""id"":""s-base"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",""b1"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""b2"",""b3"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""a3"",null,null,null] },
                                          { ""slots"":[""a4"",""b4"",""c1"",""c2""] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              {""id"":""a1"",""text"":""A1""},{""id"":""a2"",""text"":""A2""},
              {""id"":""a3"",""text"":""A3""},{""id"":""a4"",""text"":""A4""} ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              {""id"":""b1"",""text"":""B1""},{""id"":""b2"",""text"":""B2""},
              {""id"":""b3"",""text"":""B3""},{""id"":""b4"",""text"":""B4""} ]},
            { ""id"":""gc"", ""text"":""C"", ""cards"":[
              {""id"":""c1"",""text"":""C1""},{""id"":""c2"",""text"":""C2""},
              {""id"":""c3"",""text"":""C3""},{""id"":""c4"",""text"":""C4""} ]}
          ]}
        }";

        [Test]
        public void TheDungLeLaTrang_TheThanhCumLaCoMau()
        {
            var g = Build(Lv);
            Box top0 = g.TopBox(0);

            Assert.IsFalse(Game.IsWhite(top0, 0), "a1 có a2 cùng hộp → thành cụm → có màu");
            Assert.IsFalse(Game.IsWhite(top0, 1), "a2 tương tự");
            Assert.IsTrue(Game.IsWhite(top0, 2), "b1 đứng lẻ trong hộp → trắng");
            Assert.IsFalse(Game.IsWhite(top0, 3), "ô trống không phải thẻ trắng");
        }

        [Test]
        public void DemDungTongTheOLopTrenCung()
        {
            // stack 0: 3 thẻ, stack 1: 2 thẻ, stack 2 top: 1 thẻ. Hộp dưới KHÔNG tính.
            Assert.AreEqual(6, Build(Lv).TopLayerTileCount());
        }

        [Test]
        public void PhatHienDuocHopDuBonTheCungNhomOMoiLayer()
        {
            Assert.IsFalse(Build(Lv).AnyBoxHasFullGroup(), "level nền không hộp nào đủ 4");

            var g = Build(Lv);
            // Nhét 4 thẻ gc vào hộp bị chôn của stack 2 — SettleStep không soi hộp chôn,
            // nhưng bất biến của Shuffle phải bắt được.
            Box buried = g.Stacks[2].Boxes[1];
            for (int i = 0; i < buried.Slots.Length; i++)
                buried.Slots[i] = new Tile { Uid = "z" + i, CardId = "c" + i, GroupId = "gc" };

            Assert.IsTrue(g.AnyBoxHasFullGroup(), "4 thẻ gc trong hộp bị chôn phải bị bắt");
        }

        [Test]
        public void CanShuffleTheoOTrongOLopTren()
        {
            Assert.IsTrue(Build(Lv).CanShuffle(), "lớp trên còn ô trống");

            var g = Build(Lv);
            // Lấp kín mọi top box.
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                Box top = g.TopBox(s);
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null) top.Slots[i] = new Tile { Uid = "f" + s + i, CardId = "f", GroupId = "gz" };
            }
            Assert.IsFalse(g.CanShuffle(), "lớp trên đầy kín → không tạo được ô trống cho hộp chủ");
        }
    }
}
```

- [ ] **Step 2: Chạy để xác nhận nó fail**

Chuyển 4 test trên thành harness tạm (xem "Vòng phản hồi") rồi chạy.
Expected: FAIL khi compile — `'Game' does not contain a definition for 'IsWhite'`.

- [ ] **Step 3: Viết implementation tối thiểu**

Tạo `Assets/_Game/Board/Domain/GameShuffle.cs`:

```csharp
// Booster Shuffle: xáo lại nội dung thẻ để mở đường cho người chơi, KHÔNG phát clear
// miễn phí. Tách khỏi Game.cs để file đó chỉ còn luật bàn chơi.
//
// KHÔNG import UnityEngine (xem Rules.cs) — selfcheck.sh compile cả thư mục Domain/.
using System.Collections.Generic;

namespace WordStack.Board
{
    public partial class Game
    {
        /// <summary>
        /// Thẻ ở ô này có đang ĐỨNG LẺ trong hộp không (nhóm của nó có &lt;2 thẻ trong
        /// chính hộp đó). Đúng bằng điều kiện BoxColorIndices dùng để KHÔNG cấp màu —
        /// nên "trắng" ở đây là thứ người chơi thật sự nhìn thấy là trắng.
        ///
        /// Ô trống trả false: không có thẻ thì không phải thẻ trắng.
        /// </summary>
        public static bool IsWhite(Box box, int slot)
        {
            if (box == null || slot < 0 || slot >= box.Slots.Length) return false;
            Tile t = box.Slots[slot];
            if (t == null) return false;

            int same = 0;
            for (int i = 0; i < box.Slots.Length; i++)
            {
                Tile o = box.Slots[i];
                if (o != null && o.GroupId == t.GroupId) same++;
            }
            return same < 2;
        }

        /// <summary>Tổng số thẻ ở lớp trên cùng. Bất biến 1 của Shuffle giữ con số này.</summary>
        public int TopLayerTileCount()
        {
            int n = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] != null) n++;
            }
            return n;
        }

        /// <summary>
        /// Có hộp nào trên TOÀN BÀN đủ 4 thẻ cùng nhóm không — kể cả hộp bị chôn.
        ///
        /// Phải phủ cả hộp chôn: SettleStep chỉ soi top box nên bộ đủ 4 nằm dưới sẽ im
        /// lặng rồi nổ đúng lúc hộp trên bị xoá, người chơi tốn 0 nước. Đó là clear miễn
        /// phí đến trễ một nhịp, vẫn vi phạm nguyên tắc của Shuffle.
        /// </summary>
        public bool AnyBoxHasFullGroup()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    var count = new Dictionary<string, int>();
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        int n;
                        count.TryGetValue(slots[i].GroupId, out n);
                        count[slots[i].GroupId] = n + 1;
                        if (n + 1 >= Rules.GroupSize) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Bàn có xáo được không. Cần ≥1 ô trống ở lớp trên để hộp chủ chừa được chỗ cho
        /// người chơi thả thẻ thứ 4 — không có ô trống thì Nhóm mồi 3+1 vô nghĩa.
        /// </summary>
        public bool CanShuffle()
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null) return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: Chạy lại, xác nhận pass**

Harness. Expected: cả 4 nhóm assert in `ok`.

- [ ] **Step 5: Chạy hai cổng của repo**

```bash
./selfcheck.sh
```
Expected: `SelfCheck OK - 7 level, luật khớp demo/check.mjs`

```bash
./compilecheck.sh
```
Expected: `game.dll OK` · `editor.dll OK` · `meta.dll OK`

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Tests/BoardShuffleTests.cs
git commit -m "feat(booster): Shuffle domain primitives - white tiles, invariant probes"
```

---

### Task 2: Chọn nhóm để dựng mồi và kéo donor lên

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs`
- Modify: `Assets/_Game/Board/Tests/BoardShuffleTests.cs`

**Interfaces:**
- Consumes: `Game.IsWhite`, `Game.TopLayerTileCount` (Task 1).
- Produces:
  - `public struct SlotRef { public int Stack, Box, Slot; }`
  - `public int CountPrimedGroups()` — số Nhóm mồi 3+1 đang có sẵn
  - `public List<string> PickPrimeCandidates(int max)` — nhóm nên dựng, thứ tự xác định

- [ ] **Step 1: Viết test thất bại**

Thêm vào `BoardShuffleTests.cs`:

```csharp
        // Stack 0 top: 3 thẻ ga + 1 ô trống. Stack 1 top: 1 thẻ ga.
        // Đúng định nghĩa Nhóm mồi 3+1: người chơi kéo ga ở stack 1 sang là nổ.
        const string LvPrimed = @"{
          ""id"":""s-primed"", ""title"":""t"", ""note"":"""",
          ""layout"": { ""stacks"": [
            { ""pos"":[0,0], ""boxes"":[ { ""slots"":[""a1"",""a2"",""a3"",null] } ] },
            { ""pos"":[1,0], ""boxes"":[ { ""slots"":[""a4"",""b1"",null,null] } ] },
            { ""pos"":[2,0], ""boxes"":[ { ""slots"":[""b2"",""b3"",""b4"",null] } ] }
          ]},
          ""meaning"": { ""groups"": [
            { ""id"":""ga"", ""text"":""A"", ""cards"":[
              {""id"":""a1"",""text"":""A1""},{""id"":""a2"",""text"":""A2""},
              {""id"":""a3"",""text"":""A3""},{""id"":""a4"",""text"":""A4""} ]},
            { ""id"":""gb"", ""text"":""B"", ""cards"":[
              {""id"":""b1"",""text"":""B1""},{""id"":""b2"",""text"":""B2""},
              {""id"":""b3"",""text"":""B3""},{""id"":""b4"",""text"":""B4""} ]}
          ]}
        }";

        [Test]
        public void DemDungNhomMoiDangCoSan()
        {
            // ga: 3 thẻ ở stack 0 + hộp đó còn ô trống + thẻ thứ 4 ở stack 1 → là nhóm mồi.
            // gb: 3 thẻ ở stack 2 + còn ô trống + thẻ thứ 4 ở stack 1 → cũng là nhóm mồi.
            Assert.AreEqual(2, Build(LvPrimed).CountPrimedGroups());
        }

        [Test]
        public void HopDayThiKhongTinhLaNhomMoi()
        {
            var g = Build(LvPrimed);
            // Lấp nốt ô trống của stack 0 → người chơi không thả thẻ thứ 4 vào được nữa.
            g.TopBox(0).Slots[3] = new Tile { Uid = "z", CardId = "z", GroupId = "gz" };

            Assert.AreEqual(1, g.CountPrimedGroups(), "chỉ còn gb là nhóm mồi hợp lệ");
        }

        [Test]
        public void LoaiNhomChuaDuBonTheTrenBan()
        {
            var g = Build(Lv);
            // gc chỉ có c1, c2 trên bàn (c3, c4 không nằm trong layout) → không dựng được.
            List<string> picks = g.PickPrimeCandidates(3);
            CollectionAssert.DoesNotContain(picks, "gc", "nhóm chưa đủ 4 thẻ thật thì không dựng nổi");
        }
```

Thêm `using System.Collections.Generic;` vào đầu file test.

- [ ] **Step 2: Chạy để xác nhận fail**

Expected: `'Game' does not contain a definition for 'CountPrimedGroups'`.

- [ ] **Step 3: Implementation**

Thêm vào `GameShuffle.cs`, trong `partial class Game`:

```csharp
        /// <summary>Địa chỉ một ô trên bàn. Box = 0 là hộp trên cùng.</summary>
        public struct SlotRef
        {
            public int Stack, Box, Slot;
        }

        /// <summary>
        /// Số Nhóm mồi 3+1 đang có sẵn: một top box giữ đúng 3 thẻ cùng nhóm VÀ còn ô
        /// trống, cộng thêm ≥1 thẻ nữa của nhóm đó ở top box khác.
        ///
        /// Ô trống là bắt buộc: MoveTile từ chối hộp đích đầy, nên hộp chủ đầy 4 ô thì
        /// người chơi không thả được thẻ thứ 4 vào — không còn là "đúng 1 nước".
        /// </summary>
        public int CountPrimedGroups()
        {
            var primed = new HashSet<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box host = TopBox(s);
                if (host == null) continue;

                bool hasFree = false;
                var count = new Dictionary<string, int>();
                for (int i = 0; i < host.Slots.Length; i++)
                {
                    if (host.Slots[i] == null) { hasFree = true; continue; }
                    int n;
                    count.TryGetValue(host.Slots[i].GroupId, out n);
                    count[host.Slots[i].GroupId] = n + 1;
                }
                if (!hasFree) continue;

                foreach (var kv in count)
                {
                    if (kv.Value != Rules.GroupSize - 1) continue;
                    if (CountGroupOnTopLayerExcept(kv.Key, s) > 0) primed.Add(kv.Key);
                }
            }
            return primed.Count;
        }

        int CountGroupOnTopLayerExcept(string gid, int exceptStack)
        {
            int n = 0;
            for (int s = 0; s < Stacks.Count; s++)
            {
                if (s == exceptStack) continue;
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] != null && top.Slots[i].GroupId == gid) n++;
            }
            return n;
        }

        /// <summary>
        /// Các nhóm đáng dựng mồi, nhiều nhất <paramref name="max"/> nhóm.
        ///
        /// Chỉ nhận nhóm có ĐỦ 4 thẻ đang tồn tại trên bàn: nhóm cha còn nhóm con chưa
        /// collapse thì thành viên thiếu chưa tồn tại dưới dạng thẻ, không kéo lên được
        /// — cùng ràng buộc với booster Magnet.
        ///
        /// Thứ tự: nhiều thẻ sẵn ở lớp trên nhất trước (ít phải kéo donor nhất), hoà thì
        /// theo group id. Bậc cuối chỉ để kết quả xác định, test lại được.
        /// </summary>
        public List<string> PickPrimeCandidates(int max)
        {
            var onBoard = new Dictionary<string, int>();
            var onTop = new Dictionary<string, int>();
            var order = new List<string>();

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    Tile[] slots = boxes[b].Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        string gid = slots[i].GroupId;
                        int n;
                        if (!onBoard.TryGetValue(gid, out n)) { order.Add(gid); onTop[gid] = 0; }
                        onBoard[gid] = n + 1;
                        if (b == 0) onTop[gid] = onTop[gid] + 1;
                    }
                }
            }

            var eligible = new List<string>();
            for (int k = 0; k < order.Count; k++)
                if (onBoard[order[k]] == Rules.GroupSize) eligible.Add(order[k]);

            eligible.Sort(delegate (string a, string b)
            {
                if (onTop[a] != onTop[b]) return onTop[b] - onTop[a];
                return string.CompareOrdinal(a, b);
            });

            if (eligible.Count > max) eligible.RemoveRange(max, eligible.Count - max);
            return eligible;
        }
```

- [ ] **Step 4: Chạy lại, xác nhận pass**

Harness. Expected: tất cả `ok`.

- [ ] **Step 5: Hai cổng của repo**

```bash
./selfcheck.sh && ./compilecheck.sh
```
Expected: `SelfCheck OK - 7 level` và ba dòng `.dll OK`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Tests/BoardShuffleTests.cs
git commit -m "feat(booster): Shuffle primed-group detection and candidate ranking"
```

---

### Task 3: Xếp chỗ — dựng Nhóm mồi và Gom cụm

Trái tim của booster. Task này chỉ **sắp xếp**, chưa lo rollback.

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs`
- Modify: `Assets/_Game/Board/Tests/BoardShuffleTests.cs`

**Interfaces:**
- Consumes: `SlotRef`, `PickPrimeCandidates`, `CountPrimedGroups`, `IsWhite` (Task 1–2).
- Produces:
  - `public List<SlotRef> AssignableTopSlots()` — ô lớp trên được phép đụng (thẻ trắng + ô trống)
  - `public bool TryPrimeGroup(string gid)` — dựng 3+1 cho một nhóm, false nếu không xếp nổi
  - `public void ClusterLeftovers()` — Gom cụm phần thừa

- [ ] **Step 1: Viết test thất bại**

Thêm vào `BoardShuffleTests.cs`:

```csharp
        [Test]
        public void OKhaDungGomTheTrangVaOTrong_KhongGomTheCoMau()
        {
            var g = Build(Lv);
            List<Game.SlotRef> slots = g.AssignableTopSlots();

            // Stack 0: a1/a2 thành cụm → loại; b1 trắng → nhận; ô 3 trống → nhận.
            Assert.IsFalse(slots.Exists(r => r.Stack == 0 && r.Slot == 0), "a1 có màu, không đụng");
            Assert.IsFalse(slots.Exists(r => r.Stack == 0 && r.Slot == 1), "a2 có màu, không đụng");
            Assert.IsTrue(slots.Exists(r => r.Stack == 0 && r.Slot == 2), "b1 trắng");
            Assert.IsTrue(slots.Exists(r => r.Stack == 0 && r.Slot == 3), "ô trống dùng để dịch chỗ");
            Assert.IsTrue(slots.TrueForAll(r => r.Box == 0), "chỉ lớp trên cùng");
        }

        [Test]
        public void DungDuocNhomMoiVaKhongLamNoBatKyHopNao()
        {
            var g = Build(Lv);
            int before = g.TopLayerTileCount();

            Assert.IsTrue(g.TryPrimeGroup("ga"), "ga có đủ 4 thẻ trên bàn");

            Assert.AreEqual(before, g.TopLayerTileCount(), "tổng lớp trên phải giữ nguyên");
            Assert.IsFalse(g.AnyBoxHasFullGroup(), "không hộp nào được đủ 4 → không tự nổ");
            Assert.GreaterOrEqual(g.CountPrimedGroups(), 1, "phải có ít nhất một nhóm mồi");
        }

        [Test]
        public void MoiTopBoxVanGiuItNhatMotThe()
        {
            var g = Build(Lv);
            g.TryPrimeGroup("ga");
            g.ClusterLeftovers();

            for (int s = 0; s < g.Stacks.Count; s++)
            {
                int n = 0;
                foreach (Tile t in g.TopBox(s).Slots) if (t != null) n++;
                Assert.Greater(n, 0, "top box rỗng sẽ bị SettleStep xoá, làm lộ hộp dưới");
            }
        }

        [Test]
        public void GomCumUuTienKichThuocChuKhongPhaiSoLuong()
        {
            var g = Build(Lv);
            g.ClusterLeftovers();

            // Đếm cụm lớn nhất của mỗi nhóm trên lớp trên.
            int biggest = 0;
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                var count = new Dictionary<string, int>();
                foreach (Tile t in g.TopBox(s).Slots)
                {
                    if (t == null) continue;
                    int n; count.TryGetValue(t.GroupId, out n);
                    count[t.GroupId] = n + 1;
                    if (n + 1 > biggest) biggest = n + 1;
                }
            }
            Assert.GreaterOrEqual(biggest, 2, "phải dồn được ít nhất một cụm ≥2");
            Assert.LessOrEqual(biggest, Rules.GroupSize - 1, "tối đa 3 thẻ/nhóm/hộp, chạm 4 là nổ");
        }
```

- [ ] **Step 2: Chạy để xác nhận fail**

Expected: `'Game' does not contain a definition for 'AssignableTopSlots'`.

- [ ] **Step 3: Implementation**

Thêm vào `GameShuffle.cs`:

```csharp
        /// <summary>
        /// Các ô ở lớp trên cùng Shuffle được phép đụng: ô đang giữ thẻ TRẮNG, và ô TRỐNG
        /// (dùng làm đích khi dịch chỗ). Ô giữ thẻ có màu bị loại — đó là cụm người chơi
        /// đã gom, booster không phá.
        /// </summary>
        public List<SlotRef> AssignableTopSlots()
        {
            var result = new List<SlotRef>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) continue;
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null || IsWhite(top, i))
                        result.Add(new SlotRef { Stack = s, Box = 0, Slot = i });
            }
            return result;
        }

        // Số thẻ ở layer 2 của một stack — tiêu chí chọn hộp chủ. Hộp chủ nổ xong sẽ rỗng
        // và bị xoá, hộp dưới lộ ra: chọn hộp ngồi trên hộp đầy nhất thì lượt sau người
        // chơi có nhiều nguyên liệu nhất. Stack chỉ có một hộp thì đếm 0, xếp cuối.
        int Layer2TileCount(int stack)
        {
            List<Box> boxes = Stacks[stack].Boxes;
            if (boxes.Count < 2) return 0;
            int n = 0;
            foreach (Tile t in boxes[1].Slots) if (t != null) n++;
            return n;
        }

        /// <summary>
        /// Dựng Nhóm mồi 3+1 cho <paramref name="gid"/>: 3 thẻ vào hộp chủ (hộp đó kết
        /// thúc với ĐÚNG 3 ô có thẻ, chừa 1 ô trống), thẻ thứ 4 sang top box khác.
        ///
        /// Trả false khi không xếp nổi — gọi bên ngoài phải coi đó là bình thường, không
        /// phải lỗi.
        /// </summary>
        public bool TryPrimeGroup(string gid)
        {
            List<SlotRef> pool = AssignableTopSlots();
            if (pool.Count == 0) return false;

            // Hộp chủ: ưu tiên layer 2 nhiều thẻ nhất, hoà thì stack nhỏ hơn. Phải có đủ
            // ô khả dụng để chứa 3 thẻ và chừa 1 ô trống.
            int host = -1;
            for (int s = 0; s < Stacks.Count; s++)
            {
                if (CountAssignableIn(pool, s) < Rules.GroupSize) continue;
                if (host < 0 || Layer2TileCount(s) > Layer2TileCount(host)) host = s;
            }
            if (host < 0) return false;

            // Thẻ thứ 4 phải nằm ở top box KHÁC — chôn xuống dưới là người chơi không
            // với tới, không còn là "đúng 1 nước".
            int carrier = -1;
            for (int s = 0; s < Stacks.Count && carrier < 0; s++)
                if (s != host && CountAssignableIn(pool, s) > 0) carrier = s;
            if (carrier < 0) return false;

            var need = new List<Tile>();
            for (int k = 0; k < Rules.GroupSize; k++)
            {
                Tile t = TakeTileOfGroup(gid, pool);
                if (t == null) { PutBack(need, pool); return false; }
                need.Add(t);
            }

            // Dọn sạch phần khả dụng của hộp chủ và hộp mang thẻ thứ 4 trước khi đặt.
            var spill = new List<Tile>();
            DrainAssignable(pool, host, spill);
            DrainAssignable(pool, carrier, spill);

            Box hostBox = TopBox(host);
            int placed = 0;
            for (int i = 0; i < hostBox.Slots.Length && placed < Rules.GroupSize - 1; i++)
                if (IsAssignable(pool, host, i)) { hostBox.Slots[i] = need[placed++]; }

            Box carrierBox = TopBox(carrier);
            for (int i = 0; i < carrierBox.Slots.Length; i++)
                if (IsAssignable(pool, carrier, i) && carrierBox.Slots[i] == null)
                { carrierBox.Slots[i] = need[Rules.GroupSize - 1]; break; }

            // Thẻ bị đẩy ra rải vào các ô khả dụng còn trống, tránh dồn đủ 4 cùng nhóm.
            ScatterWithoutCompleting(spill, pool);
            return true;
        }

        /// <summary>
        /// Gom cụm phần thừa ở lớp trên: dồn thẻ cùng nhóm về CHUNG một hộp, tối đa
        /// <c>GroupSize - 1</c> thẻ mỗi hộp.
        ///
        /// Tối ưu theo KÍCH THƯỚC cụm chứ không phải số cụm: một hộp 3 thẻ P cách clear
        /// 1 nước, hai hộp mỗi hộp 2 thẻ P cách 2 nước. Nên duyệt nhóm theo số thẻ thừa
        /// giảm dần và dồn hết mức cho từng nhóm.
        /// </summary>
        public void ClusterLeftovers()
        {
            List<SlotRef> pool = AssignableTopSlots();
            var loose = new List<Tile>();
            DrainAll(pool, loose);

            var byGroup = new Dictionary<string, List<Tile>>();
            var order = new List<string>();
            foreach (Tile t in loose)
            {
                List<Tile> l;
                if (!byGroup.TryGetValue(t.GroupId, out l))
                { l = new List<Tile>(); byGroup[t.GroupId] = l; order.Add(t.GroupId); }
                l.Add(t);
            }
            order.Sort(delegate (string a, string b)
            {
                if (byGroup[a].Count != byGroup[b].Count) return byGroup[b].Count - byGroup[a].Count;
                return string.CompareOrdinal(a, b);
            });

            var rest = new List<Tile>();
            foreach (string gid in order)
            {
                List<Tile> tiles = byGroup[gid];
                int chunk = tiles.Count < Rules.GroupSize - 1 ? tiles.Count : Rules.GroupSize - 1;
                int target = FindStackWithFreeAssignable(pool, chunk);
                if (target < 0) { rest.AddRange(tiles); continue; }

                Box box = TopBox(target);
                int put = 0;
                for (int i = 0; i < box.Slots.Length && put < chunk; i++)
                    if (IsAssignable(pool, target, i) && box.Slots[i] == null) box.Slots[i] = tiles[put++];

                for (int k = put; k < tiles.Count; k++) rest.Add(tiles[k]);
            }
            ScatterWithoutCompleting(rest, pool);
        }
```

Và bộ hàm phụ, cũng trong `partial class Game`:

```csharp
        static bool IsAssignable(List<SlotRef> pool, int stack, int slot)
        {
            for (int k = 0; k < pool.Count; k++)
                if (pool[k].Stack == stack && pool[k].Slot == slot) return true;
            return false;
        }

        static int CountAssignableIn(List<SlotRef> pool, int stack)
        {
            int n = 0;
            for (int k = 0; k < pool.Count; k++) if (pool[k].Stack == stack) n++;
            return n;
        }

        // Nhấc mọi thẻ khỏi các ô khả dụng của một stack, gom vào spill.
        void DrainAssignable(List<SlotRef> pool, int stack, List<Tile> spill)
        {
            Box box = TopBox(stack);
            for (int i = 0; i < box.Slots.Length; i++)
                if (IsAssignable(pool, stack, i) && box.Slots[i] != null)
                { spill.Add(box.Slots[i]); box.Slots[i] = null; }
        }

        void DrainAll(List<SlotRef> pool, List<Tile> spill)
        {
            for (int k = 0; k < pool.Count; k++)
            {
                Box box = TopBox(pool[k].Stack);
                Tile t = box.Slots[pool[k].Slot];
                if (t != null) { spill.Add(t); box.Slots[pool[k].Slot] = null; }
            }
        }

        // Lấy một thẻ nhóm gid: ưu tiên ô khả dụng ở lớp trên; hết thì đổi nội dung với
        // donor ở layer dưới — trả thẻ donor lên chỗ vừa nhấc để mọi số đếm không đổi.
        Tile TakeTileOfGroup(string gid, List<SlotRef> pool)
        {
            for (int k = 0; k < pool.Count; k++)
            {
                Box box = TopBox(pool[k].Stack);
                Tile t = box.Slots[pool[k].Slot];
                if (t != null && t.GroupId == gid) { box.Slots[pool[k].Slot] = null; return t; }
            }

            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 1; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null || t.GroupId != gid) continue;
                        Tile swapIn = TakeAnyFromPool(pool);
                        if (swapIn == null) return null;
                        boxes[b].Slots[i] = swapIn;   // thẻ lớp trên xuống thế chỗ donor
                        return t;
                    }
            }
            return null;
        }

        Tile TakeAnyFromPool(List<SlotRef> pool)
        {
            for (int k = 0; k < pool.Count; k++)
            {
                Box box = TopBox(pool[k].Stack);
                Tile t = box.Slots[pool[k].Slot];
                if (t != null) { box.Slots[pool[k].Slot] = null; return t; }
            }
            return null;
        }

        void PutBack(List<Tile> tiles, List<SlotRef> pool)
        {
            ScatterWithoutCompleting(tiles, pool);
        }

        int FindStackWithFreeAssignable(List<SlotRef> pool, int need)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box box = TopBox(s);
                int free = 0;
                for (int i = 0; i < box.Slots.Length; i++)
                    if (IsAssignable(pool, s, i) && box.Slots[i] == null) free++;
                if (free >= need) return s;
            }
            return -1;
        }

        // Rải thẻ vào ô khả dụng còn trống, bỏ qua ô nào làm hộp đủ 4 thẻ cùng nhóm.
        void ScatterWithoutCompleting(List<Tile> tiles, List<SlotRef> pool)
        {
            foreach (Tile t in tiles)
            {
                bool done = false;
                for (int k = 0; k < pool.Count && !done; k++)
                {
                    Box box = TopBox(pool[k].Stack);
                    int slot = pool[k].Slot;
                    if (box.Slots[slot] != null) continue;
                    if (CountGroupInBox(box, t.GroupId) >= Rules.GroupSize - 1) continue;
                    box.Slots[slot] = t;
                    done = true;
                }
                if (!done) ForcePlaceAnywhere(t, pool);
            }
        }

        static int CountGroupInBox(Box box, string gid)
        {
            int n = 0;
            foreach (Tile t in box.Slots) if (t != null && t.GroupId == gid) n++;
            return n;
        }

        // Lưới an toàn: thẻ không còn chỗ "an toàn" thì vẫn phải nằm đâu đó, không được
        // biến mất. Bất biến ở Task 4 sẽ bắt nếu chỗ này tạo ra hộp đủ 4.
        void ForcePlaceAnywhere(Tile t, List<SlotRef> pool)
        {
            for (int k = 0; k < pool.Count; k++)
            {
                Box box = TopBox(pool[k].Stack);
                if (box.Slots[pool[k].Slot] == null) { box.Slots[pool[k].Slot] = t; return; }
            }
        }
```

- [ ] **Step 4: Chạy lại, xác nhận pass**

Harness. Expected: tất cả `ok`.

- [ ] **Step 5: Hai cổng của repo**

```bash
./selfcheck.sh && ./compilecheck.sh
```

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Tests/BoardShuffleTests.cs
git commit -m "feat(booster): Shuffle placement - prime 3+1 groups and cluster leftovers"
```

---

### Task 4: Điều phối `ApplyShuffle` + rollback

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameShuffle.cs`
- Modify: `Assets/_Game/Board/Tests/BoardShuffleTests.cs`

**Interfaces:**
- Consumes: mọi thứ Task 1–3.
- Produces:
  - `public struct ShuffleMove { public string Uid; public SlotRef From, To; }`
  - `public struct ShuffleResult { public bool Ok; public ShuffleMove[] Moves; public int PrimedGroups; }`
  - `public ShuffleResult ApplyShuffle()`

- [ ] **Step 1: Viết test thất bại**

```csharp
        [Test]
        public void ApplyShuffleGiuDuBonBatBien()
        {
            var g = Build(Lv);
            int before = g.TopLayerTileCount();
            string coloredUid = g.TopBox(0).Slots[0].Uid;

            ShuffleResult r = g.ApplyShuffle();

            Assert.IsTrue(r.Ok);
            Assert.AreEqual(before, g.TopLayerTileCount(), "bất biến 1: tổng lớp trên");
            Assert.IsFalse(g.AnyBoxHasFullGroup(), "bất biến 3: không hộp nào đủ 4");
            Assert.AreEqual(coloredUid, g.TopBox(0).Slots[0].Uid, "bất biến 4: thẻ có màu đứng yên");
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                int n = 0;
                foreach (Tile t in g.TopBox(s).Slots) if (t != null) n++;
                Assert.Greater(n, 0, "bất biến 2: mỗi top box ≥1 thẻ");
            }
        }

        [Test]
        public void ApplyShuffleKhongTinhLaMotNuocDi()
        {
            var g = Build(Lv);
            g.ApplyShuffle();
            Assert.AreEqual(0, g.Moves, "booster không phải nước đi");
        }

        [Test]
        public void LopTrenDayKinThiTuChoiVaKhongDungVaoBan()
        {
            var g = Build(Lv);
            for (int s = 0; s < g.Stacks.Count; s++)
            {
                Box top = g.TopBox(s);
                for (int i = 0; i < top.Slots.Length; i++)
                    if (top.Slots[i] == null)
                        top.Slots[i] = new Tile { Uid = "f" + s + i, CardId = "f" + s + i, GroupId = "gz" };
            }
            string before = Solver.Encode(g);

            ShuffleResult r = g.ApplyShuffle();

            Assert.IsFalse(r.Ok, "không có ô trống → không dựng nổi nhóm mồi");
            Assert.AreEqual(before, Solver.Encode(g), "từ chối thì bàn phải y nguyên");
        }
```

- [ ] **Step 2: Chạy để xác nhận fail**

Expected: `'Game' does not contain a definition for 'ApplyShuffle'`.

- [ ] **Step 3: Implementation**

```csharp
        public struct ShuffleMove
        {
            public string Uid;
            public SlotRef From, To;
        }

        public struct ShuffleResult
        {
            public bool Ok;
            public ShuffleMove[] Moves;   // view dùng để animate; rỗng khi Ok = false
            public int PrimedGroups;
        }

        /// <summary>
        /// Xáo lại lớp trên cùng theo spec Shuffle.
        ///
        /// KHÔNG đụng danh sách Boxes và KHÔNG đổi Status — gọi Settle() sau như một nước
        /// đi thường. KHÔNG tăng Moves: booster không tính là nước đi.
        ///
        /// Vi phạm bất biến thì khôi phục nguyên trạng và trả Ok = false; bên gọi phải
        /// KHÔNG trừ lượt trong trường hợp đó.
        /// </summary>
        public ShuffleResult ApplyShuffle()
        {
            var fail = new ShuffleResult { Ok = false, Moves = new ShuffleMove[0] };
            if (!CanShuffle()) return fail;

            int topBefore = TopLayerTileCount();
            Dictionary<string, SlotRef> before = SnapshotPositions();
            Game backup = Clone();

            List<string> candidates = PickPrimeCandidates(3);
            int primed = CountPrimedGroups();
            for (int k = 0; k < candidates.Count && primed < 3; k++)
                if (TryPrimeGroup(candidates[k])) primed++;

            ClusterLeftovers();

            if (!ValidateShuffle(topBefore, before))
            {
                RestoreFrom(backup);
                return fail;
            }

            return new ShuffleResult
            {
                Ok = true,
                Moves = DiffPositions(before),
                PrimedGroups = CountPrimedGroups(),
            };
        }

        bool ValidateShuffle(int topBefore, Dictionary<string, SlotRef> before)
        {
            if (TopLayerTileCount() != topBefore) return false;
            if (AnyBoxHasFullGroup()) return false;

            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                if (top == null) return false;
                bool any = false;
                foreach (Tile t in top.Slots) if (t != null) any = true;
                if (!any) return false;
            }

            // Bất biến 4: thẻ có màu ở lớp trên phải còn đúng chỗ cũ. Xét theo trạng thái
            // TRƯỚC khi xáo — sau khi xáo màu đã khác.
            for (int s = 0; s < Stacks.Count; s++)
            {
                Box top = TopBox(s);
                for (int i = 0; i < top.Slots.Length; i++)
                {
                    Tile t = top.Slots[i];
                    if (t == null) continue;
                    SlotRef old;
                    if (!before.TryGetValue(t.Uid, out old)) continue;
                    bool wasColored = old.Box == 0 && !WasWhiteBefore(before, t.Uid);
                    if (wasColored && (old.Stack != s || old.Slot != i)) return false;
                }
            }
            return true;
        }

        // Bảng phụ dựng cùng lúc với SnapshotPositions — tra "trước khi xáo thẻ này có
        // trắng không" mà không phải dựng lại cả bàn cũ.
        HashSet<string> _whiteBefore;

        bool WasWhiteBefore(Dictionary<string, SlotRef> before, string uid)
        {
            return _whiteBefore != null && _whiteBefore.Contains(uid);
        }

        Dictionary<string, SlotRef> SnapshotPositions()
        {
            var map = new Dictionary<string, SlotRef>();
            _whiteBefore = new HashSet<string>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null) continue;
                        map[t.Uid] = new SlotRef { Stack = s, Box = b, Slot = i };
                        if (b == 0 && IsWhite(boxes[b], i)) _whiteBefore.Add(t.Uid);
                    }
            }
            return map;
        }

        ShuffleMove[] DiffPositions(Dictionary<string, SlotRef> before)
        {
            var moves = new List<ShuffleMove>();
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                    for (int i = 0; i < boxes[b].Slots.Length; i++)
                    {
                        Tile t = boxes[b].Slots[i];
                        if (t == null) continue;
                        SlotRef old;
                        if (!before.TryGetValue(t.Uid, out old)) continue;
                        if (old.Stack == s && old.Box == b && old.Slot == i) continue;
                        moves.Add(new ShuffleMove
                        {
                            Uid = t.Uid,
                            From = old,
                            To = new SlotRef { Stack = s, Box = b, Slot = i },
                        });
                    }
            }
            return moves.ToArray();
        }

        // Khôi phục nội dung ô từ bản sao. KHÔNG thay danh sách Boxes — CheckStatus() đọc
        // st.Boxes[0] không kiểm rỗng, đụng vào cấu trúc là rủi ro không cần thiết.
        void RestoreFrom(Game backup)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> mine = Stacks[s].Boxes;
                List<Box> theirs = backup.Stacks[s].Boxes;
                for (int b = 0; b < mine.Count && b < theirs.Count; b++)
                    for (int i = 0; i < mine[b].Slots.Length; i++)
                        mine[b].Slots[i] = theirs[b].Slots[i];
            }
        }
```

- [ ] **Step 4: Chạy lại, xác nhận pass**

Harness. Expected: tất cả `ok`.

- [ ] **Step 5: Hai cổng của repo**

```bash
./selfcheck.sh && ./compilecheck.sh
```

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Domain/GameShuffle.cs Assets/_Game/Board/Tests/BoardShuffleTests.cs
git commit -m "feat(booster): Shuffle orchestration with invariant check and rollback"
```

---

### Task 5: Contracts + BoardController

Sau task này bấm được lệnh và bàn đổi thật, chưa có nút.

**Files:**
- Modify: `Assets/_Game/Contracts/LevelCommands.cs`
- Modify: `Assets/_Game/Contracts/LevelEvents.cs`
- Modify: `Assets/_Game/Board/Views/BoardController.cs`

**Interfaces:**
- Consumes: `Game.ApplyShuffle()`, `Game.CanShuffle()` (Task 4).
- Produces:
  - `LevelCommands.MagnetRequested` đã có; thêm `public static event Action ShuffleRequested` + `public static void RequestShuffle()`
  - `LevelSignals.ShuffleAvailable` (get) + `SetShuffleAvailable(bool)` + `event Action<bool> ShuffleAvailabilityChanged`

- [ ] **Step 1: Thêm lệnh vào Contracts**

`LevelCommands.cs`, ngay dưới khối `MagnetRequested`:

```csharp
        /// <summary>
        /// Booster Shuffle. Cùng lý do với MagnetRequested: assembly WordStack.Board chỉ
        /// tham chiếu WordStack.Contracts nên bàn không nghe Bus.Global được.
        /// </summary>
        public static event Action ShuffleRequested;

        public static void RequestShuffle() => ShuffleRequested?.Invoke();
```

Và trong `ResetStaticState()`, ngay dưới `MagnetRequested = null;`:

```csharp
            ShuffleRequested = null;
```

- [ ] **Step 2: Thêm cờ availability vào LevelSignals**

`LevelEvents.cs`, ngay dưới khối `MagnetAvailabilityChanged`:

```csharp
        /// <summary>Bàn có xáo được không — board đẩy sau mỗi lần settle và lúc nạp màn.</summary>
        public static event Action<bool> ShuffleAvailabilityChanged;

        public static bool ShuffleAvailable { get; private set; }

        public static void SetShuffleAvailable(bool available)
        {
            if (ShuffleAvailable == available) return;
            ShuffleAvailable = available;
            ShuffleAvailabilityChanged?.Invoke(available);
        }
```

Và trong `ResetStaticState()`:

```csharp
            ShuffleAvailabilityChanged = null;
            ShuffleAvailable = false;
```

- [ ] **Step 3: Đăng ký và xử lý trong BoardController**

Trong `Awake()`, dưới dòng `LevelCommands.MagnetRequested += OnMagnetRequested;`:

```csharp
            LevelCommands.ShuffleRequested += OnShuffleRequested;
```

Trong `OnDestroy()`, dưới dòng gỡ Magnet:

```csharp
            LevelCommands.ShuffleRequested -= OnShuffleRequested;
```

Ngay dưới method `OnMagnetRequested()`, thêm:

```csharp
        // Cùng bộ chốt với nam châm: đang cascade / popup meta đang mở / màn đã xong /
        // đang kéo thẻ thì bỏ qua. Không hoàn lượt ở đây — nút chỉ sáng khi
        // LevelSignals.ShuffleAvailable bật, mà cờ đó tắt trong đúng mấy trường hợp này.
        void OnShuffleRequested()
        {
            if (g == null || locked || LevelCommands.InputBlocked) return;
            if (g.Status != GameStatus.Playing) return;
            if (ghost != null) return;

            ShuffleResult r = g.ApplyShuffle();
            if (!r.Ok) return;

            // Task 7 thay hai dòng này bằng StartCoroutine(ShuffleSequence(r)).
            RebuildBoardViews();
            StartCoroutine(Settle());
        }
```

Trong method `RefreshMagnetAvailability()`, đổi thành đẩy cả hai cờ — đổi tên cho đúng việc:

```csharp
        // Quét lại xem còn dùng được booster nào không rồi đẩy sang tầng meta để xám/sáng
        // nút. Chỉ gọi khi bàn đã đứng yên, không gọi mỗi khung hình.
        void RefreshBoosterAvailability()
        {
            bool playing = g != null && g.Status == GameStatus.Playing;
            LevelSignals.SetMagnetAvailable(playing && g.FindMagnetTarget() != null);
            LevelSignals.SetShuffleAvailable(playing && g.CanShuffle());
        }
```

`RefreshMagnetAvailability` hiện chỉ được gọi ở đúng **một** chỗ: trong `Settle()`, ngay dưới `locked = false;`. Đổi dòng đó thành `RefreshBoosterAvailability();` rồi xoá method cũ. Trong `Load()`, dòng `LevelSignals.SetMagnetAvailable(false);` đổi thành:

```csharp
            LevelSignals.SetMagnetAvailable(false);
            LevelSignals.SetShuffleAvailable(false);   // Settle() cuối Load() đặt lại giá trị thật
```

Và ở đầu `Settle()`, dưới dòng `LevelSignals.SetMagnetAvailable(false);`:

```csharp
            LevelSignals.SetShuffleAvailable(false);
```

- [ ] **Step 4: Chạy compilecheck**

```bash
./compilecheck.sh
```
Expected: `game.dll OK` · `editor.dll OK` · `meta.dll OK`

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Contracts/LevelCommands.cs Assets/_Game/Contracts/LevelEvents.cs Assets/_Game/Board/Views/BoardController.cs
git commit -m "feat(booster): wire Shuffle command and availability into the board"
```

---

### Task 6: Đường mua và đường dùng ở tầng meta

**Files:**
- Modify: `Assets/BoosterModule/BoosterId.cs`
- Modify: `Assets/_Game/Currency/Transactions/ItemIds.cs`
- Modify: `Assets/_Game/Currency/UI/TransactionIds.cs`
- Modify: `Assets/_Game/Currency/Services/Impl/TransactionItemDispatcher.cs`
- Modify: `Assets/_Game/MetaSession.cs`
- Modify: `Assets/_Game/AppFlow/Installers/AppFlowInstaller.cs`
- Create: `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs`
- Create: `Assets/_Game/Gameplay/Boosters/Views/ShuffleBoosterButtonView.cs`

**Interfaces:**
- Consumes: `LevelSignals.ShuffleAvailable` + `ShuffleAvailabilityChanged` (Task 5); `LevelCommands.RequestShuffle()` (Task 5).
- Produces: `ShuffleBoosterViewModel.IsUsable` (`ReadOnlyReactiveProperty<bool>`), `ShuffleBoosterViewModel.OnButtonClicked()`.

- [ ] **Step 1: Đăng ký id**

`BoosterId.cs`, dưới `Magnet = 5,`:

```csharp
        Shuffle = 6,
```

`ItemIds.cs`, dưới `BoosterMagnet`:

```csharp
        public const string BoosterShuffle = "booster.shuffle";
```

`TransactionIds.cs`, dưới `BoosterMagnet`:

```csharp
        public const string BoosterShuffle = "t_booster_shuffle";
```

Trong `ForBooster`, dưới nhánh Magnet:

```csharp
            BoosterId.Shuffle => BoosterShuffle,
```

Trong `TryGetBoosterId`, dưới nhánh Magnet:

```csharp
                case BoosterShuffle:  boosterId = BoosterId.Shuffle;  return true;
```

`TransactionItemDispatcher.cs`, dưới case Magnet:

```csharp
                case ItemIds.BoosterShuffle:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Shuffle, amount));
                    break;
```

- [ ] **Step 2: Bắc cầu trong MetaSession**

Trong `OnBoosterActivated`, thay thân thành:

```csharp
            if (evt.Id == BoosterId.Magnet) LevelCommands.RequestMagnet();
            else if (evt.Id == BoosterId.Shuffle) LevelCommands.RequestShuffle();
```

- [ ] **Step 3: ViewModel**

Tạo `Assets/_Game/Gameplay/Boosters/ViewModels/ShuffleBoosterViewModel.cs`:

```csharp
using BoosterModule;
using R3;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>
    /// Booster Shuffle — dùng ngay, không chọn mục tiêu.
    ///
    /// KHÔNG kế thừa InstantBoosterViewModelBase: lớp đó cố ý chưa trừ lượt vì hiệu ứng
    /// của Hand/Hammer/AddQueue/AddBelt chưa nối vào bàn. Shuffle có luật thật nên trừ
    /// lượt bằng RequestUse(), và đó là mắt xích khởi động cả chuỗi:
    ///
    ///   RequestUse() → BoosterManager trừ 1 + bắn BoosterActivatedEvent
    ///     → MetaSession bắc cầu → LevelCommands.RequestShuffle()
    ///     → BoardController.ApplyShuffle() + Settle()
    /// </summary>
    public sealed class ShuffleBoosterViewModel : BoosterViewModelBase
    {
        private readonly ReactiveProperty<bool> _isUsable;

        public ShuffleBoosterViewModel() : base(BoosterId.Shuffle)
        {
            _isUsable = new ReactiveProperty<bool>(LevelSignals.ShuffleAvailable);
            LevelSignals.ShuffleAvailabilityChanged += OnAvailabilityChanged;
        }

        /// <summary>
        /// Lớp trên còn ô trống không. Hết ô trống thì không dựng nổi Nhóm mồi, mà lượt
        /// này người chơi mua bằng coin — để bấm hụt rồi mất lượt là mất tiền thật.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsUsable => _isUsable;

        public void OnButtonClicked()
        {
            if (!HasStock) return;
            if (!_isUsable.Value) return;

            RequestUse();
        }

        public override void Dispose()
        {
            LevelSignals.ShuffleAvailabilityChanged -= OnAvailabilityChanged;
            _isUsable.Dispose();
            base.Dispose();
        }

        private void OnAvailabilityChanged(bool available) => _isUsable.Value = available;
    }
}
```

- [ ] **Step 4: ButtonView**

Tạo `Assets/_Game/Gameplay/Boosters/Views/ShuffleBoosterButtonView.cs`:

```csharp
using BoosterModule;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.Gameplay.Boosters.ViewModels;
using LogosSDK.Core.Events;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>Nút Shuffle. Xám khi bàn không xáo được, tránh bấm hụt mất lượt đã mua.</summary>
    public class ShuffleBoosterButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private Image _countBoxBg;
        [SerializeField] private Sprite _addBgSprite;
        [SerializeField] private Sprite _usesBgSprite;

        [Inject] private ShuffleBoosterViewModel _viewModel;

        private DisposableBag _disposables;

        private void Start()
        {
            if (_viewModel == null) return;

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            _viewModel.Count.Subscribe(OnCountChanged).AddTo(ref _disposables);
            _viewModel.IsUsable.Subscribe(_ => UpdateInteractable()).AddTo(ref _disposables);

            OnCountChanged(_viewModel.Count.CurrentValue);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveAllListeners();
            _disposables.Dispose();
        }

        private void OnButtonClicked()
        {
            if (_viewModel == null) return;

            if (_viewModel.Count.CurrentValue <= 0)
            {
                Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(BoosterId.Shuffle)));
                return;
            }

            _viewModel.OnButtonClicked();
        }

        private void OnCountChanged(int count)
        {
            if (_countLabel != null)
                _countLabel.text = count > 0 ? count.ToString() : "+";
            if (_countBoxBg != null)
                _countBoxBg.sprite = count > 0 ? _usesBgSprite : _addBgSprite;
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            if (_button == null || _viewModel == null) return;

            // Hết lượt vẫn bấm được — đó là đường vào popup mua. Chỉ xám khi CÒN lượt mà
            // bàn không xáo được, vì bấm lúc đó là mất lượt vô ích.
            bool outOfStock = _viewModel.Count.CurrentValue <= 0;
            _button.interactable = outOfStock || _viewModel.IsUsable.CurrentValue;
        }
    }
}
```

- [ ] **Step 5: Đăng ký DI**

`AppFlowInstaller.cs`, trong mảng type, dưới `typeof(MagnetBoosterViewModel),`:

```csharp
                         typeof(ShuffleBoosterViewModel),
```

- [ ] **Step 6: Chạy compilecheck**

```bash
./compilecheck.sh
```
Expected: `game.dll OK` · `editor.dll OK` · `meta.dll OK`

- [ ] **Step 7: Commit**

```bash
git add Assets/BoosterModule/BoosterId.cs Assets/_Game/Currency Assets/_Game/MetaSession.cs Assets/_Game/AppFlow/Installers/AppFlowInstaller.cs Assets/_Game/Gameplay/Boosters
git commit -m "feat(booster): Shuffle meta wiring - ids, view model, button, DI"
```

---

### Task 7: Chuỗi diễn + chặn input

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs`

**Interfaces:**
- Consumes: `ShuffleResult` (Task 4), `RebuildBoardViews()` + `Settle()` (đã có từ Magnet).
- Produces: không có gì cho task sau.

- [ ] **Step 1: Thêm field nhịp**

Dưới dòng `[SerializeField] float magnetAnimDur = 0.6f;`:

```csharp
        [SerializeField] float shuffleAnimDur = 0.5f;   // PLACEHOLDER: nhịp cho animation shuffle
```

- [ ] **Step 2: Đổi `OnShuffleRequested` sang gọi coroutine**

Trong `OnShuffleRequested`, thay hai dòng `RebuildBoardViews(); StartCoroutine(Settle());` bằng:

```csharp
            StartCoroutine(ShuffleSequence(r));
```

- [ ] **Step 3: Thêm coroutine**

Ngay dưới `MagnetAnimation`:

```csharp
        // Khoá HAI vế suốt lúc diễn, thiếu vế nào cũng lọt input:
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

            // Bấm ngay khi vào màn thì phase còn Ready, mà NotifyPlayerActionCommittedAsync
            // đòi phase == Playing — không đẩy Ready sang Playing trước là MoveCommitted
            // lẫn EvaluationCompleted đều bị ViewModel nuốt, overlay không lên và progress
            // bar đứng im.
            if (!firstInteractionRaised)
            {
                firstInteractionRaised = true;
                LevelSignals.RaiseFirstInteraction();
            }

            LevelSignals.RaiseMoveCommitted(g.Moves);

            yield return ShuffleAnimation(r);

            RebuildBoardViews();
            yield return Settle();
        }

        // PLACEHOLDER — chỗ DUY NHẤT cần thay khi làm animation thật.
        //
        // Chuỗi thật cần diễn:
        //   1. Với mỗi ShuffleMove có From.Box == 0 && To.Box == 0: thẻ bay từ ô cũ sang
        //      ô mới trong lớp trên, lệch pha nhau như MergeTiles đang làm.
        //   2. Với move có From.Box > 0 hoặc To.Box > 0 (đổi với donor bị chôn): StackView
        //      chỉ vẽ peek layer trừu tượng nên không có tile view thật — dựng tile tạm ở
        //      vị trí peek, lật mặt, bay, rồi huỷ.
        //   3. Thẻ có màu KHÔNG được động vào: chúng không xuất hiện trong r.Moves.
        //
        // Hiện chỉ giữ đúng NHỊP thời gian. Bỏ trắng thì overlay chớp một khung hình rồi
        // tắt — không kiểm được phần chặn input có thật sự chạy hay không.
        IEnumerator ShuffleAnimation(ShuffleResult r)
        {
            yield return new WaitForSeconds(shuffleAnimDur);
        }
```

- [ ] **Step 4: Chạy hai cổng**

```bash
./compilecheck.sh && ./selfcheck.sh
```
Expected: ba dòng `.dll OK` và `SelfCheck OK - 7 level`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs
git commit -m "feat(booster): Shuffle animation placeholder with full input blocking"
```

---

## Việc trong Editor (ngoài phạm vi các task trên)

1. **Nút HUD** — nhân bản một nút booster, thay component thành `ShuffleBoosterButtonView`, gán `_button`, `_countLabel`, `_countBoxBg`, `_addBgSprite`, `_usesBgSprite`.
2. **Cấp lượt để test** — thêm `BoosterEntry { Id = Shuffle, DisplayName = "Shuffle" }` vào `Assets/_Game/Content/SO_CheatSettings.asset`. Không thêm thì cheat panel không có dòng nào cho Shuffle.
3. **Mua bằng coin** — entry `t_booster_shuffle` trong `SO_TransactionCatalog.asset`, items `booster.shuffle`.
4. **Chạy NUnit** — Window ▸ General ▸ Test Runner ▸ EditMode ▸ `BoardShuffleTests`.
5. **File `.meta`** — Unity sinh khi import; commit thêm một nhát sau lần mở đầu tiên.
