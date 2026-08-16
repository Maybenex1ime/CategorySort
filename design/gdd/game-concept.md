# Game Concept — WordStack

> Nguồn luật gốc: `GDD_WordStack.md` (bản 0.1, 2026-07-29).
> Status: **Approved** (2026-08-03 — chơi thử bản Unity trong Editor, user xác nhận "phần game ok").
> Rev 4 (2026-08-02): **pivot khỏi Category Sort**. Toàn bộ luật cũ (chồng thẻ + cột collector +
> quota + move budget) bị thay bằng luật WordStack (hộp 4 slot xếp chồng + gom nhóm 4 thẻ).
> Rev 1-3 mô tả game khác, xem lịch sử git nếu cần.

## Core Identity

| Trường | Giá trị |
|--------|---------|
| Working title | WordStack |
| Elevator pitch | Kéo thẻ giữa những chiếc hộp xếp chồng, gom đủ 4 thẻ cùng chủ đề vào một hộp để chúng biến mất — hộp trống lùi ra, hộp bên dưới lộ ra. |
| Core verb | **Sort** (kéo-thả phân loại) |
| Core fantasy | "Tôi nhìn ra trật tự trong đống hỗn loạn" |
| Unique hook | Match bằng **ý nghĩa**, và mỗi thẻ có thể là **chữ hoặc hình** — người chơi phải nhận ra chữ "Doctor" và *ảnh* xe cứu thương cùng thuộc một nhóm |
| Primary MDA aesthetic | Challenge (chính), Submission/thư giãn (phụ) |
| Platform | Mobile Android (portrait) — dev/test trên Unity Editor & Windows build |
| Engine | Unity 6000.3.8f1 (pinned — xem `docs/engine-reference/unity/VERSION.md`) |

## Luật chơi cốt lõi

*Nguồn chân lý là hành vi của `demo/wordstack-clear-demo.html` — bản đã chơi thử và duyệt.
Mỗi luật dưới đây có assert tương ứng trong `demo/check.mjs` và trong `SelfCheck` của prototype Unity.*

**Cấu trúc bàn chơi**

- **Tile** = 1 thẻ, thuộc đúng 1 **group**. Hiện ra bằng **chữ, hình, hoặc cả hai**.
- **Slot** = 1 chỗ trong hộp, chứa tối đa 1 thẻ.
- **Box** (hộp) = `BoxCapacity` slot, mặc định **4**, xếp lưới 2×2.
- **Stack** = nhiều hộp xếp chồng cùng một vị trí. `boxes[0]` là **hộp trên cùng**. Chỉ hộp trên cùng: hiện đủ nội dung, nhận kéo/thả, được xét hoàn thành nhóm. Hộp dưới chỉ lộ mép viền, **đã có sẵn thẻ nạp từ level data**, đang bị che.
- Hộp cuối mỗi stack (`IsBottom`) **không bao giờ biến mất**; rỗng thì thành khoảng trống trung chuyển.

**Nước đi — chỉ một loại**

- Kéo **một thẻ bất kỳ trong hộp trên cùng** sang **hộp trên cùng của stack khác**.
- Hợp lệ khi hộp đích còn ≥1 slot trống; đầy → từ chối, thẻ bay về chỗ cũ.
- Thẻ rơi vào **đúng slot người chơi thả trúng** nếu slot đó trống; slot bị chiếm (hoặc thả trúng khe/mép) → **slot trống đầu tiên** (trái→phải, trên→dưới).
- Thả về chính hộp cũ = huỷ. Không kéo/thả được với hộp bị che.
- **Mỗi màn có số nước tối đa** (mặc định 20, chưa per-level) — hết nước chưa thắng là thua. **Không timer.**

**Hoàn thành nhóm → CLEAR**

- Nhóm G hoàn thành khi **một hộp chứa đủ cả 4 thành viên của G cùng lúc**. Luật đếm *thành viên*, không so với sức chứa hộp — nên đổi hộp sang 16 slot sau này không phải sửa luật.
- 4 thẻ biến mất. Hộp rỗng ra: **không phải hộp đáy → hộp bị xoá, hộp dưới lộ ra**; là hộp đáy → hộp ở lại, rỗng.
- Sau **mỗi** nước đi, engine chạy dây chuyền tới khi bàn đứng yên (hộp vừa lộ ra có thể đã chứa sẵn nhóm đủ). Mỗi nhịp cách nhau ~350ms, khoá input trong lúc chạy.

**Màu gợi ý**

- Mặc định thẻ nền xám trắng. **Trong cùng một hộp**, group nào có ≥2 thẻ thì các thẻ đó tô cùng màu; thẻ đứng một mình giữ nền mặc định.
- Màu cấp phát **cục bộ theo từng hộp**, không cố định theo group toàn cục — cố ý, vì màu toàn cục thì nhìn hai hộp là biết ngay chúng cùng nhóm.

**Thắng / kẹt**

- **Thắng**: không còn thẻ nào trên bàn. Hộp đáy rỗng vẫn nằm đó.
- **Kẹt**: mọi hộp trên cùng đều đầy và không nhóm nào hoàn thành được. **Không có màn Thua** — chỉ toast + gợi ý Restart.

### Ngoài phạm vi hiện tại (tier sau)

**COLLAPSE** (4 thẻ gộp thành 1 ô chủ đề thuộc nhóm lớn hơn — §R4 của GDD gốc, chính là "gộp nhóm đệ quy"),
**Undo**, **Hint**, âm thanh, move budget, blocker. Data model đã chừa đường cho COLLAPSE (`group.group` = nhóm cha)
nhưng validate **chặn** nó ở phạm vi này để không có nhánh code chưa test.

## Sai lệch có ý thức so với GDD gốc §6.2

Schema level trong repo **khác** §6.2 của GDD. Lý do và chi tiết đầy đủ ở `docs/wordstack-design-log.md`; tóm tắt:

| GDD §6.2 | Repo | Vì sao |
|---|---|---|
| `groups[].words` là chuỗi chữ; `groupId` suy ra bằng cách **tra label** | Mỗi thẻ có **`id` slug** riêng; `text`/`art` là *cách thể hiện* | Tra theo chữ thì thẻ chỉ-có-ảnh không có gì để tra. Slug cũng độc lập ngôn ngữ — đổi `text` sang tiếng nào cũng không đụng `layout` |
| `groups` + `stacks` phẳng ở gốc | Hai phần lớn **`layout`** (vị trí) và **`meaning`** (thuộc nhóm nào + thể hiện ra sao) | Tách trục không gian khỏi trục ngữ nghĩa |
| Quan hệ cha–con = `G.name ∈ G'.words` (so chuỗi) | Field `group` trên chính entry nhóm | So chuỗi thì đổi một chữ hiển thị là đứt cạnh COLLAPSE trong im lặng |
| Vị trí stack ngầm theo thứ tự mảng | Mỗi stack có **`pos: [x, y]`** số thực | Bố cục là quyết định thiết kế, không phải hệ quả của thứ tự khai báo |
| `boxCapacity` per-level | Hằng `Rules.BoxCapacity` | Luật đếm thành viên nhóm, không so capacity |
| — | **Mỗi thẻ bắt buộc có ≥1 trong `text`/`art`**; mỗi level phải có ≥1 thẻ chỉ-ảnh và ≥1 thẻ chỉ-chữ | Giữ "chữ và ảnh ngang vai" thành thuộc tính kiểm được, không chỉ là ý định |

Hai chỗ GDD tự mâu thuẫn hoặc thiếu, đã chốt cách đọc: điều kiện **kẹt** (§7 vs §R7/E8) và điều kiện **xoá hộp**
khi hộp rỗng do người chơi kéo hết thẻ ra. Xem design log Mục 6-7.

## Core Loop

**30 giây:** Quét các hộp đang mở → đọc màu gợi ý và nội dung thẻ → quyết định kéo thẻ nào sang hộp nào để gom đủ 4 → CLEAR → hộp dưới lộ ra → chuỗi quyết định mới.

**5 phút (level):** 1–3 phút mỗi level. Căng thẳng đến từ số slot trống cạn dần. Choices: gom nhóm nào trước, giữ hộp nào làm chỗ trung chuyển, có nên dồn thẻ vào hộp sắp CLEAR không.

**Session:** chuỗi level, điểm dừng tự nhiên sau mỗi level.

## Pillars

1. **Đọc nhanh, quyết định chậm** — nhận diện nhóm phải tức thì; chiều sâu nằm ở thứ tự nước đi.
   *Design test*: nếu phải nheo mắt mới biết thẻ thuộc nhóm nào → sửa art hoặc sửa chữ.
2. **Juice là phần thưởng** — mỗi lần CLEAR phải "đã".
   *Design test*: phân vân giữa thêm mechanic hay polish cảm giác gom → chọn polish.
3. **Canh bạc đọc được** — layout cố định, không RNG lúc chơi. Thông tin ẩn trong stack là rủi ro *ước lượng được* (mép hộp cho biết còn sâu bao nhiêu).
   *Design test*: người chơi kẹt mà không chỉ ra được quyết định nào dẫn tới đó → level hỏng.

### Anti-pillars

- **KHÔNG timer, KHÔNG move budget** — người chơi suy nghĩ bao lâu tuỳ thích.
- **KHÔNG energy/lives/gacha.**
- **KHÔNG meta trang trí/thu thập.**
- **KHÔNG online/social.**

## Risks

| Risk | Loại | Mitigation |
|------|------|------------|
| Level tay tạo thế kẹt bất khả kháng, hoặc "giải được" nhưng bằng đường không dạy đúng luật | Design (lớn nhất) | **Solver trong SelfCheck** chạy 2 chế độ; bắt buộc giải được ở chế độ **chặt** (mọi hộp ẩn chỉ mở bằng một CLEAR dùng thẻ đang với tới được). Chính lưới này bắt được level-1 bản đầu không chơi được |
| Ảnh nhận diện nhanh hơn chữ → level trộn dễ hơn hẳn level chữ thuần | Design | Cân lại độ khó sau khi có art thật |
| Logic kẹt/thắng sai → người chơi mất ván oan | Technical | SelfCheck phủ toàn bộ luật, chạy được ngoài Unity (`./selfcheck.sh`) |
| Port Unity lệch khỏi demo đã duyệt | Technical | SelfCheck port 1-1 từ `demo/check.mjs`; số nước giải + số nút duyệt của solver phải **trùng khít** hai bên |
| License art | Legal | Chưa chốt nguồn art; nếu dùng emoji set mở (Twemoji/OpenMoji) thì ghi công theo license |

## Visual Identity Anchor

- **Direction**: nền tím đậm, hộp tím nhạt viền xám dày, thẻ trắng bo góc — theo ảnh tham chiếu GDD §9.1.
- **One-line rule**: *Nếu phải nheo mắt để biết thẻ thuộc nhóm nào, art (hoặc chữ) sai.*
- **Color philosophy**: nền và hộp trung tính; màu rực chỉ dùng cho **gợi ý nhóm** (palette §7.1) và VFX CLEAR.

## Next Steps

1. ~~Port sang Unity~~ · ~~Chơi thử trong Editor → Approved~~ — xong 2026-08-03.
2. Chuyển view sang prefab theo `docs/architecture/view-prefabs.md` (đang chờ duyệt thiết kế).
3. Art thật thay 12 PNG trong `Assets/Prototype/Resources/Art/`.
4. `/ccgs-map-systems` — phân rã systems index.
