# Kế hoạch: Progress bar trong màn (Cleared / TotalGroups)

> Viết 2026-08-17, chưa thực thi. Giả định: bar = tiến độ TRONG màn (nhóm đã gom /
> tổng nhóm) — khớp art `Bar - Under.png` / `Bar - Upper.png` trong `_Game/Art/Sprites/`.
> Nếu muốn progression giữa các màn (map) thì kế hoạch này không áp dụng.

## Nguồn dữ liệu — có sẵn, KHÔNG sửa domain

`Game.Cleared` + `Game.TotalGroups` ([Game.cs:57](../../Assets/_Game/Board/Domain/Game.cs))
tăng đúng cả CLEAR lẫn COLLAPSE. Domain không đổi → selfcheck/solver miễn nhiễm.
Việc còn lại là dẫn số qua 4 tầng, theo đúng đường ống moves counter (commit `0ca2d6a`).

## 5 bước (~6 file)

1. **Contracts — LevelEvents.cs**
   - `LevelStartedEvent` thêm `TotalGroups` — bar cần mẫu số lúc dựng bàn, TRƯỚC nước đầu.
     Bắt buộc đi đường LevelSignals: `AppFlowContext` không biết số này (chỉ board parse
     JSON mới biết), nên KHÔNG đi `GameplayStartContext`.
   - `LevelEvaluationEvent` thêm `GroupsCleared`.
   - Đổi ctor là breaking — call site chỉ có BoardController (bắn) + GameplayFlowAdapter (nghe).

2. **BoardController** (2 dòng)
   - `RaiseStarted(levelIndex, g.TotalGroups)` trong `BuildBoard`.
   - `RaiseEvaluationCompleted(..., groupsCleared: g.Cleared)` chỗ settle xong.

3. **GameplayFlowAdapter**
   - `OnLevelStarted`: gọi notify MỚI `NotifyLevelContentReadyAsync(int totalGroups)`
     (thêm vào `IGameplayFlowController` — một method một tham số, không chế struct mới).
   - `OnEvaluationCompleted`: nhét `GroupsCleared = evt.GroupsCleared` vào
     `GameplayEvaluationResult` (thêm field).

4. **WordStackGameplayViewModel**
   - 2 ReactiveProperty mới: `GroupsCleared`, `TotalGroups`, expose qua interface.
   - Reset 0/0 trong `StartLevelAsync`/`ResetLevelAsync`; set total ở notify mới;
     set cleared trong `NotifyEvaluationCompletedAsync`.

5. **GameplayHudView** (theo khuôn `_movesText`)
   - `[SerializeField] Image _progressFill` (sprite `Bar - Upper`, Image type Filled/Horizontal)
     + tuỳ chọn `_progressText` ("2/4").
   - `fillAmount = total > 0 ? (float)cleared / total : 0f`; total = 0 → ẩn cả cụm.
   - Juice: `DOFillAmount(target, 0.25f)` — DOTween sẵn có.

**Wiring Editor (user tự làm):** đặt `Bar - Under` + `Bar - Upper` vào
`GamePlayUIRoot .prefab` (khung `Move & Level Info` có vẻ là chỗ chứa), kéo vào 2 field.

## Edge cases

- Retry cùng màn: reset 0/0 rồi LevelStarted bắn lại total — tự đúng.
- Cheat ép kết quả (`ForceOutcomeAsync`): không có số thật — ép thắng set cleared = total
  cho đẹp, ép thua giữ nguyên (một dòng).
- COLLAPSE: `Cleared++` cả khi gộp nhóm con — bar nhích ở nhịp gộp, đúng cảm giác.
- Bar không cần clamp chiều xuống — cleared chỉ tăng trong một màn.

## Thứ tự + nghiệm thu

1. Contracts → Board → Adapter → VM → HUD; `./compilecheck.sh` sau bước 2 và 5.
2. 1 EditMode test cho VM (notify total + evaluation → RP đúng); selfcheck chạy lại cho chắc (~2s).
3. Editor: Play lv-001 → bar 0/3 lúc dựng → gom Fruit → 1/3 có tween → thắng màn bar đầy.
