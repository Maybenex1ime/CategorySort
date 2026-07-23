# Kế hoạch phát triển — Category Sort (Unity)

> Clone game **Category Sort** (Lion Studios Plus): puzzle sắp xếp memoji theo danh mục,
> kiểu Solitaire, level tăng dần độ khó với không gian giới hạn.
> Kế hoạch này bám theo pipeline **Claude Cowork Game Studio (CCGS)** đã cài trong `.claude/skills`.

---

## Tóm tắt gameplay cần clone

*(Rev 2 — chốt lại theo phân tích screenshot game gốc; nguồn chân lý chi tiết: `design/gdd/game-concept.md`)*

- Bàn chơi là **lưới các chồng thẻ** (VD 5×4); chỉ thẻ trên cùng tương tác được, thẻ dưới bị ẩn.
- Mỗi thẻ thuộc 1 **category**; item cùng category có art khác nhau (match bằng ngữ nghĩa).
- **Collector**: thẻ gom theo category với **quota** (VD 8/12) — kéo thẻ đúng loại vào để đếm dần, đủ quota thì hoàn thành.
- **Tray** 5 slot dưới đáy làm buffer đào chồng. **Move budget** giới hạn mỗi level.
- Thắng: xong mọi collector. Thua: hết move hoặc kẹt (tray đầy, không còn nước hợp lệ).
- Game gốc còn có blocker (khóa xích, băng, free-slot, deck) + booster + coins → **ngoài MVP**, để Tier 2/3.
- Chơi offline, single player, casual — session ngắn 2-4 phút/level.

---

## Pipeline tổng thể

| # | Giai đoạn | Skill dùng | Output | Thời lượng ước tính |
|---|-----------|-----------|--------|---------------------|
| 0 | Setup | `ccgs-start` | Unity project, cấu trúc thư mục, `production/review-mode.txt` | 0.5 ngày |
| 1 | Concept | `ccgs-brainstorm` | `design/gdd/game-concept.md` (pitch, pillars, core loop, MVP) | 1 ngày |
| 1b | Prototype | *(thủ công — skill `/prototype` chưa có)* | Bản chơi thử drag-sort, trả lời "có vui không?" | 1-2 ngày |
| 2 | Systems Design | `ccgs-map-systems` → `ccgs-design-system` (×N) → `ccgs-consistency-check` | `design/gdd/systems-index.md` + GDD từng system | 2-3 ngày |
| 3 | Architecture | `ccgs-create-architecture` | `docs/architecture/architecture.md` + danh sách ADR | 1-2 ngày |
| 4 | Epics & Stories | `ccgs-create-epics` → stories (thủ công) → `ccgs-story-readiness` → `ccgs-sprint-plan` | `production/epics/`, `production/sprints/sprint-01.md` | 1 ngày |
| 5 | Production | `unity-tdd-workflow` → `ccgs-code-review` + `unity-clean-architecture-review` → `ccgs-story-done` | Code + test, lặp theo story | 3 sprint (~3 tuần) |

Skill dùng ngoài luồng khi cần: `unity-bug-root-cause` (khi có bug), `ccgs-propagate-design-change` (khi sửa GDD), `ccgs-reverse-document` (tài liệu hóa ngược từ prototype), `story-review-test-fix` (review sâu cuối story).

---

## Giai đoạn 0 — Setup (0.5 ngày)

1. Tạo Unity project **2D (URP)** tại `D:\Unity\CategorySort`:
   - Unity **6 LTS** (hoặc 2022.3 LTS nếu muốn ổn định tối đa) — pin version vào `docs/engine-reference/unity/VERSION.md`.
   - Packages: Input System, TextMeshPro, Unity Test Framework. **Không** thêm framework DI (Zenject...) hay asset thừa.
2. `git init` + `.gitignore` chuẩn Unity.
3. Tạo cấu trúc CCGS: `design/gdd/`, `docs/architecture/`, `production/`, và code trong `Assets/`.
4. Ghi `production/review-mode.txt` = `lean` (solo dev — chỉ gate ở chuyển giai đoạn).

## Giai đoạn 1 — Concept (`ccgs-brainstorm`) (1 ngày)

Vì đã có game gốc để tham chiếu, brainstorm chạy nhanh, tập trung vào:
- **Elevator pitch**: "Solitaire dọn thẻ: đào các chồng thẻ, gom đủ chỉ tiêu từng nhóm — match bằng ý nghĩa, không phải bằng hình."
- **Core verb**: *sort* (kéo-thả phân loại).
- **Core loop 30s**: nhìn bàn → nhận diện category → kéo memoji → match nổ → mở khóa nước đi mới.
- **Pillars** (đề xuất, chốt khi chạy skill):
  1. *Đọc nhanh, quyết định chậm* — nhận diện category tức thì, chiến thuật nằm ở thứ tự.
  2. *Juice là phần thưởng* — mỗi match phải "đã" (âm thanh, hiệu ứng).
  3. *Canh bạc đọc được* — layout cố định, không RNG lúc chơi; rủi ro đào chồng luôn ước lượng được.
- **MVP**: 1 mechanic match duy nhất, 20 level, không ads/IAP/meta.
- **Scope tiers**: MVP → thêm booster/undo → polish phát hành.

## Giai đoạn 1b — Prototype vứt đi (1-2 ngày)

Một scene duy nhất, art placeholder (emoji font hệ thống), hardcode 1 level:
kéo-thả + đủ luật core (chồng thẻ, collector/quota, tray, move budget). Mục tiêu duy nhất: **xác nhận core loop vui**
trước khi viết GDD. Code này không mang sang production.

## Giai đoạn 2 — Systems Design (`ccgs-map-systems` → `ccgs-design-system`) (2-3 ngày)

Đề xuất sơ bộ systems index (skill sẽ chốt lại có phê duyệt):

| Layer | System | Ghi chú |
|-------|--------|---------|
| Foundation | **Item/Category Database** | ScriptableObject: category, các art variant |
| Foundation | **Level Data** | ScriptableObject/JSON: lưới chồng thẻ, collector + quota, move budget |
| Foundation | **Save System** | PlayerPrefs/JSON: level đã qua, settings |
| Core | **Pile Board System** | Lưới chồng thẻ, lộ thẻ khi lấy, trạng thái thẻ (thiết kế mở cho blocker Tier 2) — plain C# |
| Core | **Collector System** | Quota, nhận thẻ đúng category, hoàn thành — plain C# |
| Core | **Tray System** | 5 slot buffer, luật chuyển thẻ, điều kiện kẹt — plain C# |
| Core | **Turn & Win/Lose Rules** | Move budget, thắng/thua/kẹt — plain C# |
| Core | **Drag & Drop Input** | MonoBehaviour adapter → gọi domain |
| Feature | **Level Progression** | Mở khóa tuần tự, độ khó tăng dần |
| Feature | **Level Solver/Validator** | Chạy trên domain thuần: xác minh level giải được + đo move tối thiểu → đặt budget |
| Presentation | **Game UI** | Menu, HUD (moves, quota), màn thắng/thua, level select |
| Presentation | **VFX & Audio (Juice)** | Thẻ bay vào collector, quota nhảy số, hoàn thành nổ, SFX |
| Polish (Tier 2) | **Obstacles/Blockers** | Khóa xích, băng, free-slot, deck dự trữ — ngoài MVP |
| Polish (Tier 2) | **Boosters** | Undo/hint/magnet — ngoài MVP |
| Polish | **Tutorial** | 1-2 level đầu tự dạy bằng thiết kế |

Viết GDD theo thứ tự dependency (Foundation → Core → ...), mỗi GDD xong chạy `ccgs-consistency-check` để bắt drift số liệu giữa các doc.

## Giai đoạn 3 — Architecture (`ccgs-create-architecture`) (1-2 ngày)

Các ADR then chốt cần chốt:
1. **Tách domain/engine**: luật chơi (Board, Match) là **plain C# class** không dính MonoBehaviour → test EditMode được (bắt buộc, phục vụ `unity-tdd-workflow`).
2. **Định dạng level data**: ScriptableObject vs JSON — đề xuất ScriptableObject cho editor-friendly.
3. **Input**: Unity Input System, một `DragController` duy nhất.
4. **Giao tiếp giữa systems**: C# events từ domain → presentation lắng nghe (không EventBus framework).
5. **Không thêm dependency ngoài**: DOTween cân nhắc duy nhất cho tween (hoặc tự viết lerp đơn giản).

## Giai đoạn 4 — Epics, Stories, Sprint (1 ngày)

- `ccgs-create-epics layer: foundation` trước, các layer sau chỉ tạo epic khi gần đến (thiết kế sẽ thay đổi).
- Skill `/create-stories` **chưa có trong bộ cài** (gap đã ghi trong README) → viết story thủ công theo format: mô tả, GDD tham chiếu, acceptance criteria, ADR liên quan.
- Mỗi story qua `ccgs-story-readiness` (READY/NEEDS WORK/BLOCKED) trước khi code.
- `ccgs-sprint-plan new` cho Sprint 1.

## Giai đoạn 5 — Production (3 sprint)

Vòng lặp mỗi story: **`unity-tdd-workflow`** (test EditMode trước → code pass → wire MonoBehaviour) → **`ccgs-code-review`** + **`unity-clean-architecture-review`** → **`ccgs-story-done`**.

- **Sprint 1 — Core chơi được**: Item DB, Level Data, Pile Board + Collector + Tray + Turn Rules (TDD đầy đủ: gom đúng/sai category, quota hoàn thành, lộ thẻ dưới, hết move, phát hiện kẹt), Drag & Drop, 3 level test. *Kết quả: chơi được 1 level từ đầu đến cuối, art placeholder.*
- **Sprint 2 — Game hoàn chỉnh tối thiểu**: Level Progression, Save, UI flow (menu → level → thắng/thua → next), 20 level, difficulty curve tay.
- **Sprint 3 — Juice & hoàn thiện**: VFX/SFX/tween, tutorial, polish cảm giác kéo-thả, playtest + sửa theo `unity-bug-root-cause`.

Sau Sprint 3 = bản MVP hoàn chỉnh. Ads/IAP/meta chỉ làm nếu quyết định phát hành (scope tier riêng, ngoài kế hoạch này).

---

## Quy tắc xuyên suốt

- **Không hardcode magic number** — mọi giá trị gameplay lấy từ GDD/ScriptableObject.
- **Domain logic không import UnityEngine** (trừ struct cơ bản) — MonoBehaviour chỉ là adapter mỏng.
- **Mỗi GDD sửa đổi** → chạy `ccgs-propagate-design-change` để soát ADR/story bị ảnh hưởng.
- **Cập nhật `production/session-state/active.md`** đầu và cuối mỗi phiên làm việc.

## Cách chạy các skill

Các skill đã được cài đủ 18/18 vào `.claude/skills/` (vừa giải nén từ các file `.skill`).
**Mở session Claude mới** để chúng được đăng ký, sau đó gọi theo tên, ví dụ: `/ccgs-start`, `/ccgs-brainstorm`, `/ccgs-map-systems`.

Skill được pipeline nhắc đến nhưng **không có trong bộ**: `/setup-engine`, `/prototype`, `/art-bible`, `/gate-check`, `/create-stories`, `/design-review`, `/architecture-decision`, `/architecture-review` — làm thủ công theo mô tả tương đương (đã tính sẵn trong kế hoạch trên).
