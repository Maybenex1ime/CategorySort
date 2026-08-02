# Nhật ký thiết kế — chuyển repo sang WordStack

> Ghi lại phiên làm việc ngày **2026-07-31**: đọc `GDD_WordStack.md`, lên kế hoạch đổi luật
> của repo, chốt định dạng data level qua 5 vòng sửa, và dựng demo HTML.
>
> Đây là bản **cô đọng có tổ chức** của cuộc hội thoại — giữ mọi yêu cầu, mọi quyết định và
> lý do đằng sau, bỏ phần diễn giải lặp. Cái mà code không ghi lại được là chữ *tại sao*,
> nên phần đó được giữ đầy đủ.

---

## 1. Bối cảnh

Repo đang ở **Giai đoạn 1b** của pipeline CCGS với prototype **Category Sort** (chồng thẻ +
cột collector + quota + move budget) trong `Assets/Prototype/`. Người dùng đưa vào
`GDD_WordStack.md` — một game **khác hẳn**: hộp chứa 4 slot xếp chồng, gom đủ 4 từ cùng chủ
đề vào một hộp, hộp biến mất hoặc gộp thành ô chủ đề mới.

Hai điểm lệch phải chốt trước khi lên kế hoạch:

| Lệch | Chốt |
|---|---|
| GDD §11 chỉ định Vite + React + TS + Tailwind, repo là Unity 6 C# | **Unity C#, giữ repo.** §11 chỉ là tham chiếu logic, không phải ràng buộc file layout |
| WordStack là hướng mới hay prototype thứ 2? | **Thay thế Category Sort**, viết lại cả `game-concept.md` và `development-plan.md` |

Kế hoạch do model **Fable** soạn, thực thi bằng **Opus** — theo yêu cầu của người dùng.

---

## 2. Timeline quyết định

### [1] Kế hoạch chuyển đổi (Fable)

Nguyên tắc: **giữ đúng 3 file** `Assets/Prototype/*.cs`, rewrite nội dung. Domain thuần C#
không import UnityEngine, view 1 MonoBehaviour tự bootstrap, test = `SelfCheck` throw — đúng
pattern đang chạy. Không thêm file `.cs`, không package mới, không interface/factory/event bus.

Delta luật: bỏ `Knobs`(budget/collector/tray), `Cat`, `Art`, `Entry`, `Collect`, `Spend`,
`LoseReason`; viết lại `MoveToSlot`→`MoveTile`, `IsStuck`, `AllCollectorsDone`; giữ nguyên
pattern `Clone()` và khung `Solve/Dfs/Encode`.

Phase: **P-doc** (pivot tài liệu) → **P0** M0 (model + level + validate + render tĩnh) →
**P1** M1 (MoveTile + kéo thả) → **P2** M2 (CLEAR + xoá box + reveal + won/stuck) →
**P3** M3 (COLLAPSE + cascade) → **P4** M4 (màu + animation + win) → **P5** M5 (Undo /
Restart / stuck toast / Level 3). Mỗi dòng trong checklist §13 của GDD được gán phase phụ
trách + assert chứng minh.

### [2] Data level tổ chức thế nào?

Giải thích hai tầng: **tầng khai báo** (chỉ chữ) tách khỏi **tầng runtime** (object có
uid/groupId) — đúng chỗ GDD tách §6.2 khỏi §6.1. Bàn chơi chỉ chứa label; `groups[].words`
là nguồn duy nhất của quan hệ thành viên. Suy ra lúc load: `Tile.GroupId`, `Tile.Id`,
`Tile.IsTopic`, `Box.IsBottom`.

### [3] Yêu cầu: JSON + dùng ảnh, không chỉ chữ

Kiểm `Packages/manifest.json`: **Newtonsoft không có**, chỉ có `com.unity.modules.jsonserialize`.

Quyết định parser — 3 lựa chọn:

| | Đánh đổi |
|---|---|
| `JsonUtility` | 0 dep, nhưng domain hoá Unity-only → **mất console SelfCheck** (`active.md` §Ghi chú kỹ thuật) và vi phạm `development-plan.md:122`; còn không đọc được `null` trong mảng |
| Package Newtonsoft | Đúng chuẩn, nhưng +1 dependency cho 3 file level |
| **Mini reader thuần C#** (chọn) | ~60–70 dòng cho subset object/array/string/number/null + skip `//`. Domain vẫn thuần → console SelfCheck sống |

**Đảo quyết định A của Fable** (đang là "C# literal, không JSON").

File để: `Assets/Prototype/Resources/Levels/*.json` + `Resources/Art/*.png`. Loader nhận
`string json` — Unity đưa vào bằng `Resources.Load<TextAsset>`, console runner bằng
`File.ReadAllText`. Domain không biết host nào.

Ảnh: lúc này còn thiết kế như **tầng trình bày** — map `art` keyed-by-label. *(Bị bác ở bước [5].)*

### [4] Giải thích "group" vs "stack"

Hai trục vuông góc:

- `groups` = **trục ngữ nghĩa** — luật thắng. Không có vị trí, không vẽ ra màn hình.
- `stacks` = **trục không gian** — bàn chơi, chuỗi lồng `stack → box → slot → tile`.

Chạm nhau đúng một chỗ: chuỗi id trong `slots`. Level cố tình rải 4 thành viên của một group
ra nhiều stack — `groups` định nghĩa *đích*, `stacks` định nghĩa *chướng ngại*.

Hai loại "tầng" đừng lẫn: tầng **vật lý** (box chồng box, mở ra bằng CLEAR) và tầng **ngữ
nghĩa** (nhóm thuộc nhóm, mở ra bằng COLLAPSE).

Con số **4** xuất hiện hai lần (4 thành viên / 4 slot) là **trùng hợp tiện lợi, không phải
ràng buộc** — điều kiện hoàn thành đếm "đủ 4 thành viên", không so với sức chứa box.

### [5] Yêu cầu: text và ảnh **ngang vai**

Điểm gãy: map `art` keyed-by-label biến chữ thành **định danh**, ảnh thành **trang trí** — không
ngang nhau được. Ô chỉ có ảnh thì không có gì để tra.

Đổi khoá: mỗi thành viên có **`id` slug** riêng; `text` và `art` đều là *cách thể hiện*, cả hai
tuỳ chọn, **bắt buộc có ≥1**. Kèm lợi ích: `id` độc lập ngôn ngữ → đổi `text` sang tiếng nào
cũng không phải sửa một dòng nào trong layout.

Quan hệ cha–con chuyển từ so chuỗi `G.name ∈ G'.words` sang cạnh tường minh trên id — **sạch
hơn bản gốc**, vì bản gốc đổi một chữ hiển thị là đứt cạnh COLLAPSE trong im lặng.

Hệ quả gameplay: mở ra **gom nhóm xuyên phương thức** — người chơi phải nhận ra chữ "Doctor"
và *ảnh* xe cứu thương cùng thuộc Hospital. Đúng tinh thần §1, mạnh hơn bản chỉ-chữ.

Kèm 2 đảo chiều: ảnh thiếu file thành **lỗi cứng** (ô ảnh-trần không có chữ để rơi về), và
`art` map cấp level biến mất.

### [6] Yêu cầu: gộp thành hai phần lớn **Layout** và **Meaning**

`meaning` có hai bảng **cùng một shape** `{ id, group?, text?, art? }`:

| | `cards` | `groups` |
|---|---|---|
| Có mặt trong `layout` | luôn, đúng 1 lần | không bao giờ |
| Sinh ra lúc nào | nạp sẵn từ level | chỉ khi COLLAPSE |

Tách hai bảng để luật "id nào được phép nằm trên bàn" thành **cấu trúc**, không phải một dòng
validate dễ quên.

`{"ref": ...}` của bước [5] biến mất — nhóm cũng là thực thể có field `group` như thẻ:

```
hospital.group = "buildings"   → hospital là thành viên của buildings → COLLAPSE
buildings (không có "group")   → không thuộc nhóm nào                 → CLEAR
```

Một khoá `group` duy nhất diễn tả cả "thẻ thuộc nhóm" lẫn "nhóm thuộc nhóm" — đúng tinh thần
đệ quy §1. Đổi tên `tiles` → `slots` cho khớp chuỗi thuật ngữ §3.

### [7] Yêu cầu: thêm vị trí cho mỗi hộp

Nêu ngược: box trong cùng stack **luôn chung vị trí** — đó là định nghĩa "xếp chồng" §3, và §R5
nói cả stack trượt lên khi box top bị xoá. `pos` per-box sẽ là dữ liệu thừa hoặc phá khái niệm
stack. → Đặt `pos` ở **stack**.

Hệ toạ độ: `[x, y]` **số thực**, đơn vị = 1 bước hộp, gốc góc trên-trái, `y` xuống dưới (cùng
chiều đọc với thứ tự `slots`). Số thực để căn nửa bước mà không phải bịa lưới mịn hơn. Không
cần khai `grid.cols/rows` — camera fit từ bao lồi các `pos`; bỏ luôn hằng `GridCols`/`GridRows`
đang hardcode trong `PrototypeView.cs`.

**Ảnh hưởng tới domain: bằng không.** `pos` là dữ liệu câm như `art`.

### [8] "Hình như bạn đang hiểu sai luật?"

Soát lại toàn bộ §3–§7. **Tìm ra hai chỗ sai thật** — xem Mục 6.

### [9] Yêu cầu: bỏ COLLAPSE, bỏ Undo, làm demo HTML

Xem Mục 5.

---

## 3. Schema level — bản chốt

```jsonc
{
  "id": "lv-002",
  "title": "City",

  // LAYOUT — thẻ nào nằm ở đâu. Không nói gì về ý nghĩa.
  "layout": {
    "stacks": [
      { "pos": [0.5, 0], "boxes": [
          { "slots": ["doctor",  "airplane",   "backpack",  null] },
          { "slots": ["nurse",   "ticket",     "theater",   "student"] }
      ]},
      { "pos": [0, 1], "boxes": [
          { "slots": ["patient", "teacher",    null,        null] },
          { "slots": ["pilot",   "blackboard", "passenger", "ambulance"] }
      ]},
      { "pos": [1, 1], "boxes": [
          { "slots": [null, null, null, null] }
      ]}
    ]
  },

  // MEANING — thẻ thuộc nhóm nào, và hiện ra bằng gì. Không nói gì về vị trí.
  "meaning": {
    "groups": [
      { "id": "hospital",  "group": "buildings", "text": "Hospital" },
      { "id": "school",    "group": "buildings", "text": "School" },
      { "id": "airport",   "group": "buildings", "text": "Airport" },
      { "id": "buildings",                       "text": "Buildings" }
    ],
    "cards": [
      { "id": "doctor",    "group": "hospital",  "text": "Doctor", "art": "doctor" },
      { "id": "nurse",     "group": "hospital",  "text": "Nurse" },
      { "id": "ambulance", "group": "hospital",                    "art": "ambulance" },
      { "id": "patient",   "group": "hospital",  "text": "Patient","art": "patient" },
      // ...
      { "id": "theater",   "group": "buildings", "text": "Theater","art": "theater" }
    ]
  }
}
```

### Ánh xạ sang luật GDD

| Luật GDD | Trong schema này |
|---|---|
| `groupId` suy từ label (§6.2 quy ước 2) | Cấu trúc: field `group` trên entry. Không tra chuỗi |
| CLEAR vs COLLAPSE (`G.name ∈ G'.words`) | COLLAPSE ⟺ group có field `group` |
| Tên nhóm không đặt sẵn trên bàn (quy ước 3) | `slots` chỉ được chứa id thuộc `cards` |
| Topic tile nhãn `G.name` (R4) | Topic tile lấy `text`/`art` của chính group |
| Mỗi group đúng 4 từ (§3) | Đếm entry có `group == X` bằng 4 |

### Validate

| Luật | Vi phạm ví dụ |
|---|---|
| mỗi entry có ≥1 trong `text`/`art` | `{"id":"nurse","group":"hospital"}` trơn |
| `id` không trùng trên toàn `cards` ∪ `groups` | có card `theater` và group `theater` |
| mọi `group` giải được về 1 id trong `meaning.groups` | `"group": "hosptial"` |
| mỗi nhóm đúng 4 thành viên | `hospital` chỉ 3 thẻ |
| `slots` chỉ chứa id thuộc `cards`, hoặc `null` | đặt sẵn `hospital` lên bàn |
| mỗi card xuất hiện đúng 1 lần trong `layout` | thiếu, hoặc trùng |
| không cycle theo chuỗi `group` | `a.group=b`, `b.group=a` → cascade vô hạn |
| mỗi box đúng 4 `slots` | box khai 3 ô |
| box rỗng phải là box đáy | box rỗng ở giữa → box dưới không bao giờ với tới |
| mọi stack có `pos`, không hai stack trùng `pos` | hai hộp chồng lên nhau |
| mọi `art` trỏ file có trong `Resources/Art` | thiếu file → lỗi cứng, không fallback |
| ≥1 thẻ ảnh-trần **và** ≥1 thẻ chữ-trần mỗi level | giữ "ngang vai" là thuộc tính level |

> **Nhiều nhóm gốc là hợp lệ** — xem Mục 6(a).

### Ánh xạ sang domain

```
meaning.cards[].group     →  Tile.GroupId
meaning.cards[].text/art  →  Tile.Text / Tile.Art     (≥1 non-null, bất biến)
meaning.groups[].group    →  Group.ParentId           (null = nhóm gốc = CLEAR)
meaning.groups[].text/art →  Group.Text / Group.Art   (dùng cho topic tile)
layout...slots[]          →  Stack/Box/Tile.Uid       (Uid cấp lúc load, cho animation)
layout.stacks[].pos       →  Stack.Pos                (dữ liệu câm, domain không đọc)
```

`Box.IsBottom` tự gán box cuối mỗi stack. `BoxCapacity` là hằng, không nằm trong file.

---

## 4. Ngôn ngữ

**In-game text tiếng Anh**: nhãn thẻ, tên nhóm, HUD (`Moves`, `3/7 groups`, `No moves left`,
`Complete!`, `Undo`/`Restart`). Comment code + doc trong repo **giữ tiếng Việt** (dev-facing,
không phải "text trong game").

Vì `id` là slug độc lập ngôn ngữ nên đổi ngôn ngữ hiển thị không đụng tới `layout`.

---

## 5. Demo HTML (đã ship)

`demo/wordstack-clear-demo.html` — 1 file self-contained, không CDN. `demo/check.mjs` — kiểm luật.

**Scope theo yêu cầu:** chỉ **CLEAR**, **không COLLAPSE**, **không Undo**, không Hint, không âm thanh.

Có: kéo thả Pointer Events (chuột + ngón tay) · thả vào slot trống đầu tiên · từ chối box đầy
(rung) · CLEAR đủ 4 thẻ cùng nhóm · xoá box rỗng → lộ box dưới · box đáy ở lại rỗng · cascade
từng nhịp 350ms có khoá input · màu gợi ý cục bộ theo box (§R2) · phát hiện kẹt + toast · win
overlay · 2 level · panel xem JSON level đang chơi.

Validate **chặn** group có field `group` (COLLAPSE chưa hỗ trợ). `art` trỏ vào bảng emoji nội
bộ thay cho `Resources/Art/*.png` — schema y hệt, chỉ khác chỗ resolve.

### `REMOVE_EMPTY_NONBOTTOM_BOX = true`

Bỏ Undo rồi thì đọc luật chặt (chỉ CLEAR mới xoá hộp) sẽ khiến người chơi tự dọn rỗng một hộp
là **khoá chết** hộp bên dưới vĩnh viễn, không gỡ được ngoài Restart. Demo cho hộp rỗng biến mất
bất kể rỗng vì đâu.

> **Cập nhật 2026-08-02:** câu trước đây ở đây — *"Level 1 và 2 được thiết kế dựa trên luật này,
> đổi thành `false` thì cả hai đều không giải được"* — đã **hết đúng**. Đó chính là triệu chứng của
> lỗi level-1-không-chơi-được (xem Mục 6 bên dưới). Hai level đã thiết kế lại theo nguyên tắc *mọi
> hộp ẩn phải mở được bằng một CLEAR dùng thẻ đang với tới được*, nên giờ **giải được ở cả hai chế
> độ**, và `check.mjs` fail nếu chế độ chặt không giải được. Cờ này giờ là lựa chọn tự do, không bị
> data ép.

### Kiểm chứng

`check.mjs` **trích engine thẳng từ file HTML** nên test đúng code đang chạy, không phải bản copy.

```
lv-001 — giải được trong 11 nước (86038 nút, 3692ms)
lv-002 — giải được trong 13 nước (243609 nút, 13183ms)
OK — mọi check pass
```

Phủ: 10 loại level hỏng phải bị validate chặn · luật nước đi (thả về chỗ cũ, slot trống đầu
tiên, box bị che, box đầy + state không đổi) · CLEAR + xoá box + lộ box dưới · CLEAR ở box đáy ·
màu gợi ý 3 case · thắng/kẹt · beam search chứng minh cả 2 level giải được.

Chưa kiểm bằng máy: thao tác kéo thả thật và animation (phải bấm tay).

---

## 6. Hai chỗ đã hiểu sai và đã sửa

### (a) "Đúng 1 nhóm gốc" — **sai**

Từng đưa vào danh sách validate. Level 1 của GDD (§10.2) có **ba** nhóm gốc: Fruit, Planet,
Hospital — cả ba đều CLEAR, không có COLLAPSE nào. Luật đó đánh trượt chính level tutorial.

→ **Nhiều nhóm gốc là hợp lệ.** Điều kiện thật chỉ là đồ thị `group` **không có cycle** — không
cycle thì mọi chuỗi đều kết thúc ở một nhóm CLEAR, bàn dọn được.

### (b) Xoá box khi người chơi tự kéo hết thẻ ra — **sai ngữ cảnh**

Từng chọn mặc định "xoá mọi top box rỗng không-đáy, rỗng vì đâu cũng được", dựa vào câu tổng
quát ở §R4. Đọc lại: câu đó nằm dưới tiêu đề *"Quy tắc tổng quát để không phụ thuộc
`BOX_CAPACITY`"* — nó đang **siết** điều kiện xoá sau CLEAR (với box 16 slot, CLEAR 4 thẻ không
làm box rỗng nên không được xoá), chứ không mở rộng sang trường hợp rỗng do kéo tay. Pseudocode
§7 cũng chỉ xoá trong nhánh CLEAR.

→ Đọc chặt: **box chỉ bị xoá khi một CLEAR làm nó rỗng.**

> Nhưng khi bỏ Undo (bước [9]) thì luật chặt tạo ra soft-lock không gỡ được → demo dùng luật
> rộng, có cờ bật tắt. Quyết định cho bản Unity **chưa chốt** — phụ thuộc Undo có quay lại không.

---

## 7. Câu hỏi mở / mâu thuẫn trong GDD

Giả định mặc định đề nghị, để không chặn tiến độ:

| # | Vấn đề | Giả định |
|---|---|---|
| Q1 | §7 `hasAnyLegalProgress` ("còn slot trống → còn nước đi") **mâu thuẫn** §R7/E8 ("mọi top box đầy VÀ không nhóm nào hoàn thành được") | Hai định nghĩa chỉ khác nhau khi board **chưa ổn định**. Dùng định nghĩa §R7/E8, implement bằng check §7, gọi **sau khi cascade ổn định** → một điều kiện thoả cả hai. Cả hai đều KHÔNG bắt soft-lock — đúng chủ đích "không có màn Thua" |
| Q2 | Xoá box khi rỗng do kéo tay | Xem Mục 6(b) — chưa chốt |
| Q3 | Cascade vòng (A.name ∈ B.words và ngược lại) — GDD không nói | Validate reject cycle |
| Q4 | R2 lời văn ("group đạt count ≥ 2 đầu tiên") vs code §7.1 ("xuất hiện đầu tiên") | Theo code §7.1 — thuần cosmetic |
| Q5 | `boxCapacity` per-level (§6.2) vs group luôn 4 từ | Hằng `BoxCapacity = 4`, bỏ per-level. Điều kiện hoàn thành đếm thành viên, không so capacity → đổi 16 không sửa luật |
| Q6 | §10.2 Level 1: GDD tự nhận layout đầu "sai — sẽ deadlock", không cho bố trí tile cụ thể | Tự thiết kế, solver trong SelfCheck là lưới an toàn |
| Q7 | §10.4 Level 3 chỉ phác thảo | Tự thiết kế ở P5, solver verify |
| Q8 | Hint (M5) — §14.3 chưa chốt cơ chế | Cắt khỏi prototype |
| Q9 | `globalGroupColors` easy-mode (R2) | Bỏ — YAGNI |
| Q10 | E10 hyphenation — TextMesh không có `hyphens:auto` | Auto-shrink font theo độ dài + ngắt theo khoảng trắng |
| Q11 | Tiến độ "3/7 nhóm" (§9.2) — GDD không định nghĩa "nhóm đã giải" | `SolvedGroups++` mỗi lần CLEAR hoặc COLLAPSE |
| Q12 | `movesUsed` có trong state nhưng không luật nào dùng (§14.2 chưa chốt điểm) | Giữ counter, chỉ hiển thị |
| Q13 | Topic tile có nên dùng ảnh không | §R4 cố ý cho topic tile nền trắng như ô thường (không lộ diện). Nếu icon nhóm khác hẳn dòng icon item thì nó tự khoe mình → **đề nghị group render bằng chữ** kể cả khi member dùng ảnh |
| Q14 | Ảnh nhận diện nhanh hơn chữ → level trộn dễ hơn level chữ thuần | Cân lại độ khó level 1–3 sau khi có art thật |

---

## 8. Trạng thái & việc tiếp theo

**Đã xong:** demo HTML + check (commit `827ba9f`).

**Chưa chạy:** toàn bộ plan cho Unity — `Assets/` và các doc chưa bị đụng gì.

1. **P-doc** — copy GDD vào `design/gdd/`; viết lại `design/gdd/game-concept.md` (Rev 4) gồm mục
   "Sai lệch so với GDD §6.2"; viết lại `docs/development-plan.md`; cập nhật
   `production/session-state/active.md`.
2. **P0** — model + mini JSON reader + validate + `Resources/Levels/*.json` + render tĩnh.
3. **P1 → P5** theo bảng phase ở Mục 2[1].

**Cần người dùng chuẩn bị:** art PNG. Chưa có thì level chạy toàn chữ được, nhưng assert
"≥1 thẻ ảnh-trần" sẽ đỏ — chọn bật assert từ P0 hay hoãn sang P4.

**Cần chốt:** Undo có quay lại bản Unity không (kéo theo quyết định Q2).
