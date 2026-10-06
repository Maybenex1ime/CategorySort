# UI Coin Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Coin numbers count up instead of jumping, coins fly into the wallet on Claim and on shop coin purchases, Play button pulses.

**Architecture:** Two shared pieces in `Assets/_Game/UI/Common/`: `CountUpText` (plain class wrapping a TMP text) and `CoinFly` (MonoBehaviour spawning pooled coin Images). Views swap `text = v.ToString()` for `CountUpText.Set`. `CompletedPopup` gets an optional coin box and runs the fly before closing. Play pulse is prefab-only (existing `UIIdlePulseDriver`).

**Tech Stack:** Unity 6000.3.8f1, LitMotion 2.0.2 (+ Extensions), TextMeshPro, uGUI, Reflex `[Inject]`, R3, NUnit EditMode.

Spec: `docs/superpowers/specs/2026-10-06-ui-coin-anim-design.md`.

## Global Constraints

- Work only in `D:\CategorySort`. Never touch the mukbang project.
- Do not edit `.prefab`, `.unity`, `.asset` on disk; prefab wiring is the user's job in Unity.
- New `.cs` files get their `.meta` from Unity; do not hand-write `.meta`.
- Missing serialized refs must fall back to today's behaviour (Claim closes at once, shop only punches).
- `git add` explicit paths only. Commit trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Verify: `./compilecheck.sh` then `bash .git/sdd/testcheck.sh` (compiles only; tests run in Unity Test Runner).

---

### Task 1: CountUpText

**Files:**
- Create: `Assets/_Game/UI/Common/CountUpText.cs`
- Create: `Assets/_Game/Gameplay/Tests/CountUpTextTests.cs`
- Modify: `Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef` (add `"Unity.TextMeshPro"`, `"Unity.ugui"` to `references`)

**Interfaces:**
- Produces: `LogosGame.Features.UI.Common.CountUpText(TMP_Text text, float duration = 0.5f, IMotionScheduler scheduler = null)`, `void Set(int)`, `void SetImmediate(int)`, `int Shown { get; }`, `void Dispose()`.

- [ ] **Step 1: Write the failing test**

```csharp
// CountUpText: số coin đếm dần thay vì nhảy (report UI animation #2).
using LitMotion;
using LogosGame.Features.UI.Common;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    public class CountUpTextTests
    {
        GameObject _go;
        TextMeshProUGUI _text;
        ManualMotionDispatcher _clock;
        CountUpText _count;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("count");
            _text = _go.AddComponent<TextMeshProUGUI>();
            _clock = new ManualMotionDispatcher();
            _count = new CountUpText(_text, 1f, _clock.Scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            _count.Dispose();
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void FirstValueShowsImmediately()
        {
            _count.Set(120);
            Assert.AreEqual("120", _text.text);
        }

        [Test]
        public void LaterValueCountsUp()
        {
            _count.Set(100);
            _count.Set(200);
            _clock.Update(0.5);
            Assert.That(_count.Shown, Is.GreaterThan(100).And.LessThan(200), "giữa chừng phải là số trung gian");
            _clock.Update(0.6);
            Assert.AreEqual("200", _text.text);
        }

        [Test]
        public void NewTargetMidwayContinuesFromShownNumber()
        {
            _count.Set(0);
            _count.Set(100);
            _clock.Update(0.5);
            int mid = _count.Shown;
            _count.Set(50);
            Assert.AreEqual(mid, _count.Shown, "đổi đích không được nhảy số");
            _clock.Update(1.1);
            Assert.AreEqual("50", _text.text);
        }

        [Test]
        public void SetImmediateCancelsCounting()
        {
            _count.Set(0);
            _count.Set(100);
            _count.SetImmediate(7);
            _clock.Update(2);
            Assert.AreEqual("7", _text.text);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: meta-tests FAIL — `CountUpText` not found.

- [ ] **Step 3: Implement**

```csharp
using LitMotion;
using TMPro;

namespace LogosGame.Features.UI.Common
{
    /// Ô số đếm dần tới giá trị mới thay vì nhảy (report UI animation #2).
    /// Lần Set đầu gán ngay; đổi đích giữa chừng thì đếm tiếp từ số đang hiện.
    public sealed class CountUpText
    {
        private readonly TMP_Text _text;
        private readonly float _duration;
        private readonly IMotionScheduler _scheduler;   // null = mặc định; test truyền ManualMotionDispatcher
        private MotionHandle _handle;
        private int _shown;
        private int _target;
        private bool _hasValue;

        public CountUpText(TMP_Text text, float duration = 0.5f, IMotionScheduler scheduler = null)
        {
            _text = text;
            _duration = duration;
            _scheduler = scheduler;
        }

        public int Shown => _shown;

        public void Set(int value)
        {
            if (!_hasValue) { SetImmediate(value); return; }
            if (value == _target) return;   // đang đếm tới đúng số này, hoặc đã hiện nó
            _target = value;
            _handle.TryCancel();
            _handle = LMotion.Create(_shown, value, _duration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(_scheduler)
                .WithCancelOnError()
                .Bind(Show)
                .AddTo(_text.gameObject);
        }

        public void SetImmediate(int value)
        {
            _handle.TryCancel();
            _hasValue = true;
            _target = value;
            Show(value);
        }

        public void Dispose() => _handle.TryCancel();

        private void Show(int value)
        {
            _shown = value;
            _text.text = value.ToString();
        }
    }
}
```

- [ ] **Step 4: Run to verify it compiles**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected: all OK. Run `CountUpTextTests` in Unity Test Runner (EditMode) — Expected: 4 pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/UI/Common/CountUpText.cs Assets/_Game/Gameplay/Tests/CountUpTextTests.cs Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef
git commit -m "UI: CountUpText counts coin numbers up instead of jumping"
```

### Task 2: Count-up in HUD, Home, Shop

**Files:**
- Modify: `Assets/_Game/Gameplay/Views/GameplayHudView.cs` (coin subscribe in `Start`)
- Modify: `Assets/_Game/UI/Screens/MainMenuScreen.cs` (`BindCoinUI`)
- Modify: `Assets/_Game/UI/Popups/ShopPopup.cs` (`BindCoinCounter`)

**Interfaces:**
- Consumes: `CountUpText` from Task 1.

- [ ] **Step 1: Swap the three subscriptions**

Each file adds `using LogosGame.Features.UI.Common;` and a field, then routes the value through it.

`GameplayHudView`:
```csharp
private CountUpText _coinCount;
// Start():
_coinCount = new CountUpText(_coinText);
_resources.Observe(ResourceType.Coin)
    .Subscribe(value => _coinCount.Set(value))
    .AddTo(ref _disposables);
```

`MainMenuScreen.BindCoinUI`:
```csharp
private CountUpText _coinCount;
// BindCoinUI():
_coinCount = new CountUpText(_coinCountText);
_resources.Observe(ResourceType.Coin)
    .Subscribe(value => _coinCount.Set(value))
    .AddTo(ref _disposables);
```

`ShopPopup.BindCoinCounter`:
```csharp
private CountUpText _coinCounter;
// BindCoinCounter():
_coinCounter = new CountUpText(_coinCounterText);
_coinCounterSubscription = _currencyService.Coins
    .Subscribe(coins => _coinCounter.Set(coins));
```

- [ ] **Step 2: Verify**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected: OK.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Gameplay/Views/GameplayHudView.cs Assets/_Game/UI/Screens/MainMenuScreen.cs Assets/_Game/UI/Popups/ShopPopup.cs
git commit -m "UI: coin counters in HUD, Home and Shop count up"
```

### Task 3: CoinFly

**Files:**
- Create: `Assets/_Game/UI/Common/CoinFly.cs`
- Create: `Assets/_Game/Gameplay/Tests/CoinFlyTests.cs`

**Interfaces:**
- Produces: `LogosGame.Features.UI.Common.CoinFly : MonoBehaviour` with `void Play(Vector3 from, Vector3 to, Action onFirstArrive, Action onDone, Action onEachArrive = null)`, `void Stop()`, `int Count { get; }`, `IMotionScheduler Scheduler { get; set; }`.

- [ ] **Step 1: Write the failing test**

```csharp
// CoinFly: coin bung ra rồi bay về ví (report UI animation #1).
using LitMotion;
using LogosGame.Features.UI.Common;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    public class CoinFlyTests
    {
        GameObject _go;
        CoinFly _fly;
        ManualMotionDispatcher _clock;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("fly", typeof(RectTransform));
            _fly = _go.AddComponent<CoinFly>();
            _clock = new ManualMotionDispatcher();
            _fly.Scheduler = _clock.Scheduler;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void EachCallbackFiresTheRightNumberOfTimes()
        {
            int first = 0, done = 0, each = 0;
            _fly.Play(Vector3.zero, new Vector3(500f, 0f, 0f), () => first++, () => done++, () => each++);
            _clock.Update(5);

            Assert.AreEqual(1, first, "coin đầu tới nơi");
            Assert.AreEqual(1, done, "xong cả lượt");
            Assert.AreEqual(_fly.Count, each, "mỗi coin tới nơi một lần");
            foreach (Transform coin in _go.transform)
                Assert.IsFalse(coin.gameObject.activeSelf, "coin phải ẩn khi tới nơi");
        }

        [Test]
        public void ReplayDropsTheOldRoundCallbacks()
        {
            int oldDone = 0, newDone = 0;
            _fly.Play(Vector3.zero, Vector3.right * 500f, null, () => oldDone++);
            _clock.Update(0.1);
            _fly.Play(Vector3.zero, Vector3.right * 500f, null, () => newDone++);
            _clock.Update(5);

            Assert.AreEqual(0, oldDone);
            Assert.AreEqual(1, newDone);
            Assert.AreEqual(_fly.Count, _go.transform.childCount, "coin được dùng lại, không tạo thêm");
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected: meta-tests FAIL, `CoinFly` not found.

- [ ] **Step 3: Implement**

```csharp
using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Common
{
    /// Coin bung ra quanh `from` rồi lần lượt bay về `to` (report UI animation #1, mẫu AddCoinAnim).
    /// Gắn lên root popup/panel; coin là Image con, tạo lần đầu rồi dùng lại.
    [RequireComponent(typeof(RectTransform))]
    public sealed class CoinFly : MonoBehaviour
    {
        [SerializeField] private Sprite _coinSprite;
        [SerializeField] private Vector2 _coinSize = new Vector2(64f, 64f);
        [SerializeField, Min(1)] private int _count = 8;
        [SerializeField] private float _burstRadius = 80f;   // đơn vị canvas
        [SerializeField] private float _burstDuration = 0.2f;
        [SerializeField] private float _hold = 0.1f;
        [SerializeField] private float _flyDuration = 0.5f;
        [SerializeField] private float _stagger = 0.05f;
        [SerializeField] private float _doneDelay = 0.15f;

        private readonly List<RectTransform> _coins = new List<RectTransform>();
        private readonly List<MotionHandle> _running = new List<MotionHandle>();
        private int _round;

        public IMotionScheduler Scheduler { get; set; }   // null = mặc định; test truyền ManualMotionDispatcher
        public int Count => _count;

        public void Play(Vector3 from, Vector3 to, Action onFirstArrive, Action onDone, Action onEachArrive = null)
        {
            Stop();
            int round = _round;
            int arrived = 0;
            float radius = _burstRadius * transform.lossyScale.x;

            for (int i = 0; i < _count; i++)
            {
                RectTransform coin = Coin(i);
                Vector3 burst = from + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);
                coin.position = from;
                coin.gameObject.SetActive(true);

                _running.Add(LMotion.Create(from, burst, _burstDuration)
                    .WithEase(Ease.OutQuad).WithScheduler(Scheduler).WithCancelOnError()
                    .BindToPosition(coin));

                _running.Add(LMotion.Create(burst, to, _flyDuration)
                    .WithDelay(_burstDuration + _hold + i * _stagger)
                    .WithEase(Ease.InQuad).WithScheduler(Scheduler).WithCancelOnError()
                    .WithOnComplete(() =>
                    {
                        if (round != _round) return;
                        coin.gameObject.SetActive(false);
                        arrived++;
                        if (arrived == 1) onFirstArrive?.Invoke();
                        onEachArrive?.Invoke();
                        if (arrived == _count) _running.Add(After(_doneDelay, round, onDone));
                    })
                    .BindToPosition(coin));
            }
        }

        public void Stop()
        {
            _round++;
            foreach (var h in _running) h.TryCancel();
            _running.Clear();
            foreach (var coin in _coins)
                if (coin != null) coin.gameObject.SetActive(false);
        }

        private void OnDisable() => Stop();

        private MotionHandle After(float delay, int round, Action action)
        {
            return LMotion.Create(0f, 1f, delay).WithScheduler(Scheduler)
                .WithOnComplete(() => { if (round == _round) action?.Invoke(); })
                .RunWithoutBinding();
        }

        private RectTransform Coin(int i)
        {
            while (_coins.Count <= i)
            {
                var go = new GameObject("Coin", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.sizeDelta = _coinSize;
                var image = go.GetComponent<Image>();
                image.sprite = _coinSprite;
                image.raycastTarget = false;
                go.SetActive(false);
                _coins.Add(rt);
            }
            _coins[i].SetAsLastSibling();   // vẽ đè lên nội dung popup
            return _coins[i];
        }
    }
}
```

- [ ] **Step 4: Verify**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected OK. Unity Test Runner `CoinFlyTests` — Expected: 2 pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/UI/Common/CoinFly.cs Assets/_Game/Gameplay/Tests/CoinFlyTests.cs
git commit -m "UI: CoinFly bursts coins and flies them to a target"
```

### Task 4: Coin fly on Claim and on shop purchase

**Files:**
- Modify: `Assets/_Game/UI/Popups/CompletedPopup.cs`
- Modify: `Assets/_Game/UI/Popups/ShopPopup.cs` (`PlayPurchasedFeedback` + field)

**Interfaces:**
- Consumes: `CountUpText` (Task 1), `CoinFly.Play` (Task 3), `ICurrencyService.Coins` (`LogosMeta.Economy`).

- [ ] **Step 1: CompletedPopup**

Add usings `LitMotion`, `LitMotion.Extensions`, `LogosGame.Features.UI.Common`, `LogosMeta.Economy`, `Reflex.Attributes`. Add:

```csharp
[Header("Coin bay về ô coin (bỏ trống = Claim đóng ngay như cũ)")]
[SerializeField] private TextMeshProUGUI _coinBoxText;
[SerializeField] private RectTransform _coinBoxIcon;
[SerializeField] private CoinFly _coinFly;

[Inject] private ICurrencyService _currency;

private CountUpText _coinBox;
private MotionHandle _iconPunch;
private bool _claiming;

private int Coins => _currency != null ? _currency.Coins.CurrentValue : 0;
```

End of `Initialize`:
```csharp
_claiming = false;
SetButtonsInteractable(true);
if (_coinBoxText != null)
{
    _coinBox ??= new CountUpText(_coinBoxText);
    // Coin thắng màn đã cộng trước khi popup mở: ô coin bắt đầu từ số cũ, Claim mới đếm lên.
    _coinBox.SetImmediate(Mathf.Max(0, Coins - args.RewardCoinAmount));
}
```

Replace `OnClaimClicked` and add helpers:
```csharp
private void OnClaimClicked()
{
    if (_claiming) return;
    if (!CanFlyCoins()) { FinishClaim(); return; }

    _claiming = true;
    SetButtonsInteractable(false);
    _coinFly.Play(_rewardAmountText.transform.position, _coinBoxIcon.position,
        onFirstArrive: () => _coinBox.Set(Coins),
        onDone: FinishClaim,
        onEachArrive: PunchIcon);
}

private bool CanFlyCoins()
{
    return _coinFly != null && _coinBox != null && _coinBoxIcon != null && _rewardAmountText != null
        && Args != null && Args.RewardCoinAmount > 0;
}

private void FinishClaim()
{
    Dismiss();

    if (Args != null && Args.OnClaim != null)
    {
        Args.OnClaim();
    }
}

private void PunchIcon()
{
    _iconPunch.TryComplete();   // trả scale về gốc trước khi nảy tiếp
    Vector3 baseScale = _coinBoxIcon.localScale;
    _iconPunch = LMotion.Punch.Create(baseScale, baseScale * 0.15f, 0.2f)
        .WithFrequency(6).WithDampingRatio(3.1f).WithCancelOnError()
        .BindToLocalScale(_coinBoxIcon).AddTo(_coinBoxIcon.gameObject);
}

private void SetButtonsInteractable(bool interactable)
{
    if (_claimButton != null) _claimButton.interactable = interactable;
    if (_doubleRewardButton != null) _doubleRewardButton.interactable = interactable;
}
```

- [ ] **Step 2: ShopPopup**

Add field under the coin counter:
```csharp
[Header("Coin bay về counter khi mua coin (bỏ trống = chỉ nảy như cũ)")]
[SerializeField] private CoinFly _coinFly;
```

Replace `PlayPurchasedFeedback`:
```csharp
// Coin đã cộng qua subscribe (counter tự đếm dần). Có CoinFly: coin bay từ ô vừa mua về counter,
// mỗi coin tới thì counter nảy. Không có: counter nảy một lần như cũ. Banner Remove Ads tự ẩn.
private void PlayPurchasedFeedback(Transform cell, ShopPurchaseResult result)
{
    bool cellAlive = cell != null && cell.gameObject.activeInHierarchy;
    if (_coinCounterText != null && result.CoinsGranted > 0)
    {
        if (_coinFly != null && cellAlive)
            _coinFly.Play(cell.position, _coinCounterText.transform.position, null, null,
                () => _counterPunch = Punch(_coinCounterText.transform, _counterPunch));
        else
            _counterPunch = Punch(_coinCounterText.transform, _counterPunch);
    }
    if (cellAlive) _cellPunch = Punch(cell, _cellPunch);
}
```

- [ ] **Step 3: Verify**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` — Expected OK.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/UI/Popups/CompletedPopup.cs Assets/_Game/UI/Popups/ShopPopup.cs
git commit -m "UI: coins fly to the wallet on Claim and on shop coin purchases"
```

### Task 5 (user, in Unity): wiring

1. `CompletedPopup.prefab`: coin box (icon + TMP) at the top → `_coinBoxText`, `_coinBoxIcon`; `CoinFly` on root with coin sprite → `_coinFly`.
2. Shop panel in `MainMenuScreen.prefab`: `CoinFly` with coin sprite → `ShopPopup._coinFly`.
3. **Create ▸ LogosSDK ▸ UI ▸ Button Feedback** → `SO_Button_CTA`, tick **Idle Pulse Enabled**; `UIIdlePulseDriver` on **Play**, profile = `SO_Button_CTA`.
4. Commit the new `.meta` files and prefab changes; run EditMode tests.
