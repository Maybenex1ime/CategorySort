using System;
using System.Threading;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Economy
{
    public sealed class HeartService : IHeartService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<HeartService>();

        private readonly int _maxHearts;
        private readonly TimeSpan _regenInterval;

        private readonly ISaveManager _save;
        private readonly ITimeProvider _time;

        private readonly ReactiveProperty<int>      _current;
        private readonly ReactiveProperty<bool>     _isFull;
        private readonly ReactiveProperty<TimeSpan> _timeUntilNext;

        private HeartData _data;
        private readonly CancellationTokenSource _tickCts = new CancellationTokenSource();

        public ReadOnlyReactiveProperty<int>      Current       => _current;
        public ReadOnlyReactiveProperty<bool>     IsFull        => _isFull;
        public ReadOnlyReactiveProperty<TimeSpan> TimeUntilNext => _timeUntilNext;

        public HeartService(ISaveManager save, ITimeProvider time, HeartSettings settings)
        {
            _save = save;
            _time = time;
            _maxHearts = settings != null ? settings.MaxHearts : 5;
            _regenInterval = settings != null ? settings.RegenInterval : TimeSpan.FromMinutes(30);
            _data = _save.Load<HeartData>();

            ClampHearts();
            EnsureValidTimestamp();
            AccumulateOnLoad();

            _current       = new ReactiveProperty<int>(_data.Hearts);
            _isFull        = new ReactiveProperty<bool>(_data.Hearts >= _maxHearts);
            _timeUntilNext = new ReactiveProperty<TimeSpan>(ComputeTimeUntilNext());

            // Tick loop runs on Unity main thread via Awaitable.NextFrameAsync.
            // The R3 default scheduler runs off-thread, so Unity UI updates from
            // a non-main-thread subscriber would be silently dropped.
            RunTickLoop(_tickCts.Token);

            _logger.Info($"[HeartService] Ctor done. Hearts={_data.Hearts} IsFull={_data.Hearts >= _maxHearts} (instance={GetHashCode()})");
        }

        private async void RunTickLoop(CancellationToken token)
        {
            // EditMode tests construct HeartService outside Play mode; tests
            // drive UpdateTick() directly, so the loop is a no-op there.
            if (!Application.isPlaying) return;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Awaitable.NextFrameAsync(token);
                    UpdateTick();
                }
            }
            catch (OperationCanceledException) { /* disposed — normal */ }
            catch (Exception ex)
            {
                _logger.Error(ex, "[HeartService] Tick loop crashed");
            }
        }

        // Public so hosts without a frame loop (and EditMode tests) can pump it.
        public void UpdateTick()
        {
            if (_data.Hearts >= _maxHearts) return;

            DateTime lastRegen = new DateTime(_data.LastRegenUtcTicks, DateTimeKind.Utc);
            TimeSpan elapsed = _time.UtcNow - lastRegen;

            if (elapsed >= _regenInterval)
            {
                _data.Hearts++;
                if (_data.Hearts >= _maxHearts)
                {
                    _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
                }
                else
                {
                    _data.LastRegenUtcTicks = lastRegen.Add(_regenInterval).Ticks;
                }

                _save.SaveImmediate(_data);

                _current.Value       = _data.Hearts;
                _isFull.Value        = _data.Hearts >= _maxHearts;
                _timeUntilNext.Value = ComputeTimeUntilNext();
            }
            else
            {
                // Throttle: only emit when the whole-second representation changes
                // (the UI formats mm:ss). Without this, ReactiveProperty fires every
                // frame because tick-precision TimeSpan differs each frame.
                TimeSpan remainingSeconds = TimeSpan.FromSeconds((int)(_regenInterval - elapsed).TotalSeconds);
                if (_timeUntilNext.Value != remainingSeconds)
                {
                    _timeUntilNext.Value = remainingSeconds;
                }
            }
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            if (_data.Hearts >= _maxHearts) return;

            int target = _data.Hearts + amount;
            if (target > _maxHearts) target = _maxHearts;
            _data.Hearts = target;

            if (_data.Hearts >= _maxHearts)
            {
                _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
            }

            _save.SaveImmediate(_data);

            _current.Value       = _data.Hearts;
            _isFull.Value        = _data.Hearts >= _maxHearts;
            _timeUntilNext.Value = ComputeTimeUntilNext();

            _logger.Info($"[HeartService] Add({amount}) → Hearts={_data.Hearts}");
        }

        public void SetHearts(int hearts)
        {
            if (hearts < 0) hearts = 0;
            if (hearts > _maxHearts) hearts = _maxHearts;

            _data.Hearts = hearts;
            _data.LastRegenUtcTicks = _time.UtcNow.Ticks;

            _save.SaveImmediate(_data);

            _current.Value       = _data.Hearts;
            _isFull.Value        = _data.Hearts >= _maxHearts;
            _timeUntilNext.Value = ComputeTimeUntilNext();

            _logger.Info($"[HeartService] SetHearts({hearts}) → Hearts={_data.Hearts}");
        }

        public void ConsumeOne()
        {
            _logger.Info($"[HeartService] ConsumeOne called. Before: Hearts={_data.Hearts} (instance={GetHashCode()})");
            if (_data.Hearts <= 0) return;

            bool wasFull = _data.Hearts >= _maxHearts;
            _data.Hearts--;

            if (wasFull)
            {
                _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
            }

            _save.SaveImmediate(_data);

            _current.Value       = _data.Hearts;
            _isFull.Value        = _data.Hearts >= _maxHearts;
            _timeUntilNext.Value = ComputeTimeUntilNext();

            _logger.Info($"[HeartService] ConsumeOne done. After: Hearts={_data.Hearts} (instance={GetHashCode()})");
        }

        public void Dispose()
        {
            _tickCts.Cancel();
            _tickCts.Dispose();
            _current.Dispose();
            _isFull.Dispose();
            _timeUntilNext.Dispose();
        }

        private void ClampHearts()
        {
            if (_data.Hearts < 0) _data.Hearts = 0;
            if (_data.Hearts > _maxHearts) _data.Hearts = _maxHearts;
        }

        private void EnsureValidTimestamp()
        {
            if (_data.LastRegenUtcTicks == 0)
            {
                _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
            }
        }

        private void AccumulateOnLoad()
        {
            if (_data.Hearts >= _maxHearts)
            {
                _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
                return;
            }

            DateTime lastRegen = new DateTime(_data.LastRegenUtcTicks, DateTimeKind.Utc);
            TimeSpan elapsed = _time.UtcNow - lastRegen;
            if (elapsed.Ticks <= 0) return;

            int add = (int)(elapsed.TotalSeconds / _regenInterval.TotalSeconds);
            if (add <= 0) return;

            int needed = _maxHearts - _data.Hearts;
            int applied = add < needed ? add : needed;
            _data.Hearts += applied;

            if (_data.Hearts >= _maxHearts)
            {
                _data.LastRegenUtcTicks = _time.UtcNow.Ticks;
            }
            else
            {
                _data.LastRegenUtcTicks = lastRegen.Add(TimeSpan.FromTicks(_regenInterval.Ticks * applied)).Ticks;
            }

            _save.SaveImmediate(_data);
        }

        private TimeSpan ComputeTimeUntilNext()
        {
            if (_data.Hearts >= _maxHearts) return TimeSpan.Zero;
            DateTime lastRegen = new DateTime(_data.LastRegenUtcTicks, DateTimeKind.Utc);
            TimeSpan elapsed = _time.UtcNow - lastRegen;
            TimeSpan remaining = _regenInterval - elapsed;
            return remaining.Ticks < 0 ? TimeSpan.Zero : remaining;
        }
    }
}
