# Booster Shuffle — thiết kế lại + bàn chết

Ngày chốt: 2026-10-02 · Trạng thái: đã duyệt thiết kế · Nhánh `feat/shuffle-redesign`

Sửa đổi spec gốc `2026-08-26-shuffle-booster-design.md`. Phần nào không nhắc ở đây thì giữ như spec gốc
(nguyên tắc "không phát clear miễn phí", Nhóm mồi 3+1, gom cụm, donor ở layer dưới, không tính là nước đi).

## 1. Vì sao làm lại

Phân tích ngày 2026-10-02 tìm ra các trường hợp Shuffle không dựng nổi Nhóm mồi hoặc thất bại cả lượt:

| # | Trường hợp | Trước | Sau |
|---|---|---|---|
| 1 | Nhóm có thẻ băng / đóng đinh | Loại cả nhóm | Thẻ đó làm **mốc** hộp chủ (Mục 2, 3) |
| 2 | Không hộp nào trống đủ 4 ô làm hộp chủ | Bó tay | Dựng quanh cụm có sẵn, hoặc **dời một cụm đôi** giải phóng hộp (Mục 3) |
| 3 | Không có hộp mang thẻ thứ 4 | Bó tay | Giữ nguyên — trường hợp này hoặc là bàn chết (Mục 8 bắt), hoặc người chơi tự đi tiếp được |
| 4 | Thẻ của nhóm đang nằm trong cụm người chơi đã gom | Bó tay | Cụm đó thành mốc; hai cụm đôi thì dời một thẻ (Mục 3) |
| 5 | Có hộp trên rỗng mà tay cạn | Cả lượt thất bại | **Xé một cụm** để lấp (Mục 4) |
| 6 | Hộp khoá rỗng | Mọi lần bấm thất bại | Bỏ qua hộp khoá (Mục 4) |
| 7 | Xáo xong không thẻ nào đổi chỗ | Vẫn ăn lượt | Coi là thất bại, nút xám từ trước (Mục 6) |
| 8 | Thất bại sau khi đã trừ lượt | Mất lượt mua bằng coin | Không xảy ra được nữa (Mục 6) |

Bàn mà không còn nhóm nào nổ được nữa là **bàn chết**, báo kẹt để hồi sinh bằng nam châm (Mục 8). Shuffle đôi khi cứu
được bàn chết (kéo thẻ chôn lên làm mồi), nhưng đã chốt **không** thêm Shuffle vào hồi sinh — kẹt thì Shuffle và Undo
đều tắt như mọi trường hợp kẹt khác.

## 2. Chọn nhóm mồi

Ứng viên = nhóm có **đủ 4 thẻ trên bàn** (như cũ) và:

- **Không** thẻ nào nằm trong hộp khoá (ở bất kỳ layer nào) — loại cả nhóm.
- **Tối đa một** thẻ bất động (băng hoặc đóng đinh), và thẻ đó phải ở **lớp trên**. Băng bị chôn không làm mốc được.

Thứ tự thử:

1. Nhóm không băng trước.
2. Giữa các nhóm băng: băng còn ít nước tan hơn trước — mồi có băng chỉ nổ khi băng tan (thẻ băng không tính bộ 4).
3. Nhiều thẻ sẵn ở lớp trên hơn trước.
4. Hoà thì theo group id.

Thử **lần lượt mọi ứng viên** cho tới khi dựng đủ 3 mồi (trước: chỉ thử 3 ứng viên đầu).

## 3. Hộp chủ

Sau khi nhấc mọi thẻ trắng vào tay, thẻ nhóm G còn nằm ở lớp trên đều là **mốc** — thứ Shuffle không được dời: thẻ
băng, thẻ đóng đinh, cụm người chơi đã gom. Chọn hộp chủ theo thứ tự:

1. **Mốc gọn trong một hộp** (1–3 thẻ): hộp đó là hộp chủ, mốc tính vào 3 thẻ. Hộp cần `4 − số mốc` ô mở
   (thẻ còn thiếu + 1 ô chừa trống). Cụm 3 sẵn + ô trống thì chỉ cần đặt thẻ thứ 4 sang hộp khác.
2. **Hai cụm đôi G ở hai hộp**, không thẻ nào băng/đóng đinh: dời một thẻ sang hộp kia thành cụm 3, thẻ ở lại là thẻ
   thứ 4. Hộp nhận cần 2 ô mở; hộp nhiều ô mở hơn làm hộp chủ, hoà lấy stack nhỏ.
   Ví dụ: `A[chó chó · ·] B[chó chó đỏ đỏ]` → `A[chó chó chó ·] B[chó · đỏ đỏ]`.
3. **Không mốc**: hộp có đủ 4 ô mở, ưu tiên layer 2 nhiều thẻ nhất (như cũ).
4. **Không hộp nào đủ 4 ô → dời một cụm đôi**:
   - Hộp X: thứ duy nhất chiếm chỗ là **đúng một cụm đôi** (không băng/đinh), 2 ô còn lại mở. Ưu tiên layer 2 nhiều
     thẻ nhất, hoà lấy stack nhỏ.
   - Hộp D nhận cặp: hộp mở khác có ≥ 2 ô mở, **chưa có** thẻ nhóm đó (2 + 2 cùng nhóm là tự nổ), lấy stack nhỏ nhất.
   - Chỉ dời khi sau đó **vẫn còn hộp mang thẻ thứ 4**.
   - Cặp dời nguyên cặp. Cụm 3 không bao giờ dời. Mỗi nhóm mồi tối đa một lần dời.

Mốc phân bố theo hình khác (vd băng ở một hộp + đôi ở hộp khác, ≥ 3 hộp) → bỏ nhóm, thử nhóm sau.
Thẻ băng / đóng đinh **không bao giờ** là thẻ thứ 4 — người chơi không kéo được nó.

Hộp mang thẻ thứ 4: stack khác hộp chủ còn ≥ 1 ô mở, **ưu tiên hộp mở đang rỗng** (thẻ thứ 4 lấp luôn hộp đó;
dồn các thẻ thứ 4 vào chung một hộp là Mục 4 hết thẻ mượn trên bàn thưa). Không có thì nhóm đó thất bại (không xé cụm
để tạo chỗ).

Mọi ô của mồi (mốc, thẻ đặt vào, ô chừa trống, thẻ thứ 4) được giữ chỗ — pha sau không lấp, không xé.

## 4. Mỗi hộp trên đang mở phải còn thẻ

- **Hộp khoá bỏ qua** khi xét rỗng: hộp khoá không nhận thẻ, không bị xoá khi rỗng. Hộp khoá rỗng lúc đầu màn là lỗi
  level (`wordstack-rules.md` Mục 11), không phải việc của Shuffle.
- Hộp mở bị rỗng: lấy thẻ trong tay; tay cạn thì mượn thẻ trắng chưa giữ chỗ; vẫn không có thì **xé một cụm** người
  chơi đã gom. Chọn thẻ để xé:
  1. Hộp cho có ≥ 2 thẻ.
  2. Không lấy thẻ băng, đóng đinh, hộp khoá, hay ô đã giữ chỗ cho mồi.
  3. Xé cụm đôi trước cụm ba (cụm ba gần nổ hơn).
  4. Hộp nhiều thẻ nhất trước.
  5. Stack nhỏ nhất, ô nhỏ nhất.

## 5. Bất biến (nới so với spec gốc Mục 5)

1. Tổng thẻ lớp trên không đổi — giữ.
2. Mỗi top box **đang mở** giữ ≥ 1 thẻ — hộp khoá được rỗng.
3. Không hộp nào trên toàn bàn đủ 4 thẻ cùng nhóm — giữ.
4. Thẻ có màu ở lớp trên đứng yên — **trừ** ba loại được dời: cặp dời để giải phóng hộp chủ (vẫn là một cặp ở hộp
   mới), thẻ dời giữa hai cụm đôi cùng nhóm, thẻ bị xé để lấp hộp rỗng.

## 6. Thất bại và nút xám

- `Ok = false` (vỡ bất biến) → thất bại, bàn y nguyên.
- Không thẻ nào đổi chỗ (`Moves` rỗng — bàn đã là kết quả xáo, vd bấm hai lần liên tiếp): người chơi vẫn chọn tiêu
  lượt, nên **đổi chỗ nhóm thẻ giữa các hộp trên**: xoay vòng nguyên bộ thẻ (giữ vị trí ô) giữa các hộp trên đang mở
  và không chứa thẻ băng / đóng đinh. Hộp, khoá hộp, hộp chôn đứng yên; animation vẫn là thẻ bay như mọi lần xáo. Cụm
  đi nguyên bộ nên bất biến 1–3 giữ; bất biến 4 nới cho riêng bước này. Dưới 2 hộp đổi được mới thất bại, bàn y nguyên.
- Nút sáng theo **chạy thử trên bản sao**: `Clone().ApplyShuffle().Ok`, chạy sau mỗi Settle cùng lúc với các nút khác.
  ApplyShuffle xác định (cùng bàn → cùng kết quả), nên nút sáng thì bấm thật luôn thành công. Không cần đường hoàn
  lượt — tầng bàn chơi không với tới `BoosterManager`, và với chạy thử thì nhánh thất bại sau khi trừ lượt không còn
  xảy ra.

## 7. Animation

Thẻ băng, thẻ đóng đinh ở lớp trên và **mọi thẻ trong hộp đang khoá** **đứng yên, không bay vào xoáy**. Mọi thẻ khác
xoáy như cũ (kể cả thẻ không đổi chỗ — giữ nguyên hành vi hiện tại).

## 8. Kẹt mới: thêm bàn chết

`CheckStatus()` (chạy cuối mỗi Settle) báo **Stuck** khi:

- **(1) Không còn nước đi hợp lệ** — như cũ; **hoặc**
- **(2) Bàn chết** — chỉ xét các top box **đang mở** (hộp khoá chỉ mở khi có nhóm được gom, mà bàn chết thì không
  nhóm nào gom được, nên bỏ hẳn chúng ra là chính xác), và cả hai cùng đúng:

| # | Điều kiện |
|---|---|
| D1 | **Không hộp nào làm rỗng được.** Hộp X rút rỗng được khi: không phải hộp đáy, không có thẻ đóng đinh, và tổng ô trống ở các hộp mở khác ≥ số thẻ của X |
| D2 | **Không nhóm nào gom được tại chỗ.** Gom được = đủ 4 thẻ trong các hộp mở VÀ mọi thẻ đóng đinh của nhóm nằm cùng một hộp. Thẻ băng không chặn — còn nước đi là băng còn tan |

Cơ sở của D1: nước đi chỉ dời ô trống, không đổi **tổng** ô trống; rút hết thẻ khỏi X cần đủ chỗ ở các hộp khác
ngay bây giờ. Hộp đáy rỗng không bị xoá nên rút rỗng nó không lộ gì.

Hai điều kiện là phép đếm, không tìm kiếm: **không bao giờ gọi nhầm bàn sống là chết** (Solver cắt nhánh theo đúng
kết quả này nên điều đó là bắt buộc), đổi lại bỏ sót vài bàn chết hiếm — để tool xếp level / solver lo.

Kẹt → popup thua kẹt → hồi sinh = một phát nam châm miễn phí, như cũ. Không có mục tiêu nam châm thì thua.

Ví dụ bàn chết (2 ô trống, không nhóm nào đủ 4 ở lớp trên, 3 thẻ chó còn chôn):

```
A [chó  mèo  gà   ·  ]    hộp trên, có hộp dưới
B [đỏ   đỏ   xanh ·  ]
C [tím  tím  vàng vàng]
```

Vẫn kéo được thẻ qua lại nhưng không hộp nào rút rỗng được, không nhóm nào gom đủ 4 → trước đây người chơi đi mãi tới
hết nước, giờ báo kẹt ngay.

## 9. Kiểm thử

Kiểm mới viết vào `SelfCheck` (chạy ngoài Unity bằng `./selfcheck.sh`, cùng bàn luật + solver lv-009, lv-010):

- **8i Bàn chết:** chết → Stuck dù còn nước; nhóm đủ 4 ở lớp trên → sống; đủ 4 ô trống → sống; ô trống trong hộp khoá
  không tính; thẻ đóng đinh ở hai hộp → chết; thẻ băng không chặn; hộp duy nhất rút được có đinh → chết.
- **8j Chọn mồi:** một thẻ đinh / băng ở lớp trên vẫn nhận; hai thẻ bất động, băng chôn, thẻ trong hộp khoá → loại;
  nhóm băng xếp sau, băng ít nước tan trước.
- **8k Hộp chủ:** mốc đinh / băng làm hộp chủ và đứng yên, không có trong Moves; cụm đôi có sẵn thành hộp chủ; hai
  cụm đôi → đúng một thẻ dời; dời cụm đôi giải phóng hộp chủ, cặp dời vẫn là cặp.
- **8l Lấp hộp rỗng:** xé cụm đôi ở hộp nhiều thẻ nhất, cụm ba giữ nguyên; hộp khoá rỗng bỏ qua và không làm xáo thất bại.
- **8m Nút xám:** bàn không đổi được gì → `ShuffleWouldChange` false, chạy thử không đụng bàn, ApplyShuffle thất bại.
- Hai câu kiểm cũ ở 8g/8h đổi theo luật mới — nhóm có **một** thẻ đóng đinh giờ vẫn làm mồi.

EditMode `WordStack.Board.Tests`: 16 test cũ của `BoardShuffleTests` giữ nguyên và phải xanh (chạy trong Unity).

Play mode (người dùng): bàn chết báo thua kẹt + hồi sinh nam châm; nút Shuffle xám khi xáo không đổi được gì; thẻ
băng/đinh đứng yên lúc xoáy.

## 10. Ngoài phạm vi

- Shuffle làm lựa chọn hồi sinh — đã cân nhắc, bỏ.
- Xé cụm để tạo hộp mang thẻ thứ 4 — bỏ, vì trường hợp đó hoặc là bàn chết (Mục 8) hoặc người chơi tự đi tiếp được.
- Bàn chết mà D1/D2 bỏ sót, hộp khoá rỗng lúc đầu màn, màn kẹt ngay nước đầu — ghi vào việc chờ của tool xếp
  level / solver (`wordstack-rules.md` Mục 11).
- Bản JavaScript (`demo/`) của Shuffle và kẹt.
