using System;
using LogosSDK.Core.Logging;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Flow
{
    /// <summary>
    /// Cầu nối gameplay → ViewModel. BoardController (Assembly-CSharp) không được
    /// tham chiếu WordStack.Meta, nên nó chỉ bắn LevelSignals; adapter này nghe rồi
    /// gọi Notify* trên <see cref="IGameplayFlowController"/>.
    ///
    /// Là POCO, dựng bởi DI, sống suốt app. Đăng ký eager để có mặt trước khi màn
    /// đầu tiên chạy — không ai inject nó, nếu Lazy thì không bao giờ được tạo.
    /// </summary>
    public sealed class GameplayFlowAdapter : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GameplayFlowAdapter>();

        private readonly IGameplayFlowController _flow;

        // Board đếm số nước ĐÃ dùng, còn ViewModel muốn số nước CÒN LẠI.
        // Chốt mốc lúc màn bắt đầu, khi RemainingMoves vẫn đang bằng StartingMoves.
        private int _startingMoves;

        // Luật thua hết nước sống Ở ĐÂY, không trong domain: CheckStatus của board
        // không biết move cap (đổi nó là solver/selfcheck lệch demo/check.mjs).
        // Vì board không tự bắn Finished (status còn Playing), adapter phải bắn
        // kênh meta thay — cùng khuôn ForceOutcomeAsync của AppFlowContext.
        private int _levelIndex;
        private bool _outOfMovesReported;

        public GameplayFlowAdapter(IGameplayFlowController flow)
        {
            _flow = flow;

            LevelSignals.Started += OnLevelStarted;
            LevelSignals.FirstInteraction += OnFirstInteraction;
            LevelSignals.MoveCommitted += OnMoveCommitted;
            LevelSignals.EvaluationCompleted += OnEvaluationCompleted;
            LevelSignals.AnimationCompleted += OnAnimationCompleted;
        }

        public void Dispose()
        {
            LevelSignals.Started -= OnLevelStarted;
            LevelSignals.FirstInteraction -= OnFirstInteraction;
            LevelSignals.MoveCommitted -= OnMoveCommitted;
            LevelSignals.EvaluationCompleted -= OnEvaluationCompleted;
            LevelSignals.AnimationCompleted -= OnAnimationCompleted;
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            _startingMoves = _flow.RemainingMoves.CurrentValue;
            _levelIndex = evt.LevelIndex;
            _outOfMovesReported = false;
        }

        private void OnFirstInteraction() => Fire(() => _flow.NotifyFirstInteractionAsync());

        private void OnMoveCommitted(int movesUsed)
        {
            Fire(() => _flow.NotifyPlayerActionCommittedAsync(new GameplayActionContext
            {
                RemainingMoves = Remaining(movesUsed),
            }));
        }

        private void OnEvaluationCompleted(LevelEvaluationEvent evt)
        {
            // Hết nước mà bàn chưa ngã ngũ → thua. Guard _startingMoves > 0 để màn
            // chưa cấu hình moves (mốc 0) không thành thua-ngay-nước-đầu.
            bool loseByMoves = !evt.IsWin && !evt.IsLose && !_outOfMovesReported
                               && _startingMoves > 0 && Remaining(evt.MovesUsed) <= 0;
            if (loseByMoves)
            {
                _outOfMovesReported = true;
                // Kênh meta trước (trừ tim, progression) rồi mới báo ViewModel —
                // cùng thứ tự với kết quả thật từ board và với ForceOutcomeAsync.
                LevelSignals.RaiseFinished(false, _levelIndex, evt.MovesUsed);
            }

            Fire(() => _flow.NotifyEvaluationCompletedAsync(new GameplayEvaluationResult
            {
                IsWin = evt.IsWin,
                IsLose = evt.IsLose || loseByMoves,
                HasPendingAnimation = evt.HasPendingAnimation,
                RemainingMoves = Remaining(evt.MovesUsed),
                CanRetry = true,
                CanContinueToNext = evt.IsWin,
            }));
        }

        private void OnAnimationCompleted() => Fire(() => _flow.NotifyAnimationSequenceCompletedAsync());

        private int Remaining(int movesUsed)
        {
            int left = _startingMoves - movesUsed;
            return left < 0 ? 0 : left;
        }

        // Notify* trả Awaitable nhưng thân hàm chạy đồng bộ tới hết, không await gì —
        // nên gọi là xong, không cần await. Bọc try/catch để lỗi phía meta không nổ
        // ngược vào coroutine Settle() của board.
        private void Fire(Func<UnityEngine.Awaitable> call)
        {
            try { call(); }
            catch (Exception ex) { _logger.Error(ex, "[GameplayFlowAdapter] Notify thất bại."); }
        }
    }
}
