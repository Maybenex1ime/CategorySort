# Game Concept — Category Sort (clone)

> Nguồn tham chiếu: *Category Sort* (Lion Studios Plus, Google Play).
> Đây là project clone phục vụ học + xây dựng game hoàn chỉnh theo pipeline CCGS.
> Status: **Draft** — chốt sau khi prototype xác nhận core loop.
> Rev 2 (2026-07-24): sửa lại toàn bộ luật cốt lõi theo phân tích screenshot game gốc
> (mô hình collector/quota + chồng thẻ, thay cho mô hình kệ-3 sai ở Rev 1).
> Rev 3 (2026-07-24): gộp còn 2 loại nước đi (gom / đảo sang slot trống — tray chỉ là
> slot trống sẵn); collector nằm CỘT GIỮA cố định, deck collector góc phải trên lấp ô trống;
> chốt mọi thao tác tốn 1 move.

## Core Identity

| Trường | Giá trị |
|--------|---------|
| Working title | Category Sort |
| Elevator pitch | Solitaire dọn thẻ: đào các chồng thẻ, gom đủ chỉ tiêu từng nhóm — match bằng **ý nghĩa**, không phải bằng hình. |
| Core verb | **Sort** (kéo-thả phân loại) |
| Core fantasy | "Tôi nhìn ra trật tự trong đống hỗn loạn" — thỏa mãn của việc dọn dẹp hoàn hảo |
| Unique hook | Như triple-match, **AND ALSO** item cùng nhóm trông *khác nhau* — nhận diện bằng ngữ nghĩa (3 con rắn khác màu đều là "Snake") |
| Primary MDA aesthetic | Challenge (chính), Submission/thư giãn (phụ) |
| Estimated scope | Small-Medium (~3-4 tuần MVP, solo + AI) |
| Platform | Mobile Android (portrait) — dev/test trên Unity Editor & Windows build |
| Engine | Unity 6000.3.8f1 (pinned — xem `docs/engine-reference/unity/VERSION.md`) |

## Luật chơi cốt lõi (nguồn chân lý cho mọi GDD sau)

*Xác nhận từ screenshot Level 72 của game gốc; mục đánh dấu (?) cần chơi thử để chốt trong bước prototype research.*

- **Bàn chơi**: lưới các **slot**, mỗi slot chứa một **chồng thẻ** (pile) hoặc trống. Chỉ **thẻ trên cùng** của mỗi chồng tương tác được; lấy nó đi thì thẻ bên dưới lộ ra. Thẻ dưới chồng bị **ẩn** (chỉ thấy mép).
- **Thẻ**: mỗi thẻ thuộc đúng **1 category** (VD: Abacus, Snake, Zero Heroes). Item cùng category có art khác nhau.
- **Cột collector (cột giữa)**: cột giữa bàn CHỈ chứa các **ô collector** (~3 ô hoạt động (?); game gốc mở thêm ô bằng ads — Tier 3). Mỗi collector gom 1 category với **quota** (VD 8/12): kéo thẻ đúng category vào → đếm +1, thẻ biến mất. Đủ quota → ô được clear.
- **Deck collector (góc phải trên)**: hàng chờ collector. Bất kỳ khi nào một ô collector trống (do clear, hoặc do mở thêm ô), thẻ collector kế tiếp từ deck **lấp vào ô trống** nếu deck còn thẻ (?: lấp ngay lập tức hay có delay/animation).
- **Chỉ có 2 loại nước đi**:
  1. Kéo thẻ trên cùng của chồng → **collector cùng category**: quota +1, thẻ biến mất.
  2. Kéo thẻ trên cùng của chồng → **slot trống bất kỳ**. Khay 5 slot dưới đáy chỉ là các slot trống sẵn — cùng luật với slot lưới đã cạn thẻ. Thẻ nằm một mình trong slot chính là "thẻ trên cùng của chồng 1 thẻ" nên vẫn gom được như thường (không cần luật riêng cho tray).
- **Move budget**: mỗi level có số nước đi giới hạn (VD 160). **Mọi thao tác đều tốn 1 move như nhau** (đã chốt) — kể cả **kéo thẻ vào collector sai category**: thẻ bật về chỗ cũ nhưng vẫn trừ 1 move (phạt thao tác sai).
- **Cân bằng thẻ–quota**: mỗi level, số thẻ của từng category = quota collector category đó → tổng thẻ = tổng quota của mọi collector. Không có thẻ thừa/junk.
- **Thắng**: deck hết + mọi ô collector được clear (nhờ cân bằng thẻ–quota, thắng đồng nghĩa **dọn sạch bàn**). **Thua**: hết move, hoặc kẹt (hết slot trống + không gom được thẻ nào).
- **Deterministic layout**: bố cục chồng thẻ + thứ tự deck cố định theo level, không RNG lúc chơi — nhưng thông tin ẩn trong chồng khiến việc **đào là một canh bạc có tính toán**.
- Độ khó điều khiển bằng: độ sâu chồng, số category đồng thời, quota, move budget, **thứ tự collector trong deck**, số ô collector, decoy (item dễ nhầm nhóm), và (ngoài MVP) blocker.

### Blocker & meta của game gốc (NGOÀI MVP — Tier 2/3)

Ghi nhận để không thiết kế bít đường: thẻ khóa xích, thẻ băng (mở sau N lượt), slot "Free" mở dần, **mở thêm ô collector bằng ads**, deck dự trữ, booster (hint/undo/magnet), tiền coins. MVP không có các thứ này nhưng kiến trúc Board/Card phải cho phép gắn **trạng thái thẻ** (locked/frozen/normal) và **số ô collector thay đổi** về sau.

## Core Loop

**30 giây (moment-to-moment):** Quét các thẻ trên cùng → quyết định: gom vào ô collector cột giữa / gửi sang slot trống để đào / nhịn chờ collector đúng loại lên từ deck → thẻ mới lộ ra → chuỗi quyết định tiếp theo. Cảm giác đã đến từ: gom chuỗi liên tiếp + đào trúng thẻ đang cần.

**5 phút (level):** Mỗi level 2-4 phút. Căng thẳng tăng dần khi move budget cạn và slot trống cạn dần. Choices: đầu tư move vào đào chồng nào, giữ slot trống nào làm dự phòng, canh thời điểm clear ô để collector kế tiếp lên từ deck.

**Session (10-30 phút):** Chuỗi 3-10 level. Điểm dừng tự nhiên sau mỗi level. Hook quay lại: "level sau nhóm gì, bàn xếp kiểu gì?"

**Progression (ngày/tuần):** Tuyến level tuần tự. Người chơi lớn lên bằng kỹ năng **đọc bàn + quản trị rủi ro đào thẻ + tiết kiệm move**. Game "xong" khi hết level.

## Player Motivation (Self-Determination Theory)

- **Autonomy**: nhiều đường giải — đào chồng nào trước, dùng tray thế nào, gom nhóm nào trước.
- **Competence**: kỹ năng đọc bàn và quản trị rủi ro thấy rõ qua số move còn thừa khi thắng. Thua vì hết move truy được về các nước lãng phí.
- **Relatedness**: không có (offline, single player) — chấp nhận, không bù đắp giả tạo.

## Pillars

1. **Đọc nhanh, quyết định chậm** — Nhận diện category phải tức thì; chiều sâu nằm ở thứ tự nước đi và quản lý move budget.
   *Design test*: nếu một feature/art khiến người chơi nheo mắt mới biết category → sửa hoặc cắt. Mơ hồ chỉ được phép khi là decoy **cố ý**.
2. **Juice là phần thưởng** — Mỗi lượt gom phải "đã" (bay vào collector, đếm số nhảy, hoàn thành quota nổ to).
   *Design test*: phân vân giữa thêm mechanic mới hay polish cảm giác gom hiện tại → chọn polish.
3. **Canh bạc đọc được** — Layout cố định, không RNG lúc chơi. Thông tin ẩn trong chồng là rủi ro *có thể ước lượng* (mép thẻ cho biết độ sâu, quota cho biết còn thiếu bao nhiêu). Người chơi luôn biết mình đang đánh cược gì.
   *Design test*: nếu người chơi thua mà không thể chỉ ra quyết định rủi ro nào của mình dẫn đến đó → level/mechanic đó hỏng. Không bao giờ thêm yếu tố ngẫu nhiên phát sinh lúc chơi.

### Anti-pillars (game này KHÔNG phải)

- **KHÔNG real-time timer** — áp lực duy nhất là move budget (đếm theo lượt, người chơi suy nghĩ bao lâu tùy thích).
- **KHÔNG energy/lives/gacha** — chơi lại thoải mái, thua không mất gì ngoài thời gian.
- **KHÔNG meta trang trí/thu thập** — scope creep kinh điển của thể loại; chỉ cân nhắc sau khi MVP ship.
- **KHÔNG online/social** — offline hoàn toàn.

## Player Types

- **Primary**: người chơi casual puzzle thiên về flow/completion (Quantic Foundry: Strategy thấp-vừa + Completion cao) — chơi để thư giãn có não.
- **Secondary**: Achievers thích thắng dư nhiều move, sau này ăn hệ thống sao/perfect (tier 2).
- **KHÔNG dành cho**: người tìm action/phản xạ, người tìm story, người chơi competitive.
- **Market validation**: Category Sort gốc 1M+ download; họ hàng Goods Sort/Triple Match đã chứng minh thể loại. Với project clone, validation quan trọng là *scope khả thi solo*.

## Scope & Feasibility

- **Art pipeline**: emoji set nguồn mở (Twemoji CC-BY / OpenMoji CC BY-SA) cho toàn bộ thẻ → gần như **zero chi phí vẽ**. Ghi công theo license. Quyết định cuối ở bước art-bible.
- **Content scope MVP**: 20 level tay, 8-10 category, mỗi category ≥6 art khác nhau (~60-80 sprite từ emoji set).
- **MVP definition** (câu hỏi MVP trả lời: *core loop có vui không?*):
  - Core mechanic đầy đủ: lưới slot chồng thẻ + cột collector giữa + deck collector + khay 5 slot trống + move budget
  - Thắng/thua/kẹt đúng luật, save tiến độ, UI tối thiểu (menu → level → win/lose → next)
  - Juice cơ bản: tween kéo/bay vào collector + hiệu ứng hoàn thành quota + SFX
  - KHÔNG: blocker (khóa/băng/free-slot/deck), booster, coins, ads, IAP, tutorial phức tạp (level 1-2 tự dạy bằng thiết kế)
- **Scope tiers**:
  - **Tier 1 — MVP** (3-4 sprint): như trên.
  - **Tier 2 — Depth**: blocker (khóa xích, băng, free-slot, deck dự trữ), booster (undo/hint/magnet), hệ thống sao, 50+ level.
  - **Tier 3 — Release**: economy (coins/shop), tutorial polish, ads/IAP, analytics, store assets.

## Risks

| Risk | Loại | Mitigation |
|------|------|------------|
| Level tay + thông tin ẩn có thể tạo thế kẹt bất khả kháng hoặc move budget lệch | Design (lớn nhất) | **Level Solver** chạy trên domain thuần: xác minh mọi level giải được, đo số move tối thiểu → đặt budget = tối thiểu × hệ số |
| Chi tiết mechanic chưa chắc (số ô collector? deck có lộ thứ tự không? lấp ô ngay hay có delay?) | Design | Chơi game gốc 30-60 phút trong bước prototype research, cập nhật các mục (?) của doc này |
| Match-theo-ngữ-nghĩa gây nhầm lẫn ngoài ý muốn | Design | Category tay chọn, mỗi item thuộc đúng 1 category trong data; decoy là công cụ có chủ đích |
| Logic kẹt/thua sai → người chơi mất ván oan | Technical | TDD EditMode toàn bộ luật (pillar 3 phụ thuộc trực tiếp) |
| License emoji set | Legal | Twemoji/OpenMoji cho phép thương mại kèm ghi công; ghi rõ trong credits |

## Visual Identity Anchor

- **Direction**: "Thẻ bài nổi trên bàn gỗ ấm" (bám theo game gốc: nền gỗ, thẻ trắng bo góc, item chiếm trọn mặt thẻ)
- **One-line rule**: *Nếu phải nheo mắt để biết category, art sai.*
- **Nguyên tắc**:
  1. Item lớn, bão hòa cao trên mặt thẻ trắng; nền gỗ/pastel trầm — contrast phục vụ nhận diện (test: screenshot thu nhỏ 50% vẫn đọc được category).
  2. Mọi phản hồi là chuyển động — kéo, bay vào collector, số quota nhảy, chồng dồn lên đều có tween (test: không có state change nào "teleport").
- **Color philosophy**: nền và mặt bàn trung tính nhường sân khấu cho thẻ; màu rực chỉ dùng cho VFX gom/hoàn thành và CTA. Nhãn collector màu vàng nổi bật (theo game gốc).

## Next Steps (theo pipeline)

1. **Prototype research**: chơi game gốc 30-60 phút — chốt các mục (?) (số ô collector, deck có lộ thứ tự không, lấp ô ngay hay delay, move budget điển hình).
2. **Prototype vứt đi (1-2 ngày)**: 1 scene, luật đầy đủ, 1 level hardcode — xác nhận vui → chốt doc này sang Approved.
3. `/ccgs-map-systems` — phân rã thành systems index.
4. `/ccgs-design-system` từng system theo thứ tự dependency.
