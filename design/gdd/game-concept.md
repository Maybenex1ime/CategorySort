# Game Concept — Category Sort (clone)

> Nguồn tham chiếu: *Category Sort* (Lion Studios Plus, Google Play).
> Đây là project clone phục vụ học + xây dựng game hoàn chỉnh theo pipeline CCGS.
> Status: **Draft** — chốt sau khi prototype xác nhận core loop.

## Core Identity

| Trường | Giá trị |
|--------|---------|
| Working title | Category Sort |
| Elevator pitch | Solitaire sắp xếp emoji: kéo memoji về đúng nhóm — 3 cùng loại thì nổ. Match bằng **ý nghĩa**, không phải bằng hình. |
| Core verb | **Sort** (kéo-thả phân loại) |
| Core fantasy | "Tôi nhìn ra trật tự trong đống hỗn loạn" — thỏa mãn của việc dọn dẹp hoàn hảo |
| Unique hook | Như Goods Sort, **AND ALSO** item trong cùng nhóm trông *khác nhau* — nhận diện bằng ngữ nghĩa (mèo ≠ mèo giống nhau, mà là "đều là mèo") |
| Primary MDA aesthetic | Challenge (chính), Submission/thư giãn (phụ) |
| Estimated scope | Small (~3 tuần MVP, solo + AI) |
| Platform | Mobile Android (portrait) — dev/test trên Unity Editor & Windows build |
| Engine | Unity 6000.3.8f1 (pinned — xem `docs/engine-reference/unity/VERSION.md`) |

## Luật chơi cốt lõi (nguồn chân lý cho mọi GDD sau)

- Bàn chơi gồm **N kệ**, mỗi kệ **3 slot**.
- Mỗi **memoji** thuộc đúng **1 category** (VD: động vật biển, trái cây, mặt cười, nghề nghiệp).
- Một nước đi = kéo 1 memoji từ kệ bất kỳ sang kệ còn slot trống.
- Kệ chứa **3 memoji cùng category → clear** (bay khỏi bàn, +điểm, giải phóng kệ).
- **Thắng**: bàn sạch. **Kẹt**: mọi slot đầy và không kệ nào có 3 cùng category → thua.
- **Deterministic**: không có yếu tố ngẫu nhiên sau khi level bắt đầu; toàn bộ thông tin hiển thị công khai.
- Độ khó điều khiển bằng: số category đồng thời, số kệ/slot trống, **decoy** (memoji dễ nhầm nhóm — dơi là động vật hay Halloween?), và (ngoài MVP) hàng ẩn phía sau kệ.

## Core Loop

**30 giây (moment-to-moment):** Quét bàn → nhận diện category → chọn memoji → kéo thả → *match nổ* (juice) → kệ trống mở ra nước đi mới. Cảm giác đã đến từ: nhận diện đúng decoy + chuỗi match liên tiếp.

**5 phút (level):** Mỗi level 1-3 phút. Cấu trúc "one more level": màn thắng ngắn gọn → nút Next ngay. Choices ở tầng này: thứ tự giải nhóm nào trước, giữ slot trống nào làm buffer.

**Session (10-30 phút):** Chuỗi 5-15 level. Điểm dừng tự nhiên sau mỗi level. Hook quay lại: "level sau có category gì mới?"

**Progression (ngày/tuần):** Tuyến level tuần tự (level 1 → N). Người chơi lớn lên bằng **kỹ năng đọc bàn** (không có power-up progression trong MVP). Game "xong" khi hết level — content là progression.

## Player Motivation (Self-Determination Theory)

- **Autonomy**: chọn thứ tự giải nhóm, chọn kệ buffer — nhiều đường đến cùng đáp án.
- **Competence**: đường cong khó rõ ràng; người chơi *cảm thấy* mình đọc bàn nhanh hơn qua từng level. Thua luôn truy được về nước đi sai của chính mình.
- **Relatedness**: không có (offline, single player) — chấp nhận, không bù đắp giả tạo.

## Pillars

1. **Đọc nhanh, quyết định chậm** — Nhận diện category phải tức thì; chiều sâu nằm ở thứ tự nước đi.
   *Design test*: nếu một feature/art khiến người chơi nheo mắt mới biết category → sửa hoặc cắt. Mơ hồ chỉ được phép khi là decoy **cố ý**.
2. **Juice là phần thưởng** — Mỗi match phải "đã" (nổ, âm thanh, dồn kệ).
   *Design test*: phân vân giữa thêm mechanic mới hay polish match hiện tại → chọn polish.
3. **Kẹt là do mình** — Không RNG sau khi level bắt đầu, thông tin công khai 100%.
   *Design test*: nếu người chơi thua mà không thể chỉ ra nước sai của mình → level/mechanic đó hỏng.

### Anti-pillars (game này KHÔNG phải)

- **KHÔNG đồng hồ đếm ngược** trong MVP — áp lực thời gian phá pillar 1 (đọc nhanh nhưng *quyết định chậm*).
- **KHÔNG energy/lives/gacha** — phá pillar 3 (thua phải do mình, không phải do hết lượt).
- **KHÔNG meta trang trí/thu thập** — scope creep kinh điển của thể loại; chỉ cân nhắc sau khi MVP ship.
- **KHÔNG online/social** — offline hoàn toàn.

## Player Types

- **Primary**: người chơi casual puzzle thiên về flow/completion (Quantic Foundry: Strategy thấp-vừa + Completion cao) — chơi để thư giãn có não.
- **Secondary**: Achievers thích clear sạch, sau này ăn hệ thống sao/perfect (tier 2).
- **KHÔNG dành cho**: người tìm action/phản xạ, người tìm story, người chơi competitive.
- **Market validation**: Goods Sort, Hexa Sort, Triple Match — thể loại đã chứng minh (game gốc 1M+ download). Với project clone, validation quan trọng là *thể loại này scope nhỏ và khả thi solo*.

## Scope & Feasibility

- **Art pipeline**: emoji set nguồn mở (Twemoji CC-BY / OpenMoji CC BY-SA) cho toàn bộ memoji → gần như **zero chi phí vẽ**. Ghi công theo license. Quyết định cuối ở bước art-bible.
- **Content scope MVP**: 20 level tay, 6-8 category, mỗi category ≥6 memoji khác nhau (~50 sprite, lấy từ emoji set).
- **MVP definition** (câu hỏi MVP trả lời: *core loop có vui không?*):
  - 1 mechanic duy nhất: match-3-cùng-category
  - 20 level, thắng/thua/kẹt, save tiến độ, UI tối thiểu (menu → level → win/lose → next)
  - Juice cơ bản: tween kéo thả + hiệu ứng match + SFX
  - KHÔNG: undo, hint, booster, ads, IAP, tutorial phức tạp (level 1-2 tự dạy bằng thiết kế)
- **Scope tiers**:
  - **Tier 1 — MVP** (3 sprint): như trên.
  - **Tier 2 — Depth**: undo, hint, hàng ẩn sau kệ, kệ khóa, 50+ level, hệ thống sao.
  - **Tier 3 — Release**: tutorial polish, ads/IAP, analytics, store assets, icon.

## Risks

| Risk | Loại | Mitigation |
|------|------|------------|
| Match-theo-ngữ-nghĩa gây nhầm lẫn ngoài ý muốn (dơi = động vật hay Halloween?) | Design (lớn nhất) | Category do tay chọn, mỗi memoji thuộc đúng 1 category trong data; decoy là công cụ thiết kế có chủ đích, test ở prototype |
| Level tay khó cân bằng độ khó | Design | Domain thuần cho phép chạy solver kiểm tra level giải được + đếm số nước tối thiểu |
| Logic phát hiện kẹt sai → người chơi mất ván oan | Technical | TDD EditMode cho toàn bộ luật (pillar 3 phụ thuộc trực tiếp) |
| License emoji set | Legal | Twemoji/OpenMoji đều cho phép thương mại kèm ghi công; ghi rõ trong credits |

## Visual Identity Anchor

- **Direction**: "Emoji nổi trên kệ tối giản"
- **One-line rule**: *Nếu phải nheo mắt để biết category, art sai.*
- **Nguyên tắc**:
  1. Memoji lớn, bão hòa cao, nền pastel dịu — contrast phục vụ nhận diện (test: screenshot thu nhỏ 50% vẫn đọc được category).
  2. Mọi phản hồi là chuyển động — kéo, thả, dồn kệ, nổ match đều có tween (test: không có state change nào "teleport").
- **Color philosophy**: nền và kệ trung tính/pastel nhường sân khấu cho memoji; màu rực chỉ dùng cho VFX match và CTA.

## Next Steps (theo pipeline)

1. **Prototype vứt đi (1-2 ngày)**: 1 scene, luật match đầy đủ, 1 level hardcode — xác nhận vui → chốt doc này từ Draft sang Approved.
2. `/ccgs-map-systems` — phân rã thành systems index.
3. `/ccgs-design-system` từng system theo thứ tự dependency.
