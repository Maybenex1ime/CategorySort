# Kế hoạch phát triển — Category Sort (Unity)

> Clone game **Category Sort** (Lion Studios Plus): puzzle sắp xếp memoji theo danh mục,
> kiểu Solitaire, level tăng dần độ khó với không gian giới hạn.
> Kế hoạch này bám theo pipeline **Claude Cowork Game Studio (CCGS)** đã cài trong `.claude/skills`.

---

## Tóm tắt gameplay cần clone

- Bàn chơi gồm các **kệ/slot** chứa các **memoji** thuộc nhiều **category** (cảm xúc, nghề nghiệp, con vật...).
- Người chơi **kéo-thả** memoji giữa các kệ; đủ **3 memoji cùng category trên một kệ → clear** (match).
- Thắng khi clear hết bàn; thua/kẹt khi hết chỗ trống mà không còn nước đi.
- Độ khó tăng: nhiều category hơn, ít slot trống hơn, memoji "gài bẫy" (nhìn giống nhau khác category).
- Chơi offline, single player, casual — session ngắn 1-3 phút/level.

*(Chi tiết mechanic chính xác sẽ được chốt ở giai đoạn Concept + Prototype — chơi thử game gốc để xác nhận luật match, số slot/kệ, có undo/booster không.)*

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
- **Elevator pitch**: "Solitaire sắp xếp emoji: kéo memoji về đúng nhóm, 3 cùng loại thì nổ — dễ học, càng chơi càng xoắn não."
- **Core verb**: *sort* (kéo-thả phân loại).
- **Core loop 30s**: nhìn bàn → nhận diện category → kéo memoji → match nổ → mở khóa nước đi mới.
- **Pillars** (đề xuất, chốt khi chạy skill):
  1. *Đọc nhanh, quyết định chậm* — nhận diện category tức thì, chiến thuật nằm ở thứ tự.
  2. *Juice là phần thưởng* — mỗi match phải "đã" (âm thanh, hiệu ứng).
  3. *Kẹt là do mình* — thua vì quyết định sai, không phải vì may rủi.
- **MVP**: 1 mechanic match duy nhất, 20 level, không ads/IAP/meta.
- **Scope tiers**: MVP → thêm booster/undo → polish phát hành.

## Giai đoạn 1b — Prototype vứt đi (1-2 ngày)

Một scene duy nhất, art placeholder (emoji font hệ thống), hardcode 1 level:
kéo-thả + luật match 3-cùng-category. Mục tiêu duy nhất: **xác nhận core loop vui**
trước khi viết GDD. Code này không mang sang production.

## Giai đoạn 2 — Systems Design (`ccgs-map-systems` → `ccgs-design-system`) (2-3 ngày)

Đề xuất sơ bộ systems index (skill sẽ chốt lại có phê duyệt):

| Layer | System | Ghi chú |
|-------|--------|---------|
| Foundation | **Item/Category Database** | ScriptableObject: category, sprite, id |
| Foundation | **Level Data** | ScriptableObject/JSON: bố cục kệ, memoji ban đầu |
| Foundation | **Save System** | PlayerPrefs/JSON: level đã qua, settings |
| Core | **Board System** | Kệ, slot, trạng thái bàn chơi — plain C#, test được |
| Core | **Match Rules** | Luật 3-cùng-category, phát hiện thắng/thua/kẹt — plain C# |
| Core | **Drag & Drop Input** | MonoBehaviour adapter → gọi Board System |
| Feature | **Level Progression** | Mở khóa tuần tự, độ khó tăng dần |
| Feature | **Difficulty/Level Generator** | Tay hoặc semi-procedural (quyết ở GDD) |
| Presentation | **Game UI** | Menu, HUD level, màn thắng/thua, level select |
| Presentation | **VFX & Audio (Juice)** | Match nổ, tween kéo thả, SFX |
| Polish | **Tutorial** | 1-2 level đầu có hướng dẫn |
| Polish | **Boosters/Undo** | Ngoài MVP |

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

- **Sprint 1 — Core chơi được**: Item DB, Level Data, Board System + Match Rules (TDD đầy đủ: match đúng category, phát hiện thắng, phát hiện kẹt, không match khác category), Drag & Drop, 3 level test. *Kết quả: chơi được 1 level từ đầu đến cuối, art placeholder.*
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
