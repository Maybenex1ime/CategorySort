# WordStack — Luật chơi

> File này **tự đủ nghĩa**: đọc một mình nó là hiểu game, không cần xem code hay tài liệu khác.
> Mô tả **luật đang chạy thật** trong Unity (`Assets/_Game/Board/Domain/`),
> không phải ý định thiết kế. Chỗ nào luật chạy khác tài liệu gốc (GDD) đều được nói rõ.
>
> Cập nhật: 2026-08-04.

## 0. Tóm tắt 30 giây

Bàn có nhiều **hộp**, mỗi hộp 4 ô. Mỗi thẻ thuộc một **nhóm** (Fruit, Animal, Vehicle...), mỗi nhóm
đúng 4 thẻ. Người chơi kéo thẻ giữa các hộp; gom đủ **cả 4 thẻ cùng nhóm vào một hộp** thì chúng
biến mất (**CLEAR**). Hộp rỗng lùi ra, để lộ hộp bên dưới với thẻ mới. Hết thẻ trên bàn là thắng.
Không timer, không giới hạn nước đi, không điểm, không thua.

## 1. Thành phần bàn chơi

| Khái niệm | Định nghĩa |
|---|---|
| **Tile** (thẻ) | Đơn vị nhỏ nhất. Thuộc đúng **một** nhóm. Hiện bằng **chữ, ảnh, hoặc cả hai**. |
| **Slot** (ô) | Một chỗ trong hộp, chứa tối đa 1 thẻ. Có thể trống. |
| **Box** (hộp) | 4 slot, bày lưới 2×2. |
| **Stack** (chồng) | Nhiều hộp xếp chồng tại **một vị trí** trên lưới. `boxes[0]` là hộp trên cùng. |
| **Group** (nhóm) | Tập đúng **4 thẻ** cùng chủ đề. Nhóm là thứ người chơi phải gom. |

**Hằng số:** `BoxCapacity = 4` · `GroupSize = 4` · `PaletteSize = 6` (số màu gợi ý).

**Chỉ hộp trên cùng của mỗi stack là "sống":** nó hiện đủ nội dung, nhận kéo/thả, và được xét
hoàn thành nhóm. Các hộp bên dưới **đã có sẵn thẻ nạp từ dữ liệu level** nhưng đang bị che — chỉ
lộ mép viền để người chơi ước lượng stack còn sâu bao nhiêu. Không tương tác được với hộp bị che.

**Hộp đáy** (hộp cuối cùng của một stack) **không bao giờ biến mất**. Rỗng thì nó ở lại thành
khoảng trống trung chuyển.

## 2. Nước đi — chỉ có một loại

Kéo **một thẻ bất kỳ trong hộp trên cùng** sang **hộp trên cùng của stack khác**.

- **Hợp lệ** khi hộp đích còn ≥1 slot trống.
- Hộp đích đầy → **từ chối**, thẻ bay về chỗ cũ (hộp đích rung để báo).
- Thả về **chính stack cũ**, hoặc thả ra ngoài bàn → huỷ thao tác, không tính là nước đi.
- Thẻ rơi vào **đúng slot người chơi thả trúng**, nếu slot đó trống. Thả trúng slot đã có thẻ,
  hoặc trúng khe giữa các slot / mép hộp → rơi về **slot trống đầu tiên**, quét trái→phải rồi
  trên→dưới (index 0,1,2,3).
- Slot nào **không đổi luật gom nhóm** (CLEAR đếm thành viên trong hộp, không quan tâm vị trí)
  — nên solver cố tình bỏ qua lựa chọn slot, chỉ đi bản "slot trống đầu tiên".
- Thẻ rút đi để lại **ô trống tại chỗ** — các thẻ còn lại trong hộp nguồn **không dồn lại**.
- Mỗi màn có **số nước tối đa** (đổi 2026-08-17; hiện là mặc định 20 cho mọi màn —
  `GameplayStartContext.StartingMoves`, chưa cấu hình per-level). Hết nước mà bàn chưa
  thắng → **thua**. Luật này sống ở tầng meta (`GameplayFlowAdapter`), KHÔNG trong domain:
  `CheckStatus`/solver/selfcheck không biết move cap, số nút beam search vẫn khớp
  `demo/check.mjs`. Không timer.

## 3. Hoàn thành nhóm → CLEAR

Nhóm G hoàn thành khi **một hộp chứa đủ cả 4 thành viên của G cùng lúc**.

- Luật đếm **thành viên của nhóm**, không so với sức chứa hộp. Đổi hộp sang 16 slot sau này
  không phải sửa luật này.
- 4 thẻ đó biến mất khỏi bàn. Bộ đếm "đã giải" tăng 1.
- Một hộp chỉ có 4 slot nên tối đa một nhóm hoàn thành mỗi lần xét.

## 4. Xoá hộp và lộ hộp dưới

Đây là chỗ luật chạy **rộng hơn** tài liệu gốc, cố ý:

- **Hộp trên cùng rỗng và không phải hộp đáy → hộp bị xoá, hộp bên dưới lộ ra** cùng toàn bộ thẻ
  đã nạp sẵn trong nó.
- Điều này áp dụng **bất kể vì sao hộp rỗng**: do CLEAR, **hoặc do người chơi kéo hết thẻ ra bằng
  tay**.
- Hộp đáy rỗng thì **ở lại**, không bị xoá.

> **Ghi chú lệch tài liệu:** GDD gắn việc xoá hộp với CLEAR ("4 thẻ biến mất, hộp rỗng ra → hộp bị
> xoá"). Đọc chặt thì hộp bị kéo rỗng bằng tay phải nằm lại. Bản này chọn luật rộng vì **không có
> Undo**: luật chặt cho phép người chơi tự khoá chết hộp bên dưới vĩnh viễn, chỉ gỡ được bằng
> Restart. Đây là cờ `Rules.RemoveEmptyNonBottomBox = true`, đảo được bằng một dòng; cả 3 level
> hiện có đều giải được ở cả hai chế độ với cùng số nước. Sẽ cân nhắc lại nếu Undo xuất hiện.

## 5. Dây chuyền (cascade)

Sau **mỗi** nước đi, engine chạy lặp tới khi bàn đứng yên:

1. Quét các stack theo thứ tự; stack nào có hộp trên cùng chứa đủ một nhóm → **CLEAR** nhóm đó.
   Nếu hộp rỗng ra và không phải hộp đáy → xoá hộp, lộ hộp dưới.
2. Nếu không CLEAR được nữa: quét tìm hộp trên cùng **rỗng, không phải đáy** → xoá, lộ hộp dưới.
3. Không còn gì để làm → bàn đứng yên, xét thắng/kẹt.

Hộp vừa lộ ra **có thể đã chứa sẵn một nhóm đủ 4** → CLEAR tiếp, thành dây chuyền nhiều nhịp.
Mỗi nhịp cách nhau ~350ms và **khoá input** trong lúc chạy để người chơi nhìn kịp.

Dây chuyền cũng chạy **ngay khi nạp level**, phòng trường hợp dữ liệu bày sẵn một nhóm đủ.

## 6. Thắng / kẹt

- **Thắng**: không còn thẻ nào trên bàn. Các hộp đáy rỗng vẫn nằm đó.
- **Kẹt**: bàn đã đứng yên và **mọi hộp trên cùng đều đầy** (không còn slot trống nào ở bất kỳ
  hộp trên cùng nào) → không nước đi nào hợp lệ nữa.
- **Không có màn Thua.** Kẹt chỉ hiện một toast gợi ý bấm Restart.

## 7. Màu gợi ý nhóm

- Mặc định thẻ nền xám trắng.
- **Trong cùng một hộp**, nhóm nào đang có **≥2 thẻ** thì các thẻ đó được tô cùng một màu. Thẻ
  đứng một mình giữ nền mặc định.
- Màu cấp phát **cục bộ theo từng hộp**, theo thứ tự nhóm xuất hiện trong hộp (slot 0→3), lấy
  lần lượt trong bảng 6 màu. **Không cố định theo nhóm trên toàn bàn** — cố ý: màu toàn cục thì
  liếc hai hộp là biết ngay chúng cùng nhóm, mất hết phần suy luận.
- Hệ quả cần nhớ khi làm view: **một nước đi làm HAI hộp đổi màu**, không chỉ hộp đích — hộp
  nguồn mất một thẻ thì cặp ở đó có thể tan, thẻ còn lại quay về nền mặc định.

## 8. Định dạng dữ liệu level (JSON)

Hai phần tách bạch: **`layout`** (thẻ nằm ở đâu) và **`meaning`** (thẻ thuộc nhóm nào, hiện ra sao).

```json
{
  "id": "lv-003",
  "title": "Smallest",
  "note": "ghi chú cho người thiết kế, không ảnh hưởng luật",

  "layout": {
    "stacks": [
      { "pos": [0,0], "boxes": [ { "slots": ["apple","banana",null,null] } ] },
      { "pos": [1,0], "boxes": [ { "slots": ["grape","orange",null,null] } ] },
      { "pos": [0,1], "boxes": [ { "slots": [null,null,null,null] } ] }
    ]
  },

  "meaning": {
    "groups": [
      { "id": "fruit", "text": "Fruit", "cards": [
          { "id": "apple",  "text": "Apple",  "art": "apple"  },
          { "id": "banana",                   "art": "banana" },
          { "id": "grape",  "text": "Grape"                   },
          { "id": "orange", "text": "Orange", "art": "orange" }
      ]}
    ]
  }
}
```

- `pos: [x, y]` là toạ độ stack trên lưới. **y đi xuống** (y=1 nằm dưới y=0).
- `boxes[0]` là hộp trên cùng; các phần tử sau là hộp bị che, càng về sau càng sâu.
- `slots` luôn đúng 4 phần tử; `null` là ô trống.
- Thẻ **lồng trong nhóm** (`groups[].cards[]`) chứ không có field "group" trỏ ngược — nhờ vậy
  "mỗi thẻ thuộc đúng một nhóm" là bất khả vi phạm về cấu trúc, không cần kiểm.
- `art` là **tên file ảnh** (không đuôi, không đường dẫn).

**Luật kiểm dữ liệu (level sai thì từ chối nạp, không chạy nửa vời):**

1. Mỗi nhóm phải có **đúng 4 thẻ**.
2. Mỗi thẻ và mỗi nhóm phải có **ít nhất một** trong `text` / `art`.
3. Mỗi level phải có **≥1 thẻ chỉ-ảnh** và **≥1 thẻ chỉ-chữ** — giữ "chữ và ảnh ngang vai" thành
   thuộc tính kiểm được.
4. Id không trùng: giữa các nhóm, giữa các thẻ, và giữa nhóm với thẻ.
5. **Một ảnh chỉ thuộc về một thẻ (hoặc một nhóm)** — hai thẻ dùng chung ảnh là kéo nhầm asset.
6. Mọi thẻ khai báo trong `meaning` phải **có mặt trên bàn**, và mỗi thẻ chỉ xuất hiện **một lần**.
7. Id đặt trong `slots` phải là id **thẻ**, không được là id nhóm.
8. Hai stack không được trùng `pos`. Mỗi stack phải có ≥1 hộp.
9. **Hộp rỗng mà không phải hộp đáy → từ chối**: hộp bên dưới sẽ không bao giờ với tới được...
   *(lưu ý: với luật rộng ở Mục 4 thì hộp rỗng đó sẽ bị xoá ngay khi nạp; luật kiểm này vẫn giữ
   để bắt lỗi bày level.)*
10. Nhóm **không được có nhóm cha** (`group`) — đó là cơ chế COLLAPSE, chưa nằm trong phạm vi.

## 9. Ngoài phạm vi bản này

Những thứ có trong thiết kế dài hạn nhưng **chưa có** trong luật đang chạy:

- **COLLAPSE** — nhóm-của-nhóm: 4 nhóm con clear xong sinh ra một "thẻ chủ đề" cho nhóm cha.
  Dữ liệu đã chừa chỗ (`group` trên entry nhóm) nhưng luật kiểm đang **chặn** nó.
- **Undo** — không có. Đây là lý do luật xoá hộp chọn bản rộng (Mục 4).
- **Hint**, điểm số, timer, độ khó động, màn Thua.

## 10. Bất biến kiểm được (dùng làm test)

Bộ tự kiểm chạy mọi level khi khởi động, và có bản chạy ngoài Unity:

- Mọi level phải **qua được toàn bộ luật kiểm dữ liệu** ở Mục 8.
- Mọi level phải **giải được**: một solver (beam search) tìm ra chuỗi nước đi dẫn tới trạng thái
  không còn thẻ nào. Solver chạy ở **cả hai chế độ** của cờ xoá hộp (Mục 4), và kết quả phải khớp
  giữa hai chế độ về số nước tối thiểu.
- Luật phải khớp với bản tham chiếu HTML (`demo/wordstack-clear-demo.html` + `demo/check.mjs`) —
  lệch chỗ nào là bug chỗ đó.
