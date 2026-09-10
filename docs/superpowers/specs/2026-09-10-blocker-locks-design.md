# Blocker — Hộp khoá, Thẻ băng, Chìa khoá

Ngày chốt: 2026-09-10 · Trạng thái: đã duyệt thiết kế

## 1. Mục tiêu

Ba vật cản đầu tiên của WordStack. Cả ba đều là **một đối tượng bị vô hiệu, gỡ bằng một
điều kiện tiến độ** — người chơi không có hành động nào nhắm thẳng vào vật cản, họ chơi
tiếp và nó tự mở.

| Blocker | Khoá cái gì | Nguồn đếm | Mở khi |
|---|---|---|---|
| Hộp khoá | một hộp | số nhóm đã gom trên toàn bàn | đủ N nhóm |
| Thẻ băng | một thẻ | số nước đi, chỉ tính khi thẻ đã lộ ở hộp trên cùng | đủ N nước |
| Chìa khoá | một hộp mang ổ | thẻ mang chìa cùng id bị gom | thẻ đó biến mất khỏi bàn |

Đây là điều kiện chi phối: **không blocker nào thêm loại nước đi mới**. Game vẫn chỉ có
một hành động là kéo thẻ từ hộp trên cùng sang hộp trên cùng khác. Nhờ vậy Solver không
phải học cách sinh nước đi mới, nó chỉ cần biết nước nào bị cấm.

## 2. Quyết định hành vi đã chốt

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | "Clear một hộp" đếm theo gì? | Mỗi lần gom đủ 4 thẻ cùng nhóm, kể cả COLLAPSE. Dùng thẳng `Game.Cleared` |
| 2 | Hộp khoá cấm gì? | **Cả hai chiều** — không nhặt ra, không thả vào, và không tự nổ khi đủ bộ |
| 3 | Thẻ băng có tính vào bộ 4? | **Không**. Nó chiếm ô như đá cho tới khi tan |
| 4 | Chìa hoạt động ra sao? | Chìa gắn trên một **thẻ**, ổ gắn trên một **hộp**, ghép cặp bằng id. Thẻ chìa bị gom là hộp mở ngay trong cùng dây chuyền, không có vật phẩm chìa phải nhặt |

Quyết định 3 an toàn **vì và chỉ vì** điều kiện gỡ là số nước đi. Nước đi luôn tăng nên
băng chắc chắn tan, không có thế chết cứng. Nếu sau này đổi băng sang gỡ bằng số nhóm đã
gom thì mất tính chất đó: sức chứa hộp bằng đúng cỡ nhóm (cả hai là 4), nên hộp chứa thẻ
băng không bao giờ gom nổi nhóm nào, và nó tự khoá chính điều kiện mở của mình.

Hệ quả của quyết định 3 mà người bày màn phải biết: **hộp nào chứa thẻ băng thì không gom
được nhóm nào cho tới lúc băng tan.** Đó là ý đồ, không phải lỗi.

### 2.1 Bất biến phải giữ: mọi thẻ luôn còn đường biến mất

Điều kiện thắng là bàn sạch thẻ. Nên mọi vật cản phải giữ một bất biến: **không thẻ nào bị
cắt đứt vĩnh viễn khỏi khả năng biến mất.** Vi phạm nó thì màn thành *thua ngầm*, tệ hơn
hẳn thế kẹt: `CheckStatus` chỉ báo kẹt khi **mọi** hộp trên cùng đều đầy, nên một nhúm thẻ
chết cứng ở một chỗ không kích hoạt được nó. Bàn vẫn báo còn chơi được, người chơi đi tiếp
trong ván đã hỏng mà không ai báo.

Ba blocker ở đây giữ được bất biến:

- **Hộp khoá và hộp có ổ** — số nhóm đã gom chỉ tăng, thẻ đã biến mất không quay lại, và
  cổng xuất bản đã chứng minh màn gom hết được, nên điều kiện mở luôn tới nơi.
- **Thẻ băng** — số nước đi luôn tăng nên băng chắc chắn tan.

Người chơi tự đi vào thế bí trước khi khoá mở thì `CheckStatus` báo kẹt như thường, đó là
thế kẹt thật chứ không phải thua ngầm.

Đây cũng là bộ lọc cho vật cản sau này. Hộp một chiều (nhận vào, không cho ra) vi phạm
ngay: bốn thẻ khác nhóm rơi vào là kẹt vĩnh viễn trong khi bàn vẫn báo còn chơi được. Muốn
dùng dạng đó thì phải kèm một trong hai thứ — hộp chỉ nhận thẻ cùng một nhóm, hoặc thẻ bỏ
vào đó không tính vào điều kiện thắng — và cái sau biến nó thành van xả cứu nguy, không
còn là vật cản.

## 3. Một cơ chế cho cả ba

```
Lock {
    Kind     : Clears | Moves | Key
    Need     : int
    Have     : int       // chỉ có nghĩa với Kind = Moves
    KeyId    : string    // chỉ có nghĩa với Kind = Key — id chìa mà hộp này cần
}
```

Gắn `Lock` lên `Box` (Kind = Clears hoặc Key) và lên `Tile` (Kind = Moves). Một hàm
`IsOpen(Game g)` trả lời cho cả ba, một chỗ mã hoá cho Solver, một chỗ kiểm dữ liệu.

Thẻ mang chìa **không bị khoá gì**, nó kéo và gom như thẻ thường, nên chìa không nằm trong
`Lock` mà là một field riêng `Tile.KeyId`. Hộp `Kind = Key` mở khi trên bàn không còn thẻ
nào có `KeyId` bằng của nó.

Bản này chỉ nhận đúng ba tổ hợp trên; tổ hợp khác (thẻ khoá bằng số nhóm, hộp khoá bằng
số nước) bị kiểm dữ liệu từ chối. Mở thêm khi có nhu cầu thật.

### Vì sao hai trong ba blocker KHÔNG làm nở không gian tìm kiếm

`Solver.Encode` hiện mã hoá nội dung các hộp, không mã hoá `Cleared` hay `Moves`. Điều đó
vẫn đúng sau thay đổi này, với hai loại khoá gắn trên hộp:

- **Clears**: mỗi nhóm chỉ gom được đúng một lần, nên cùng một bố cục thẻ luôn ứng với
  cùng một số nhóm đã gom. `Cleared` suy ra được từ nội dung, không cần mã hoá lại.
- **Key**: "thẻ chìa đã bị gom" tương đương "thẻ đó không còn trên bàn". Chìa là thuộc
  tính cố định của thẻ, mà bộ mã hoá đã ghi id thẻ, nên cũng suy ra được từ nội dung.

Chỉ **Moves** phải mã hoá thêm, vì số nước đã trôi qua kể từ lúc thẻ lộ ra phụ thuộc
đường đi chứ không phải bố cục. Nên chỉ màn có thẻ băng mới kiểm chậm hơn.

## 4. Luật chi tiết

### 4.1 Hộp khoá (Clears) và hộp có ổ (Key)

| Điểm chạm | Luật |
|---|---|
| Nước đi | `from` là hộp khoá → từ chối. `to` là hộp khoá → từ chối |
| Dây chuyền | Hộp còn khoá **không tham gia** tìm nhóm đủ. Không tự nổ |
| Thế kẹt | Hộp còn khoá **không tính** là còn ô trống trong `CheckStatus` |
| Xoá hộp rỗng | Hộp còn khoá không bị xoá dù rỗng |

Điểm thứ hai là điểm dễ bỏ sót nhất. Nếu hộp khoá vẫn tự nổ khi đủ bộ thì khoá mất ý
nghĩa: người chơi được thưởng mà không phải mở nó.

Điểm thứ ba cũng bắt buộc. Bỏ qua thì bàn báo "còn chơi được" trong khi mọi hộp mở đều
đầy, và màn treo thay vì báo kẹt.

### 4.2 Thẻ băng (Moves)

| Điểm chạm | Luật |
|---|---|
| Nước đi | Thẻ còn băng → không kéo đi được |
| Đếm bộ 4 | Thẻ còn băng **không tính** khi tìm nhóm đủ |
| Khi nhóm nổ | Thẻ còn băng cùng nhóm **không bị xoá** — nó không tham gia bộ đó |
| Bộ đếm | Sau mỗi nước đi thành công, mọi thẻ băng đang ở hộp trên cùng cộng 1 vào `Have`; chạm `Need` thì tan |

Thứ tự trong một lượt: **nước đi → giảm băng → chạy dây chuyền**. Nhờ thứ tự này, băng tan
đúng lúc hộp đã sẵn ba thẻ cùng nhóm thì nhóm nổ ngay trong cùng nhịp, người chơi không
phải đi thêm một nước vô nghĩa để kích hoạt.

Bộ đếm chỉ chạy khi thẻ đã ở hộp trên cùng. Đếm cả lúc còn chìm thì băng tan trước khi
người chơi nhìn thấy nó, vật cản thành vô hình.

Nước đi bị từ chối không làm giảm băng.

### 4.3 Tương tác với ba booster đang chạy

Bắt buộc phải chốt, vì Nam châm và Xáo đang chạy sẽ gặp blocker ngay khi màn đầu tiên có
vật cản xuất hiện.

| Booster | Với blocker | Lý do |
|---|---|---|
| Nam châm | Bỏ qua nhóm có thành viên đang băng hoặc đang nằm trong hộp khoá | Hút được là phá vật cản miễn phí, vật cản mất ý nghĩa |
| Xáo | Không đụng thẻ băng, không đụng hộp khoá | Thẻ băng không di chuyển được; hộp khoá đang đóng cả hai chiều |
| Undo | Không cần luật riêng | Ảnh chụp là toàn bàn nên mức băng và mức khoá tự khôi phục theo |

Với Nam châm, cách rẻ nhất là để `FindMagnetTarget` bỏ qua nhóm không đủ 4 thẻ **hút
được**, dùng lại đúng ràng buộc "đủ 4 thẻ đang tồn tại" đang có.

### 4.4 Blocker chồng lên nhau

Ba blocker gặp nhau trên cùng thẻ hoặc cùng chồng là chuyện thường. Không cần luật riêng
ngoài luật kiểm ở Mục 5, nhưng thứ tự gỡ phải rõ để bộ giải và tự kiểm cùng một cách hiểu:

| Tình huống | Xử lý |
|---|---|
| Thẻ vừa băng vừa mang chìa | Cặp được phép trong bảng tương thích. Băng tan trước vì nước đi luôn tăng, rồi thẻ gom được cùng nhóm, rồi hộp mở. Chuỗi điều kiện, không phải vòng: bộ đếm băng không phụ thuộc hộp có ổ |
| Thẻ băng nằm trong hộp khoá đang ở trên cùng | Băng vẫn đếm vì thẻ đang lộ. Khoá chặn truy cập, băng chặn di chuyển, hai bộ đếm chạy độc lập. Lúc hộp mở có thể băng đã tan |
| Thẻ băng nằm dưới hộp khoá | Không đếm cho tới khi hộp trên mở rồi bị xoá — đúng luật "chỉ đếm khi lộ" |
| Thẻ chìa, hoặc thẻ cùng nhóm với nó, nằm trong hoặc dưới hộp mà chìa đó mở | Cấm ở luật kiểm 6, khoá vĩnh viễn |
| Hộp `locked` cần nhiều nhóm hơn số nhóm còn gom được | Không bắt được bằng kiểm dữ liệu, để cổng xuất bản bắt |

## 5. Định dạng dữ liệu màn

Giữ nguyên khung `layout` / `meaning`. Blocker chia theo thứ nó gắn vào: **blocker của thẻ
nằm trên entry thẻ trong `meaning`, blocker của hộp nằm trên hộp trong `layout`.** Mỗi thẻ
xuất hiện đúng một lần trên bàn nên đặt trên entry thẻ là không lệch được, và `slots` giữ
nguyên là mảng id.

Trên cả hai, blocker gom trong một object `blockers`: key là **id blocker**, giá trị là tham
số của nó. Id là tên cố định — thêm blocker mới là thêm key, data cũ không đổi. Thẻ mang
nhiều blocker là nhiều key trong cùng object. Bộ đọc màn coi mọi key trong `blockers` là id
blocker, gặp id lạ thì từ chối, nên field khác thêm vào thẻ hay hộp sau này không va chạm.

```json
"meaning": { "groups": [
  { "id": "fruit", "text": "Fruit", "cards": [
      { "id": "banana", "text": "Banana", "blockers": { "ice": 5 } },
      { "id": "apple",  "art": "apple",   "blockers": { "key": "k1" } }
  ]}
]},
"layout": { "stacks": [
  { "pos": [0,0], "boxes": [
      { "slots": ["apple","banana",null,null], "blockers": { "locked": 3 } }
  ]},
  { "pos": [1,0], "boxes": [
      { "slots": ["grape","plum",null,null],   "blockers": { "keylock": "k1" } }
  ]}
]}
```

Bảng id blocker của bản này. Đây là **sổ đăng ký**: blocker mới phải thêm một dòng vào đây
trước khi được dùng trong data. Lock and Key là một blocker nhưng có hai nửa, mỗi nửa một id.

| id | gắn vào | tham số | ý nghĩa |
|---|---|---|---|
| `locked` | hộp | số nguyên ≥ 1 | hộp đóng cho tới khi `Cleared` đạt số đó |
| `keylock` | hộp | id chìa | hộp đóng cho tới khi thẻ mang `key` cùng id biến mất khỏi bàn |
| `ice` | thẻ | số nguyên ≥ 1 | thẻ bất động cho tới khi đủ số nước kể từ lúc lộ ở hộp trên cùng |
| `key` | thẻ | id chìa | **không khoá gì**; thẻ này bị gom là mở mọi hộp `keylock` cùng id |

`Game.Build` chép `ice` sang `Tile.Lock`, `key` sang `Tile.KeyId`, và blocker của hộp sang
`Box.Lock` (Mục 3). Sau khi dựng bàn, luật không đọc lại data.

Luật kiểm dữ liệu thêm:

1. Mọi key trong `blockers` phải có trong sổ đăng ký, và đúng chỗ: id của hộp không được
   nằm trên thẻ, id của thẻ không được nằm trên hộp.
2. Hộp mang **tối đa một** blocker. `locked` và `keylock` không đi cùng nhau.
3. Thẻ mang từ hai blocker trở lên phải theo **bảng tương thích** — một bảng khai cặp id nào
   được phép đứng chung trên một thẻ. Bản này bảng có đúng một cặp: `ice` + `key`. Blocker
   thẻ thứ ba chỉ cần thêm dòng, không sửa code kiểm.
4. `locked` và `ice` là số nguyên ≥ 1.
5. `keylock` phải trỏ một id chìa có **đúng một** thẻ mang. Một chìa mở được nhiều hộp cùng
   id thì được; hai thẻ cùng mang một id chìa thì từ chối, để không phải định nghĩa "mở khi
   thẻ nào biến mất".
6. Thẻ chìa **và mọi thẻ cùng nhóm với nó** không được nằm trong hộp mà chìa đó mở, hoặc
   trong bất kỳ hộp nào bên dưới hộp đó trong cùng chồng. Hộp có ổ không lấy thẻ ra được nên
   không bao giờ rỗng, không bao giờ bị xoá, hộp dưới nó không lộ ra chừng nào chưa mở. Thẻ
   chìa nằm dưới là khoá vĩnh viễn; thẻ cùng nhóm nằm dưới cũng vậy, vì chìa chỉ bị gom khi
   cả nhóm về chung một hộp.

Luật 6 chỉ bắt được vòng trực tiếp trong một chồng. Vòng gián tiếp qua nhiều hộp, ví dụ
thẻ cùng nhóm với chìa bị kẹt vì một hộp `locked` khác, để cổng xuất bản bắt.

## 6. Bộ giải và cổng xuất bản

- Vòng sinh nước bỏ sớm: hộp khoá ở cả `from` lẫn `to`, và thẻ còn băng.
- `Encode` thêm mức băng còn lại vào mã của thẻ, để hai bàn giống hệt về thẻ mà khác mức
  băng không bị gộp làm một. Hai loại khoá gắn trên hộp không thêm gì (xem Mục 3).
- `CheckStatus` đã sửa theo Mục 4.1 nên Solver dùng lại được, không cần luật riêng.
- Bề rộng tìm kiếm hiện là 600. Màn có thẻ băng có thể phải nới; chỉ nới khi đo thấy cần,
  không nới trước.

Cổng không đổi: màn có blocker vẫn phải giải được ở **cả hai chế độ** xoá hộp mới được
xuất bản.

## 7. Tự kiểm

Thêm một mục vào `SelfCheck`, dựng bàn tay như mục Undo đang làm:

- Hộp khoá: không nhặt ra, không thả vào, không tự nổ khi đủ bộ; gom đủ N nhóm thì mở.
- Thẻ băng: không kéo được; ba thẻ cùng nhóm quanh nó không nổ; đủ N nước thì tan và nhóm
  nổ ngay trong cùng nhịp.
- Chìa: gom nhóm chứa thẻ chìa thì hộp mở, nhóm khác nổ thì không; thẻ vừa băng vừa chìa
  thì tan rồi gom mới mở.
- Kiểm dữ liệu từ chối đủ 6 trường hợp ở Mục 5, kể cả id blocker lạ và id đặt sai chỗ.
- Ba màn hiện có vẫn xanh ở cả hai chế độ.

## 8. Chia nhịp

| Nhịp | Nội dung | Kiểm bằng |
|---|---|---|
| 1 | Luật bàn, đọc màn, Solver, tự kiểm. Chưa có hình | `./selfcheck.sh` ngoài Unity |
| 2 | Hình trong Unity cho cả ba, kèm một màn mẫu dựng tay | `./compilecheck.sh` + chơi thật |
| 3 | Công cụ dựng màn hiểu blocker | Xuất một màn từ công cụ, chạy lại cổng |

Nhịp 1 là nhịp nặng nhất về tư duy và kiểm được trong vài giây mà không cần mở Unity.
Nhịp 3 có thể hoãn: cho tới lúc đó, màn có blocker viết tay bằng JSON.

## 9. Ngoài phạm vi

- Chìa là vật phẩm nằm trên bàn, phải kéo tới hộp khoá. Cần thêm loại nước đi mới, Solver
  và cổng xuất bản đều phải sửa lớn.
- Blocker hay chìa trên thẻ do COLLAPSE sinh ra giữa ván. Thẻ đó không có entry trong
  `cards`, nên muốn khoá nó hay cho nó mang chìa thì `blockers` phải đặt được trên entry
  nhóm — chưa cần.
- Băng lan sang thẻ kề, hoặc bất kỳ vật cản nào tự biến đổi khi người chơi không làm gì.
- Booster mới để phá vật cản.
- Hình động khi khoá mở và khi băng vỡ. Nhịp 2 chỉ cần trạng thái tĩnh đọc được.
