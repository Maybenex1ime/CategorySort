# Systems Index — WordStack

*Created: 2026-08-03*
*Status: Draft*
*Last updated by: /ccgs-map-systems*

> Nguồn: `design/gdd/game-concept.md` (Approved 2026-08-03). Hành vi tham chiếu:
> `demo/wordstack-clear-demo.html`. Với các hệ thống đã có code, **code là nguồn chân lý** —
> xem cột *Code* và cột *Cách viết* ở mục Design Order.

---

## Overview

| Field | Value |
| ---- | ---- |
| **Total systems** | 21 |
| **MVP systems** | 11 |
| **Vertical Slice systems** | 5 |
| **Alpha systems** | 2 |
| **Full Vision systems** | 3 |
| **Số GDD sẽ viết** | 17 (một số GDD gộp nhiều hệ thống — xem Design Order) |
| **Designed so far** | 0 / 21 |

**Điều kiện đặc biệt của dự án này:** 11/11 hệ thống MVP **đã có code chạy được và đã kiểm chứng**
trong prototype (`Assets/Prototype/`). Khối lượng thiết kế thật sự nằm từ tier Vertical Slice trở đi.

---

## System Enumeration

Cột **Code**: ✅ đã có và đã kiểm chứng · ⚠️ có một phần hoặc chưa nghiệm thu · ❌ chưa có.

| # | System | Category | Description | Source | Code |
| ---- | ---- | ---- | ---- | ---- | ---- |
| 1 | Level Data | Technical | Schema JSON `layout` (pos/hộp/slot) + `meaning` (group/card/text/art), parser, và nội dung thẻ: `id` slug → text/art, nguồn art + license | Explicit | ✅ |
| 2 | Level Validator | Technical | 12 luật chạy lúc load; sai là ném lỗi rõ ràng, không để level hỏng tới tay người chơi | Explicit | ✅ |
| 3 | Save System | Technical | Level đã qua + settings (PlayerPrefs/JSON) | Implicit | ❌ |
| 4 | Stack/Box Board | Gameplay | Hộp `BoxCapacity` slot xếp chồng thành stack; chỉ hộp trên cùng hoạt động; hộp đáy không bao giờ biến mất | Explicit | ✅ |
| 5 | Move Rules | Gameplay | Nước đi duy nhất: kéo thẻ từ hộp trên cùng sang hộp trên cùng stack khác, vào **slot trống đầu tiên**; hộp đầy → từ chối | Explicit | ✅ |
| 6 | CLEAR Rules | Gameplay | Đủ 4 thành viên một group trong cùng một hộp → 4 thẻ biến mất; hộp rỗng không phải đáy thì bị xoá, hộp dưới lộ ra | Explicit | ✅ |
| 7 | Cascade Resolver | Gameplay | `SettleStep` từng nhịp (~350ms) tới khi bàn đứng yên; view có nhịp, input bị khoá | Explicit | ✅ |
| 8 | Win/Stuck Rules | Gameplay | Sạch bàn = thắng. Mọi hộp trên cùng đầy và không nhóm nào hoàn thành được = kẹt. **Không có màn Thua** | Explicit | ✅ |
| 9 | Group Color Hints | Gameplay | Trong cùng một hộp, group có ≥2 thẻ được tô cùng màu. Cấp phát **cục bộ theo hộp**, trả index palette | Explicit | ✅ |
| 10 | Level Solver | Technical | Beam search trên domain thuần, 2 chế độ; level phải giải được ở **chế độ chặt** mới được xuất bản | Explicit | ✅ |
| 11 | Level Progression | Meta | Thứ tự level, mở khoá tuần tự, difficulty curve chỉnh tay | Implicit | ❌ |
| 12 | Level Authoring Tool | Technical | `WordStack ▸ Level Editor` + pipeline validate → solve trước khi ship một level | Implicit | ✅ |
| 13 | Board Presentation | UI | View retained-mode dựng từ 5 prefab; camera fit theo bbox của `pos`; mép hộp dưới lộ ra báo độ sâu | Implicit | ⚠️ |
| 14 | Drag Input Adapter | UI | Hit-test `Zone` từ hình học view, ghost bám tay, highlight đích, snap-back, rung khi hộp đầy | Explicit | ⚠️ |
| 15 | VFX & Game Feel | UI | Juice CLEAR, thẻ bay vào slot, đổi màu khi thành cặp, hộp trượt lên khi lộ ra | Explicit (Pillar 2) | ⚠️ |
| 16 | Game UI Flow | UI | Menu → level select → chơi → thắng/kẹt → next; HUD tiến độ nhóm; toast + Restart | Implicit | ⚠️ |
| 17 | Audio (SFX) | UI | Tiếng cho kéo, thả, từ chối, CLEAR, lộ hộp | Explicit (ghi rõ ngoài phạm vi hiện tại) | ❌ |
| 18 | Tutorial | Meta | 1-2 level đầu tự dạy bằng thiết kế, không popup | Implicit | ❌ |
| 19 | COLLAPSE | Gameplay | Gộp nhóm đệ quy (§R4 GDD gốc). Data đã chừa `group.group`; validate đang **chặn** | Explicit (tier sau) | ❌ |
| 20 | Assist (Undo + Hint) | Gameplay | Gộp một hệ thống — cùng chạm vào lịch sử nước đi; Hint chạy Solver | Explicit (tier sau) | ❌ |
| 21 | Localization | Technical | Slug đã độc lập ngôn ngữ; còn thiếu lớp tra chuỗi hiển thị | Implicit | ❌ |

### Cố ý KHÔNG liệt kê

Anti-pillar trong concept loại thẳng: networking · matchmaking · anti-cheat · analytics ·
energy/lives/gacha · meta thu thập/trang trí · IAP/ads · move budget · timer · blocker · booster.
Settings gộp vào **Save System** — game này gần như không có tuỳ chọn nào.

---

## Dependency Map

Mũi tên = "không có cái này thì không chạy được". Tên tầng giữ theo `docs/development-plan.md`.

### Foundation

- **Level Data** → *(không phụ thuộc gì)*
- **Level Validator** → Level Data
- **Save System** → Level Data *(chỉ ở mức level id — hợp đồng mỏng)*

### Core *(plain C#, không import UnityEngine)*

- **Stack/Box Board** → Level Data
- **Move Rules** → Stack/Box Board
- **CLEAR Rules** → Stack/Box Board
- **Cascade Resolver** → CLEAR Rules · Stack/Box Board
- **Win/Stuck Rules** → Stack/Box Board · CLEAR Rules
- **Group Color Hints** → Stack/Box Board

### Feature

- **Level Solver** → Stack/Box Board · Move Rules · CLEAR Rules · Cascade Resolver · Win/Stuck Rules
- **Level Progression** → Level Data · Save System
- **Level Authoring Tool** → Level Data · Level Validator · Level Solver

### Presentation

- **Board Presentation** → Stack/Box Board · Group Color Hints · Level Data
- **Drag Input Adapter** → Move Rules · Board Presentation · Cascade Resolver
- **VFX & Game Feel** → Board Presentation · Cascade Resolver · CLEAR Rules
- **Game UI Flow** → Win/Stuck Rules · Level Progression · Save System · Board Presentation

### Polish

- **Audio (SFX)** → Cascade Resolver · Drag Input Adapter · VFX & Game Feel
- **Tutorial** → Level Progression · Level Data
- **COLLAPSE** → CLEAR Rules · Level Validator · Level Data
- **Assist (Undo + Hint)** → Move Rules · Cascade Resolver · Stack/Box Board · Level Solver
- **Localization** → Level Data

### Circular Dependencies

**Không có vòng nào.** Hai chỗ *trông như* vòng, đã kiểm và không phải:

- **Drag Input Adapter ↔ Board Presentation** — một chiều. `Zone` (`Rect` hit-test) đọc ra từ
  hình học view, nên drag phụ thuộc view; view hoàn toàn không biết gì về drag. Đây là lý do
  Drag Input Adapter nằm ở Presentation chứ không phải Core: nửa luật đã tách sẵn thành
  **Move Rules** (`Game.MoveTile`, plain C#, dùng chung với Solver).

### Cạnh rủi ro — phụ thuộc ngược chiều tầng

- **Assist (Undo) → `Rules.RemoveEmptyNonBottomBox`** *(hằng trong CLEAR Rules)*. Hiện đây là
  lựa chọn tự do: cả 3 level chạy được ở cả hai chế độ. Làm Undo thì phải chốt cứng, vì hoàn tác
  một nước đã xoá hộp là phải dựng lại hộp đó. **Chưa quyết** — Assist ở tier Full Vision, chốt
  bây giờ là quyết định cho thứ có thể không bao giờ làm. Ghi lại để đừng ai bỏ sót.

### Bottleneck Systems

Đếm theo **phụ thuộc trực tiếp**.

| System | Số hệ thống phụ thuộc | Giảm nhẹ |
| ---- | ---- | ---- |
| **Level Data** | 9 | Đã ổn định qua 3 level + solver + SelfCheck. Sửa schema vẫn là thay đổi đắt nhất trong dự án |
| **Stack/Box Board** | 8 | Đã có code, SelfCheck phủ toàn bộ |
| **CLEAR Rules** | 5 | Đã có code |
| **Cascade Resolver** | 5 | Đã có code |
| **Move Rules** | 3 | Nhỏ, nhưng Solver và người chơi **dùng chung đúng một hàm** — sai một dòng là lệch cả hai, và số nước giải của solver hết còn là bằng chứng |

Cả 5 bottleneck đều đã có code chạy và kiểm chứng — rủi ro thật thấp hơn con số gợi ý.

**Lá nút** (không ai phụ thuộc, thiết kế muộn thoải mái): Localization · Tutorial · Audio ·
Level Authoring Tool · Game UI Flow · Assist · COLLAPSE.

---

## Priority Assignment

Bốn mốc: **MVP** = chơi trọn một level, art placeholder · **Vertical Slice** = một game thật với
vòng đời đầy đủ, ít level · **Alpha** = đủ content · **Full Vision** = tier sau.

| System | Tier | Why |
| ---- | ---- | ---- |
| Level Data | MVP | 9 hệ thống phụ thuộc trực tiếp. Cũng là chỗ Pillar 1 sống: ràng buộc ≥1 thẻ chỉ-ảnh + ≥1 chỉ-chữ ép "chữ và ảnh ngang vai" thành thứ kiểm được, không phải ý định suông |
| Level Validator | MVP | Level sai luật lọt tới tay người chơi thì họ không phân biệt được "mình dốt" với "màn hỏng" — giết Pillar 3 |
| Stack/Box Board | MVP | Bàn cờ. Không có thì không có gì để quyết định |
| Move Rules | MVP | Nước đi duy nhất của game. "Slot trống đầu tiên, người chơi không chọn slot" giữ Pillar 1 — bớt một tầng thao tác để đầu óc dành cho việc đọc nhóm |
| CLEAR Rules | MVP | Kết cục duy nhất của MVP. Đếm *thành viên* chứ không so sức chứa hộp → đổi hộp 16 slot sau này không phải sửa luật |
| Cascade Resolver | MVP | Hộp vừa lộ nổ dây chuyền chính là khoảnh khắc "đã" mà Pillar 2 đòi. Nhịp ~350ms là quyết định thiết kế, không phải chi tiết kỹ thuật |
| Win/Stuck Rules | MVP | Không có màn Thua là quyết định thiết kế — kẹt chỉ ra toast + Restart. Sai luật kẹt là người chơi mất ván oan |
| Group Color Hints | MVP | Cấp màu **cục bộ theo hộp** chứ không toàn cục — cố ý giấu thông tin; màu toàn cục thì nhìn hai hộp là biết ngay cùng nhóm, Pillar 3 hết ý nghĩa |
| Level Solver | MVP | Lưới an toàn cho **rủi ro lớn nhất** trong concept: level tay tạo thế kẹt bất khả kháng. Chính nó bắt được level-1 bản đầu không chơi được |
| Board Presentation | MVP | Không nhìn thấy bàn thì không chơi được. Mép hộp dưới lộ ra là kênh **duy nhất** báo "còn sâu bao nhiêu" — Pillar 3 |
| Drag Input Adapter | MVP | Nửa duy nhất người chơi chạm vào thật. Hộp đầy rung + snap-back là phản hồi từ chối, không phải trang trí |
| VFX & Game Feel | Vertical Slice | Pillar 2 nói thẳng "Juice là phần thưởng". MVP mới chứng minh luật đúng, chưa chứng minh nó *đã*. Design test của pillar chỉ chạy được từ mốc này |
| Game UI Flow | Vertical Slice | Menu → level select → chơi → thắng → next. Không có nó thì có luật chơi chứ chưa có game |
| Save System | Vertical Slice | "Điểm dừng tự nhiên sau mỗi level" trong core loop không tồn tại nếu đóng app là mất tiến độ |
| Level Progression | Vertical Slice | Độ khó tăng dần là cách duy nhất dạy luật mà không cần popup |
| Level Authoring Tool | Vertical Slice | Người chơi không thấy, nhưng nó quyết định làm được bao nhiêu level. Bản cơ bản đã có |
| Tutorial | Alpha | 1-2 level đầu tự dạy bằng thiết kế — đúng tinh thần Pillar 1, dạy bằng bố cục chứ không bằng chữ |
| Audio (SFX) | Alpha | Concept ghi rõ ngoài phạm vi hiện tại. Pillar 2 hụt một nửa nếu CLEAR không có tiếng |
| COLLAPSE | Full Vision | Data đã chừa `group.group`, validate đang chặn để không có nhánh code chưa test |
| Assist (Undo + Hint) | Full Vision | Kéo theo việc chốt cứng `RemoveEmptyNonBottomBox` — xem mục Cạnh rủi ro |
| Localization | Full Vision | Slug đã độc lập ngôn ngữ từ đầu; còn thiếu lớp tra chuỗi hiển thị |

---

## Recommended Design Order

21 hệ thống → **17 GDD**. Thứ tự = dependency sort trong từng tier.

**Cách viết:** *reverse* = `/ccgs-reverse-document` (đọc code + `demo/check.mjs`, viết GDD mô tả
đúng luật đang chạy) · *design* = `/ccgs-design-system` (thiết kế mới).

| # | GDD | Hệ thống gộp | Tier | Cách viết |
| ---- | ---- | ---- | ---- | ---- |
| 1 | `level-data` | Level Data | MVP | reverse |
| 2 | `level-validator` | Level Validator | MVP | reverse |
| 3 | `board-structure` | Stack/Box Board · Move Rules | MVP | reverse |
| 4 | `resolution-rules` | CLEAR Rules · Cascade Resolver · Win/Stuck Rules · Group Color Hints | MVP | reverse |
| 5 | `level-solver` | Level Solver | MVP | reverse |
| 6 | `board-presentation` | Board Presentation | MVP | reverse |
| 7 | `drag-input` | Drag Input Adapter | MVP | reverse |
| 8 | `save-system` | Save System | VS | design |
| 9 | `level-progression` | Level Progression | VS | design |
| 10 | `level-authoring-tool` | Level Authoring Tool | VS | reverse |
| 11 | `vfx-game-feel` | VFX & Game Feel | VS | design *(DOTween + 4 behaviour đã có — reverse một phần)* |
| 12 | `game-ui-flow` | Game UI Flow | VS | design *(HUD đã có)* |
| 13 | `tutorial` | Tutorial | Alpha | design |
| 14 | `audio` | Audio (SFX) | Alpha | design |
| 15 | `collapse` | COLLAPSE | FV | design |
| 16 | `assist` | Assist (Undo + Hint) | FV | design |
| 17 | `localization` | Localization | FV | design |

**Vì sao thứ tự này:**

- `level-data` đi đầu vì 9 hệ thống phụ thuộc trực tiếp — chốt schema xong thì mọi GDD sau chỉ
  việc tham chiếu thay vì tự mô tả lại (và tự mô tả lại là cách drift bắt đầu).
- **8 GDD đầu có code làm nguồn chân lý** → viết nhanh, gần như không có quyết định mở.
  Việc thiết kế thật bắt đầu từ #8 `save-system`.
- Sáu hệ thống Core gộp thành **2 GDD** (`board-structure`, `resolution-rules`) vì cả sáu nằm
  trong cùng `PrototypeDomain.cs` và được cùng một `SelfCheck` phủ. Ranh giới giữa chúng vẫn giữ
  ở mục Enumeration để `/ccgs-create-architecture` còn chỗ chia module. Chia làm hai theo đúng
  chỗ người chơi cảm nhận được: *luật bàn cờ* (cấu trúc + nước đi) và *luật giải quyết*
  (điều gì xảy ra sau mỗi nước).

---

## Progress Tracker

| System | Status | GDD Path | Last Updated |
| ---- | ---- | ---- | ---- |
| Level Data | Not Started | `design/gdd/level-data.md` | — |
| Level Validator | Not Started | `design/gdd/level-validator.md` | — |
| Stack/Box Board | Not Started | `design/gdd/board-structure.md` | — |
| Move Rules | Not Started | `design/gdd/board-structure.md` | — |
| CLEAR Rules | Not Started | `design/gdd/resolution-rules.md` | — |
| Cascade Resolver | Not Started | `design/gdd/resolution-rules.md` | — |
| Win/Stuck Rules | Not Started | `design/gdd/resolution-rules.md` | — |
| Group Color Hints | Not Started | `design/gdd/resolution-rules.md` | — |
| Level Solver | Not Started | `design/gdd/level-solver.md` | — |
| Board Presentation | Not Started | `design/gdd/board-presentation.md` | — |
| Drag Input Adapter | Not Started | `design/gdd/drag-input.md` | — |
| Save System | Not Started | `design/gdd/save-system.md` | — |
| Level Progression | Not Started | `design/gdd/level-progression.md` | — |
| Level Authoring Tool | Not Started | `design/gdd/level-authoring-tool.md` | — |
| VFX & Game Feel | Not Started | `design/gdd/vfx-game-feel.md` | — |
| Game UI Flow | Not Started | `design/gdd/game-ui-flow.md` | — |
| Tutorial | Not Started | `design/gdd/tutorial.md` | — |
| Audio (SFX) | Not Started | `design/gdd/audio.md` | — |
| COLLAPSE | Not Started | `design/gdd/collapse.md` | — |
| Assist (Undo + Hint) | Not Started | `design/gdd/assist.md` | — |
| Localization | Not Started | `design/gdd/localization.md` | — |

---

## Ghi chú mang sang GDD

Những chỗ đã biết trước, ghi ở đây để khỏi phát hiện lại lúc viết GDD.

- **`level-data`** — `Resources/` gói **toàn bộ** thư mục vào build và nạp theo string lúc chạy
  (`Resources.Load<Sprite>("Art/" + key)`). 12 file thì không sao; lên vài trăm thẻ thì đây là chỗ
  đổi sang SpriteAtlas + map ScriptableObject, hoặc Addressables. Ghi thành mục *known ceiling*,
  chưa sửa.
- **`level-data`** — nguồn art thật + license **chưa chốt**. 12 PNG hiện tại là placeholder có nhãn.
  Nếu dùng emoji set mở (Twemoji/OpenMoji) thì ghi công theo license.
- **`resolution-rules`** — `Rules.RemoveEmptyNonBottomBox = true` là lựa chọn tự do, không phải
  luật bắt buộc. Ghi rõ nó là lựa chọn và ghi cạnh phụ thuộc tới Assist(Undo).
- **`drag-input`** — chưa ai kéo tay qua kéo-thả / cascade / Won / Stuck trên bàn prefab mới.
  GDD viết được từ code, nhưng **acceptance vẫn treo**.
- **`board-presentation`** — ba chỗ lệch so với draft đã ghi ở Mục 8 của
  `docs/architecture/view-prefabs.md`, mang nguyên sang GDD.
- Câu hỏi mở Q1-Q14 nằm ở `docs/wordstack-design-log.md` Mục 7 — soát lại khi viết GDD tương ứng.

---

## Director Notes

`production/review-mode.txt` = **lean** → cả ba gate đều **bỏ qua, không phải passed**:

- **TD-SYSTEM-BOUNDARY** — skipped (lean mode). Ranh giới hệ thống chưa qua Technical Director.
- **PR-SCOPE** — skipped (lean mode). Nhận xét thay thế: 11/21 là MVP nhưng **11/11 đã có code**,
  nên khối lượng còn lại thật sự nằm ở Vertical Slice trở đi.
- **CD-SYSTEMS** — skipped (lean mode). Bộ hệ thống chưa qua Creative Director.

Muốn có chữ ký chính thức thì chạy `/gate-check systems-design` (kích hoạt CD-SYSTEMS +
TD-SYSTEM-BOUNDARY) — bắt vấn đề scope và ranh giới trước khi chúng bị khoá vào 17 tài liệu.
