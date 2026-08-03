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

> Cập nhật 2026-08-02 (bước [14]): bảng `meaning.cards` phẳng đã **gộp vào**
> `meaning.groups[].cards`. Mục này là bản hiện hành.

```jsonc
{
  "id": "lv-001",
  "title": "Warm-up",
  "note": "Free text cho người viết level. Engine bỏ qua.",

  // LAYOUT — thẻ nào nằm ở đâu. Không nói gì về ý nghĩa.
  "layout": {
    "stacks": [
      { "pos": [0,0], "boxes": [
          { "slots": ["apple","banana",null,null] },
          { "slots": ["rabbit","bear","airplane","bicycle"] }
      ]},
      { "pos": [1,0], "boxes": [
          { "slots": ["grape","orange","dog","cat"] }
      ]},
      { "pos": [0,1], "boxes": [ { "slots": [null,null,null,null] } ]}
    ]
  },

  // MEANING — thẻ thuộc nhóm nào, và hiện ra bằng gì. Không nói gì về vị trí.
  "meaning": {
    "groups": [
      { "id":"fruit", "text":"Fruit", "cards": [
          { "id":"apple",  "text":"Apple",  "art":"apple"  },
          { "id":"banana",                  "art":"banana" },   // chỉ ảnh
          { "id":"grape",  "text":"Grape"                  },   // chỉ chữ
          { "id":"orange", "text":"Orange", "art":"orange" }
      ]}
      // ... mỗi nhóm đúng 4 thẻ
    ]
  }
}
```

Quan hệ nhóm-cha (COLLAPSE, ngoài phạm vi hiện tại) là field `"group"` trên **chính entry
nhóm** — không nhét nhóm con vào mảng `cards`, để mảng đó luôn thuần một kiểu.

### Ánh xạ sang luật GDD

| Luật GDD | Trong schema này |
|---|---|
| `groupId` suy từ label (§6.2 quy ước 2) | Cấu trúc: card nằm lồng trong group của nó |
| CLEAR vs COLLAPSE (`G.name ∈ G'.words`) | COLLAPSE ⟺ group có field `group` |
| Tên nhóm không đặt sẵn trên bàn (quy ước 3) | `slots` chỉ được chứa card id |
| Topic tile nhãn `G.name` (R4) | Topic tile lấy `text`/`art` của chính group |
| Mỗi group đúng 4 từ (§3) | `cards` đúng 4 phần tử |

### Validate

| Luật | Vi phạm ví dụ |
|---|---|
| mỗi entry có ≥1 trong `text`/`art` | `{"id":"grape"}` trơn |
| `id` không trùng trên toàn bộ card ∪ group | có card `fruit` và group `fruit` |
| mỗi nhóm đúng 4 thẻ | `fruit` chỉ 3 thẻ |
| **mỗi art key thuộc đúng một thẻ hoặc nhóm** | hai thẻ cùng `"art":"apple"` → kéo nhầm asset |
| `slots` chỉ chứa card id, hoặc `null` | đặt sẵn `fruit` (một group) lên bàn |
| mỗi card xuất hiện đúng 1 lần trong `layout` | thiếu, hoặc trùng |
| mỗi box đúng 4 `slots` | box khai 3 ô |
| box rỗng phải là box đáy | box rỗng ở giữa → box dưới không bao giờ với tới |
| mọi stack có `pos`, không hai stack trùng `pos` | hai hộp chồng lên nhau |
| mọi `art` trỏ file có trong `Resources/Art` | thiếu file → lỗi cứng, không fallback |
| ≥1 thẻ ảnh-trần **và** ≥1 thẻ chữ-trần mỗi level | giữ "ngang vai" là thuộc tính level |

> **Nhiều nhóm gốc là hợp lệ** — xem Mục 6(a).

Hai luật **biến mất** khi gộp cards vào groups, vì cấu trúc đã loại trừ: *card trỏ group không
tồn tại* và *một card thuộc hai group*. Luật **cycle theo chuỗi `group`** tạm chưa cần vì
`ParentId` đang bị chặn hẳn — thêm lại khi COLLAPSE vào.

### Ánh xạ sang domain

```
groups[] chứa card nào       →  Tile.GroupId
groups[].cards[].text/art    →  Tile.Text / Tile.Art     (≥1 non-null, bất biến)
meaning.groups[].group       →  Group.ParentId           (null = nhóm gốc = CLEAR)
meaning.groups[].text/art    →  Group.Text / Group.Art   (dùng cho topic tile)
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

## 8. Timeline, phần 2 — port sang Unity

### [10] Kế hoạch port (Fable) + P-doc/P1a/P1b — commit `9328f1e`

Fable soạn plan; Opus thực thi. Giữ đúng 3 file `.cs`, không thêm file code, không thêm package.

Ràng buộc cứng: `PrototypeDomain.cs` **không import UnityEngine**, để `SelfCheck` chạy được
ngoài Editor. Hai hệ quả thiết kế trực tiếp từ đó:

- `BoxColorIndices` trả **index** palette, không trả `Color` — màu hex sống ở view.
- `Validate` nhận `Predicate<string> hasArt` do host cấp, thay vì tự gọi `Resources.Load`.
  Unity truyền `Resources.Load`, console truyền `File.Exists`.

Parser: **mini JSON reader thuần C# ~150 dòng**. Không `JsonUtility` (nằm trong UnityEngine →
phá ràng buộc trên, lại không đọc được `null` trong mảng), không Newtonsoft (không có trong
`manifest.json` — đã kiểm).

`selfcheck.sh`: build bằng Roslyn đi kèm Unity Hub, không cần .NET SDK, ~3 giây. Né hai bẫy môi
trường đã gặp: đường dẫn Unity có dấu cách nên `-r` **phải** đi qua response file; và build vào
`%TEMP%` của user bị Application Control policy chặn thực thi → build vào `Temp/` của repo.

**Bằng chứng port đúng:** số nước giải *và* số nút beam search trùng khít bản JS
(6/6/9/9 nước, 31322/32318/96530/104202 nút). Cùng đường duyệt trên cùng cây trạng thái, không
chỉ cùng kết quả. C# nhanh hơn ~13×.

### [11] MCP Unity + worktree — không commit

`/mcp` không thấy tool. Truy ra: `unity-mcp` khai ở user scope, relay đang chạy, nhưng bridge
đăng ký cho **repo chính** (`D:\CategorySort`, đang ở `main` = prototype Category Sort cũ), không
phải worktree. Mở Unity thứ hai trỏ vào worktree; bridge vẫn không lên vì package
`com.unity.ai.assistant` (thứ cung cấp bridge) chỉ nằm trong working tree của repo chính và
**chưa commit**. Thêm dòng đó vào `Packages/manifest.json` của worktree → bridge lên.

Giữ `manifest.json` **uncommitted** cho giống repo chính. Đây là tooling, không phải dependency
của game.

### [12] View + Level Editor — commit `89769b3`

`PrototypeView.cs` viết lại theo luật WordStack. Bốn quyết định riêng của Unity:

- Cascade dùng **coroutine**, không state machine trong `Update()` — bản dịch 1-1 của
  `async settle()` + `sleep`. Domain expose `SettleStep(drain)` trả một event mỗi lần gọi.
- Giữ **Rebuild kiểu destroy-hết-dựng-lại** (demo cũng xoá sạch DOM). Animation chạy trên
  GameObject của lần Rebuild trước; `SettleStep` trả `DoomedUids` để tra.
- Zone kéo-thả: `Tile` (nguồn, từng thẻ trong top box) và `Stack` (đích, nguyên top box).
- `pos` → world: **đảo dấu y** (data y xuống, Unity y lên); camera fit từ bbox các `pos`,
  bỏ hằng `GridCols`/`GridRows`.

Level Editor (`WordStack ▸ Level Editor`): proxy `ScriptableObject` tạm + `SerializedObject` +
`PropertyField` → kéo-thả object field, foldout, list +/- là đồ Unity cho sẵn, không viết GUI riêng.

Một lỗi **mất dữ liệu âm thầm** suýt lọt: mở level lúc thiếu PNG → mọi `Sprite` null → Save là
xoá sạch key art khỏi file. Vá bằng `artKey` giữ chuỗi gốc song song. Kiểm trong Editor thật:
lv-001 giữ 9/9 key, lv-002 giữ 12/12 dù chưa có PNG nào.

### [13] `Art Key` read-only + level nhỏ nhất — commit `3c4f8d8`

Khoá `artKey` bằng `ReadOnlyAttribute` + drawer. Khoá xong lộ ngay lỗi thứ hai: `artKey` cũ chỉ
set lúc mở file, nên kéo Sprite mới vào thì ô read-only hiện giá trị cũ — mà ô read-only nói sai
còn tệ hơn ô sửa được. Thêm `SyncArtKeys()` chạy mỗi lần vẽ.

`lv-003 "Smallest"`: 1 nhóm, 4 thẻ, 3 stack, thắng trong 2 nước. Dựng qua **đúng đường Save của
tool** nên cũng là bằng chứng đường `Sprite → key` chạy đầu-cuối.

### [14] Gộp Cards vào Groups + luật không trùng sprite — commit `a9d8137`

Người dùng chỉ ra: một card thuộc đúng một group, một group đúng 4 card, không có card/sprite
dùng chung. Vậy `meaning.cards` phẳng nên gộp vào `meaning.groups[].cards`.

Đúng — và điểm được nhất là **hai luật rời khỏi validate** vì cấu trúc đã loại trừ chúng. `ParentId`
vẫn giữ là field riêng trên group; nhét nhóm con vào mảng `cards` sẽ làm mảng đó thành hỗn hợp
hai kiểu, đúng cái đã bỏ ở bước [6].

Thêm luật **mỗi art key thuộc đúng một thẻ hoặc nhóm**. Nó kéo theo một chỗ không nhìn kỹ thì
không thấy: test cũ *"level không còn thẻ chỉ-chữ"* tạo điều kiện bằng cách gán chung một ảnh cho
mọi thẻ — giờ luật trùng-sprite bắn trước, test vẫn xanh nhưng **đang kiểm nhầm thứ**. Đổi sang
cấp key giả duy nhất cho từng thẻ.

Demo **migrate luôn** (không đóng băng) để repo chỉ có một schema. Bằng chứng không đổi hành vi:
số nút beam search trùng khít bản trước migrate ở cả JS lẫn C#.

---

## 9. Timeline, phần 3 — art, va chạm phiên song song, DOTween

### [15] Sinh art placeholder, và lần đầu thấy view chạy — commit `a3d31f4` (bị gỡ, xem [17])

Blocker 9 PNG treo nhiều lượt nên tự sinh bằng Node (Unity đang đóng): encoder PNG tay
~40 dòng dùng `zlib` có sẵn. Màu **xáo thứ tự** để không tương quan với nhóm — nếu không thì
chơi thử chỉ cần nhìn màu là biết nhóm, hỏng luôn mục đích playtest.

`./selfcheck.sh` **xanh toàn bộ lần đầu**: 3 level, cả hai chế độ luật.

Nghiệm thu view bằng hai lớp. Trước hết là **histogram pixel**: render camera ra RenderTexture,
đếm màu. Kết quả đọc đúng bảng GDD §9.1 — nền `#3A2E5F` 81%, hộp `#6B5CA8`, header `#6A5BA5`,
viền `#A5A5A5`/`#8B8B8B`, và **hai** màu gợi ý nhóm `#F4B740` + `#5BC98C` (tức luật màu đang
chạy thật). Sau đó mới tới ảnh chụp.

Công cụ `Unity_Camera_Capture` của MCP trả về ảnh **trắng trơn** — nó lấy Scene View chứ không
phải camera game. Tự render ra RenderTexture rồi `EncodeToPNG` mới ra ảnh thật.

### [16] Hai lỗi chỉ lộ khi nhìn ảnh — commit `e9565ce`

Cả `compilecheck` lẫn `SelfCheck` đều xanh mà màn hình vẫn sai:

1. `Label` **ngắt dòng ở mọi khoảng trắng** khi chuỗi dài → câu HUD *"Drag a tile onto another
   stack · R restart · N next level"* thành **cột dọc một từ mỗi dòng**.
2. Hằng bề rộng ký tự **đoán** `0.30 × size`. Đo thật bằng `Renderer.bounds` trong Play mode ra
   **≈ 0.085** — đoán cao gấp 3.5×, nên mọi nhãn co còn ~1/4 mức cần, gần như không đọc được.

Bài học: `SelfCheck` kiểm **luật**, không kiểm **hình**. Lớp view chỉ có mắt người và ảnh chụp
làm lưới. Và khi một hằng số là *ước lượng*, đo nó rẻ hơn nhiều so với đoán lại.

Cũng ở đây phát hiện Unity **không tự compile khi mất focus** — hai lần chụp đầu là ảnh của
assembly cũ. Từ đó mọi lần verify đều phải chờ `Assembly-CSharp.dll` mới hơn file nguồn.

### [17] Va chạm với phiên song song

Push bị từ chối: một phiên Claude khác đã đẩy **4 commit** lên cùng branch —
art placeholder **có nhãn** cho 12 thẻ, `docs/architecture/view-prefabs.md`, và quan trọng nhất
**đóng Giai đoạn 1b**: user đã chơi bản Unity, xác nhận *"phần game ok"*, `game-concept.md` →
**Approved**.

Nghĩa là tiền đề *"chưa ai thấy view chạy"* đã lỗi thời từ trước khi bắt đầu, và 9 PNG ở [15]
là **việc trùng**. Cách gỡ: `reset --soft` commit của mình, bỏ art trùng (art có nhãn của họ tốt
hơn ô màu trơn), giữ lại đúng phần sửa code — remote không đụng file `.cs` nào nên không mất gì.

Bài học: branch dùng chung thì `git fetch` trước khi làm việc dài, không phải lúc sắp push.

### [18] Hai quyết định lật thiết kế prefab — commit `e3b69e7`

User đọc doc rồi lật **Q1**: bỏ rebuild-toàn-bàn, chuyển **retained-mode**. Lý do thật không phải
hiệu năng mà là **animation xuyên trạng thái** — rebuild làm đứt danh tính GameObject nên không
tween được thẻ trượt sang slot mới, và COLLAPSE sau này cần 4 thẻ bay về tâm hộp.

Kèm **Mục 4a — invariant check**: retained-mode đánh mất tính "màn hình là hàm thuần của state",
mà `SelfCheck` chỉ kiểm domain còn demo HTML render kiểu rebuild → **không bộ test nào trong repo
nhìn tới lớp view**. Nên bắt buộc assert sau mỗi lần bàn đứng yên: tập uid GameObject bằng đúng
tập tile trong top box.

**Q3 mới: DOTween.** Doc cũng ghi chỗ **không** dùng nó — ghost đuổi con trỏ là bám đích *đang di
chuyển* mỗi frame, không phải tween có điểm đến cố định.

### [19] Cài DOTween + adapt — commit `d1ee532`, `075d4b0`

Không cài được bằng máy: **license DOTween cấm redistribute** nên nó không có trên OpenUPM hay
bất kỳ UPM registry công khai nào (đã tra cả registry lẫn search). Asset Store `.unitypackage` khi
import **không để lại dấu vết gì** trong project, nên cũng không có cơ chế "khai một chỗ, máy khác
tự tải" như `Packages/manifest.json` — cái đó chỉ chạy với gói trên registry.

Kiểm ra repo **đang public**, tức commit DOTween là phát tán công khai. Đã nêu; user chốt commit.
Ghi vào commit message như quyết định có ý thức.

**Bẫy tốn nhiều thời gian nhất phiên:** sau khi import, Unity **im lặng không sinh assembly mới**.
Nguyên nhân: DOTween Setup **tự thêm define `DOTWEEN_EPO`** dù `DOTweenSettings` ghi
`epoOutlineEnabled: 0`. Define bật → `DOTweenModuleEPOOutline.cs` compile → tham chiếu asset
*Easy Performant Outline* không có → **hỏng toàn bộ compile**, không lỗi nào nổi lên rõ ràng.
Đã gỡ define khỏi **16 build target** và cho `DOTweenSettings` khớp lại (`epoOutlineEnabled: 0`),
nếu không lần Setup sau nó tự thêm lại.

Adapt: `FlyAnim` + `AnimateTransients` → `DOMove`; clear 4 thẻ → `Sequence` + `Insert`; xoá hộp →
`DOScale` + `ToAlpha` (thêm được fade đúng §9.3, trước thiếu); rung → `DOPunchPosition`; hover →
tween lúc vào/ra thay vì lerp mỗi frame. Dùng `DOTween.ToAlpha` (core) thay `sr.DOFade` để **không
phụ thuộc module Sprite** có được bật hay không. Mọi tween `.SetLink(go)` — Destroy là tween tự chết.

`compilecheck.sh`: compile cả 2 assembly ngoài Unity. Phải **tách đúng như Unity** vì hai thế giới
reference xung khắc — `DOTween.dll` theo mscorlib, `UnityEditor.dll` theo netstandard; nối bằng
`Facades/netstandard.dll`.

---

## 10. Trạng thái & việc tiếp theo

**Đã xong:** demo HTML + check · domain + validate + solver + SelfCheck · `selfcheck.sh` ·
`compilecheck.sh` · view runtime (đã chơi được, user duyệt) · Level Editor · 3 level · 12 art
placeholder có nhãn · DOTween + adapt · doc pivot.

**Giai đoạn 1b ĐÃ ĐÓNG.** `game-concept.md` = **Approved** (2026-08-03).

**Việc đang mở:** chuyển view sang prefab + retained-mode theo `docs/architecture/view-prefabs.md`.
Mục 1 (Q1 retained-mode, Q3 DOTween) đã chốt; **phần còn lại — 5 prefab, scene, phân công — chờ
user duyệt**. Sau khi duyệt: viết 5 script view + `BoardController`, user dựng prefab tay, rồi
chuyển sang `Instantiate` và nghiệm thu.

**Còn treo:**

- `Rules.RemoveEmptyNonBottomBox` đang `true`. Cả 3 level chạy được ở **cả hai** chế độ nên đây
  là lựa chọn tự do, không bị data ép. Cân nhắc đảo về `false` (đọc chặt §7) nếu Undo quay lại.
- Nguồn art thật + license (12 file hiện tại là placeholder có nhãn).
- COLLAPSE, Undo, Hint — schema đã chừa đường (`group.group`), validate đang chặn.
- DOTween nằm trong repo public — quyết định có ý thức của user, không phải sơ suất.
