# Định dạng file level

File mẫu: [`level-sample.json`](level-sample.json). Nó dùng đủ mọi field và cả bốn loại blocker, qua được validator,
nhưng **không nhằm để chơi** (không giải được) — chỉ để đọc hiểu.

Level thật nằm ở `Assets/_Game/Content/Levels/*.json`. File JSON thuần, không viết được chú thích; ghi chú đặt vào `note`.

## Tổng quan

```
level
├─ id, title, note, difficulty, moves      thông tin màn
├─ layout.stacks[]                         bàn chơi: các chồng hộp và thẻ đặt sẵn
│   ├─ pos                                 vị trí chồng trên lưới
│   └─ boxes[]                             các hộp trong chồng, phần tử đầu = hộp trên cùng
│       ├─ slots[4]                        4 ô, mỗi ô là id thẻ hoặc null
│       └─ blockers                        (tuỳ chọn) khoá hộp
└─ meaning.groups[]                        nội dung: các nhóm và thẻ của từng nhóm
    ├─ id, text, art, group                nhóm; "group" = id nhóm cha (tuỳ chọn)
    └─ cards[]                             thẻ thuộc nhóm
        ├─ id, text, art
        └─ blockers                        (tuỳ chọn) khoá thẻ
```

Luật chơi cốt lõi để hiểu data: người chơi kéo thẻ giữa các **hộp trên cùng**. Một hộp chứa đủ **4 thành viên** của
cùng một nhóm thì nhóm đó nổ (clear). Hộp trên cùng rỗng và không phải hộp đáy thì biến mất, hộp bên dưới lộ ra.

## Field cấp màn

| Field | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `id` | chuỗi | có | Định danh màn, **phải trùng tên file** (`Level_3.json` → `"Level_3"`). Cũng là address Addressables để nạp màn. |
| `title` | chuỗi | nên có | Tên hiển thị. Thiếu thì dùng `id`. |
| `note` | chuỗi | không | Ghi chú cho người làm level. Game không dùng. |
| `difficulty` | `"Normal"` / `"Hard"` / `"Crazy"` | không | Độ khó, quyết định màu nút Play. Thiếu = `Normal`. |
| `moves` | số nguyên | không | Số nước đi cho phép. Thiếu hoặc `0` = không giới hạn. |

`difficulty` và `moves` chỉ được đọc lúc chạy tool `WordStack ▸ Build Level Catalog` (chép vào `SO_LevelCatalog`).
Sửa hai field này xong phải chạy lại tool; các field còn lại game đọc thẳng từ JSON mỗi lần nạp màn.

## `layout.stacks[]` — bàn chơi

Mỗi phần tử là một **chồng hộp** (stack) tại một vị trí trên bàn.

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `pos` | `[x, y]` | Vị trí trên lưới: `x` = cột từ trái sang (0, 1, 2…), `y` = hàng từ trên xuống. Nhận số lẻ để xếp so le (mẫu: `[0.5, 1]`). Hai stack không được trùng `pos`. |
| `boxes` | mảng | Các hộp của chồng. **Phần tử đầu là hộp trên cùng** (đang nhìn thấy, đang chơi được); phần tử cuối là hộp đáy. |

Mỗi hộp trong `boxes`:

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `slots` | mảng đúng 4 phần tử | Mỗi ô là `id` của một thẻ, hoặc `null` nếu ô trống. Thứ tự ô = vị trí hiển thị trong hộp. |
| `blockers` | object | (tuỳ chọn) Khoá hộp, xem mục Blocker. Một hộp mang **tối đa một** blocker. |

Ràng buộc:
- Mỗi thẻ khai trong `meaning` phải xuất hiện **đúng một lần** trong `slots` của toàn bàn.
- Chỉ đặt id **thẻ** vào `slots`, không đặt id nhóm.
- Hộp không phải hộp đáy không được rỗng hoàn toàn (hộp dưới nó sẽ không bao giờ lộ ra). Hộp đáy được rỗng — dùng làm chỗ trống cho người chơi xoay thẻ.

Trong mẫu: stack `[0,0]` có 2 hộp (hộp trên `poodle, corgi, ·, red` che hộp dưới `ball, kite`); 5 stack còn lại 1 hộp.

## `meaning.groups[]` — nhóm và thẻ

Mỗi nhóm:

| Field | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `id` | chuỗi | có | Định danh nhóm, không trùng nhóm khác và không trùng id thẻ nào. |
| `text` | chuỗi | `text` hoặc `art` | Tên nhóm. |
| `art` | chuỗi | `text` hoặc `art` | Tên ảnh của nhóm (xem mục Art). Chỉ thật sự cần cho nhóm con (ảnh của thẻ sinh ra khi gộp) và nhóm được dùng làm `grouplock` (ảnh hiện trên hộp khoá). |
| `group` | chuỗi | không | `id` của **nhóm cha**. Có field này thì đây là nhóm con, xem "Nhóm cha – con". |
| `cards` | mảng | có | Các thẻ thuộc nhóm. |

Mỗi thẻ trong `cards`:

| Field | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `id` | chuỗi | có | Định danh thẻ, duy nhất trong màn. Đây là chuỗi đặt vào `slots`. |
| `text` | chuỗi | `text` hoặc `art` | Chữ trên thẻ. |
| `art` | chuỗi | `text` hoặc `art` | Tên ảnh trên thẻ. Có cả hai thì thẻ hiện ảnh. |
| `blockers` | object | không | Khoá thẻ, xem mục Blocker. |

### Nhóm cha – con (gộp)

Mỗi nhóm phải có **đúng 4 thành viên**. Thành viên = thẻ khai trong `cards` **+** các nhóm con trỏ `group` vào nó.

- Nhóm **không có** `group` là nhóm gốc: gom đủ 4 thì nổ, biến mất khỏi bàn.
- Nhóm **có** `group` là nhóm con: gom đủ 4 thẻ thì **gộp thành một thẻ mới** nằm lại trong hộp đó. Thẻ mới mang
  `text` / `art` của nhóm con và là thành viên của nhóm cha.

Trong mẫu: `g_dog` (4 thẻ) và `g_cat` (4 thẻ) đều có `"group": "g_pet"`. `g_pet` chỉ khai 2 thẻ (`fish`, `parrot`),
cộng 2 nhóm con = 4 thành viên. Người chơi gom 4 chó → ra 1 thẻ "dog"; gom 4 mèo → ra 1 thẻ "cat"; rồi gom
`dog + cat + fish + parrot` → nhóm `g_pet` nổ. `g_color` và `g_toy` là nhóm gốc thường.

Phải có ít nhất một nhóm gốc, và chuỗi cha–con không được vòng tròn.

### Art

`art` là **tên file ảnh không đuôi** trong `Assets/_Game/Content/Resources/Art/` (`"art": "poodle"` → `poodle.png`).
Ảnh phải tồn tại, và trong một màn **mỗi ảnh chỉ dùng cho một thẻ hoặc một nhóm** (hai thẻ chung ảnh bị coi là kéo nhầm).

Trong mẫu, `poodle` có cả `text` lẫn `art`; `robot` chỉ có `text` (thẻ chữ); `g_toy` chỉ có `text` (nhóm gốc không cần ảnh).

## Blocker

Có 4 loại: 2 loại đặt trên **hộp** (`boxes[].blockers`), 2 loại đặt trên **thẻ** (`cards[].blockers`).
Đặt nhầm phía (blocker hộp lên thẻ hoặc ngược lại) là lỗi. Theo quy ước thiết kế, blocker chỉ dùng ở **hộp trên cùng**.

| Blocker | Đặt trên | Giá trị | Tác dụng | Mở khi |
|---|---|---|---|---|
| `locked` | hộp | số nguyên ≥ 1 | Hộp đóng: không thả thẻ vào, không lấy thẻ ra, không tự nổ. Số hiện trên hộp. | Người chơi đã nổ đủ **N nhóm** trên toàn bàn. |
| `grouplock` | hộp | `id` một nhóm | Hộp đóng như trên, hiện ảnh của nhóm đó. | Nhóm đó được **gom sạch khỏi bàn**. |
| `ice` | thẻ | số nguyên ≥ 1 | Thẻ đóng băng: không nhặt được, booster không dời được. Vẫn tính vào bộ 4. | Sau **N nước đi** thành công, chỉ đếm khi thẻ ở hộp trên cùng và hộp đó đang mở. |
| `fixed` | thẻ | `true` | Thẻ đóng đinh: người chơi không bao giờ nhặt được. Vẫn tính vào bộ 4 — phải mang 3 thẻ còn lại đến hộp của nó. | Không mở; biến mất khi nhóm của nó nổ / gộp (Magnet hút được). |

Trong mẫu:
- Stack `[1,0]`: `"blockers": { "locked": 2 }` — hộp mở sau khi nổ 2 nhóm bất kỳ.
- Stack `[2,0]`: `"blockers": { "grouplock": "g_color" }` — hộp mở khi 4 thẻ màu đã nổ hết.
- Thẻ `persian`: `"blockers": { "ice": 2 }` — băng tan sau 2 nước đi.
- Thẻ `corgi`: `"blockers": { "fixed": true }` — đóng đinh trong hộp trên của stack `[0,0]`.

Ràng buộc của blocker (validator báo lỗi nếu vi phạm):
- `grouplock`: thẻ của nhóm bị khoá (kể cả thẻ của nhóm con nó) **không được nằm trong hoặc dưới** chính hộp đó — nếu không hộp khoá vĩnh viễn.
- `fixed`: thẻ đóng đinh chỉ được nằm ở **hộp trên cùng**. Một thẻ không mang cả `fixed` lẫn `ice`.
- Nên tránh (validator chưa bắt, solver bắt): đặt thẻ cùng nhóm với thẻ `fixed` ở hộp **bên dưới** nó — hộp trên không
  bao giờ rỗng khi nhóm chưa gom xong, nên thẻ bên dưới không bao giờ lộ ra.

## Kiểm tra một level

- `./selfcheck.sh` (chạy ngoài Unity): validate mọi file trong thư mục Levels, kiểm ảnh tồn tại, và chạy solver xem màn
  có giải được không.
- Trong Unity: sau khi thêm / đổi `id`, `title`, `difficulty`, `moves` thì chạy `WordStack ▸ Build Level Catalog` để
  đăng ký màn vào Addressables và cập nhật catalog.
- Level xuất từ tool theo định dạng `traits / tiles / keyTiles / board`: chạy `python tools/convert-trait-levels.py` để
  chuyển sang định dạng này.
