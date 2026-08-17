# Kế hoạch: Thông tin lớp dưới của stack (Under Tray + tile markers)

> Viết 2026-08-17, **đã thực thi cùng ngày**. Hiển thị trên mỗi stack: (a) còn bao nhiêu
> lớp hộp bên dưới = số sprite `Under Tray` đè lên nhau (tối đa theo số lớp author trong
> prefab, hiện là 4 — sâu hơn vẫn hiện tối đa, bỏ text "+N"); (b) hộp NGAY DƯỚI chứa bao
> nhiêu thẻ = bật bấy nhiêu object Tile nhỏ ở mép Under Tray trên cùng (tối đa 4).

## Vì sao rẻ — không sửa domain

- Nước đi chỉ đụng **top box**, CLEAR xóa hộp trên, COLLAPSE gộp trong hộp trên → ruột
  hộp nằm dưới không bao giờ đổi khi đang nằm dưới. Chỉ cần update ở đúng 2 chỗ vốn gọi
  `ShowDepth`: `BuildBoard` + `RevealBox`.
- Số thẻ hộp dưới = `Rules.BoxCapacity - Game.FreeCount(Boxes[1])` — helper domain có sẵn.

## Đã làm

1. **StackView** — `ShowDepth(int hidden, int tilesInNext)`:
   - `peekLayers` giữ vòng lặp cũ, mảng nâng lên 4 (Peek1–4, sprite Under Tray, author
     trong `Assets/Prefabs/Stack.prefab`); quá số lớp author thì nghiễm nhiên cap.
   - Xóa field + logic `overflow` ("+N").
   - Thêm `nextTileMarkers[4]` — bật `hidden > 0 && i < tilesInNext`, null-safe từng phần
     tử (chưa wire vẫn chạy).
2. **BoardController** — helper `TilesInSecondBox(Stack)`, truyền vào 2 call site.
3. **Stack.prefab** — nối Peek4 vào mảng `peekLayers` (user đã tự dựng Peek1–4).

## Wiring còn lại (user tự làm)

Tạo 4 object Tile nhỏ ở mép Under Tray trên cùng trong `Stack.prefab`, kéo vào
`nextTileMarkers`. Chưa wire thì phần (b) im lặng không hiện — không lỗi.

## Lưu ý layout

`PitchY` chừa 0.62 world unit cho phần lấp ló ([BoardController.cs](../../Assets/_Game/Board/Views/BoardController.cs)
const `PitchY`) — 4 lớp Under Tray thò sâu hơn 3 lớp cũ, nếu đè hàng dưới thì tăng số này.

## Nghiệm thu

- `./compilecheck.sh` OK (chỉ tầng view đổi, selfcheck/solver miễn nhiễm).
- Play lv-001: stack `[0,0]` (2 hộp) hiện 1 Under Tray + 4 tile marker (hộp dưới full);
  stack 1 hộp không hiện gì; clear hộp trên → tắt hết.
