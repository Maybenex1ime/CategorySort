# Kế hoạch phát triển — WordStack (Unity)

> Puzzle sắp xếp: kéo thẻ giữa những chiếc hộp xếp chồng, gom đủ 4 thẻ cùng chủ đề vào một hộp
> để chúng biến mất, hộp bên dưới lộ ra.
> Kế hoạch này bám theo pipeline **Claude Cowork Game Studio (CCGS)** đã cài trong `.claude/skills`.
>
> **2026-08-02 — pivot:** dự án đổi từ *Category Sort* (collector/quota/move budget) sang
> **WordStack**. Mọi mô tả gameplay Rev 3 trở về trước đã hết hiệu lực.

---

## Tóm tắt gameplay

*(Nguồn chân lý chi tiết: `design/gdd/game-concept.md`; hành vi tham chiếu: `demo/wordstack-clear-demo.html`)*

- Bàn chơi là các **stack**, mỗi stack là nhiều **hộp 4 slot xếp chồng**. Chỉ **hộp trên cùng** tương tác được; hộp dưới đã có sẵn thẻ nhưng bị che, chỉ lộ mép viền.
- Mỗi thẻ thuộc đúng 1 **group** (4 thành viên). Thẻ hiện ra bằng **chữ, hình, hoặc cả hai** — match bằng ngữ nghĩa, xuyên cả hai phương thức.
- **Chỉ 1 loại nước đi**: kéo thẻ bất kỳ trong hộp trên cùng sang hộp trên cùng của stack khác, vào **slot trống đầu tiên**. Hộp đích đầy → từ chối. **Không giới hạn nước đi, không timer.**
- Gom đủ **4 thành viên của một group vào cùng một hộp** → **CLEAR**: 4 thẻ biến mất; hộp rỗng mà không phải hộp đáy thì bị xoá, hộp dưới lộ ra. Sau mỗi nước đi engine chạy **dây chuyền** tới khi bàn đứng yên.
- **Màu gợi ý**: trong cùng một hộp, group có ≥2 thẻ được tô cùng màu; cấp phát cục bộ theo từng hộp, không cố định toàn cục.
- **Thắng**: dọn sạch bàn. **Kẹt**: mọi hộp trên cùng đầy và không nhóm nào hoàn thành được — không có màn Thua, chỉ toast + Restart.
- **Ngoài phạm vi hiện tại**: COLLAPSE (gộp nhóm đệ quy), Undo, Hint, âm thanh, blocker, booster.
- Chơi offline, single player, casual — 1-3 phút/level.

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

Đã chạy — kết quả ở `design/gdd/game-concept.md` (Rev 4). Tóm tắt để khỏi mở file:
- **Elevator pitch**: "Kéo thẻ giữa những chiếc hộp xếp chồng, gom đủ 4 thẻ cùng chủ đề vào một hộp để chúng biến mất."
- **Core verb**: *sort* (kéo-thả phân loại).
- **Core loop 30s**: quét các hộp đang mở → đọc màu gợi ý + nội dung thẻ → kéo thẻ gom đủ 4 → CLEAR → hộp dưới lộ ra.
- **Pillars**: *Đọc nhanh, quyết định chậm* · *Juice là phần thưởng* · *Canh bạc đọc được*.
- **MVP**: chỉ kết cục CLEAR, không ads/IAP/meta.
- **Scope tiers**: MVP (CLEAR) → COLLAPSE + Undo/Hint + nhiều level → polish phát hành.

## Giai đoạn 1b — Prototype vứt đi (đang làm)

Hai bước, bước 1 xong:

**Bước 1 — demo HTML (xong, đã duyệt).** `demo/wordstack-clear-demo.html`: 1 file self-contained,
đủ luật core, 2 level, chơi được bằng chuột lẫn ngón tay. `demo/check.mjs` trích engine thẳng từ
file HTML nên test đúng code đang chạy; có beam-search solver chứng minh cả 2 level giải được ở
**cả hai** cách đọc luật xoá hộp. Mục tiêu "core loop có vui không?" — đã trả lời.

**Bước 2 — port sang Unity (đang làm).** Vẫn đúng 3 file trong `Assets/Prototype/`, không thêm
file `.cs` nào, không thêm package nào.

| Phase | Việc | Xong khi |
|---|---|---|
| P-doc | Pivot tài liệu | Không doc nào còn coi collector/quota/move-budget là luật hiện hành |
| P1a | Model + mini JSON reader + validate + build; console main đọc file | `./selfcheck.sh` chạy được, **không cần mở Unity** |
| P1b | Engine + màu + Encode/Score/beam solver + SelfCheck đầy đủ | `./selfcheck.sh` exit 0; số nước giải + số nút duyệt **trùng khít** `demo/check.mjs` |
| P2 | View tĩnh: stack/hộp/mép viền/thẻ, sprite+chữ, camera fit từ bbox của `pos` | Bấm Play thấy lv-001 khớp demo |
| P3 | Kéo thả: zone mới + tái dùng khối game-feel sẵn có | Kéo tay đúng luật, hộp đầy rung + snap-back |
| P4 | Coroutine cascade + khoá input + toast/overlay + settle ngay sau load | Chơi tay thắng cả 2 level trong Editor |

Code này không mang sang production. Ràng buộc quan trọng nhất: `PrototypeDomain.cs`
**không import UnityEngine**, nên luật kiểm được bằng `./selfcheck.sh` trong ~2 giây thay vì
mở Editor — đó là vòng phản hồi nhanh nhất khi sửa luật hoặc sửa level.

## Giai đoạn 2 — Systems Design (`ccgs-map-systems` → `ccgs-design-system`) (2-3 ngày)

Đề xuất sơ bộ systems index (skill sẽ chốt lại có phê duyệt):

| Layer | System | Ghi chú |
|-------|--------|---------|
| Foundation | **Level Data** | JSON: `layout` (stack có `pos`, hộp, slot) + `meaning` (group, card, text/art) |
| Foundation | **Level Validator** | 12 luật, chạy lúc load, sai là ném lỗi rõ ràng — plain C# |
| Foundation | **Save System** | PlayerPrefs/JSON: level đã qua, settings |
| Core | **Stack/Box Board** | Stack các hộp 4 slot, chỉ hộp trên cùng hoạt động, lộ hộp khi xoá — plain C# |
| Core | **CLEAR Rules** | Đếm 4 thành viên nhóm trong 1 hộp, xoá thẻ, xoá hộp, hộp đáy ở lại — plain C# |
| Core | **Cascade Resolver** | `SettleStep` từng bước để view có nhịp + khoá input — plain C# |
| Core | **Win/Stuck Rules** | Sạch bàn = thắng; mọi hộp trên cùng đầy = kẹt — plain C# |
| Core | **Group Color Hints** | Cấp màu cục bộ theo hộp, trả index (màu hex ở view) — plain C# |
| Core | **Drag & Drop Input** | MonoBehaviour adapter → gọi domain |
| Feature | **Level Progression** | Mở khoá tuần tự, độ khó tăng dần |
| Feature | **Level Solver** | Beam search trên domain thuần: xác minh level giải được ở **chế độ chặt** (mọi hộp ẩn chỉ mở bằng một CLEAR dùng thẻ đang với tới được) |
| Feature (tier sau) | **COLLAPSE** | Gộp nhóm đệ quy — data model đã chừa `group.group`, validate đang chặn |
| Presentation | **Game UI** | Menu, HUD (tiến độ nhóm), màn thắng, level select |
| Presentation | **VFX & Audio (Juice)** | Thẻ bay vào slot đích, đổi màu khi thành cặp, CLEAR nổ, hộp trượt lên khi lộ, SFX |
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

- **Sprint 1 — Core chơi được**: Level Data + Validator, Stack/Box Board + CLEAR Rules + Cascade + Win/Stuck + Color Hints (TDD đầy đủ: slot trống đầu tiên, hộp đầy bị từ chối, hộp bị che bất khả xâm, CLEAR + xoá hộp + lộ hộp dưới, CLEAR ở hộp đáy, màu 3 case, thắng/kẹt), Level Solver, Drag & Drop, 3 level. *Kết quả: chơi được 1 level từ đầu đến cuối, art placeholder.*
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
