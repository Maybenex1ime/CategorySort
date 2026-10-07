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
