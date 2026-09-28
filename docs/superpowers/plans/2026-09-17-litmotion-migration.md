# Chuyển DOTween → LitMotion — kế hoạch thực thi

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gỡ hẳn DOTween khỏi CategorySort, thay bằng LitMotion 2.0.2 ở cả 19 file đang dùng, mang sang bộ công cụ LitMotion của Mukbang ASMR (SacredTimeline, Spiral Move, Bouncing Scale, shim `DO*`), và **mọi animation trông y như trước**.

**Architecture:** LitMotion cài qua git URL khoá commit. Bộ công cụ dùng chung sống ở assembly mới `LogosSDK.Tween` (`Assets/_StudioSDK/Tween/`). Code game gọi thẳng API gốc LitMotion (`LMotion`/`LSequence`) — không đi qua shim `DO*`, vì shim trả `MotionHandle` đã chạy nên không đặt được scheduler unscaled và không cho vào sequence. Migrate theo assembly (UI SDK → Board → Meta/Module → SceneSnapshot), DOTween vẫn cài song song tới Task 7 để mỗi task tự compile được.

**Tech Stack:** Unity 6000.3.8f1 · LitMotion 2.0.2 + LitMotion.Animation (commit `ab6e92bfe78ff911def2fd3c4e9c79bb5a186946`) · Reflex · cổng máy `./compilecheck.sh` + Unity Test Runner (EditMode) · nghiệm thu bằng mắt.

**Nguồn bộ công cụ:** repo `D:\mukbang-asmr` commit `b050e5bb`, thư mục `Assets/_Game/Scripts/Utils/` (`LitMotionExtensions.cs`, `LitAnim/*`, `SacredTimeline.cs`, `Fu.cs`).

## Global Constraints

**Bảng quy đổi — áp cho MỌI dòng tween, đọc kỹ trước khi viết code:**

| DOTween | LitMotion | Vì sao |
|---|---|---|
| không gọi `SetEase` | `.WithEase(Ease.OutQuad)` | `Assets/Resources/DOTweenSettings.asset` có `defaultEaseType: 6` = OutQuad; LitMotion mặc định Linear |
| `useSafeMode: 1` (global) | `.WithCancelOnError()` trên mọi motion con và mọi sequence | target bị huỷ thì huỷ motion thay vì spam lỗi |
| `SetUpdate(true)` | `.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)` (sequence: đặt trong `Run(b => …)`) | chạy theo unscaled time |
| `SetLink(go)` | `.AddTo(go)` — chỉ trên handle ngoài cùng; trong sequence chỉ gắn cho `Run()` | `LinkBehavior.CancelOnDestroy` mặc định |
| `await t.AsyncWaitForCompletion()` | `await handle.WaitAsync()` (Task 2, `LogosSDK.Tween`) | `ToAwaitable()` mặc định **ném** `OperationCanceledException` khi motion bị huỷ; DOTween trả về êm |
| `yield return t.WaitForCompletion()` | `yield return handle.ToYieldInstruction()` | |
| field `Tween`/`Tweener`/`Sequence` + `.Kill()` | field `MotionHandle` + `.TryCancel()` | `TryCancel` an toàn với handle mặc định/đã xong |
| `DOKill(true)` / `DOComplete()` / `DOTween.Kill(id, true)` | lưu handle, `.TryComplete()` | LitMotion không có sổ đăng ký theo target/id |
| `SetEase(Ease.InBack, 0.6f)` | `Bind` tay + `InBack(t, 0.6f)` | `LitMotion.Ease` không nhận overshoot |
| `RotateMode.FastBeyond360` | tween `float` từ `transform.eulerAngles.z` (đọc đúng lúc tạo) tới đích, `BindToEulerAnglesZ` | DOTween cũng lấy góc đầu đã chuẩn hoá về [0,360) |
| `DOLocalRotate(v)` (mode Fast) | `LMotion.Create(Quaternion, Quaternion, dur).BindToLocalRotation` | đường ngắn nhất như DOTween |
| `DOPunch*(v, d, vibrato, elasticity)` | `LMotion.Punch.Create(start, v, d).WithFrequency(vibrato).WithDampingRatio(elasticity)` | **công thức khác** — Task 8 so bằng mắt |
| `DOShakeScale(d, s, vibrato, rand)` | `LMotion.Shake.Create(start, Vector3.one * s, d).WithFrequency(vibrato)` | như trên |

**Ba bẫy của `LSequence` — không tuân là animation sai mà không báo lỗi:**

1. **Giá trị đầu chốt lúc TẠO motion**, không phải lúc nó bắt đầu chạy như DOTween. Trong sequence, nhịp sau trên cùng thuộc tính phải khai `from` = `to` của nhịp trước.
2. **`Append` khác nghĩa.** DOTween `Append`/`AppendInterval` đặt ở **cuối toàn chuỗi** (tính cả nhịp `Join`/`Insert` dài hơn); LitMotion chỉ nối sau chuỗi `Append` trước đó. Quy ước plan này: **chỉ dùng `Insert(vị trí, …)`**, vị trí tính theo nghĩa DOTween (đã tính sẵn trong code từng task).
3. **Không `Destroy` target trong lúc sequence còn chạy.** Sequence vẫn ghi giá trị cho motion con đã xong mỗi frame; target mất là cả chuỗi lỗi, thẻ khác khựng giữa đường bay. Gom GameObject lại, huỷ **sau** `yield return handle.ToYieldInstruction()`.

**Hạ tầng:**

- `Ease` serialize thành số nguyên. `DG.Tweening.Ease` có `Unset = 0` đứng đầu nên **mọi tên lệch 1** so với `LitMotion.Ease` (vd `OutBack` = 27 → 26). Đổi kiểu field mà quên đổi số là ease âm thầm sai. Quy đổi bằng `Temp/remap_ease.py` (Task 3): `0 → 5` (OutQuad), `1..31 → v-1`, `≥ 32` → dừng.
- Chạy `remap_ease.py` **đúng một lần mỗi file** — trước khi chạy phải `git diff --quiet -- <file>` sạch. Unity không được focus trong lúc chạy.
- KHÔNG import UnityEngine trong `Assets/_Game/Board/Domain/` (selfcheck compile cả thư mục).
- `WordStack.Board` chỉ được thêm reference `LitMotion`, `LitMotion.Extensions` — **không** `LogosSDK.Tween` (target `game` của compilecheck là thế giới mscorlib, không kéo Reflex/Audio vào).
- Git: repo đang có 3 file user sửa dở (`AddressableAssetSettings.asset`, 2 font SDF) — **không bao giờ** `git add -A`/`git add .`; chỉ add đúng đường dẫn liệt kê trong task. Commit message tiếng Anh mệnh lệnh, dòng cuối `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`. Push do user làm.
- File `.cs`/`.asmdef` mới: focus Unity cho nó sinh `.meta`, rồi add cả `.meta`. File xoá: xoá cả `.meta`.
- Unity mất focus không compile — sau khi sửa `.cs`, focus Editor, đợi hết spinner rồi mới đọc Console/chạy test.

---

## Vòng phản hồi

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `./compilecheck.sh` | Board + Board.Editor + toàn bộ SDK/meta/module, ngoài Unity | `game.dll OK` `editor.dll OK` `meta.dll OK` |
| Unity Console sau khi compile | cả project, gồm asmdef boundary mà compilecheck không canh | 0 error |
| Test Runner → EditMode → `LogosSDK.Tween.Tests`, `LogosSDK.UI.Tests` | toán Spiral, service UI | tất cả xanh |
| Play `Main.unity` → CHEAT → Level 5 (lv-010) | nghiệm thu mắt | xem Task 8 |

## Bản đồ file

| File | Task | Việc |
|---|---|---|
| `Packages/manifest.json`, `compilecheck.sh` | 1 | cài LitMotion, thêm ref |
| `Assets/_StudioSDK/Tween/**` (mới) | 2 | bộ công cụ + test |
| `Assets/_StudioSDK/UI/{Animation,Components,Transitions}/*.cs`, `LogosSDK.UI.asmdef`, 9 SO `_Shared/UI/Profiles/**`, `MainMenuScreen.prefab` | 3 | migrate UI SDK + đổi số Ease |
| `Assets/_Game/Board/Views/{BoardController,GhostView,BoosterAnimSettings}.cs`, `WordStack.Board.asmdef`, `SO_BoosterAnim.asset` | 4 | migrate bàn chơi |
| `GameplayHudView.cs`, `LoadingScreen.cs`, `CheatToastView.cs`, `BoosterSlotView.cs` + 3 asmdef | 5 | migrate meta/module |
| `Assets/_StudioSDK/Editor/SceneSnapshot/**` | 6 | spawner + bỏ DOTweenKillProcessor |
| `Assets/Plugins/`, `DOTweenSettings.asset`, `Packages/com.brunomikoski.animationsequencer/`, `ProjectSettings.asset`, `UniTask.DOTween.asmdef`, `compilecheck.sh`, `LevelEvents.cs`, docs | 7 | gỡ DOTween + cập nhật ADR |
| — | 8 | nghiệm thu mắt |

---

### Task 0: Mốc chuẩn trước khi đụng code

**Files:** không sửa file nào.

- [ ] **Step 1: Tạo nhánh**

```bash
git -C D:/CategorySort switch -c feat/litmotion-migration
git -C D:/CategorySort status --short
```

Expected: đang ở `feat/litmotion-migration`; `status` chỉ có đúng 3 dòng ` M` của user (AddressableAssetSettings + 2 font SDF).

- [ ] **Step 2: Cổng máy xanh trước khi sửa**

Run: `./compilecheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`. Đỏ ở bước này thì dừng, báo user — không migrate trên nền đang gãy.

- [ ] **Step 3: Quay video mốc bản DOTween**

Mở Unity, Play `Main.unity`. Quay màn hình (Win+Alt+R, Xbox Game Bar) lần lượt mọi mục trong checklist Task 8 Step 2. Lưu **ngoài repo**: `D:/CategorySort-baseline/dotween-<mục>.mp4`. Đây là thước đo duy nhất cho câu "trông y như trước".

---

### Task 1: Cài LitMotion và nối vào compilecheck

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `compilecheck.sh`

**Interfaces:**
- Produces: assembly `LitMotion`, `LitMotion.Extensions`, `LitMotion.Animation` (tên dùng trong asmdef các task sau); biến bash `LITMOTION_REFS` trong `compilecheck.sh`.

- [ ] **Step 1: Thêm package**

Trong `Packages/manifest.json`, thêm hai dòng **đầu** khối `dependencies` (trước `"com.unity.2d.animation"`):

```json
    "com.annulusgames.lit-motion": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion#ab6e92bfe78ff911def2fd3c4e9c79bb5a186946",
    "com.annulusgames.lit-motion.animation": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion.Animation#ab6e92bfe78ff911def2fd3c4e9c79bb5a186946",
```

Burst/Collections/Mathematics đã có sẵn trong `packages-lock.json` (collections 2.6.2 ≥ 1.5.1) — không thêm tay.

- [ ] **Step 2: Cho Unity import**

Focus Unity, đợi Package Manager tải xong và compile hết. Máy cần `git` trong PATH.

Run: `ls Library/ScriptAssemblies | grep -E "^(LitMotion|LitMotion.Extensions|LitMotion.Animation|Unity.Burst|Unity.Collections|Unity.Mathematics)\.dll$"`
Expected: đúng 6 dòng. Console: 0 error.

- [ ] **Step 3: Nối DLL LitMotion vào compilecheck**

Trong `compilecheck.sh`, ngay **sau** dòng `w() { cygpath -w "$1"; }`, chèn:

```bash
# LitMotion + Burst/Collections/Mathematics là package → chỉ có dạng .dll sau khi Editor
# import. Mượn Library/ScriptAssemblies giống INPUTSYS; worktree thì mượn của repo chính.
SA="$PWD/Library/ScriptAssemblies"
[ -d "$SA" ] || SA="$(git rev-parse --git-common-dir)/../Library/ScriptAssemblies"
LITMOTION_REFS=()
for d in LitMotion LitMotion.Extensions LitMotion.Animation Unity.Burst Unity.Collections Unity.Mathematics; do
  [ -f "$SA/$d.dll" ] || { echo "Không thấy $d.dll — mở Unity một lần cho nó import LitMotion."; exit 1; }
  LITMOTION_REFS+=("$SA/$d.dll")
done
```

Trong **ba** khối `{ … } > "$OUT/game.rsp"`, `editor.rsp`, `meta.rsp`: ngay sau dòng `echo "-r:\"$(w "$PWD/Assets/Plugins/Demigiant/DOTween/DOTween.dll")\""` của mỗi khối, thêm:

```bash
  for f in "${LITMOTION_REFS[@]}"; do echo "-r:\"$(w "$f")\""; done
```

(trong khối `meta.rsp` dòng đó thụt 4 dấu cách — giữ đúng thụt lề của khối.)

Xoá hai dòng giờ đã trùng ở đoạn meta (ngay trước `NS_REF=`):

```bash
SA="$PWD/Library/ScriptAssemblies"
[ -d "$SA" ] || SA="$(git rev-parse --git-common-dir)/../Library/ScriptAssemblies"
```

- [ ] **Step 4: Chạy cổng máy**

Run: `./compilecheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`.

Nếu `game` hoặc `editor` báo `CS0012`/`CS1701` nhắc tới `netstandard, Version=2.1.0.0` (LitMotion.dll build theo netstandard2.1, còn hai target này là thế giới mscorlib 4.7.1): **dừng, gửi log cho user** — đổi ref set của compilecheck là quyết định riêng, không tự xoay trong task này.

- [ ] **Step 5: Commit**

```bash
git -C D:/CategorySort add Packages/manifest.json Packages/packages-lock.json compilecheck.sh
git -C D:/CategorySort commit -m "Add LitMotion 2.0.2 packages and wire them into compilecheck" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Bộ công cụ `LogosSDK.Tween` (port từ Mukbang)

**Files:**
- Create: `Assets/_StudioSDK/Tween/LogosSDK.Tween.asmdef`
- Create: `Assets/_StudioSDK/Tween/MotionHandleCompat.cs`
- Create: `Assets/_StudioSDK/Tween/SpiralMotionAdapter.cs`
- Create: `Assets/_StudioSDK/Tween/SpiralMoveAnimation.cs`
- Create: `Assets/_StudioSDK/Tween/TweenFx.cs`
- Create: `Assets/_StudioSDK/Tween/BouncingScaleAnimation.cs`
- Create: `Assets/_StudioSDK/Tween/LitMotionAnimationExtensions.cs`
- Create: `Assets/_StudioSDK/Tween/SacredTimeline.cs`
- Create: `Assets/_StudioSDK/Tween/LitMotionExtensions.cs`
- Test: `Assets/_StudioSDK/Tween/Tests/LogosSDK.Tween.Tests.asmdef`
- Test: `Assets/_StudioSDK/Tween/Tests/EditMode/SpiralMotionAdapterTests.cs`

**Interfaces:**
- Consumes: assembly `LitMotion`, `LitMotion.Extensions`, `LitMotion.Animation` (Task 1).
- Produces (namespace `LogosSDK.Tween`):
  - `static Awaitable WaitAsync(this MotionHandle handle)` — Task 3, 5 dùng.
  - `struct SpiralOption(float revolutions = 1f, OrbitPlane plane = OrbitPlane.XY)`, `readonly struct SpiralMotionAdapter : IMotionAdapter<Vector3, SpiralOption>`.
  - `SpiralMoveAnimation`, `BouncingScaleAnimation` (menu `Custom/Spiral Move`, `Custom/Bouncing Scale`).
  - `static MotionHandle TweenFx.BouncingScale(Transform target, float duration = 0.5f, float strength = 0.2f, Ease ease = Ease.Linear)`.
  - `static IEnumerator RunCoroutine(this LitMotionAnimation animation, bool playForward = true, float delay = 0f)`.
  - `SacredTimeline` (MonoBehaviour), `AnimationSequenceItem`, `AutoPlayMode`.
  - `static class LitMotionExtensions` — 38 hàm `DO*` (ease mặc định OutQuad, khớp DOTweenSettings cũ).

- [ ] **Step 1: Viết test hỏng trước**

`Assets/_StudioSDK/Tween/Tests/LogosSDK.Tween.Tests.asmdef`:

```json
{
    "name": "LogosSDK.Tween.Tests",
    "rootNamespace": "LogosSDK.Tween.Tests",
    "references": [
        "LogosSDK.Tween",
        "LitMotion",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_StudioSDK/Tween/Tests/EditMode/SpiralMotionAdapterTests.cs`:

```csharp
using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace LogosSDK.Tween.Tests.EditMode
{
    [Category("UnitTest")]
    public class SpiralMotionAdapterTests
    {
        // Số liệu thật của TimelineSuccess bên Mukbang (Tray_0/Slot): xoáy 1.5 vòng vào giữa bàn.
        static readonly Vector3 Start = new Vector3(-3f, 0f, 0f);
        static readonly Vector3 End = new Vector3(0f, -2.75f, 0f);

        static Vector3 Eval(Vector3 start, Vector3 end, float progress, SpiralOption.OrbitPlane plane = SpiralOption.OrbitPlane.XY)
        {
            var adapter = default(SpiralMotionAdapter);
            var options = new SpiralOption(1.5f, plane);
            return adapter.Evaluate(ref start, ref end, ref options, new MotionEvaluationContext { Progress = progress });
        }

        [Test]
        public void AtProgressZero_ReturnsStartValue()
        {
            Assert.That(Vector3.Distance(Eval(Start, End, 0f), Start), Is.LessThan(1e-4f));
        }

        [Test]
        public void AtProgressOne_ReturnsEndValue()
        {
            Assert.That(Vector3.Distance(Eval(Start, End, 1f), End), Is.LessThan(1e-4f));
        }

        [Test]
        public void AtHalfway_RadiusIsHalfTheInitialRadius()
        {
            float initial = Vector3.Distance(Start, End);
            Assert.That(Vector3.Distance(Eval(Start, End, 0.5f), End), Is.EqualTo(initial * 0.5f).Within(1e-4f));
        }

        [Test]
        public void StartEqualsEnd_ReturnsEndValue()
        {
            Assert.That(Eval(End, End, 0.3f), Is.EqualTo(End));
        }

        [Test]
        public void XZPlane_KeepsEndY()
        {
            var start = new Vector3(2f, 5f, 0f);
            var end = new Vector3(0f, 1f, 0f);
            Assert.That(Eval(start, end, 0.4f, SpiralOption.OrbitPlane.XZ).y, Is.EqualTo(1f).Within(1e-5f));
        }
    }
}
```

- [ ] **Step 2: Xác nhận test hỏng**

Focus Unity. Expected: Console báo lỗi compile `LogosSDK.Tween.Tests` — không tìm thấy assembly `LogosSDK.Tween` / kiểu `SpiralMotionAdapter`, `SpiralOption`.

- [ ] **Step 3: Tạo assembly và adapter**

`Assets/_StudioSDK/Tween/LogosSDK.Tween.asmdef`:

```json
{
    "name": "LogosSDK.Tween",
    "rootNamespace": "LogosSDK.Tween",
    "references": [
        "LitMotion",
        "LitMotion.Extensions",
        "LitMotion.Animation",
        "UnityEngine.UI",
        "LogosSDK.Audio",
        "Reflex"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_StudioSDK/Tween/SpiralMotionAdapter.cs`:

```csharp
// Chuyển động xoáy ốc: quay quanh điểm đích, bán kính co tuyến tính về 0 nên vật xoáy vào
// đúng endValue. Port từ Mukbang ASMR (Utils/LitAnim/SpiralMotionAdapter.cs @ b050e5bb).
using System;
using LitMotion;
using UnityEngine;

namespace LogosSDK.Tween
{
    [Serializable]
    public struct SpiralOption : IMotionOptions, IEquatable<SpiralOption>
    {
        public float revolutions;
        public OrbitPlane plane;

        public enum OrbitPlane
        {
            XZ = 0,
            XY = 1
        }

        public static SpiralOption Default => new();

        public SpiralOption(float revolutions = 1f, OrbitPlane plane = OrbitPlane.XY)
        {
            this.revolutions = Mathf.Max(0f, revolutions);
            this.plane = plane;
        }

        public readonly bool Equals(SpiralOption other)
        {
            return revolutions.Equals(other.revolutions) && plane == other.plane;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is SpiralOption other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(revolutions, (int)plane);
        }
    }

    public readonly struct SpiralMotionAdapter : IMotionAdapter<Vector3, SpiralOption>
    {
        public Vector3 Evaluate(ref Vector3 startValue, ref Vector3 endValue, ref SpiralOption options, in MotionEvaluationContext context)
        {
            // Lệch từ điểm đích — giữ nguyên góc xuất phát quanh đích.
            Vector3 startOffset = startValue - endValue;
            float initialRadius = startOffset.magnitude;

            if (initialRadius < 0.0001f)
                return endValue;

            float initialAngle = options.plane == SpiralOption.OrbitPlane.XZ
                ? Mathf.Atan2(startOffset.z, startOffset.x)
                : Mathf.Atan2(startOffset.y, startOffset.x);

            float angle = initialAngle + options.revolutions * Mathf.PI * 2f * context.Progress;
            float currRadius = initialRadius * (1f - context.Progress);

            Vector3 offset = options.plane == SpiralOption.OrbitPlane.XZ
                ? new Vector3(Mathf.Cos(angle) * currRadius, 0f, Mathf.Sin(angle) * currRadius)
                : new Vector3(Mathf.Cos(angle) * currRadius, Mathf.Sin(angle) * currRadius, 0f);

            return endValue + offset;
        }
    }
}
```

- [ ] **Step 4: Chạy test**

Focus Unity → Window › General › Test Runner › EditMode → chạy `LogosSDK.Tween.Tests`.
Expected: 5/5 PASS.

- [ ] **Step 5: Các file còn lại của bộ công cụ**

`Assets/_StudioSDK/Tween/MotionHandleCompat.cs`:

```csharp
using LitMotion;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class MotionHandleCompat
    {
        // Thay DOTween AsyncWaitForCompletion: motion bị huỷ (AddTo — object bị destroy) thì await
        // vẫn trả về êm. ToAwaitable() mặc định lại ném OperationCanceledException.
        public static Awaitable WaitAsync(this MotionHandle handle)
        {
            return handle.ToAwaitable(CancelBehavior.None, cancelAwaitOnMotionCanceled: false);
        }
    }
}
```

`Assets/_StudioSDK/Tween/SpiralMoveAnimation.cs`:

```csharp
using System;
using LitMotion.Animation;
using LitMotion.Animation.Components;

namespace LogosSDK.Tween
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Spiral Move")]
    public sealed class SpiralMoveAnimation : TransformPositionAnimationBase<SpiralOption, SpiralMotionAdapter> { }
}
```

`Assets/_StudioSDK/Tween/TweenFx.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class TweenFx
    {
        // Nảy kiểu thạch: bẹt ngang → cao dọc → về gốc, ba nhịp bằng nhau.
        // Port Fu.DOBouncingScale (Mukbang). Toàn Append nên nghĩa giống hệt DOTween.
        public static MotionHandle BouncingScale(Transform target, float duration = 0.5f, float strength = 0.2f, Ease ease = Ease.Linear)
        {
            var original = target.localScale;
            var hs = strength / 2f;
            var t0 = Vector3.Scale(original, new Vector3(1 + hs, 1 - strength, 1 + hs));
            var t1 = Vector3.Scale(original, new Vector3(1 - hs, 1 + hs, 1 - hs));
            float step = duration / 3f;

            return LSequence.Create()
                .Append(LMotion.Create(original, t0, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Append(LMotion.Create(t0, t1, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Append(LMotion.Create(t1, original, step).WithEase(ease).WithCancelOnError().BindToLocalScale(target))
                .Run(b => b.WithCancelOnError());
        }
    }
}
```

`Assets/_StudioSDK/Tween/BouncingScaleAnimation.cs`:

```csharp
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace LogosSDK.Tween
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Bouncing Scale")]
    public sealed class BouncingScaleAnimation : LitMotionAnimationComponent
    {
        [SerializeField] Transform target;
        [SerializeField] float duration = 0.5f;
        [SerializeField] Ease easing = Ease.Linear;
        [SerializeField] float strength = 0.2f;

        Vector3 originalScale;

        public override MotionHandle Play()
        {
            originalScale = target.localScale;
            return TweenFx.BouncingScale(target, duration, strength, easing);
        }

        public override void OnStop()
        {
            if (target != null) target.localScale = originalScale;
        }
    }
}
```

`Assets/_StudioSDK/Tween/LitMotionAnimationExtensions.cs`:

```csharp
using System.Collections;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class LitMotionAnimationExtensions
    {
        // Chờ LitMotionAnimation chạy xong trong coroutine. playForward = false: tua ngược từ cuối
        // bằng PlaybackSpeed = -1. Port Fu.RunCoroutine (Mukbang).
        public static IEnumerator RunCoroutine(this LitMotionAnimation animation, bool playForward = true, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (playForward)
            {
                if (animation.IsActive) animation.Restart();
                else animation.Play();
                yield return new WaitWhile(() => animation.IsPlaying);
            }
            else
            {
                float maxTime = 0f;
                foreach (var component in animation.Components)
                {
                    var handle = component.TrackedHandle;
                    if (!handle.IsActive()) continue;
                    handle.Time = handle.Duration;   // về cuối trước khi tua ngược
                    handle.PlaybackSpeed = -1f;
                    if (handle.Duration > maxTime) maxTime = handle.Duration;
                }
                yield return new WaitForSeconds(maxTime);
            }
        }
    }
}
```

`Assets/_StudioSDK/Tween/SacredTimeline.cs`:

```csharp
// Chuỗi animation dựng trong Inspector: mỗi bước nối tiếp (Append) hoặc chạy cùng bước trước
// (Join), loại bước là LitMotionAnimation / UnityEvent / âm thanh. Port từ Mukbang ASMR
// (Utils/SacredTimeline.cs @ b050e5bb). Khác bản gốc:
//   - bỏ bước Spine/SpineUI: dự án không có Spine;
//   - bỏ nút xem trước trong Editor: dự án không có EditorCoroutines;
//   - bước Audio phát qua IAudioService.PlaySFX(id) thay cho AudioClip;
//   - bước Animation thiếu animation không còn làm treo chuỗi (bản gốc yield break
//     trước activeCount--, đếm không bao giờ về 0).
using System;
using System.Collections;
using LitMotion.Animation;
using LogosSDK.Audio;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace LogosSDK.Tween
{
    public enum AutoPlayMode
    {
        None,
        OnStart,
        OnEnable
    }

    public class SacredTimeline : MonoBehaviour
    {
        [SerializeField] AutoPlayMode autoPlayMode = AutoPlayMode.None;
        [SerializeField] AnimationSequenceItem[] items;

        [Inject] IAudioService _audio;

        public bool IsPlaying { get; private set; }
        int activeCount;

        void OnEnable()
        {
            if (autoPlayMode == AutoPlayMode.OnEnable) Play();
        }

        void Start()
        {
            if (autoPlayMode == AutoPlayMode.OnStart) Play();
        }

        public void Play()
        {
            StartCoroutine(RunSequenceInternal());
        }

        public void Stop()
        {
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item != null && item.animation != null) item.animation.Stop();
            }
        }

        public IEnumerator PlayCoroutine()
        {
            return RunSequenceInternal();
        }

        [ContextMenu("Play")]
        void PlayFromContextMenu()
        {
            if (Application.isPlaying) Play();
        }

        IEnumerator RunSequenceInternal()
        {
            IsPlaying = true;
            var waitForEmpty = new WaitUntil(() => activeCount == 0);

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].sequence == AnimationSequenceItem.SequenceType.Append)
                    yield return waitForEmpty;
                StartCoroutine(PlayItem(items[i]));
            }

            yield return waitForEmpty;
            IsPlaying = false;
        }

        IEnumerator PlayItem(AnimationSequenceItem item)
        {
            activeCount++;
            if (item.delay > 0) yield return new WaitForSeconds(item.delay);
            switch (item.type)
            {
                case AnimationSequenceItem.Type.Animation:
                    if (item.animation != null) yield return item.animation.RunCoroutine(!item.isReverse);
                    break;
                case AnimationSequenceItem.Type.Event:
                    item.unityEvent?.Invoke();
                    break;
                case AnimationSequenceItem.Type.Audio:
                    if (!string.IsNullOrEmpty(item.sfxId)) _audio?.PlaySFX(item.sfxId);
                    break;
            }
            activeCount--;
        }
    }

    [Serializable]
    public class AnimationSequenceItem
    {
        public enum SequenceType
        {
            Append,
            Join
        }

        public enum Type
        {
            Animation,
            Delay,
            Event,
            Audio
        }

        public SequenceType sequence = SequenceType.Append;
        public float delay;
        public Type type;

        public LitMotionAnimation animation;
        public bool isReverse;
        public UnityEvent unityEvent;
        [Tooltip("Id truyền cho IAudioService.PlaySFX")]
        public string sfxId;
    }
}
```

Shim `DO*` — chép nguyên văn từ Mukbang, chỉ bọc namespace (file gốc ở global namespace; để global là mọi file trong project thấy 38 hàm `DO*`):

```bash
mkdir -p Assets/_StudioSDK/Tween
git -C D:/mukbang-asmr show b050e5bb:Assets/_Game/Scripts/Utils/LitMotionExtensions.cs \
  | awk 'NR==5 { print "namespace LogosSDK.Tween"; print "{" } { print } END { print "}" }' \
  > Assets/_StudioSDK/Tween/LitMotionExtensions.cs
head -8 Assets/_StudioSDK/Tween/LitMotionExtensions.cs
grep -c "public static MotionHandle DO" Assets/_StudioSDK/Tween/LitMotionExtensions.cs
```

Expected: `head` in 4 dòng `using`, rồi `namespace LogosSDK.Tween`, `{`, dòng trống, `public static class LitMotionExtensions {`. `grep -c` > 0 (file gốc có 38 tên hàm `DO*`, nhiều overload).

- [ ] **Step 6: Compile và kiểm trong Editor**

Focus Unity. Expected: Console 0 error; test `LogosSDK.Tween.Tests` vẫn 5/5.
Tạo GameObject tạm trong một scene nháp (không lưu scene) → Add Component `LitMotion Animation` → nút thêm component có mục `Custom/Spiral Move` và `Custom/Bouncing Scale`. Xoá GameObject tạm.

Run: `./compilecheck.sh`
Expected: 3 dòng OK (target `meta` compile cả `_StudioSDK/Tween`, bỏ qua `Tests/`).

- [ ] **Step 7: Commit**

```bash
git -C D:/CategorySort add Assets/_StudioSDK/Tween Assets/_StudioSDK/Tween.meta
git -C D:/CategorySort status --short
git -C D:/CategorySort commit -m "Add LogosSDK.Tween: port Mukbang LitMotion toolkit" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

Expected trước commit: `status` chỉ có dòng `A` dưới `Assets/_StudioSDK/Tween` + 3 dòng ` M` cũ của user.

---

### Task 3: Migrate UI SDK + đổi số Ease của profile UI

**Files:**
- Modify: `Assets/_StudioSDK/UI/Animation/UIAnimationService.cs` (viết lại)
- Modify: `Assets/_StudioSDK/UI/Animation/UIPanelAnimationSO.cs`, `UIStaggerAnimationSO.cs`, `UIButtonFeedbackSO.cs` (dòng `using`)
- Delete: `Assets/_StudioSDK/UI/Animation/UIAnimationProfileSO.cs` (+ `.meta`)
- Modify: `Assets/_StudioSDK/UI/Components/UIButtonFeedbackDriver.cs`, `UIIdlePulseDriver.cs`, `UIToggleFeedbackDriver.cs`
- Modify: `Assets/_StudioSDK/UI/Transitions/FadeTransition.cs`, `ScaleTransition.cs`, `SlideTransition.cs`
- Modify: `Assets/_StudioSDK/UI/LogosSDK.UI.asmdef`
- Modify (data): `Assets/_Shared/UI/Profiles/Button/SO_Button_CTA.asset`, `SO_Button_Default.asset`; `Assets/_Shared/UI/Profiles/Panel/SO_Panel_{DropBounce,FadePopupBG,FadeScreen,ScaleFade,SlideDownBox}.asset`; `Assets/_Shared/UI/Profiles/Stagger/SO_Stagger_{Pop,StampDrop}.asset`; `Assets/_Shared/Prefab/Screen/MainMenuScreen.prefab`
- Create (không commit): `Temp/remap_ease.py`

**Interfaces:**
- Consumes: `LogosSDK.Tween.MotionHandleCompat.WaitAsync` (Task 2).
- Produces: `Ease` của `UIPanelAnimationSO.EnterEase/ExitEase`, `UIStaggerAnimationSO.ElementEase`, `UIButtonFeedbackSO.ReleaseEase/ToggleOnEase` giờ là `LitMotion.Ease`. `IUIAnimationService` không đổi chữ ký.

- [ ] **Step 1: Script đổi số Ease**

`Temp/remap_ease.py` (Temp/ đã nằm trong .gitignore):

```python
# Đổi số Ease DOTween → LitMotion trong file YAML của Unity, chỉ đụng các khoá truyền vào.
# DG.Tweening.Ease có Unset = 0 đứng đầu nên mọi tên lệch 1 so với LitMotion.Ease.
# Chạy ĐÚNG MỘT LẦN mỗi file.
#   python Temp/remap_ease.py khoa1,khoa2 file1 file2 ...
import re
import sys

keys, paths = sys.argv[1].split(','), sys.argv[2:]


def remap(v):
    if v == 0:
        return 5        # Unset → DOTweenSettings.defaultEaseType = OutQuad → LitMotion.Ease.OutQuad = 5
    if 1 <= v <= 31:
        return v - 1    # Linear(1)..InOutBounce(31) → Linear(0)..InOutBounce(30)
    sys.exit(f"Ease {v} (Flash/INTERNAL) không có trong LitMotion — dừng")


pat = re.compile(r'^(\s*(?:' + '|'.join(map(re.escape, keys)) + r'): )(\d+)(\r?)$', re.M)
for p in paths:
    with open(p, encoding='utf-8', newline='') as f:
        text = f.read()
    new, n = pat.subn(lambda m: m.group(1) + str(remap(int(m.group(2)))) + m.group(3), text)
    with open(p, 'w', encoding='utf-8', newline='') as f:
        f.write(new)
    print(f"{p}: {n} dòng")
```

- [ ] **Step 2: Đổi số trong 9 profile + MainMenuScreen**

Unity **không** được focus trong bước này.

```bash
UI_ASSETS="Assets/_Shared/UI/Profiles/Button/SO_Button_CTA.asset Assets/_Shared/UI/Profiles/Button/SO_Button_Default.asset Assets/_Shared/UI/Profiles/Panel/SO_Panel_DropBounce.asset Assets/_Shared/UI/Profiles/Panel/SO_Panel_FadePopupBG.asset Assets/_Shared/UI/Profiles/Panel/SO_Panel_FadeScreen.asset Assets/_Shared/UI/Profiles/Panel/SO_Panel_ScaleFade.asset Assets/_Shared/UI/Profiles/Panel/SO_Panel_SlideDownBox.asset Assets/_Shared/UI/Profiles/Stagger/SO_Stagger_Pop.asset Assets/_Shared/UI/Profiles/Stagger/SO_Stagger_StampDrop.asset"
MENU=Assets/_Shared/Prefab/Screen/MainMenuScreen.prefab
git diff --quiet -- $UI_ASSETS $MENU && echo CLEAN
grep -c "_enterEase:" $MENU
python Temp/remap_ease.py _enterEase,_exitEase,_elementEase,_releaseEase,_toggleOnEase $UI_ASSETS $MENU
grep -hn -E "_(enter|exit|element|release|toggleOn)Ease:" $UI_ASSETS $MENU
```

Expected: `CLEAN`; `grep -c` = `1` (prefab chỉ có đúng một FadeTransition — khác 1 thì dừng, prefab có component khác trùng tên khoá). Sau remap:

| File | Giá trị mới |
|---|---|
| SO_Button_CTA, SO_Button_Default | `_releaseEase: 26`, `_toggleOnEase: 26` (OutBack) |
| SO_Panel_DropBounce, ScaleFade, SlideDownBox | `_enterEase: 26` (OutBack), `_exitEase: 25` (InBack) |
| SO_Panel_FadePopupBG, FadeScreen | `_enterEase: 8` (OutCubic), `_exitEase: 7` (InCubic) |
| SO_Stagger_Pop, StampDrop | `_elementEase: 26` |
| MainMenuScreen.prefab | `_enterEase: 5` (Unset → OutQuad), `_exitEase: 7` |

Sai một giá trị: `git checkout -- <file>` rồi chạy lại **riêng file đó**.

- [ ] **Step 3: Đổi kiểu Ease trong ba SO và xoá profile cũ**

Trong `UIPanelAnimationSO.cs`, `UIStaggerAnimationSO.cs`, `UIButtonFeedbackSO.cs`: thay dòng `using DG.Tweening;` bằng `using LitMotion;`. Tên giá trị mặc định (`Ease.OutBack`, `Ease.InBack`) có sẵn trong `LitMotion.Ease` nên không sửa gì khác.

`UIAnimationProfileSO.cs` tự ghi "Delete this file … once existing prefab references have been re-wired" — xác nhận không còn asset nào trỏ tới rồi xoá:

```bash
grep -rl --include=*.asset --include=*.prefab --include=*.unity d1a9edef961c04efca7b4ef56dc86ce4 Assets || echo "NO REFS"
rm Assets/_StudioSDK/UI/Animation/UIAnimationProfileSO.cs Assets/_StudioSDK/UI/Animation/UIAnimationProfileSO.cs.meta
```

Expected: `NO REFS`.

- [ ] **Step 4: Viết lại `UIAnimationService.cs`**

```csharp
using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
using LogosSDK.Core.Logging;
using LogosSDK.Tween;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.UI.Animation
{
    public sealed class UIAnimationService : IUIAnimationService
    {
        // SetUpdate(true) của bản DOTween = unscaled time. CancelOnError: target bị huỷ giữa
        // chừng thì huỷ cả chuỗi (bản DOTween dựa vào safe mode).
        private static readonly Action<MotionBuilder<double, NoOptions, DoubleMotionAdapter>> Unscaled =
            b => b.WithCancelOnError().WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);

        private readonly UIAnimationSettingsSO _settings;
        private readonly ILogger _logger = LogManager.GetLogger<UIAnimationService>();

        private UIPanelAnimationSO _cachedPanelProfile;
        private UIPanelAnimationSO _cachedPopupProfile;
        private UIButtonFeedbackSO _cachedButtonProfile;

        public UIAnimationService(UIAnimationSettingsSO settings)
        {
            _settings = settings;
        }

        public UIPanelAnimationSO GetPanelProfile(bool isPopup)
        {
            if (isPopup)
            {
                if (_cachedPopupProfile == null) _cachedPopupProfile = _settings.DefaultPopupProfile;
                return _cachedPopupProfile;
            }
            if (_cachedPanelProfile == null) _cachedPanelProfile = _settings.DefaultPanelProfile;
            return _cachedPanelProfile;
        }

        public UIButtonFeedbackSO GetButtonProfile()
        {
            if (_cachedButtonProfile == null) _cachedButtonProfile = _settings.DefaultButtonProfile;
            return _cachedButtonProfile;
        }

        public async Awaitable PlayPanelEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (rt == null || profile == null) return;
            switch (profile.EnterType)
            {
                case PanelEnterType.ScaleFade:       await PlayScaleFadeEnter(rt, cg, profile); break;
                case PanelEnterType.SlideUpBounce:   await PlaySlideBounceEnter(rt, cg, profile, -300f); break;
                case PanelEnterType.SlideDownBounce: await PlaySlideBounceEnter(rt, cg, profile, 300f); break;
                case PanelEnterType.DropBounce:      await PlayDropBounceEnter(rt, cg); break;
                case PanelEnterType.FadeOnly:        await PlayFadeEnter(cg, profile); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.EnterType), profile.EnterType, null);
            }
        }

        public async Awaitable PlayPanelExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (rt == null || profile == null) return;
            switch (profile.ExitType)
            {
                case PanelExitType.ScaleFade: await PlayScaleFadeExit(rt, cg, profile); break;
                case PanelExitType.SlideDown: await PlaySlideExit(rt, cg, profile, -300f); break;
                case PanelExitType.SlideUp:   await PlaySlideExit(rt, cg, profile, 300f); break;
                case PanelExitType.FadeOnly:  await PlayFadeExit(cg, profile); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.ExitType), profile.ExitType, null);
            }
        }

        public async Awaitable PlayStagger(IReadOnlyList<RectTransform> elements, UIStaggerAnimationSO profile)
        {
            if (elements == null || elements.Count == 0 || profile == null) return;

            GameObject linkTarget = null;
            for (int i = 0; i < elements.Count; i++)
                if (elements[i] != null) { linkTarget = elements[i].gameObject; break; }
            if (linkTarget == null) return;

            var seq = LSequence.Create();

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;

                float delay = i * profile.DelayBetweenElements;
                float dur = profile.ElementDuration;
                element.TryGetComponent<CanvasGroup>(out var elemCg);

                switch (profile.StaggerType)
                {
                    case StaggerType.Pop:
                    {
                        var peak = Vector3.one * 1.15f;
                        element.localScale = Vector3.zero;
                        seq.Insert(delay, Scale(element, Vector3.zero, peak, dur * 0.6f, profile.ElementEase));
                        seq.Insert(delay + dur * 0.6f, Scale(element, peak, Vector3.one, dur * 0.4f, Ease.OutQuad));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.5f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.SlideUp:
                    {
                        var endPos = element.anchoredPosition;
                        var startPos = new Vector2(endPos.x, endPos.y - 24f);
                        element.anchoredPosition = startPos;
                        seq.Insert(delay, Move(element, startPos, endPos, dur, profile.ElementEase));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.7f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.FadeIn:
                        if (elemCg == null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug($"[UIAnimationService] FadeIn stagger: '{element.name}' has no CanvasGroup — skipping");
                            break;
                        }
                        elemCg.alpha = 0f;
                        seq.Insert(delay, Fade(elemCg, 0f, 1f, dur, profile.ElementEase));
                        break;

                    case StaggerType.PopWithSpin:
                    {
                        var peak = Vector3.one * 1.1f;
                        var tilt = Quaternion.Euler(0f, 0f, -15f);
                        element.localScale = Vector3.zero;
                        element.localRotation = tilt;
                        seq.Insert(delay, Scale(element, Vector3.zero, peak, dur * 0.6f, profile.ElementEase));
                        seq.Insert(delay + dur * 0.6f, Scale(element, peak, Vector3.one, dur * 0.4f, Ease.OutQuad));
                        seq.Insert(delay, LMotion.Create(tilt, Quaternion.identity, dur * 0.7f)
                                                 .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalRotation(element));
                        if (elemCg != null) { elemCg.alpha = 0f; seq.Insert(delay, Fade(elemCg, 0f, 1f, dur * 0.5f, Ease.OutQuad)); }
                        break;
                    }

                    case StaggerType.StampDrop:
                    {
                        var rest = element.anchoredPosition;
                        var high = new Vector2(rest.x, rest.y + 60f);
                        var squash = new Vector3(1.18f, 0.82f, 1f);
                        element.anchoredPosition = high;
                        element.localScale = Vector3.one;
                        seq.Insert(delay, Move(element, high, rest, dur * 0.35f, Ease.InQuad));
                        seq.Insert(delay + dur * 0.35f, Scale(element, Vector3.one, squash, dur * 0.15f, Ease.OutQuad));
                        seq.Insert(delay + dur * 0.5f, Scale(element, squash, Vector3.one, dur * 0.5f, Ease.OutBack));
                        break;
                    }

                    default:
                        seq.Dispose();
                        throw new ArgumentOutOfRangeException(nameof(profile.StaggerType), profile.StaggerType, null);
                }
            }

            await seq.Run(Unscaled).AddTo(linkTarget).WaitAsync();
        }

        // ── Panel Enter helpers ──────────────────────────────────────────────

        private async Awaitable PlayScaleFadeEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            rt.localScale = Vector3.zero;
            var seq = LSequence.Create();
            seq.Insert(0f, Scale(rt, Vector3.zero, Vector3.one, profile.EnterDuration, profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, profile.EnterDuration, Ease.OutCubic)); }
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        // offsetY âm = trồi từ dưới lên (SlideUpBounce), dương = rơi từ trên xuống (SlideDownBounce).
        private async Awaitable PlaySlideBounceEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile, float offsetY)
        {
            var restPos = rt.anchoredPosition;
            var startPos = new Vector2(restPos.x, restPos.y + offsetY);
            rt.anchoredPosition = startPos;
            var seq = LSequence.Create();
            seq.Insert(0f, Move(rt, startPos, restPos, profile.EnterDuration, profile.EnterEase));
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, profile.EnterDuration * 0.7f, Ease.OutCubic)); }
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        // Mốc thời gian chép đúng bản DOTween: Append đặt ở CUỐI toàn chuỗi, tính cả cú mờ vào đã
        // Join — có CanvasGroup thì cú co bắt đầu sau 0.1s, không có thì bắt đầu ngay.
        private async Awaitable PlayDropBounceEnter(RectTransform rt, CanvasGroup cg)
        {
            var big = Vector3.one * 1.3f;
            var small = Vector3.one * 0.92f;
            rt.localScale = big;
            var seq = LSequence.Create();
            float at = 0f;
            if (cg != null) { cg.alpha = 0f; seq.Insert(0f, Fade(cg, 0f, 1f, 0.1f, Ease.OutCubic)); at = 0.1f; }
            seq.Insert(at, Scale(rt, big, small, 0.12f, Ease.InQuad));
            seq.Insert(at + 0.12f, Scale(rt, small, Vector3.one, 0.1f, Ease.OutBack));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
        }

        private async Awaitable PlayFadeEnter(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            cg.alpha = 0f;
            await LMotion.Create(0f, 1f, profile.EnterDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(cg)
                .AddTo(cg.gameObject)
                .WaitAsync();
        }

        // ── Panel Exit helpers ───────────────────────────────────────────────

        private async Awaitable PlayScaleFadeExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile)
        {
            var seq = LSequence.Create();
            seq.Insert(0f, Scale(rt, rt.localScale, Vector3.zero, profile.ExitDuration, profile.ExitEase));
            if (cg != null) seq.Insert(0f, Fade(cg, cg.alpha, 0f, profile.ExitDuration, Ease.InCubic));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
            if (rt != null) rt.localScale = Vector3.one;
        }

        // offsetY âm = trượt xuống (SlideDown), dương = trượt lên (SlideUp).
        private async Awaitable PlaySlideExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile, float offsetY)
        {
            var currentPos = rt.anchoredPosition;
            var seq = LSequence.Create();
            seq.Insert(0f, Move(rt, currentPos, new Vector2(currentPos.x, currentPos.y + offsetY), profile.ExitDuration, profile.ExitEase));
            if (cg != null) seq.Insert(0f, Fade(cg, cg.alpha, 0f, profile.ExitDuration * 0.5f, Ease.InCubic));
            await seq.Run(Unscaled).AddTo(rt.gameObject).WaitAsync();
            if (rt != null) rt.anchoredPosition = currentPos;
        }

        private async Awaitable PlayFadeExit(CanvasGroup cg, UIPanelAnimationSO profile)
        {
            if (cg == null) return;
            await LMotion.Create(cg.alpha, 0f, profile.ExitDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(cg)
                .AddTo(cg.gameObject)
                .WaitAsync();
        }

        // ── Motion con cho LSequence (scheduler đặt ở Run, không đặt ở đây) ─

        private static MotionHandle Scale(Transform t, Vector3 from, Vector3 to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToLocalScale(t);
        }

        private static MotionHandle Move(RectTransform rt, Vector2 from, Vector2 to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToAnchoredPosition(rt);
        }

        private static MotionHandle Fade(CanvasGroup cg, float from, float to, float dur, Ease ease)
        {
            return LMotion.Create(from, to, dur).WithEase(ease).WithCancelOnError().BindToAlpha(cg);
        }
    }
}
```

- [ ] **Step 5: Ba driver**

`Assets/_StudioSDK/UI/Components/UIButtonFeedbackDriver.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogosSDK.UI.Components
{
    public sealed class UIButtonFeedbackDriver : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private UIButtonFeedbackSO _profile;
        [SerializeField] private RectTransform _target;

        [Inject] private IUIAnimationService _animationService;

        private UIButtonFeedbackSO _resolvedProfile;
        private RectTransform _resolvedTarget;
        private MotionHandle _activeTween;

        private void Awake()
        {
            _resolvedTarget = _target != null ? _target : GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (_animationService == null)
            {
                _resolvedProfile = _profile;
                return;
            }
            _resolvedProfile = _profile != null ? _profile : _animationService.GetButtonProfile();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_resolvedTarget == null || _resolvedProfile == null) return;
            _activeTween.TryCancel();
            _activeTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one * _resolvedProfile.PressScale, _resolvedProfile.PressDuration)
                .WithEase(Ease.Linear)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_resolvedTarget == null || _resolvedProfile == null) return;
            _activeTween.TryCancel();
            _activeTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one, _resolvedProfile.ReleaseDuration)
                .WithEase(_resolvedProfile.ReleaseEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            _activeTween.TryCancel();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
```

`Assets/_StudioSDK/UI/Components/UIIdlePulseDriver.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Components
{
    public sealed class UIIdlePulseDriver : MonoBehaviour
    {
        [SerializeField] private UIButtonFeedbackSO _profile;
        [SerializeField] private RectTransform _target;

        [Inject] private IUIAnimationService _animationService;

        private UIButtonFeedbackSO _resolvedProfile;
        private RectTransform _resolvedTarget;
        private MotionHandle _pulseTween;
        private MotionHandle _wobbleTween;
        private WaitForSeconds _wobbleWait;

        private void Awake()
        {
            _resolvedTarget = _target != null ? _target : GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (_animationService == null)
            {
                _resolvedProfile = _profile;
                return;
            }
            _resolvedProfile = _profile != null ? _profile : _animationService.GetButtonProfile();

            // idleThreshold=0 → continuous jiggle, space repeats by animation duration
            // idleThreshold>0 → idle CTA hint, fire every N seconds
            float wobbleInterval = _resolvedProfile.IdleThreshold > 0f
                ? _resolvedProfile.IdleThreshold
                : _resolvedProfile.WobbleDuration + 0.1f;
            _wobbleWait = new WaitForSeconds(wobbleInterval);

            if (gameObject.activeInHierarchy && enabled)
            {
                if (_resolvedProfile.IdlePulseEnabled) StartPulse();
                if (_resolvedProfile.IdleWobbleEnabled) StartCoroutine(WobbleRoutine());
            }
        }

        private void OnEnable()
        {
            if (_resolvedProfile == null) return;
            if (_resolvedProfile.IdlePulseEnabled) StartPulse();
            if (_resolvedProfile.IdleWobbleEnabled) StartCoroutine(WobbleRoutine());
        }

        private void OnDisable()
        {
            _pulseTween.TryCancel();
            _wobbleTween.TryCancel();
            StopAllCoroutines();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }

        public void NotifyPressed()
        {
            if (_resolvedProfile == null || !_resolvedProfile.IdleWobbleEnabled) return;
            StopAllCoroutines();
            StartCoroutine(WobbleRoutine());
        }

        private void StartPulse()
        {
            _pulseTween.TryCancel();
            _pulseTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one * _resolvedProfile.PulseScale, _resolvedProfile.PulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        private System.Collections.IEnumerator WobbleRoutine()
        {
            while (true)
            {
                yield return _wobbleWait;
                if (!enabled || !gameObject.activeInHierarchy) yield break;
                _wobbleTween.TryCancel();
                // DOPunchRotation(…, vibrato 10, elasticity 0.5). Công thức punch LitMotion khác
                // DOTween — Task 8 so bằng mắt.
                _wobbleTween = LMotion.Punch.Create(_resolvedTarget.localEulerAngles, new Vector3(0f, 0f, _resolvedProfile.WobbleAngle), _resolvedProfile.WobbleDuration)
                    .WithFrequency(10)
                    .WithDampingRatio(0.5f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .WithCancelOnError()
                    .BindToLocalEulerAngles(_resolvedTarget)
                    .AddTo(gameObject);
            }
        }
    }
}
```

`Assets/_StudioSDK/UI/Components/UIToggleFeedbackDriver.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Components
{
    public sealed class UIToggleFeedbackDriver : MonoBehaviour
    {
        [SerializeField] private UIButtonFeedbackSO _profile;
        [SerializeField] private RectTransform _target;

        [Inject] private IUIAnimationService _animationService;

        private UIButtonFeedbackSO _resolvedProfile;
        private RectTransform _resolvedTarget;
        private MotionHandle _activeTween;

        private void Awake()
        {
            _resolvedTarget = _target != null ? _target : GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (_animationService == null)
            {
                _resolvedProfile = _profile;
                return;
            }
            _resolvedProfile = _profile != null ? _profile : _animationService.GetButtonProfile();
        }

        public void SetState(bool on)
        {
            if (_resolvedProfile == null || _resolvedTarget == null) return;
            _activeTween.TryCancel();
            if (on)
            {
                var up = Vector3.one * _resolvedProfile.ToggleOnScale;
                _activeTween = LSequence.Create()
                    .Insert(0f, LMotion.Create(_resolvedTarget.localScale, up, 0.1f)
                                       .WithEase(_resolvedProfile.ToggleOnEase).WithCancelOnError().BindToLocalScale(_resolvedTarget))
                    .Insert(0.1f, LMotion.Create(up, Vector3.one, 0.08f)
                                         .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(_resolvedTarget))
                    .Run(b => b.WithCancelOnError().WithScheduler(MotionScheduler.UpdateIgnoreTimeScale))
                    .AddTo(gameObject);
            }
            else if (_resolvedProfile.ToggleOffShake)
            {
                // DOShakeScale(0.25f, 0.12f, vibrato 8, randomness 90). Công thức shake khác — Task 8 so mắt.
                _activeTween = LMotion.Shake.Create(_resolvedTarget.localScale, Vector3.one * 0.12f, 0.25f)
                    .WithFrequency(8)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .WithCancelOnError()
                    .BindToLocalScale(_resolvedTarget)
                    .AddTo(gameObject);
            }
        }

        private void OnDisable()
        {
            _activeTween.TryCancel();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
```

- [ ] **Step 6: Ba transition**

`Assets/_StudioSDK/UI/Transitions/FadeTransition.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public sealed class FadeTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _enterDuration = 0.25f;
        [SerializeField] private float _exitDuration = 0.2f;
        [SerializeField] private Ease _enterEase = Ease.OutCubic;
        [SerializeField] private Ease _exitEase = Ease.InCubic;

        public async Awaitable PlayEnter(RectTransform target)
        {
            _canvasGroup.alpha = 0f;
            await LMotion.Create(0f, 1f, _enterDuration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(_canvasGroup)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await LMotion.Create(_canvasGroup.alpha, 0f, _exitDuration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAlpha(_canvasGroup)
                .AddTo(gameObject)
                .WaitAsync();
        }
    }
}
```

`Assets/_StudioSDK/UI/Transitions/ScaleTransition.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public sealed class ScaleTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private float _enterDuration = 0.22f;
        [SerializeField] private float _exitDuration = 0.18f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        public async Awaitable PlayEnter(RectTransform target)
        {
            target.localScale = Vector3.zero;
            await LMotion.Create(Vector3.zero, Vector3.one, _enterDuration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(target)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            await LMotion.Create(target.localScale, Vector3.zero, _exitDuration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(target)
                .AddTo(gameObject)
                .WaitAsync();
            target.localScale = Vector3.one;
        }
    }
}
```

`Assets/_StudioSDK/UI/Transitions/SlideTransition.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using LogosSDK.Tween;
using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public enum SlideDirection { Left, Right, Up, Down }

    public sealed class SlideTransition : MonoBehaviour, IUITransition
    {
        [SerializeField] private SlideDirection _enterFrom = SlideDirection.Right;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Ease _enterEase = Ease.OutCubic;
        [SerializeField] private Ease _exitEase = Ease.InCubic;

        public async Awaitable PlayEnter(RectTransform target)
        {
            Vector2 startPos = GetOffscreenPos(target, _enterFrom);
            target.anchoredPosition = startPos;
            await LMotion.Create(startPos, Vector2.zero, _duration)
                .WithEase(_enterEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAnchoredPosition(target)
                .AddTo(gameObject)
                .WaitAsync();
        }

        public async Awaitable PlayExit(RectTransform target)
        {
            Vector2 endPos = GetOffscreenPos(target, _enterFrom);
            await LMotion.Create(target.anchoredPosition, endPos, _duration)
                .WithEase(_exitEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToAnchoredPosition(target)
                .AddTo(gameObject)
                .WaitAsync();
            target.anchoredPosition = Vector2.zero;
        }

        private static Vector2 GetOffscreenPos(RectTransform rt, SlideDirection dir)
        {
            Vector2 size = rt.rect.size;
            return dir switch
            {
                SlideDirection.Left  => new Vector2(-size.x, 0f),
                SlideDirection.Right => new Vector2(size.x, 0f),
                SlideDirection.Up    => new Vector2(0f, size.y),
                SlideDirection.Down  => new Vector2(0f, -size.y),
                _                   => Vector2.zero
            };
        }
    }
}
```

- [ ] **Step 7: asmdef**

`Assets/_StudioSDK/UI/LogosSDK.UI.asmdef` — trong `references`, thay hai dòng `"DOTween.Modules",` và `"DOTweenPro.Scripts"` bằng:

```json
        "LitMotion",
        "LitMotion.Extensions",
        "LogosSDK.Tween"
```

- [ ] **Step 8: Kiểm**

```bash
grep -rn "DG.Tweening" Assets/_StudioSDK/UI || echo "UI SẠCH"
./compilecheck.sh
```

Expected: `UI SẠCH`; 3 dòng OK.
Focus Unity. Expected: Console 0 error. Test Runner → EditMode → `LogosSDK.UI.Tests`: 5/5 PASS; `LogosSDK.Tween.Tests`: 5/5 PASS.
Chọn `SO_Panel_ScaleFade` trong Project → Inspector hiện Enter Ease = **OutBack**, Exit Ease = **InBack** (không phải InOutBack/OutBack — lệch là remap sai).
Play `Main.unity`: mở/đóng một popup, bấm một nút CTA, bật/tắt toggle âm thanh — không lỗi Console, có animation.

- [ ] **Step 9: Commit**

```bash
git -C D:/CategorySort add Assets/_StudioSDK/UI Assets/_Shared/UI/Profiles Assets/_Shared/Prefab/Screen/MainMenuScreen.prefab
git -C D:/CategorySort status --short
git -C D:/CategorySort commit -m "Migrate LogosSDK.UI animations from DOTween to LitMotion" -m "Remap serialized Ease values (DOTween Unset offset) in UI profiles and MainMenuScreen." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

Expected trước commit: dòng `M`/`D` chỉ nằm dưới các đường dẫn đã add + 3 dòng ` M` cũ của user.

---

### Task 4: Migrate bàn chơi (`WordStack.Board`)

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs`
- Modify: `Assets/_Game/Board/Views/GhostView.cs`
- Modify: `Assets/_Game/Board/Views/BoosterAnimSettings.cs`
- Modify: `Assets/_Game/Board/WordStack.Board.asmdef`
- Modify (data): `Assets/_Game/Content/SO_BoosterAnim.asset`

**Interfaces:**
- Consumes: `Temp/remap_ease.py` (Task 3); assembly `LitMotion`, `LitMotion.Extensions`.
- Produces: `RemoveTiles`/`FadeBox` đổi kiểu trả về từ `Sequence` sang `IEnumerator` (chỉ dùng nội bộ `BoardController`).

- [ ] **Step 1: Đổi số Ease của SO_BoosterAnim**

Unity **không** được focus.

```bash
F=Assets/_Game/Content/SO_BoosterAnim.asset
git diff --quiet -- $F && echo CLEAN
python Temp/remap_ease.py magnetFlyEase,magnetBurstEase,magnetParentFlyEase,shuffleSpinEase,shuffleMoveInEase,shuffleMoveOutEase,shuffleScaleInEase,shuffleScaleOutEase,undoFlyEase,undoBoxSlideEase $F
grep -n "Ease:" $F
```

Expected: `CLEAN`; `10 dòng`; rồi:

```
magnetFlyEase: 9          (InOutCubic)
magnetBurstEase: 25       (InBack)
magnetParentFlyEase: 9    (InOutCubic)
shuffleSpinEase: 8        (OutCubic)
shuffleMoveInEase: 25     (InBack)
shuffleMoveOutEase: 26    (OutBack)
shuffleScaleInEase: 4     (InQuad)
shuffleScaleOutEase: 5    (OutQuad)
undoFlyEase: 8            (OutCubic)
undoBoxSlideEase: 8       (OutCubic)
```

- [ ] **Step 2: `BoosterAnimSettings.cs` và asmdef**

`BoosterAnimSettings.cs`: thay `using DG.Tweening;` bằng `using LitMotion;`. Không đổi gì khác.

`Assets/_Game/Board/WordStack.Board.asmdef` — `references` thành:

```json
    "references": [
        "WordStack.Contracts",
        "Unity.InputSystem",
        "LitMotion",
        "LitMotion.Extensions"
    ],
```

- [ ] **Step 3: `GhostView.cs`**

Thay đầu file (dòng 1–8):

```csharp
// Thẻ đang kéo. Giữ toàn bộ "feel" kéo-thả kiểu Balatro: lerp đuổi con trỏ + xoay Z theo độ
// trễ chuyển động + lắc sin/cos + pop scale. Mọi hằng feel là SerializeField để chỉnh trong
// Inspector mà không phải build lại.
//
// KHÔNG tween phần đuổi con trỏ: đích di chuyển mỗi frame, không phải tween có điểm đến
// cố định. LitMotion chỉ lo cú pop lúc nhấc lên.
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
```

Dòng khai báo `lift`:

```csharp
        float lift;                                  // 0..shadowLift, tween nhấc lúc Begin
```

Trong `Begin`, thay hai lệnh DOTween (dòng `transform.DOScale(...)` và khối `if (shadow != null) DOTween.To(...)`) bằng:

```csharp
            LMotion.Create(transform.localScale, Vector3.one * dragScale, 0.12f)
                   .WithEase(Ease.OutBack).WithCancelOnError()
                   .BindToLocalScale(transform).AddTo(gameObject);

            // Bóng lùi ra xa lúc nhấc (CardVisual.PointerDown). Ghost chết lúc thả nên
            // không cần trả về chỗ cũ. Tween biến `lift` chứ không tween thẳng transform:
            // Follow() đặt lại vị trí bóng mỗi frame, hai bên ghi cùng một transform thì đá nhau.
            if (shadow != null)
                LMotion.Create(0f, shadowLift, 0.12f)
                       .WithEase(Ease.OutBack).WithCancelOnError()
                       .Bind(this, (v, self) => self.lift = v).AddTo(gameObject);
```

- [ ] **Step 4: `BoardController.cs` — khai báo**

Thay `using DG.Tweening;` bằng:

```csharp
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
```

Xoá dòng `const int HoverPunchId = 2;            // để Kill cú punch trước, không đụng tween scale`.

Ngay sau dòng `bool locked;` (khối field kéo-thả), thêm:

```csharp
        MotionHandle hoverPunch;           // cú giật hover đang chạy — TryComplete trước khi giật cú mới
        readonly Dictionary<int, MotionHandle> shakes = new Dictionary<int, MotionHandle>();   // stack → cú rung đang chạy

        // ---- LitMotion (thay DOTween 2026-09-17) — đọc Global Constraints của plan chuyển đổi.
        // Sequence: lỗi (target bị huỷ) thì huỷ cả chuỗi thay vì ghi lỗi mỗi frame.
        static readonly Action<MotionBuilder<double, NoOptions, DoubleMotionAdapter>> SeqCfg = b => b.WithCancelOnError();

        // DOTween Ease.InBack kèm overshoot tuỳ biến — LitMotion.Ease không nhận overshoot.
        static float InBack(float t, float s) { return t * t * ((s + 1f) * t - s); }

        // Huỷ thẻ SAU khi chuỗi chạy xong, không huỷ trong callback: sequence vẫn ghi giá trị cho
        // motion con đã xong mỗi frame, target mất giữa chừng là cả chuỗi lỗi và thẻ khác khựng
        // giữa đường bay. Thẻ đã về scale 0 nên nằm thêm vài nhịp cũng không ai thấy.
        void DestroyAll(List<GameObject> gos)
        {
            foreach (var go in gos) if (go != null) Destroy(go);
        }
```

- [ ] **Step 5: `BoardController.cs` — Nam châm**

Thay toàn bộ method `MagnetAnimation` và `AppendParentFlight` (từ dòng `IEnumerator MagnetAnimation(` tới hết `AppendParentFlight`, giữ comment phía trên mỗi method) bằng:

```csharp
        IEnumerator MagnetAnimation(MagnetResult r, Dictionary<string, Tile> faces)
        {
            var a = A;
            Vector3 center = cam.ViewportToWorldPoint(
                new Vector3(a.magnetGatherViewport.x, a.magnetGatherViewport.y, -cam.transform.position.z));
            center.z = 0f;

            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            float lastBurst = 0f;   // lúc thẻ CUỐI bắt đầu nổ — thẻ cha nở đúng nhịp đó
            for (int i = 0; i < r.Picks.Length; i++)
            {
                var p = r.Picks[i];
                TileView tv = null;
                bool temp = p.Box > 0 || !tiles.TryGetValue(p.Uid, out tv) || tv == null;
                if (temp)
                {
                    Tile face;
                    if (!faces.TryGetValue(p.Uid, out face) || p.Stack < 0 || p.Stack >= boxViews.Length) continue;
                    tv = Instantiate(tilePrefab, root, false);
                    tv.transform.position = boxViews[p.Stack].transform.position;
                    tv.transform.localScale = Vector3.zero;
                    tv.Bind(face, ArtOf(face));
                }
                else
                {
                    tiles.Remove(p.Uid);
                    tv.transform.SetParent(root, true);
                }
                tv.SetFlying(true);
                var tr = tv.transform;
                float at = i * a.magnetStagger;

                // LitMotion chốt giá trị đầu lúc TẠO motion, nên mỗi nhịp khai "đi từ đâu" = đích của
                // nhịp trước trên cùng thuộc tính.
                Vector3 scale = tr.localScale;
                if (temp)
                {
                    seq.Insert(at, LMotion.Create(Vector3.zero, Vector3.one, a.magnetRevealDur)
                                          .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
                    at += a.magnetRevealDur;
                    scale = Vector3.one;
                }
                var pop = Vector3.one * a.magnetPopScale;
                seq.Insert(at, LMotion.Create(scale, pop, a.magnetPopDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
                at += a.magnetPopDur;
                var gather = Vector3.one * a.magnetGatherScale;
                seq.Insert(at, LMotion.Create(tr.position, center, a.magnetFlyDur)
                                      .WithEase(a.magnetFlyEase).WithCancelOnError().BindToPosition(tr));
                seq.Insert(at, LMotion.Create(pop, gather, a.magnetFlyDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
                if (Mathf.Abs(a.magnetSpin) > 0.01f)
                    // RotateMode.FastBeyond360: từ góc hiện tại (đã chuẩn hoá [0,360)) tới đúng magnetSpin.
                    seq.Insert(at, LMotion.Create(tr.eulerAngles.z, a.magnetSpin, a.magnetFlyDur)
                                          .WithEase(a.magnetFlyEase).WithCancelOnError().BindToEulerAnglesZ(tr));
                at += a.magnetFlyDur + a.magnetHold;
                if (at > lastBurst) lastBurst = at;
                seq.Insert(at, LMotion.Create(gather, Vector3.zero, a.magnetBurstDur)
                                      .WithEase(a.magnetBurstEase).WithCancelOnError().BindToLocalScale(tr));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); yield break; }
            AppendParentFlight(seq, r, center, lastBurst);
            yield return seq.Run(SeqCfg).AddTo(this).ToYieldInstruction();
            DestroyAll(doomed);
        }

        // COLLAPSE qua nam châm: thẻ cha nở ra TẠI ĐIỂM GỘP đúng lúc thẻ cuối nổ, khựng một nhịp,
        // rồi bay về ô mà domain đã đặt nó trong hộp r.NewTileStack, co về cỡ thường. Là thẻ
        // tạm dưới root — nó nằm yên ở ô đó tới khi Rebuild (DestroyBoard) huỷ và dựng thẻ thật
        // đúng chỗ, nên không có khung hình nào ô bị trống.
        void AppendParentFlight(MotionSequenceBuilder seq, MagnetResult r, Vector3 center, float at)
        {
            if (r.NewTileUid == null || r.NewTileStack < 0 || r.NewTileStack >= boxViews.Length) return;
            var box = g.TopBox(r.NewTileStack);
            int slot = box == null ? -1 : Array.FindIndex(box.Slots, t => t != null && t.Uid == r.NewTileUid);
            if (slot < 0) return;

            var a = A;
            var t = box.Slots[slot];
            var tv = Instantiate(tilePrefab, root, false);
            tv.transform.position = center;
            tv.transform.localScale = Vector3.zero;
            tv.Bind(t, ArtOf(t));
            var cc = GroupCountsIn(box);
            tv.SetMatchState(cc[t.GroupId], OrdinalOf(PairOrdinalsFor(r.NewTileStack, box, cc), t.GroupId));
            tv.SetFlying(true);

            var tr = tv.transform;
            Vector3 dest = boxViews[r.NewTileStack].Slot(slot).position;
            var gather = Vector3.one * a.magnetGatherScale;

            seq.Insert(at, LMotion.Create(Vector3.zero, gather, a.magnetParentBloomDur)
                                  .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tr));
            if (Mathf.Abs(mergeSpin) > 0.01f)
            {
                tr.localEulerAngles = new Vector3(0f, 0f, mergeSpin);
                seq.Insert(at, LMotion.Create(tr.localRotation, Quaternion.identity, a.magnetParentBloomDur)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToLocalRotation(tr));
            }
            at += a.magnetParentBloomDur + a.magnetParentHold;
            seq.Insert(at, LMotion.Create(center, dest, a.magnetParentFlyDur)
                                  .WithEase(a.magnetParentFlyEase).WithCancelOnError().BindToPosition(tr));
            seq.Insert(at, LMotion.Create(gather, Vector3.one, a.magnetParentFlyDur)
                                  .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(tr));
        }
```

- [ ] **Step 6: `BoardController.cs` — Xáo**

Trong method `Vortex`, thay từ dòng `var seq = DOTween.Sequence().SetLink(pivot.gameObject);` tới hết dòng `yield return seq.WaitForCompletion();` bằng:

```csharp
            // Góc đầu đọc từ eulerAngles NGAY lúc tạo motion — đã chuẩn hoá về [0,360), đúng như
            // DOTween RotateMode.FastBeyond360 làm. Hệ quả giữ nguyên từ bản DOTween: pha ra có
            // shuffleTurns nguyên (asset đang là 3) thì pivot đọc được 0 → không xoáy, thẻ tự quay.
            var seq = LSequence.Create();
            seq.Insert(0f, LMotion.Create(pivot.eulerAngles.z, inward ? spin : 0f, dur)
                                  .WithEase(A.shuffleSpinEase).WithCancelOnError().BindToEulerAnglesZ(pivot));
            for (int i = 0; i < kids.Count; i++)
            {
                Transform k = kids[i];
                Vector3 slot = k.localPosition;        // ô của thẻ trong hệ pivot (đã tính rotation)
                if (!inward) { k.localPosition = Vector3.zero; k.localScale = Vector3.one * A.shuffleGatherScale; }

                seq.Insert(0f, LMotion.Create(k.localPosition, inward ? Vector3.zero : slot, dur)
                                      .WithEase(inward ? A.shuffleMoveInEase : A.shuffleMoveOutEase)
                                      .WithCancelOnError().BindToLocalPosition(k));
                seq.Insert(0f, LMotion.Create(k.localScale, Vector3.one * (inward ? A.shuffleGatherScale : 1f), dur)
                                      .WithEase(inward ? A.shuffleScaleInEase : A.shuffleScaleOutEase)
                                      .WithCancelOnError().BindToLocalScale(k));
                // Giữ thẻ thẳng: quay ngược -spin CÙNG ease với pivot, tổng góc ≡ 0.
                seq.Insert(0f, LMotion.Create(k.localEulerAngles.z, -spin, dur)
                                      .WithEase(A.shuffleSpinEase).WithCancelOnError().BindToLocalEulerAnglesZ(k));
            }
            yield return seq.Run(SeqCfg).AddTo(pivot.gameObject).ToYieldInstruction();
```

- [ ] **Step 7: `BoardController.cs` — Undo và nền booster**

Trong `UndoAnimation`, thay khối từ `var slide = DOTween.Sequence().SetLink(bv.gameObject);` tới hết `yield return pop.WaitForCompletion();` bằng:

```csharp
                var slide = LSequence.Create();
                slide.Insert(0f, LMotion.Create(bv.transform.localPosition, Vector3.zero, a.undoBoxSlideDur)
                                        .WithEase(a.undoBoxSlideEase).WithCancelOnError().BindToLocalPosition(bv.transform));
                // Không SetEase ở bản DOTween → OutQuad (ease mặc định trong DOTweenSettings).
                slide.Insert(0f, LMotion.Create(0f, 1f, a.undoBoxSlideDur)
                                        .WithEase(Ease.OutQuad).WithCancelOnError().Bind(bv, (v, b) => b.SetAlpha(v)));
                yield return slide.Run(SeqCfg).AddTo(bv.gameObject).ToYieldInstruction();

                // Thẻ của hộp cũ nở ra — trừ thẻ sắp bay về, nó đang đứng ở chỗ khác.
                var box = g.TopBox(s);
                var pop = LSequence.Create();
                int popped = 0;
                for (int i = 0; i < box.Slots.Length; i++)
                {
                    var t = box.Slots[i];
                    if (t == null || t.Uid == movedUid) continue;
                    var tv = Instantiate(tilePrefab, bv.Slot(i), false);
                    tv.transform.localPosition = Vector3.zero;
                    tv.transform.localScale = Vector3.zero;
                    tv.Bind(t, ArtOf(t));
                    tiles[t.Uid] = tv;
                    pop.Insert(0f, LMotion.Create(Vector3.zero, Vector3.one, a.undoBoxTilePopDur)
                                          .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tv.transform));
                    popped++;
                }
                if (popped > 0) yield return pop.Run(SeqCfg).AddTo(this).ToYieldInstruction();
                else pop.Dispose();
```

(Comment `// Thẻ của hộp cũ nở ra …` cũ nằm giữa hai khối — xoá bản cũ, bản mới đã có trong đoạn thay.)

Trong khối thẻ bay ngược, thay từ `var seq = DOTween.Sequence().SetLink(mv.gameObject);` tới hết `yield return seq.WaitForCompletion();` bằng:

```csharp
                // DOTween: Append pop @0, Append bay @undoPopDur, Join co về 1 @undoPopDur.
                var mt = mv.transform;
                var popS = Vector3.one * a.undoPopScale;
                var seq = LSequence.Create();
                seq.Insert(0f, LMotion.Create(mt.localScale, popS, a.undoPopDur)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(mt));
                seq.Insert(a.undoPopDur, LMotion.Create(mt.localPosition, Vector3.zero, a.undoFlyDur)
                                                .WithEase(a.undoFlyEase).WithCancelOnError().BindToLocalPosition(mt));
                seq.Insert(a.undoPopDur, LMotion.Create(popS, Vector3.one, a.undoFlyDur)
                                                .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(mt));
                yield return seq.Run(SeqCfg).AddTo(mv.gameObject).ToYieldInstruction();
```

Trong `Backdrop`, thay hai dòng `var tw = DOTween.To(...)` + `.SetEase(...)` và dòng `yield return tw.WaitForCompletion();` bằng:

```csharp
            yield return LMotion.Create(cg.alpha, on ? 1f : 0f, dur)
                                .WithEase(Ease.OutQuad).WithCancelOnError()
                                .BindToAlpha(cg).AddTo(boosterBackdrop).ToYieldInstruction();
```

- [ ] **Step 8: `BoardController.cs` — kéo, thả, hover, rung**

Trong method bắt đầu kéo, thay `DOTween.Kill(HoverPunchId, true);             // trả góc quay về 0 trước khi nhấc` bằng:

```csharp
            hoverPunch.TryComplete();                     // trả góc quay về 0 trước khi nhấc
```

Thay toàn bộ thân `FlyTo`, `Hover`, `Shake` bằng:

```csharp
        void FlyTo(TileView tv, Transform anchor, Vector3 fromWorld)
        {
            tv.transform.SetParent(anchor, false);
            tv.transform.position = fromWorld;
            tv.transform.localScale = Vector3.one;
            tv.SetFlying(true);
            LMotion.Create(tv.transform.localPosition, Vector3.zero, flyDur)
                   .WithEase(Ease.OutCubic)
                   .WithOnComplete(() => { if (tv != null) tv.SetFlying(false); })
                   .WithCancelOnError()
                   .BindToLocalPosition(tv.transform)
                   .AddTo(tv.gameObject);
        }

        void Hover(Vector2 pt)
        {
            TileView h = null;
            foreach (var z in zones)
            {
                if (z.Kind != ZoneKind.Tile || !z.Rect.Contains(pt)) continue;
                tiles.TryGetValue(z.Uid, out h);
                break;
            }
            if (h == hoverTile) return;           // chỉ tween lúc VÀO/RA, không mỗi frame
            if (hoverTile != null && !IsLifted(hoverTile))
                LMotion.Create(hoverTile.transform.localScale, Vector3.one, 0.12f)
                       .WithEase(Ease.OutBack).WithCancelOnError()
                       .BindToLocalScale(hoverTile.transform).AddTo(hoverTile.gameObject);
            hoverTile = h;
            if (hoverTile != null && !IsLifted(hoverTile))
            {
                LMotion.Create(hoverTile.transform.localScale, Vector3.one * HoverScale, 0.12f)
                       .WithEase(Ease.OutBack).WithCancelOnError()
                       .BindToLocalScale(hoverTile.transform).AddTo(hoverTile.gameObject);
                // Giật một cái lúc con trỏ vào (CardVisual.PointerEnter). Complete cú trước để góc
                // quay không cộng dồn khi rê nhanh qua nhiều thẻ. DOPunchRotation(vibrato 20,
                // elasticity 1) — công thức punch LitMotion khác, Task 8 so bằng mắt.
                hoverPunch.TryComplete();
                hoverPunch = LMotion.Punch.Create(hoverTile.transform.localEulerAngles, Vector3.forward * HoverPunchAngle, 0.12f)
                                    .WithFrequency(20).WithDampingRatio(1f).WithCancelOnError()
                                    .BindToLocalEulerAngles(hoverTile.transform).AddTo(hoverTile.gameObject);
            }
        }

        // Thẻ đang bị "nhấc lên" (scale 0) vì đang kéo. Đừng đụng scale của nó.
        static bool IsLifted(TileView tv) { return tv.transform.localScale.x < 0.01f; }

        void Shake(int stack)
        {
            var bv = boxViews[stack];
            if (bv == null) return;
            MotionHandle prev;
            if (shakes.TryGetValue(stack, out prev)) prev.TryComplete();   // rung dồn: kết thúc cú trước đã
            // DOPunchPosition(…, vibrato 6, elasticity 0.6) — Task 8 so bằng mắt.
            shakes[stack] = LMotion.Punch.Create(bv.transform.localPosition, new Vector3(0.12f, 0f, 0f), 0.22f)
                                   .WithFrequency(6).WithDampingRatio(0.6f).WithCancelOnError()
                                   .BindToLocalPosition(bv.transform).AddTo(bv.gameObject);
        }
```

(`IsLifted` và comment của nó nằm giữa `Hover` và `Shake` — đoạn trên đã gồm, xoá bản cũ.)

- [ ] **Step 9: `BoardController.cs` — cascade**

Trong `Settle`, thay:

```csharp
                    var seq = RemoveTiles(ev.DoomedUids);   // 4 thẻ co về 0, lệch nhau clearStagger
                    if (seq != null) yield return seq.WaitForCompletion();
```

bằng:

```csharp
                    yield return RemoveTiles(ev.DoomedUids);   // 4 thẻ co về 0, lệch nhau clearStagger
```

và `yield return FadeBox(ev.Stack).WaitForCompletion();` bằng `yield return FadeBox(ev.Stack);`.

Thay toàn bộ `RemoveTiles`, `MergeTiles`, `FadeBox` (kèm comment phía trên `FadeBox`) và phần tween cuối `SpawnCollapsedTile` bằng:

```csharp
        // 4 thẻ co về 0 lệch nhau clearStagger rồi huỷ. Không còn thẻ nào có view thì kết thúc ngay.
        IEnumerator RemoveTiles(string[] uids)
        {
            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            for (int i = 0; i < uids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(uids[i], out tv) || tv == null) continue;
                tiles.Remove(uids[i]);
                seq.Insert(i * clearStagger, LMotion.Create(tv.transform.localScale, Vector3.zero, clearDur)
                                                    .WithEase(Ease.InBack).WithCancelOnError().BindToLocalScale(tv.transform));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); yield break; }
            yield return seq.Run(SeqCfg).AddTo(this).ToYieldInstruction();
            DestroyAll(doomed);
        }

        // COLLAPSE nhìn phải ra "gộp", không phải "biến mất rồi mọc lại" — nên 4 thẻ BAY CHỤM về
        // đúng ô mà domain đã đặt thẻ mới, nén về 0 ở đó, rồi thẻ mới bung ra từ chính điểm ấy.
        // Domain đã mutate xong trước khi hàm này chạy, nên thẻ mới đã nằm sẵn trong Slots — tra
        // ra ô của nó chính là cách biết điểm hội tụ.
        IEnumerator MergeTiles(int s, string[] doomedUids, string newUid)
        {
            var box = g.TopBox(s);
            int dest = box == null ? -1 : Array.FindIndex(box.Slots, t => t != null && t.Uid == newUid);
            if (dest < 0)
            {
                // Không tra ra ô đích thì lùi về cách cũ — thà xấu còn hơn nuốt mất thẻ.
                yield return RemoveTiles(doomedUids);
                SpawnCollapsedTile(s, newUid);
                yield break;
            }

            var destPos = boxViews[s].Slot(dest).position;
            var shrink = Vector3.one * mergeShrink;
            var seq = LSequence.Create();
            var doomed = new List<GameObject>();
            for (int i = 0; i < doomedUids.Length; i++)
            {
                TileView tv;
                if (!tiles.TryGetValue(doomedUids[i], out tv) || tv == null) continue;
                tiles.Remove(doomedUids[i]);
                tv.SetFlying(true);                          // bay chụm về ô đích thì nổi lên trên hộp
                var tr = tv.transform;
                var from = tr.position;
                float at = i * mergeStagger;

                // InBack: nhích ra ngoài một chút rồi mới lao vào — cú lấy đà làm chuyển động
                // đọc ra là "bị hút vào" thay vì "trượt tới". Overshoot 0.6 như bản DOTween;
                // LitMotion.Ease không nhận overshoot nên nội suy tay (motion Linear 0→1).
                seq.Insert(at, LMotion.Create(0f, 1f, mergeGather).WithCancelOnError()
                                      .Bind(k => tr.position = Vector3.LerpUnclamped(from, destPos, InBack(k, 0.6f))));
                seq.Insert(at, LMotion.Create(tr.localScale, shrink, mergeGather)
                                      .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                // Tới nơi thì nén nốt về 0: nhịp "cộp" ngăn giữa lúc 4 thẻ tắt và lúc thẻ mới bung.
                seq.Insert(at + mergeGather, LMotion.Create(shrink, Vector3.zero, Mathf.Max(mergeHold, 0.01f))
                                                    .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(tr));
                doomed.Add(tv.gameObject);
            }
            if (doomed.Count == 0) { seq.Dispose(); SpawnCollapsedTile(s, newUid); yield break; }

            yield return seq.Run(SeqCfg).AddTo(this).ToYieldInstruction();
            DestroyAll(doomed);
            SpawnCollapsedTile(s, newUid);
        }

        // Hộp co lại + mờ dần (GDD §9.3 "Xoá box"). Mờ qua BoxView.SetAlpha — hộp nhiều
        // SpriteRenderer, không tween một renderer.
        IEnumerator FadeBox(int s)
        {
            var bv = boxViews[s];
            var seq = LSequence.Create();
            seq.Insert(0f, LMotion.Create(bv.transform.localScale, Vector3.one * 0.9f, clearDur)
                                  .WithEase(Ease.InQuad).WithCancelOnError().BindToLocalScale(bv.transform));
            // Không SetEase ở bản DOTween → OutQuad (ease mặc định trong DOTweenSettings).
            seq.Insert(0f, LMotion.Create(1f, 0f, clearDur)
                                  .WithEase(Ease.OutQuad).WithCancelOnError().Bind(bv, (v, b) => b.SetAlpha(v)));
            yield return seq.Run(SeqCfg).AddTo(bv.gameObject).ToYieldInstruction();
        }
```

Trong `SpawnCollapsedTile`, thay từ `var seq = DOTween.Sequence().SetLink(go);` tới hết khối `if (Mathf.Abs(mergeSpin) > 0.01f) { … }` bằng:

```csharp
            var seq = LSequence.Create();
            seq.Insert(0f, LMotion.Create(Vector3.zero, Vector3.one, mergeBloom)
                                  .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(tv.transform));
            if (Mathf.Abs(mergeSpin) > 0.01f)
            {
                tv.transform.localEulerAngles = new Vector3(0f, 0f, mergeSpin);
                seq.Insert(0f, LMotion.Create(tv.transform.localRotation, Quaternion.identity, mergeBloom)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToLocalRotation(tv.transform));
            }
            seq.Run(SeqCfg).AddTo(go);
```

- [ ] **Step 10: Kiểm**

```bash
grep -rn "DG.Tweening\|DOTween\|\.DO[A-Z]\|WaitForCompletion\|HoverPunchId" Assets/_Game/Board || echo "BOARD SẠCH"
./compilecheck.sh
```

Expected: `BOARD SẠCH`; 3 dòng OK.
Focus Unity: Console 0 error. Chọn `SO_BoosterAnim` → Magnet Fly Ease = **InOutCubic**, Shuffle Move Out Ease = **OutBack**.
Play `Main.unity` → CHEAT → Level 5: kéo thả 3 nước, làm một CLEAR, bấm Nam châm, Xáo, Undo một lần mỗi cái — không lỗi Console, bàn không kẹt `locked` (kéo tiếp được sau mỗi booster).

- [ ] **Step 11: Commit**

```bash
git -C D:/CategorySort add Assets/_Game/Board/Views/BoardController.cs Assets/_Game/Board/Views/GhostView.cs Assets/_Game/Board/Views/BoosterAnimSettings.cs Assets/_Game/Board/WordStack.Board.asmdef Assets/_Game/Content/SO_BoosterAnim.asset
git -C D:/CategorySort commit -m "Migrate board view animations from DOTween to LitMotion" -m "Remap serialized Ease values in SO_BoosterAnim; destroy tiles after sequences finish." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Migrate meta và module

**Files:**
- Modify: `Assets/_Game/Gameplay/Views/GameplayHudView.cs`
- Modify: `Assets/_Game/UI/Screens/LoadingScreen.cs`
- Modify: `Assets/_Modules/CheatPanel/Views/CheatToastView.cs`
- Modify: `Assets/BoosterModule/BoosterSlotView.cs`
- Modify: `Assets/_Game/WordStack.Meta.asmdef`, `Assets/_Modules/CheatPanel/LogosMeta.CheatPanel.asmdef`, `Assets/BoosterModule/BoosterModule.asmdef`

**Interfaces:**
- Consumes: `LogosSDK.Tween.MotionHandleCompat.WaitAsync` (Task 2).

- [ ] **Step 1: asmdef**

- `WordStack.Meta.asmdef`: thay dòng `"DOTween.Modules"` (dòng cuối `references`) bằng `"LitMotion",` `"LitMotion.Extensions",` `"LogosSDK.Tween"`.
- `LogosMeta.CheatPanel.asmdef`: thay `"DOTween.Modules",` bằng `"LitMotion",` `"LitMotion.Extensions",`.
- `BoosterModule.asmdef`: thay `"DOTween.Modules"` bằng `"LitMotion",` `"LitMotion.Extensions"`.

(Giữ JSON hợp lệ: dấu phẩy giữa phần tử, không phẩy sau phần tử cuối.)

- [ ] **Step 2: `GameplayHudView.cs`**

Thay `using DG.Tweening;` bằng `using LitMotion;` + `using LitMotion.Extensions;`.

Sau `private DisposableBag _disposables;` thêm:

```csharp
        private MotionHandle _progressTween;
```

Trong `RefreshProgress`, thay khối `if (_progressFill != null) { … }` bằng:

```csharp
            if (_progressFill != null)
            {
                float target = (float)cleared / total;
                _progressTween.TryCancel();
                if (cleared == 0)
                {
                    _progressFill.fillAmount = 0f;   // vào màn/chơi lại: snap, khỏi tween tụt về 0
                }
                else
                {
                    // Không SetEase ở bản DOTween → OutQuad (ease mặc định trong DOTweenSettings).
                    _progressTween = LMotion.Create(_progressFill.fillAmount, target, 0.25f)
                        .WithEase(Ease.OutQuad)
                        .WithCancelOnError()
                        .BindToFillAmount(_progressFill)
                        .AddTo(_progressFill.gameObject);
                }
            }
```

- [ ] **Step 3: `LoadingScreen.cs`**

```csharp
using LitMotion;
using LogosSDK.Tween;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Screens
{
    public sealed class LoadingScreen : ScreenBase
    {
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private float _fillDuration = 2f;

        private MotionHandle _fillTween;

        public override async Awaitable Show(object args = null)
        {
            if (_loadingSlider != null)
            {
                _loadingSlider.value = 0f;
                _fillTween = LMotion.Create(0f, 0.9f, _fillDuration)
                    .WithEase(Ease.OutCubic)
                    .WithCancelOnError()
                    .Bind(_loadingSlider, (v, slider) => slider.value = v)
                    .AddTo(gameObject);
            }

            await base.Show(args);
        }

        public override async Awaitable Hide()
        {
            if (_loadingSlider != null)
            {
                _fillTween.TryCancel();

                await LMotion.Create(_loadingSlider.value, 1f, 0.3f)
                    .WithEase(Ease.OutQuad)
                    .WithCancelOnError()
                    .Bind(_loadingSlider, (v, slider) => slider.value = v)
                    .AddTo(gameObject)
                    .WaitAsync();
            }

            await base.Hide();
        }
    }
}
```

- [ ] **Step 4: `CheatToastView.cs`**

Giữ nguyên nhịp của bản DOTween — kể cả việc toast đang hiện **~5.0s** chứ không phải ~2.5s như summary ghi (xem Mục "Việc để sau"):

```csharp
using System;
using LitMotion;
using LitMotion.Extensions;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// Fly-up notification banner. Subscribes to <see cref="ICheatNotificationSource.Notifications"/>
    /// and shows each message for ~2.5s before fading out. Stays out of the popup
    /// queue so it never blocks gameplay input.
    /// </summary>
    public sealed class CheatToastView : MonoBehaviour
    {
        [Inject] private readonly ICheatNotificationSource _notifications;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _successColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _failureColor = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private float _floatDistance = 60f;
        [SerializeField] private float _visibleDuration = 2.5f;
        [SerializeField] private float _fadeDuration = 0.35f;

        private DisposableBag _disposables;
        private MotionHandle _activeSequence;
        private Vector2 _restAnchoredPosition;

        private void Awake()
        {
            if (_root != null) _restAnchoredPosition = _root.anchoredPosition;
            HideImmediate();
        }

        private void Start()
        {
            if (_notifications == null) return;
            _notifications.Notifications
                .Subscribe(OnNotification)
                .AddTo(ref _disposables);
        }

        private void OnDestroy()
        {
            _activeSequence.TryCancel();
            _disposables.Dispose();
        }

        private void OnNotification(CheatNotification notification)
        {
            ShowToast(notification);
        }

        private void ShowToast(CheatNotification notification)
        {
            _activeSequence.TryCancel();

            if (_label != null)
            {
                _label.text = notification.Message ?? string.Empty;
                _label.color = notification.Success ? _successColor : _failureColor;
            }

            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // Mốc thời gian chép đúng bản DOTween: Append/AppendInterval đặt ở CUỐI toàn chuỗi,
            // tính cả cú trôi lên đã Join. Không SetEase ở bản cũ → OutQuad (mặc định DOTweenSettings).
            float fadeInEnd = _canvasGroup != null ? _fadeDuration : 0f;
            float floatEnd = _root != null ? _visibleDuration + _fadeDuration : 0f;
            float fadeOutAt = Math.Max(fadeInEnd, floatEnd) + Math.Max(0f, _visibleDuration - _fadeDuration);
            float end = fadeOutAt + (_canvasGroup != null ? _fadeDuration : 0f);

            var seq = LSequence.Create();
            if (_canvasGroup != null)
            {
                seq.Insert(0f, LMotion.Create(0f, 1f, _fadeDuration)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToAlpha(_canvasGroup));
                seq.Insert(fadeOutAt, LMotion.Create(1f, 0f, _fadeDuration)
                                             .WithEase(Ease.OutQuad).WithCancelOnError().BindToAlpha(_canvasGroup));
            }
            if (_root != null)
                seq.Insert(0f, LMotion.Create(_restAnchoredPosition.y, _restAnchoredPosition.y + _floatDistance, _visibleDuration + _fadeDuration)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToAnchoredPositionY(_root));
            // Mốc rỗng giữ đúng tổng thời lượng khi thiếu CanvasGroup (DOTween vẫn đếm AppendInterval).
            seq.Insert(end, LMotion.Create(0f, 0f, 0f).RunWithoutBinding());

            _activeSequence = seq.Run(b => b.WithCancelOnError().WithOnComplete(HideImmediate)).AddTo(gameObject);
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
        }
    }
}
```

- [ ] **Step 5: `BoosterSlotView.cs`**

```csharp
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoosterModule
{
    public class BoosterSlotView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private BoosterId _id;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private float _punchScale = 1.2f;

        [Header("References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _button;

        private BoosterSlotViewModel _viewModel;
        private MotionHandle _punch;

        private void Start()
        {
            _viewModel = new BoosterSlotViewModel(_id);
            _viewModel.OnCountChanged += UpdateUI;
            
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
            
            // Initial state
            UpdateUI(_viewModel.Count);
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.OnCountChanged -= UpdateUI;
                _viewModel.Dispose();
            }
        }

        private void OnClicked()
        {
            // Visual feedback — kết thúc cú trước (DOKill(complete) cũ) rồi punch lại.
            // DOPunchScale mặc định vibrato 10, elasticity 1; công thức LitMotion khác — Task 8 so mắt.
            _punch.TryComplete();
            _punch = LMotion.Punch.Create(transform.localScale, Vector3.one * (_punchScale - 1f), _animationDuration)
                .WithFrequency(10)
                .WithDampingRatio(1f)
                .WithCancelOnError()
                .BindToLocalScale(transform)
                .AddTo(gameObject);
            
            _viewModel.RequestUse();
        }

        private void UpdateUI(int count)
        {
            if (_countText != null)
            {
                _countText.text = count.ToString();
            }
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = count > 0 ? 1f : 0.5f;
            }
        }
    }
}
```

- [ ] **Step 6: Kiểm**

```bash
grep -rn "DG.Tweening" Assets/_Game Assets/_Modules Assets/BoosterModule || echo "META SẠCH"
./compilecheck.sh
```

Expected: `META SẠCH`; 3 dòng OK.
Focus Unity: Console 0 error. Play `Main.unity` từ màn Loading: thanh loading chạy tới ~90% rồi đầy; vào level, gom một nhóm → progress bar trượt; mở CHEAT, bấm một lệnh → toast trôi lên và mờ đi; bấm một nút booster → nút nảy.

- [ ] **Step 7: Commit**

```bash
git -C D:/CategorySort add Assets/_Game/Gameplay/Views/GameplayHudView.cs Assets/_Game/UI/Screens/LoadingScreen.cs Assets/_Modules/CheatPanel/Views/CheatToastView.cs Assets/BoosterModule/BoosterSlotView.cs Assets/_Game/WordStack.Meta.asmdef Assets/_Modules/CheatPanel/LogosMeta.CheatPanel.asmdef Assets/BoosterModule/BoosterModule.asmdef
git -C D:/CategorySort commit -m "Migrate meta and module animations from DOTween to LitMotion" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: SceneSnapshot

**Files:**
- Modify: `Assets/_StudioSDK/Editor/SceneSnapshot/Tests/Runtime/SnapshotTestSpawner.cs`
- Modify: `Assets/_StudioSDK/Editor/SceneSnapshot/Tests/Runtime/LogosSDK.Editor.SceneSnapshot.Tests.Runtime.asmdef`
- Delete: `Assets/_StudioSDK/Editor/SceneSnapshot/PostProcessors/DOTweenKillProcessor.cs` (+ `.meta`)
- Modify: `Assets/_StudioSDK/Editor/SceneSnapshot/Core/SnapshotOptions.cs`, `Core/SceneSnapshotter.cs`, `UI/SceneSnapshotWindow.cs`, `README.md`

LitMotion không có sổ tween theo target nên `DOTweenKillProcessor` không có bản tương đương; sau khi gỡ DOTween nó chỉ còn là code chết (tìm kiểu bằng reflection, không thấy thì thôi).

- [ ] **Step 1: Spawner và asmdef**

`SnapshotTestSpawner.cs`:

```csharp
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot.Tests
{
    /// <summary>
    /// Runtime helper for the SceneSnapshot acceptance tests.
    /// Spawns N cubes at random positions and starts a LitMotion rotation tween
    /// on the first cube to verify the snapshot carries no running tween.
    /// </summary>
    public sealed class SnapshotTestSpawner : MonoBehaviour
    {
        [SerializeField] private int _cubeCount = 50;
        [SerializeField] private float _spawnRange = 10f;
        [SerializeField] private bool _spawnTween = true;

        private void Start()
        {
            var parent = new GameObject("Cubes").transform;
            for (int i = 0; i < _cubeCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Cube_{i:00}";
                go.transform.SetParent(parent, worldPositionStays: true);
                go.transform.position = new Vector3(
                    Random.Range(-_spawnRange, _spawnRange),
                    Random.Range(0f, _spawnRange),
                    Random.Range(-_spawnRange, _spawnRange));
                go.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);

                if (_spawnTween && i == 0)
                {
                    // DORotate(0,360,0, FastBeyond360).SetLoops(-1): không SetEase → OutQuad.
                    LMotion.Create(go.transform.eulerAngles.y, 360f, 2f)
                        .WithEase(Ease.OutQuad)
                        .WithLoops(-1)
                        .BindToEulerAnglesY(go.transform)
                        .AddTo(go);
                }
            }
        }
    }
}
```

`LogosSDK.Editor.SceneSnapshot.Tests.Runtime.asmdef` — `references` thành:

```json
    "references": [
        "LitMotion",
        "LitMotion.Extensions"
    ],
```

- [ ] **Step 2: Bỏ processor**

```bash
rm Assets/_StudioSDK/Editor/SceneSnapshot/PostProcessors/DOTweenKillProcessor.cs Assets/_StudioSDK/Editor/SceneSnapshot/PostProcessors/DOTweenKillProcessor.cs.meta
```

- `SnapshotOptions.cs`: xoá dòng `public bool killDOTweenTweens = true;`
- `SceneSnapshotter.cs`: xoá dòng `new DOTweenKillProcessor(),`
- `SceneSnapshotWindow.cs`: xoá dòng `_options.killDOTweenTweens = EditorGUILayout.Toggle("Kill DOTween Tweens", _options.killDOTweenTweens);`
- `README.md`:
  - xoá dòng bảng bắt đầu `| \`DOTweenKillProcessor\` |`
  - `The Reflex/R3/DOTween/Addressables processors use **reflection**` → `The Reflex/R3/Addressables processors use **reflection**`
  - trong khối `SnapshotOptions`, xoá dòng `public bool killDOTweenTweens;            // default true`
  - `randomly-positioned cubes + a DOTween rotation tween on the first cube).` → `randomly-positioned cubes + a LitMotion rotation tween on the first cube).`

- [ ] **Step 3: Kiểm**

```bash
grep -rni "dotween" Assets/_StudioSDK || echo "SDK SẠCH"
./compilecheck.sh
```

Expected: `SDK SẠCH`; 3 dòng OK. Focus Unity: Console 0 error. Mở cửa sổ Scene Snapshot: mục Options không còn "Kill DOTween Tweens", danh sách Post-Processors còn 4.

- [ ] **Step 4: Commit**

```bash
git -C D:/CategorySort add Assets/_StudioSDK/Editor/SceneSnapshot
git -C D:/CategorySort commit -m "Drop DOTween from SceneSnapshot: LitMotion spawner, remove kill processor" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: Gỡ DOTween khỏi repo + cập nhật ADR

**Files:**
- Delete: `Assets/Plugins/` (chỉ chứa `Demigiant/`) + `Assets/Plugins.meta`
- Delete: `Assets/Resources/DOTweenSettings.asset` (+ `.meta`)
- Delete: `Packages/com.brunomikoski.animationsequencer/` (package nhúng dựa trên DOTween, 0 prefab/scene dùng `AnimationSequencerController` — guid `c10d422b52559da41aa9676770a8fc65`)
- Modify: `ProjectSettings/ProjectSettings.asset` (define), `Packages/com.cysharp.unitask/Runtime/External/DOTween/UniTask.DOTween.asmdef`, `compilecheck.sh`, `Assets/_Game/Contracts/LevelEvents.cs`
- Modify (docs): `docs/architecture/view-prefabs.md`, `docs/development-plan.md`, `design/gdd/systems-index.md`

**Đóng Unity trước khi bắt đầu** — Editor đang mở sẽ ghi đè `ProjectSettings.asset`.

- [ ] **Step 1: Xác nhận không còn code nào dùng DOTween**

```bash
grep -rl --include=*.cs "DG.Tweening" Assets | grep -v "^Assets/Plugins/Demigiant" || echo "KHÔNG CÒN"
grep -rl --include=*.prefab --include=*.unity --include=*.asset c10d422b52559da41aa9676770a8fc65 Assets || echo "SEQUENCER KHÔNG DÙNG"
```

Expected: `KHÔNG CÒN`, `SEQUENCER KHÔNG DÙNG`. Khác đi thì dừng — task trước còn sót.

- [ ] **Step 2: Xoá file**

```bash
git -C D:/CategorySort rm -r -q Assets/Plugins Assets/Plugins.meta Assets/Resources/DOTweenSettings.asset Assets/Resources/DOTweenSettings.asset.meta Packages/com.brunomikoski.animationsequencer
ls Assets/Plugins 2>/dev/null || echo "Plugins đã xoá"
```

- [ ] **Step 3: Define và asmdef UniTask**

Trong `ProjectSettings/ProjectSettings.asset`, thay nguyên khối từ dòng `  scriptingDefineSymbols:` tới hết dòng `    tvOS: DOTWEEN;DOTWEEN_UITOOLKIT` (19 dòng — mọi define trong khối đều thuộc DOTween: `DOTWEEN`, `DOTWEEN_UITOOLKIT`, `DOTWEEN_ENABLED`, `UNITASK_DOTWEEN_SUPPORT`) bằng một dòng:

```yaml
  scriptingDefineSymbols: {}
```

`UNITASK_DOTWEEN_SUPPORT` phải đi: nó bật code `DOTweenAsyncExtensions.cs` của UniTask trên Standalone/WebGL, thiếu DOTween là build hai nền tảng đó gãy.

`Packages/com.cysharp.unitask/Runtime/External/DOTween/UniTask.DOTween.asmdef` — `references` thành:

```json
    "references": [
        "UniTask"
    ],
```

- [ ] **Step 4: compilecheck.sh**

- Xoá cả **ba** dòng `echo "-r:\"$(w "$PWD/Assets/Plugins/Demigiant/DOTween/DOTween.dll")\""` (game, editor, meta).
- Trong lệnh `find` của khối meta, xoá dòng `"$PWD/Assets/Plugins/Demigiant/DOTween/Modules" -name '*.cs' \` và chuyển `-name '*.cs' \` lên cuối dòng `"$PWD/Assets/BoosterModule"`, thành:

```bash
    find "$PWD/Assets/_StudioSDK" "$PWD/Assets/_Modules" "$PWD/Assets/_Game" "$PWD/Assets/BoosterModule" -name '*.cs' \
         -not -path '*/Tests/*' -not -path '*/_Game/Board/*' | while read -r f; do
```

- Xoá dòng comment `    # DOTween/Modules đi kèm vì CheatToastView dùng DOFade/DOAnchorPosY trên UI`.
- Thay ba dòng comment

```bash
    # Bộ define khớp Unity thật (đọc từ .rsp Unity sinh ra). NET_STANDARD_2_0 và
    # UNITY_2018_1_OR_NEWER là bắt buộc: DOTweenModuleUnityVersion giấu
    # AsyncWaitForCompletion sau đúng hai cờ đó, mà LogosSDK.UI await nó.
```

bằng `    # Bộ define khớp Unity thật (đọc từ .rsp Unity sinh ra).`

- Thay đầu file

```bash
# Tách làm 3 assembly đúng như Unity: WordStack.Board (domain + view, cần DOTween),
# WordStack.Board.Editor (tool level, cần UnityEditor) và WordStack.Meta (thế giới
# netstandard2.1: R3/Reflex/EventBus). Không tách thì xung khắc reference: DOTween.dll
# build theo mscorlib, còn UnityEditor.dll/R3.dll theo netstandard.
```

bằng

```bash
# Tách làm 3 assembly đúng như Unity: WordStack.Board (domain + view, cần LitMotion),
# WordStack.Board.Editor (tool level, cần UnityEditor) và WordStack.Meta (thế giới
# netstandard2.1: R3/Reflex/EventBus). Ranh giới mscorlib/netstandard có từ thời DOTween.dll
# (build theo mscorlib); DOTween đã gỡ 2026-09-17, cách tách giữ nguyên vì vẫn chạy đúng.
```

- Trong comment khối game, `R3/Reflex là netstandard2.1, xung khắc DOTween)` → `R3/Reflex là netstandard2.1, xung khắc ref set mscorlib 4.7.1)`.

- [ ] **Step 5: `LevelEvents.cs`**

Thay hai dòng

```csharp
    // compilecheck.sh: R3 là netstandard2.1 còn DOTween là mscorlib, và
    // Core.EventBus dùng ValueTask — kiểu không có trong ref set 4.7.1-api.
```

bằng

```csharp
    // compilecheck.sh: R3 là netstandard2.1 còn target `game` build theo mscorlib 4.7.1,
    // và Core.EventBus dùng ValueTask — kiểu không có trong ref set 4.7.1-api.
```

- [ ] **Step 6: ADR và tài liệu**

`docs/architecture/view-prefabs.md`, dòng 3: `Q3 DOTween.` → `Q3 DOTween → **LitMotion** (đổi 2026-09-17, xem cuối Q3).`

Cùng file, ngay sau đoạn bắt đầu `Chỗ **không** dùng DOTween: ghost đuổi con trỏ.` (hết đoạn đó), thêm:

```markdown
**Cập nhật 2026-09-17 — Q3 đổi sang LitMotion 2.0.2.** *(User chốt.)* Thống nhất với bộ công cụ
animation của Mukbang ASMR (SacredTimeline, Spiral Move, dựng animation trong Inspector bằng
`LitMotion.Animation`) — nay sống ở `Assets/_StudioSDK/Tween/` (assembly `LogosSDK.Tween`) — và
không sinh rác bộ nhớ. Cài qua git URL khoá commit trong `Packages/manifest.json`, hết phải commit
asset Asset Store vào repo public. Chuyển đổi: `docs/superpowers/plans/2026-09-17-litmotion-migration.md`.

Ba điều phải nhớ khi viết tween mới:
- LitMotion chốt giá trị đầu lúc **tạo** motion; trong `LSequence` mỗi nhịp phải khai "đi từ đâu".
- `Append` của LitMotion không tính nhịp `Join`/`Insert` dài hơn — chuỗi có nhịp chồng nhau thì dùng `Insert(vị trí, …)`.
- Không `Destroy` target trong lúc sequence còn chạy; huỷ sau khi chuỗi xong.
```

`docs/development-plan.md` dòng 126:
`5. **Không thêm dependency ngoài**: DOTween cân nhắc duy nhất cho tween (hoặc tự viết lerp đơn giản).`
→ `5. **Không thêm dependency ngoài**: thư viện tween duy nhất là LitMotion (đổi từ DOTween 2026-09-17, xem \`docs/architecture/view-prefabs.md\` Q3).`

`design/gdd/systems-index.md` dòng 191: `*(DOTween + 4 behaviour đã có — reverse một phần)*` → `*(LitMotion + 4 behaviour đã có — reverse một phần)*`.

`production/session-state/active.md` là nhật ký phiên cũ — **không sửa**.

- [ ] **Step 7: Mở Unity và kiểm**

Mở Unity, đợi import + compile xong.
Expected: Console 0 error. `git -C D:/CategorySort diff --stat Packages/packages-lock.json` có thay đổi (mất mục `com.brunomikoski.animationsequencer`). Player Settings › Scripting Define Symbols rỗng ở Standalone và Android.

```bash
./compilecheck.sh
grep -rIli "dotween\|DG\.Tweening" --exclude-dir={Library,Temp,obj,Logs,Build,.git,.claude,docs,production,UserSettings} . | grep -v "\.csproj$\|\.sln$"
```

Expected: 3 dòng OK. `grep` chỉ còn đúng 3 file vendored của UniTask (code của họ, bị `#if UNITASK_DOTWEEN_SUPPORT` tắt):

```
./Packages/com.cysharp.unitask/Runtime/External/DOTween/DOTweenAsyncExtensions.cs
./Packages/com.cysharp.unitask/Runtime/External/DOTween/UniTask.DOTween.asmdef
./Packages/com.cysharp.unitask/Runtime/_InternalVisibleTo.cs
```

(Build Settings không có Standalone/WebGL để thử build thì thôi — define đã gỡ ở Step 3.)

- [ ] **Step 8: Commit**

```bash
git -C D:/CategorySort add ProjectSettings/ProjectSettings.asset Packages/packages-lock.json Packages/com.cysharp.unitask/Runtime/External/DOTween/UniTask.DOTween.asmdef compilecheck.sh Assets/_Game/Contracts/LevelEvents.cs docs/architecture/view-prefabs.md docs/development-plan.md design/gdd/systems-index.md
git -C D:/CategorySort status --short
git -C D:/CategorySort commit -m "Remove DOTween and the unused AnimationSequencer package" -m "Clear DOTween scripting defines, update compilecheck and ADR Q3 to LitMotion." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

Expected trước commit: ngoài các file vừa add (kể cả `D` từ `git rm` ở Step 2), `status` chỉ còn 3 dòng ` M` cũ của user.

---

### Task 8: Nghiệm thu bằng mắt

**Files:** không sửa file nào (lệch thì sửa ở task tương ứng rồi commit riêng).

- [ ] **Step 1: Chuẩn bị**

Play `Main.unity`. Mở video mốc `D:/CategorySort-baseline/` (Task 0) cạnh cửa sổ Game.

- [ ] **Step 2: So từng mục với video DOTween**

| # | Mục | Cách gọi | Nhìn gì |
|---|---|---|---|
| 1 | Loading | vào game | thanh chạy nhanh rồi chậm tới ~90%, đầy lúc ẩn |
| 2 | MainMenu vào/ra | mở/đóng menu | FadeTransition mờ vào **OutQuad** (Unset cũ) |
| 3 | Popup ScaleFade / DropBounce / SlideDownBox / FadePopupBG | mở từng popup dùng profile đó | nhịp nảy, hướng trượt, độ vượt OutBack |
| 4 | Stagger Pop / StampDrop | màn có danh sách nở | từng phần tử lệch nhịp, dập xuống bẹt rồi bật |
| 5 | Nút bấm | nhấn giữ rồi thả một nút | co 0.88 tuyến tính, thả nảy OutBack |
| 6 | Nút CTA thở / lắc | để yên trên menu | nhịp thở; **wobble punch** (công thức khác) |
| 7 | Toggle âm thanh | bật / tắt | bật nảy; **tắt rung scale** (công thức khác) |
| 8 | Nút booster | bấm | **punch scale** (công thức khác) |
| 9 | Toast CHEAT | bấm một lệnh CHEAT | trôi lên, hiện ~5.0s, mờ đi |
| 10 | Progress bar | gom một nhóm | trượt 0.25s |
| 11 | Kéo thẻ | nhấc một thẻ | ghost phồng OutBack, bóng lùi ra |
| 12 | Hover | rê qua nhiều thẻ | phồng 1.07 + **giật xoay** (công thức khác), không cộng dồn góc |
| 13 | Thả sai | thả vào hộp đầy | **hộp rung ngang** (công thức khác), rung dồn không trôi vị trí |
| 14 | Thả đúng / SnapBack | thả thẻ | bay về ô OutCubic 0.16s |
| 15 | CLEAR | gom đủ 4 thẻ | 4 thẻ co về 0 lệch nhịp InBack, hộp rỗng co + mờ |
| 16 | COLLAPSE | gom nhóm có cha | 4 thẻ **lấy đà ra ngoài** rồi chụm (overshoot 0.6), thẻ mới nở + xoay 140° |
| 17 | Nam châm | booster Nam châm (có nhóm cha nếu được) | nền xám mờ vào; thẻ chôn nhô lên, phồng, bay về điểm hội tụ, khựng, nổ; thẻ cha nở tại điểm gộp rồi bay về ô |
| 18 | Xáo | booster Xáo | pha vào xoáy 3 vòng; pha ra **giữ nguyên hành vi cũ** (pivot không xoáy, thẻ tự quay) |
| 19 | Undo | kéo 1 nước làm lộ hộp dưới, rồi Undo | hộp cũ trượt từ trên xuống + hiện dần, thẻ trong nó nở, thẻ vừa kéo nảy rồi bay về |

Mục in **đậm "công thức khác"** (6, 7, 8, 12, 13): LitMotion không có punch/shake giống DOTween. Nếu lệch rõ, chỉnh `WithFrequency`/`WithDampingRatio` tại đúng dòng đã ghi chú trong code — mỗi lần chỉnh là một commit `Tune <mục> punch to match DOTween feel`.

Các mục còn lại phải **không phân biệt được** với video mốc. Lệch → xem lại bảng quy đổi và ba bẫy `LSequence` ở Global Constraints cho đoạn code đó.

- [ ] **Step 3: Báo user**

Liệt kê: số mục khớp, mục đã chỉnh punch/shake (kèm giá trị), mục còn lệch (nếu có). User duyệt thì push nhánh `feat/litmotion-migration`.

---

## Việc để sau (không làm trong plan này)

- **Toast CHEAT hiện ~5.0s** dù summary của `CheatToastView` ghi ~2.5s. Nguyên nhân: `AppendInterval` của DOTween nối sau cú trôi lên 2.85s, không phải sau cú mờ vào. Plan giữ nguyên để việc chuyển thư viện không đổi hành vi; muốn 2.5s thì đặt `fadeOutAt = _visibleDuration` trong `ShowToast`.
- **Xáo, pha bung ra:** `shuffleTurns` là số nguyên nên pivot đọc góc đầu = 0, không xoáy, thay vào đó từng thẻ tự quay 1080°. Đã có từ bản DOTween, plan giữ nguyên. Muốn xoáy ra đối xứng pha vào thì dùng `Create(-spin, 0f, dur)` cho pivot và `Create(spin, 0f, dur)` cho thẻ (khai góc tường minh thay vì đọc `eulerAngles`).
- **Pha Xáo có thể dùng lại `SpiralMotionAdapter`** thay cho pivot xoay. Đây là thay đổi thiết kế, không phải việc chuyển thư viện.
- **`BrunoMikoski.AnimationSequencer.csproj`** và các `DOTween*.csproj` ở gốc repo là file IDE sinh ra (đã `.gitignore`), sẽ tự mất khi Unity sinh lại project file.
