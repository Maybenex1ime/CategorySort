# Session State

> Cập nhật cuối: 2026-07-27. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b — Prototype vứt đi** (theo `docs/development-plan.md`). Concept ở Rev 3, status vẫn **Draft**.
Prototype đã dựng xong và tự kiểm chứng bằng code, **nhưng chưa ai bấm Play chơi thử** — đó là việc còn thiếu duy nhất để đóng giai đoạn này.

## Luật chơi hiện hành (Rev 3)

Nguồn chân lý đầy đủ: `design/gdd/game-concept.md` mục "Luật chơi cốt lõi". Tóm tắt để khỏi phải mở file:

- Lưới slot chứa **chồng thẻ**, chỉ thẻ trên cùng tương tác được; thẻ dưới ẩn.
- **Cột giữa chỉ chứa collector** (3 ô hoạt động). **Deck** góc phải trên lấp collector kế tiếp vào ô vừa clear.
- **Chỉ 2 loại nước đi**: gom thẻ vào collector cùng category, hoặc đảo thẻ sang **slot trống bất kỳ** (khay 5 slot dưới đáy = slot trống sẵn, cùng luật với slot lưới đã cạn).
- **Mọi thao tác tốn 1 move**, kể cả **gom sai category** (thẻ bật về chỗ cũ nhưng vẫn trừ move — phạt).
- **Cân bằng thẻ–quota**: mỗi category số thẻ = quota collector category đó → thắng = dọn sạch bàn, không thẻ thừa.
- Thắng: deck hết + mọi ô collector clear. Thua: hết move, hoặc kẹt (hết slot trống + không gom được).

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `Assets/Prototype/PrototypeDomain.cs` | Toàn bộ luật bằng C# thuần (không import UnityEngine) + Level 1 hardcode + `SelfCheck` kèm DFS solver |
| `Assets/Prototype/PrototypeView.cs` | View tự bootstrap khi bấm Play ở **bất kỳ scene nào** (không cần sửa scene), vẽ bằng code, kéo-thả qua Input System |
| `Assets/Prototype/PrototypeSelfCheckMain.cs` | Entry point chạy SelfCheck ngoài Unity (`#if !UNITY_5_3_OR_NEWER`) |

SelfCheck kiểm: luật gom đúng/sai + phạt move, luật đảo slot, deck lấp ô trống, phát hiện kẹt, hết move,
invariant cân bằng thẻ–quota theo từng category, và **Level 1 giải được trong 23 nước / budget 30 mà bắt buộc phải dùng nước đảo slot**
(nếu chỉ gom thẳng thì không thắng được — đảm bảo level thật sự test cơ chế đào).
SelfCheck tự chạy khi vào Play trong Editor (in ra Console); fail thì `Debug.LogError`.

**Game feel** đã cấy theo `D:\Balatro-Feel` (`Assets/Scripts/CardVisual.cs`), tự viết lerp thay DOTween:
ghost lerp đuổi cursor, xoay Z theo độ trễ chuyển động, tilt lắc sin/cos, scale pop, shadow tách lớp,
thẻ bay vào collector, snap-back khi thả hụt/gom sai, punch collector khi ăn thẻ, hover phồng nhẹ.
Các hằng tinh chỉnh nằm gọn trong khối `// Feel` đầu `PrototypeView.cs`.

## Việc tiếp theo (thứ tự)

1. **Chơi thử** — mở Unity, bấm Play, trả lời câu hỏi duy nhất của giai đoạn 1b: *core loop có vui không?*
2. **Chơi game gốc 30-60 phút** — chốt các mục `(?)` còn lại (xem dưới), cập nhật `Knobs` + concept doc.
3. Vui → concept sang **Approved** → chạy `/ccgs-map-systems` để phân rã systems index.

## Chặn / cần quyết định

*(Không còn blocker kỹ thuật. Chỉ chờ người chơi thử.)*

Unity `6000.3.8f1` đã cài trong Hub, project đã mở (có `Library/`) — khớp `ProjectVersion.txt`
và `docs/engine-reference/unity/VERSION.md`. Mở Unity, bấm Play là chạy.

## Câu hỏi (?) chờ chơi game gốc

- Số ô collector cột giữa (prototype đang 3) — game gốc mở thêm bằng ads, để Tier 3.
- Deck có lộ thứ tự collector kế tiếp cho người chơi thấy không?
- Ô collector clear xong thì deck lấp **ngay lập tức** hay có delay/animation?
- Move budget điển hình theo cỡ level (game gốc ~160 cho level lớn; Level 1 prototype đang 30).

Mọi giả định trên nằm ở khối `Knobs` đầu `PrototypeDomain.cs` — sửa một chỗ, SelfCheck sẽ báo nếu level vỡ.

## Ghi chú kỹ thuật

- **Chạy SelfCheck không cần mở Unity**: dùng Roslyn đi kèm Unity Hub — compile
  `PrototypeDomain.cs` + `PrototypeSelfCheckMain.cs` bằng
  `C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Data\NetCoreRuntime\dotnet.exe`
  với `...\Editor\Data\DotNetSdkRoslyn\csc.dll`, ref tới `NetCoreRuntime\shared\Microsoft.NETCore.App\6.0.21`.
  Hữu ích để kiểm luật nhanh khi Unity chưa mở.
- Emoji trên thẻ render qua `TextMesh` + font hệ thống — có máy hiện ô trống; khi đó vẫn đọc được
  bằng tên item tiếng Việt + màu viền theo category, không ảnh hưởng việc test luật.
- Shader của Balatro-Feel (`BG-Shader` nền twirl, `Edition-Foil/Polychrome/Negative`) **chưa dùng**.
  Prototype hiện không có shader nào. Cân nhắc `BG-Shader` cho Sprint 3 (juice).
