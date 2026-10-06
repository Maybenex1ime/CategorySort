# UI coin animation: count-up, coin fly, Play pulse

Nguồn ý tưởng: `docs/references/mukbang-ui-animation-report.md` (nhánh `origin/claude/mukbang-ui-animation-report`), mục #1 coin bay về ví, #2 số đếm dần, #3 idle pulse.

## Mục tiêu

- **#2 Count-up** — mọi ô số coin (HUD gameplay, Home, Shop) đổi số bằng cách đếm dần trong 0.5 s thay vì nhảy.
- **#1 Coin fly** — coin bay vào ví ở hai chỗ: bấm Claim của `CompletedPopup`, và mua gói coin trong Shop.
- **#3 Idle pulse** — chỉ nút **Play** ở Home thở nhẹ để kéo mắt.

Ngoài phạm vi: nút x2 reward (`OnDoubleReward` vẫn là TODO ở AppFlow), Revive popup, mọi nút khác.

## Thành phần

Hai mảnh dùng chung, đặt ở `Assets/_Game/UI/Common/` (asmdef `WordStack.Meta`).

### `CountUpText` (class C# thường, không phải component)

- `new CountUpText(TMP_Text text, float duration = 0.5f, IMotionScheduler scheduler = null)`.
- `Set(int value)`: lần đầu gán ngay; các lần sau đếm từ số đang hiện tới `value` trong `duration`, ease OutQuad. Đang đếm dở mà có số mới thì huỷ motion cũ, đếm tiếp từ số đang hiện.
- `SetImmediate(int value)`: huỷ motion, gán ngay, không đếm.
- `Dispose()`: huỷ motion.
- Motion gắn `.WithCancelOnError()` và `.AddTo(text.gameObject)` — text bị huỷ thì motion tự dừng, không ném MissingReference.
- `scheduler` chỉ để test (ManualMotionDispatcher); null = scheduler mặc định.

### `CoinFly` (MonoBehaviour, gắn lên root popup/panel)

- Inspector: `_coinSprite`, `_coinSize` (64×64), `_count` (8), `_burstRadius` (80, đơn vị canvas), `_burstDuration` (0.2), `_hold` (0.1), `_flyDuration` (0.5), `_stagger` (0.05), `_doneDelay` (0.15).
- `Play(Vector3 from, Vector3 to, Action onFirstArrive, Action onDone, Action onEachArrive = null)` — `from`/`to` là world position của RectTransform (canvas Overlay: chính là pixel màn hình).
  - Mỗi coin là một `Image` con của CoinFly (tạo lần đầu, dùng lại các lần sau).
  - Coin i: bung từ `from` ra `from + offset_i` (offset ngẫu nhiên trong vòng tròn bán kính `_burstRadius × lossyScale`, `_burstDuration`, OutQuad); chờ `_hold + i × _stagger`; bay tới `to` (`_flyDuration`, InQuad); tới nơi thì ẩn.
  - Coin đầu tới nơi gọi `onFirstArrive` (một lần). Mỗi coin tới nơi gọi `onEachArrive`. Coin cuối tới nơi, chờ `_doneDelay`, gọi `onDone` (một lần).
  - `Play` khi đang chạy: dừng lượt cũ trước (không gọi callback của lượt cũ).
- `Stop()`: huỷ mọi motion, ẩn coin. `OnDisable` gọi `Stop()`.
- `public IMotionScheduler Scheduler { get; set; }` — chỉ để test.

## Dùng ở đâu

| Chỗ | Thay đổi |
|---|---|
| `GameplayHudView._coinText` | subscribe coin → `CountUpText.Set` |
| `MainMenuScreen._coinCountText` | như trên |
| `ShopPopup._coinCounterText` | như trên |
| `ShopPopup.PlayPurchasedFeedback` | `CoinsGranted > 0` và có `_coinFly` → coin bay từ ô vừa mua tới `_coinCounterText`, mỗi coin tới nơi nảy counter. Counter tự đếm dần qua subscribe (coin được cộng trước feedback). Giữ nảy ô vừa mua như cũ. |
| `CompletedPopup` | ô coin mới trong popup + Claim cho coin bay (dưới) |
| Nút Play ở Home | `UIIdlePulseDriver` + profile `SO_Button_CTA` — không đổi code |

## Luồng Claim của `CompletedPopup`

Coin thắng màn đã được `CoinRewardService` cộng lúc `LevelResultEvent`, trước khi popup mở.

1. `Initialize`: nếu có `_coinBoxText`, hiện `max(0, Coins − RewardCoinAmount)` bằng `SetImmediate`. Mở khoá hai nút.
2. Bấm Claim, khi đủ điều kiện (có `_coinFly`, `_coinBoxText`, `_coinBoxIcon`, `_rewardAmountText` và reward > 0):
   - khoá Claim + x2 (bấm lại trong lúc bay không làm gì);
   - `_coinFly.Play(from: _rewardAmountText, to: _coinBoxIcon)`;
   - coin đầu tới → `_coinBox.Set(Coins)` (đếm lên số thật);
   - mỗi coin tới → nảy `_coinBoxIcon` (Punch như Shop);
   - xong → `Dismiss()` rồi `Args.OnClaim()` như hiện tại.
3. Thiếu bất kỳ tham chiếu nào hoặc reward = 0 → Claim đóng popup ngay như hiện tại.

Số coin đọc từ `ICurrencyService.Coins.CurrentValue` qua `[Inject]` (cùng cách `NoHeartsPopup`).

## Việc làm trong Unity (người dùng)

1. `CompletedPopup.prefab`: thêm ô coin (icon + TMP) phía trên, gán `_coinBoxText`, `_coinBoxIcon`; thêm `CoinFly` lên root, gán sprite coin, gán vào `_coinFly`.
2. Shop panel trong `MainMenuScreen.prefab`: thêm `CoinFly`, gán sprite, gán vào `ShopPopup._coinFly`.
3. **Create ▸ LogosSDK ▸ UI ▸ Button Feedback** → `SO_Button_CTA`, tick **Idle Pulse Enabled** (scale 1.06, 0.9 s). Thêm `UIIdlePulseDriver` lên nút **Play**, gán profile.

Code không sửa prefab/scene; chưa gán thì mọi thứ chạy như cũ.

## Kiểm tra

- EditMode (`WordStack.Meta.Tests`), chạy motion bằng `ManualMotionDispatcher`:
  - `CountUpText`: lần đầu gán ngay; đếm qua giá trị giữa rồi đúng đích; đổi đích giữa chừng tiếp tục từ số đang hiện; `SetImmediate` huỷ đếm.
  - `CoinFly`: `onFirstArrive` và `onDone` mỗi cái đúng một lần, `onEachArrive` đúng `_count` lần; coin ẩn hết khi xong; `Play` lần hai không gọi callback lượt một.
- `./compilecheck.sh`, `bash .git/sdd/testcheck.sh`.
- Hình ảnh: người dùng xem trong Play mode.
