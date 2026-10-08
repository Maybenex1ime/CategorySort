using System;
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
        private readonly Action<int> _onDecrease;        // số giảm (vd tiêu coin) → bên gọi hiện "-N"
        private MotionHandle _handle;
        private int _shown;
        private int _target;
        private bool _hasValue;

        public CountUpText(TMP_Text text, float duration = 0.5f, IMotionScheduler scheduler = null, Action<int> onDecrease = null)
        {
            _text = text;
            _duration = duration;
            _scheduler = scheduler;
            _onDecrease = onDecrease;
        }

        public int Shown => _shown;

        public void Set(int value)
        {
            if (!_hasValue) { SetImmediate(value); return; }
            if (value == _target) return;   // đang đếm tới đúng số này, hoặc đã hiện nó
            if (value < _target) _onDecrease?.Invoke(_target - value);
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
