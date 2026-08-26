# Booster Shuffle — thiết kế logic

Ngày chốt: 2026-08-26 · Trạng thái: đã duyệt thiết kế, chưa triển khai

## 1. Mục tiêu

Shuffle xáo lại nội dung thẻ để **mở đường cho người chơi**, không phải để tự dọn bàn hộ họ. Sau khi bấm, bàn phải ở trạng thái người chơi nhìn ra ngay việc cần làm nhưng **vẫn phải bỏ nước đi ra để làm**.

Nguyên tắc chi phối toàn bộ thiết kế: **không phát clear miễn phí**. Không hộp nào được tự nổ ngay sau shuffle, kể cả nổ trễ khi hộp trên bị xoá.

## 2. Từ vựng

| Từ | Nghĩa |
|---|---|
| **Lớp trên cùng** | `TopBox(s)` của mọi stack — thứ người chơi tương tác được |
| **Thẻ trắng** | Thẻ mà nhóm của nó có <2 thẻ trong chính hộp đó, nên `Game.BoxColorIndices` không cấp màu. Tức thẻ đang đứng lẻ |
| **Thẻ có màu** | Thẻ thuộc cụm ≥2 thẻ cùng nhóm trong một hộp — công sức người chơi đã gom |
| **Nhóm mồi** | Bố cục 3+1: 3 thẻ cùng nhóm trong một hộp còn ô trống, thẻ thứ 4 ở top box khác. Người chơi tốn **đúng 1 nước** để nổ |
| **Gom cụm** | Dồn thẻ cùng nhóm về chung hộp, tối đa 3 thẻ/nhóm/hộp |
| **Donor** | Thẻ ở layer 2 trở xuống được đổi nội dung để đẩy thẻ cần thiết lên lớp trên |

## 3. Phạm vi được đụng

```
Layer 1 (top box)              Layer 2+ (bị chôn)
┌────┬────┬────┬────┐          ┌────┬────┬────┬────┐
│ A  │ A  │ B  │ ·  │          │ C  │ C  │ D  │ E  │
└────┴────┴────┴────┘          └────┴────┴────┴────┘
  └──┬──┘   │                    tất cả đều có thể làm donor
  KHOÁ    đụng được
(có màu)   (trắng)
```

- **Layer 1:** chỉ **thẻ trắng**. Thẻ có màu đứng nguyên chỗ, nguyên nội dung — booster không phá cụm người chơi đã gom.
- **Layer 2 trở xuống:** mọi thẻ đều có thể làm donor, **nhưng chỉ những ô thật sự donate mới đổi nội dung**. Phần còn lại giữ nguyên tuyệt đối. Nguyên tắc xáo trộn tối thiểu.

## 4. Hai phép biến đổi

| Phép | Phạm vi | Bảo toàn |
|---|---|---|
| Đổi nội dung 1-đổi-1 | hai ô bất kỳ trong phạm vi được đụng, kể cả khác layer | mọi số đếm, ở mọi hộp |
| Dịch chỗ thẻ | **chỉ trong lớp trên cùng**, được dùng ô trống | tổng lớp trên; số thẻ từng hộp thay đổi được |

Phép dịch chỗ là thứ cho phép tạo ô trống ở hộp chủ. Không có nó thì Nhóm mồi chỉ dựng được ở hộp sẵn có đúng 3 ô đầy.

## 5. Bất biến — kiểm sau khi xếp

1. **Tổng số thẻ ở lớp trên cùng không đổi.**
2. **Mỗi top box giữ ≥1 thẻ.** Suy ra từ (1): hộp top rỗng bị `SettleStep` xoá → hộp dưới lộ ra → tổng lớp trên tăng.
3. **Không hộp nào trên toàn bàn có 4 thẻ cùng nhóm** → `CompletedGroupIn(box) == null` với mọi hộp, mọi layer.
4. **Thẻ có màu ở lớp trên giữ nguyên vị trí và nội dung.**

### Vì sao bất biến 3 phải phủ cả hộp bị chôn

`SettleStep` chỉ soi top box, nên 4 thẻ cùng nhóm ở hộp bị chôn nằm im — rồi **nổ ngay khoảnh khắc hộp trên bị xoá**, người chơi tốn 0 nước:

```
TRƯỚC                                SAU (nếu không kiểm)
layer 1:  │ H │ ·  │ …               │ G │ ·  │ …      ← G kéo lên
layer 2:  │ H │ H │ H │ G │          │ H │ H │ H │ H │  ← H bị đẩy xuống → nổ khi lộ ra
```

Chi phí kiểm rất nhỏ: chỉ cần soi những hộp dưới **thật sự nhận** thẻ bị đẩy xuống, số đó bằng đúng số thẻ kéo lên.

## 6. Thuật toán

### 6.1 Thống kê

Với mỗi nhóm: số thẻ ở lớp trên, số thẻ ở dưới, tổng trên bàn. Đếm ô trống ở lớp trên.

### 6.2 Nhóm mồi

**Đếm cái đã có sẵn.** Nhóm G tính là nhóm mồi sẵn có nếu một top box chứa đúng 3 thẻ G **và** còn ≥1 ô trống, **và** có ≥1 thẻ G nữa ở top box khác.

**Dựng thêm cho tới tối đa 3.** Ứng viên phải có **đủ 4 thẻ đang tồn tại trên bàn**. Nhóm cha còn nhóm con chưa collapse thì mới có 3 thẻ thật, bị loại — cùng ràng buộc với booster Magnet.

Với mỗi nhóm được chọn:

1. **Chọn hộp chủ.** Trong các top box, ưu tiên hộp có **layer 2 nhiều thẻ nhất**. Lý do: hộp chủ nổ xong sẽ rỗng và bị xoá, hộp dưới lộ ra — chọn hộp ngồi trên một hộp đầy thì lượt sau người chơi có nhiều nguyên liệu. Stack chỉ có một hộp thì layer 2 đếm 0, xếp cuối. Hoà thì lấy chỉ số stack nhỏ hơn.
2. **Hộp chủ phải kết thúc với đúng 3 ô có thẻ, cả 3 đều là G.** Thẻ thừa trong hộp chủ dịch sang ô trống ở top box khác.
3. **Thẻ G thứ 4 đặt vào một top box khác.** Bắt buộc là top box — chôn xuống dưới là người chơi không với tới.
4. Thiếu thẻ G thì đổi nội dung với donor ở layer dưới.

**Số nhóm dựng được bị chặn bởi số ô trống ở lớp trên**, vì mỗi hộp chủ phải chừa 1 ô. Lớp trên chỉ còn 1 ô trống thì tối đa 1 nhóm mồi, không phải 3. Dựng được bao nhiêu hay bấy nhiêu.

### 6.3 Gom cụm

Áp cho **thẻ trắng còn thừa ở lớp trên cùng**. Layer dưới không đụng ngoài các ô donate.

**Tối ưu theo KÍCH THƯỚC cụm, không phải số cụm.** Một hộp có 3 thẻ P tốt hơn hai hộp mỗi hộp 2 thẻ P: cụm 3 cách clear 1 nước, cụm 2 cách 2 nước.

Tham lam, xác định được:

1. Duyệt nhóm theo **số thẻ thừa giảm dần**; hoà thì theo thứ tự bảng chữ cái của group id.
2. Mỗi nhóm dồn `min(số thẻ thừa, 3)` thẻ vào **một** hộp.
3. Chọn hộp theo **chỉ số stack tăng dần** — mỗi stack chỉ có một top box nên chỉ số stack chính là định danh hộp ở lớp trên.

Bậc hoà ở (1) và thứ tự ở (3) chỉ để kết quả xác định: cùng một bàn phải luôn ra cùng một cách xếp, không thì không test lại được.

### 6.4 Kiểm hậu điều kiện

Chạy lại toàn bộ bất biến ở Mục 5. Không đạt thì huỷ, giữ nguyên bàn, **không trừ lượt**.

## 7. Tương tác giữa Gom cụm và Nhóm mồi

Cụm 3 thẻ nằm trong hộp còn ô trống, với thẻ thứ 4 ở lớp trên, **chính là** một Nhóm mồi. Nên Gom cụm đôi khi vô tình tạo ra nhóm mồi thứ 4, vượt cap 3.

**Cap 3 chỉ giới hạn phần cố ý dựng.** Nhóm mồi vô tình để nguyên — phá nó ra chỉ để giữ đúng con số là làm bàn xấu đi vô cớ.

## 8. Nối vào game

Dùng lại nguyên hạ tầng vừa dựng cho Magnet:

| Mảnh | Cách làm |
|---|---|
| Lệnh meta → bàn | `LevelCommands.RequestShuffle()` — không nghe `Bus.Global` được, `WordStack.Board` chỉ tham chiếu `WordStack.Contracts` để `compilecheck.sh` build được target `game` |
| Cầu nối | `MetaSession` nghe `BoosterActivatedEvent`, thêm nhánh `BoosterId.Shuffle` |
| Xám nút | `LevelSignals.ShuffleAvailable`, board đẩy sau mỗi settle |
| Chặn input khi diễn | Hai vế như Magnet: `locked` chặn kéo thẻ, `RaiseMoveCommitted(g.Moves)` đẩy phase khỏi `Playing` để overlay phủ nút HUD |
| ViewModel | Theo `MagnetBoosterViewModel` — không kế thừa `InstantBoosterViewModelBase` vì lớp đó cố ý chưa trừ lượt |
| Enum | `BoosterId.Shuffle = 6`. **Không đánh 0** — `None = 0` là sentinel của ba chốt trong `BoosterManager` |

**Điều kiện xám nút:** cần ≥1 ô trống ở lớp trên để hộp chủ chừa được ô. Mà "lớp trên còn ô trống" đúng bằng định nghĩa `Status == Playing` trong `CheckStatus()`. Nên điều kiện gần như cho không.

## 9. Quyết định đã chốt

- **Shuffle không tính là một nước đi** — `Moves` không tăng, giống Magnet.
- **Không chạy `Solver.Solve()` kiểm màn còn giải được.** Shuffle chỉ hoán vị nội dung nên bộ thẻ toàn bàn không đổi, và luôn xếp về hướng tốt hơn. Beam 600 mỗi lần bấm là quá đắt trên mobile so với rủi ro.
- **Logic sống ở `Domain/`**, không import `UnityEngine` — để `selfcheck.sh` compile và test được ngoài Unity, như `GameMagnet.cs`.
- **Animation để placeholder** ở nhịp đầu, giống `MagnetAnimation`. Chặn input phải chạy thật ngay từ nhịp đó.

## 10. Ngoài phạm vi

- Animation thật (lật thẻ / thẻ bay) — làm ở nhịp riêng.
- Nút trong prefab HUD, entry cheat, entry `t_booster_shuffle` trong `SO_TransactionCatalog` — việc trong Editor.
- Gỡ thế `Stuck`. Lớp trên đầy kín 100% thì Shuffle bó tay, nhưng lúc đó bàn đã `Stuck` và nút vốn đã tắt.
