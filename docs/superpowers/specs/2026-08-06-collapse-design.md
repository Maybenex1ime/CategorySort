# COLLAPSE — gộp nhóm thành một thẻ mới

*Ngày: 2026-08-06 · Trạng thái: Design, chưa implement*

Hệ thống #19 trong `design/gdd/systems-index.md` (tier Full Vision). Doc này chốt luật và
toàn bộ thay đổi kéo theo, để lúc implement không phải quyết lại.

Hành vi tham chiếu: `wordstack.html` (bản demo có COLLAPSE, dòng 374-410) — khác
`demo/wordstack-clear-demo.html` trong repo, bản đó chỉ có CLEAR.

---

## 1. Luật

Gộp đủ 4 thẻ cùng nhóm trong một hộp thì có **hai kết cục**, tuỳ nhóm đó có nhóm cha hay không:

| Nhóm | Kết cục |
|---|---|
| **không** có nhóm cha (nhóm gốc) | CLEAR như hiện nay — 4 thẻ biến mất, hộp rỗng thì lùi ra, hộp dưới lộ ra |
| **có** nhóm cha | **COLLAPSE** — 4 thẻ biến mất, sinh ra **1 thẻ mới** đặt vào ô trống đầu tiên của chính hộp đó |

Thẻ sinh ra **chính là nhóm vừa gộp**: mang tên và ảnh của nhóm đó, và là **một trong 4 thành
viên của nhóm cha**. Gộp đủ 4 thẻ "Bác sĩ / Y tá / Xe cứu thương / Bệnh nhân" thì ra một thẻ
"Bệnh viện", và "Bệnh viện" là một thành viên của nhóm "Công trình".

Chuỗi cha-con **nhiều tầng** chạy được: `Chó` → `Thú cưng` → `Sở thích`. Chuỗi phải kết thúc ở
một nhóm gốc — đó là chỗ thẻ thật sự biến mất và ván có thể kết thúc.

**Hộp KHÔNG bị xoá và hộp dưới KHÔNG lộ ra** khi collapse. Đây là điểm khác CLEAR quan trọng
nhất: hộp vừa gộp giữ nguyên vị trí, giờ có 1 thẻ và 3 ô trống. Muốn đào xuống hộp dưới thì
người chơi phải chuyển thẻ vừa sinh đi chỗ khác trước.

### Quyết định thiết kế đã chốt

| | Chốt | Lý do |
|---|---|---|
| Thẻ sinh ra là gì | Chính nhóm vừa gộp, thành viên của nhóm cha | Không phải thêm field nào vào schema; chuỗi nhiều tầng chạy được ngay |
| Nhìn ra sao | **Giống hệt thẻ thường** | Không cần trạng thái hiển thị riêng |
| Ô nào nhận | **Ô trống đầu tiên** | Cùng quy ước với `MoveTile`; demo cũng dùng `placeInFirstFreeSlot` |
| Bộ đếm | **Tính là một lần clear** | Với người chơi nó là một lần ăn; HUD và % tiến trình đọc chung một con số |

---

## 2. Level JSON — không thêm field nào

Schema đã chừa sẵn chỗ, có ghi lý do ở `PrototypeDomain.cs:212-215`:

> *"ParentId (quan hệ nhóm-cha, tức COLLAPSE) vẫn là field riêng — nhét nhóm con vào `Cards`
> sẽ làm mảng đó thành hỗn hợp hai kiểu."*

```json
{ "id":"hospital", "text":"Bệnh viện", "group":"building",
  "cards":[ { "id":"bac-si", "text":"Bác sĩ" }, ... 4 thẻ lá ... ] },

{ "id":"building", "text":"Công trình",
  "cards":[ { "id":"nha-hat", "text":"Nhà hát" } ] }
```

| Field đã có | Vai trò trong COLLAPSE |
|---|---|
| `group` (→ `GroupDef.ParentId`) | Nhóm con trỏ lên nhóm cha. Parser đã đọc sẵn |
| `group.text` / `group.art` | **Mặt của thẻ sinh ra.** Đây là lý do nhóm có text/art riêng |
| `Tile.CardId` tách khỏi `Tile.GroupId` | Thẻ sinh ra mang id **nhóm** ở `CardId` và id **nhóm cha** ở `GroupId`; xem Mục 4 |

**Số thành viên của một nhóm = số card khai trực tiếp + số nhóm con trỏ vào nó.** Nhóm
`building` khai đúng 1 card (`Nhà hát`) và có 3 nhóm con → 4 thành viên.

---

## 3. Validate — sửa 2, thêm 3, giữ 1

| | Luật | Ghi chú |
|---|---|---|
| **sửa** | `g.Cards.Count == GroupSize` → `g.Cards.Count + (số nhóm con trỏ vào g) == GroupSize` | `PrototypeDomain.cs:313` |
| **sửa** | Bỏ chặn `ParentId != null` | `PrototypeDomain.cs:311` |
| **thêm** | `group` phải trỏ tới một nhóm **có thật** trong level | |
| **thêm** | **Không có chu trình** trong chuỗi cha-con | Comment ở `:374` đã hẹn sẵn việc này |
| **thêm** | Phải có **ít nhất một nhóm gốc** (không có `group`) | Không có thì không gì biến mất được → không thắng được |
| **giữ** | *"Mọi card phải có mặt trên bàn"* — **không đục ngoại lệ** | Nhóm con không phải card, nên luật này đúng nguyên xi |

Luật cuối là chỗ dễ hiểu nhầm nhất: thoạt nhìn tưởng phải miễn trừ cho các thành viên "sinh ra
lúc chạy", nhưng vì thành viên đó là *nhóm* chứ không phải *card*, `AllCards()` không hề đếm
nó. Không phải sửa gì.

---

## 4. Domain

### `Box` — thêm một cờ

```csharp
public bool HadCollapse;   // hộp này đã từng xảy ra collapse
```

### `Game` phải giữ bảng nhóm — hiện đang KHÔNG giữ

Đây là chỗ hụt lớn nhất khi implement. `Game.Build(lv)` nướng `GroupId`/`Text`/`Art` vào từng
`Tile` rồi **vứt `lv` đi**; `Game` chỉ còn `LevelId`, `Title`, `TotalGroups`, `Cleared`, `Moves`,
`Status`, `Stacks`, `uidSeq`. Nên `SettleStep` không có cách nào tra "nhóm `gid` có cha là ai,
tên và ảnh của nó là gì".

Phải thêm một bảng tra `gid → (ParentId, Text, Art)` vào `Game`, dựng trong `Build`.

**`Clone()` phải CHIA SẺ tham chiếu bảng này, không được sao sâu.** Bảng là dữ liệu chỉ-đọc,
giống nhau ở mọi trạng thái. Solver clone rất dày — riêng `lv-002` đã duyệt 104.202 nút — nên
sao sâu một Dictionary mỗi nút là mất tốc độ thật chứ không phải lo xa.

### `SettleStep` — nhánh clear rẽ đôi

Sau khi xoá 4 thẻ của nhóm `gid`, tra nhóm cha của `gid`:

- **Có cha** → dựng một `Tile` mới, đặt vào ô trống đầu tiên của hộp, bật `box.HadCollapse = true`.
  Hộp không rỗng nên luật xoá-hộp hiện tại **tự động không nổ** — phần "không đẩy hộp dưới lên"
  không cần viết dòng nào.
- **Không cha** → giữ nguyên hành vi hiện tại.

`Cleared++` chạy cho **cả hai** nhánh.

### Thẻ sinh ra

```csharp
new Tile {
    Uid    = "t" + (++g.uidSeq),   // cùng bộ đếm với Game.Build
    CardId = <id của nhóm vừa gộp>,
    GroupId= <ParentId của nhóm vừa gộp>,
    Text   = <Text của nhóm vừa gộp>,
    Art    = <Art của nhóm vừa gộp>
}
```

**`CardId` phải là id của nhóm vừa gộp, không được để `null`.** `Solver.Encode` (`:625`) mã hoá
trạng thái bằng `t.CardId`; để `null` thì mọi thẻ sinh ra đều ra chuỗi rỗng và hai trạng thái
khác nhau bị coi là một — solver ăn nhầm memo và trả kết quả sai. Dùng id nhóm là an toàn vì
`Validate:328` đã cấm một id vừa là card vừa là group, nên không thể va nhau. Nhờ vậy `Encode`
**không phải sửa dòng nào**.

### `SettleEvent` — thêm một kind

View cần phân biệt để làm animation gộp (4 thẻ chụm lại thành 1) khác animation biến mất. Demo
cũng tách `type:'collapse'` khỏi `type:'clear'`. Thẻ sinh ra *nhìn* giống thẻ thường — chỉ
animation lúc sinh là khác.

---

## 5. Chế độ chặt học thêm một luật

`Rules.RemoveEmptyNonBottomBox` có hai chế độ và `SelfCheck` bắt mọi level giải được ở **cả hai**:

- **rộng** (`true`, luật đang chạy thật): hộp trên cùng rỗng vì bất kỳ lý do gì → lùi ra.
- **chặt** (`false`): chỉ CLEAR mới xoá hộp.

Ghép nguyên xi với COLLAPSE thì chế độ chặt **hỏng có hệ thống**:

```
hộp gộp xong → nhận 1 thẻ → không rỗng → không bị xoá        (đúng ý)
người chơi kéo thẻ đó đi → hộp rỗng, nhưng rỗng KHÔNG do CLEAR
                         → chặt không xoá → hộp thành nắp đậy vĩnh viễn
                         → hộp bên dưới không bao giờ với tới được
```

**Chốt: chế độ chặt cũng xoá hộp rỗng nếu `HadCollapse == true`.** Đúng tinh thần chặt ("chỉ
việc ghép bộ mới xoá hộp") vì collapse *là* ghép bộ, chỉ khác ở chỗ nó để lại một thẻ. Giữ được
lưới an toàn kép — chính nó đã bắt được `lv-001` bản đầu không chơi được.

---

## 6. Những thứ không phải sửa

| Thứ | Vì sao vẫn đúng |
|---|---|
| Luật xoá hộp / lộ hộp dưới | Hộp có 1 thẻ thì không rỗng, `IsEmpty` trả false, luật hiện tại tự im |
| Điều kiện Thắng (`TotalTiles() == 0`) | Chuỗi kết thúc ở nhóm gốc, gộp đủ là biến mất hẳn |
| Điều kiện Kẹt (mọi hộp trên cùng đầy) | Không đổi. Ngược lại collapse còn **gỡ kẹt**: một hộp đang đầy 4 thành 1 thẻ + 3 ô trống |
| `Solver.Encode` | `CardId` = id nhóm lo phần thẻ sinh ra; nhưng phải THÊM dấu `!` cho box `HadCollapse` — chặt phân biệt hai bàn cùng thẻ khác cờ (phát hiện khi implement, 2026-08-06) |
| Cascade lồng nhau | Với `BoxCapacity == GroupSize == 4`, hộp vừa gộp chỉ còn đúng 1 thẻ nên không thể lập tức gộp tiếp trong cùng hộp. Không có nguy cơ lặp vô hạn |

---

## 7. Chỗ đâm vào thiết kế bộ sinh tile động

Bộ sinh tile động (`/loop` thiết kế cùng ngày, đang dừng ở bước chọn kiến trúc) chốt

> tiến trình % = số thẻ đã clear / tổng thẻ pool, với tổng = `N nhóm × 4`

COLLAPSE phá mẫu số đó: mỗi lần gộp nuốt 4 thẻ và đẻ ra 1, nên **tổng số thẻ từng tồn tại** là

```
L  +  (số nhóm không phải gốc)

L = số card lá khai trong level
```

Vẫn tính được lúc soạn level, nhưng công thức khác. Nếu hai tính năng cùng về đích thì sửa chỗ
này **một lần**; bỏ qua thì % nhảy sai đúng đoạn cuối ván — chỗ pattern độ khó của GD hay đặt
băng Easy để hạ cánh cho mượt.

---

## 8. Data đã có sẵn

Hai level đã chuyển từ demo, đang đỗ **ngoài `Resources/`** vì chưa chạy được:

| File | Nội dung |
|---|---|
| `docs/levels-collapse/lv-005.json` | 4 nhóm, chuỗi 1 tầng: `hospital`/`school`/`airport` → `building`. 13 thẻ trên bàn |
| `docs/levels-collapse/lv-006.json` | 5 nhóm, chuỗi **2 tầng**: `dog`/`cat`/`bird` → `pet` → `hobby`. 16 thẻ trên bàn. Dùng `group.art` cho nhóm `dog` |

Đỗ ngoài `Resources/` chứ không phải trong thư mục con, vì `Directory.GetFiles` của
`selfcheck.sh` **không đệ quy** nhưng `Resources.LoadAll<TextAsset>("Levels")` của game **có** —
để trong thư mục con thì lọt khỏi selfcheck mà vẫn bị game nạp rồi ném lỗi lúc chạy.

Khi luật land: chuyển hai file vào `Assets/Prototype/Resources/Levels/` rồi `./selfcheck.sh`.
**Chưa có gì bảo đảm chúng giải được** — demo tự ghi là chơi được, nhưng đó là luật của demo.

---

## 9. Ngoài phạm vi

- **`BoxCapacity > GroupSize`.** Lúc đó một hộp chứa được nhiều nhóm, nên "ô trống đầu tiên" và
  "gộp xong còn thẻ khác trong hộp" mới thành câu hỏi thật. Hiện `4 == 4` nên không phải trả lời.
- **Undo.** Hoàn tác một collapse phải dựng lại 4 thẻ *và* xoá thẻ sinh ra *và* trả `HadCollapse`
  về. `Assist (Undo + Hint)` vẫn ở tier Full Vision.
- **Heuristic `Score` của solver.** Có thể phải chỉnh lại vì giờ "gom đủ 4" không còn nghĩa là
  "bớt được 4 thẻ". Đo rồi sửa, đừng đoán trước.
- **Animation gộp.** Thuộc `VFX & Game Feel`.
