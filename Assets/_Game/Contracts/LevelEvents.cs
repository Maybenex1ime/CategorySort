using System;
using UnityEngine;

namespace WordStack.Contracts
{
    // Ranh giới giữa gameplay và tầng meta. Assembly này KHÔNG tham chiếu gì —
    // nhờ vậy BoardController (Assembly-CSharp) báo được sự kiện mà không kéo
    // R3/Reflex/LogosMeta/EventBus vào, thứ sẽ làm hỏng target `game` của
    // compilecheck.sh: R3 là netstandard2.1 còn DOTween là mscorlib, và
    // Core.EventBus dùng ValueTask — kiểu không có trong ref set 4.7.1-api.
    //
    // Vì vậy gameplay bắn qua LevelSignals (C# thuần), MetaSession chuyển tiếp
    // lên Bus.Global để phía meta vẫn nghe bus đúng như aquapark.

    /// <summary>Bắn khi một màn bắt đầu. Tầng meta trừ tim ở đây.</summary>
    public readonly struct LevelStartedEvent
    {
        public int LevelIndex { get; }

        // Mẫu số của progress bar. Phải đi đường này chứ không phải
        // GameplayStartContext: chỉ board parse JSON mới biết số nhóm.
        public int TotalGroups { get; }

        // Difficulty đã rời khỏi event này: runtime đọc độ khó từ catalog
        // (GameplayStartContext → IDifficultyStateProvider), file level chỉ còn
        // là nguồn seed cho tool build catalog.
        public LevelStartedEvent(int levelIndex, int totalGroups)
        {
            LevelIndex = levelIndex;
            TotalGroups = totalGroups;
        }
    }

    /// <summary>
    /// Kết quả một lượt tính (cascade xong). Khác LevelResultEvent: cái này bắn
    /// SAU MỖI nước đi, còn LevelResultEvent chỉ bắn một lần cuối màn.
    /// </summary>
    public readonly struct LevelEvaluationEvent
    {
        public bool IsWin { get; }
        public bool IsLose { get; }

        /// <summary>Có cascade chạy trong lượt này → còn animation đang diễn.</summary>
        public bool HasPendingAnimation { get; }

        public int MovesUsed { get; }

        /// <summary>Số nhóm đã gom xong (Game.Cleared) — tử số của progress bar.</summary>
        public int GroupsCleared { get; }

        public LevelEvaluationEvent(bool isWin, bool isLose, bool hasPendingAnimation, int movesUsed, int groupsCleared)
        {
            IsWin = isWin;
            IsLose = isLose;
            HasPendingAnimation = hasPendingAnimation;
            MovesUsed = movesUsed;
            GroupsCleared = groupsCleared;
        }
    }

    /// <summary>Bắn đúng MỘT lần khi màn kết thúc (thắng hoặc kẹt).</summary>
    public readonly struct LevelResultEvent
    {
        public bool IsWin { get; }
        public int LevelIndex { get; }
        public int MovesUsed { get; }

        public LevelResultEvent(bool isWin, int levelIndex, int movesUsed)
        {
            IsWin = isWin;
            LevelIndex = levelIndex;
            MovesUsed = movesUsed;
        }
    }

    /// <summary>
    /// Điểm phát tín hiệu của gameplay. Không có người nghe thì không xảy ra gì —
    /// prototype vẫn chơi được y như cũ khi tầng meta chưa gắn vào scene.
    /// </summary>
    public static class LevelSignals
    {
        public static event Action<LevelStartedEvent> Started;
        public static event Action<LevelResultEvent> Finished;

        /// <summary>Chạm đầu tiên trong màn — tầng meta dùng để tắt overlay "sẵn sàng".</summary>
        public static event Action FirstInteraction;

        /// <summary>Vừa đi xong một nước (chưa cascade). Tham số: tổng số nước đã dùng.</summary>
        public static event Action<int> MoveCommitted;

        /// <summary>Cascade tính xong.</summary>
        public static event Action<LevelEvaluationEvent> EvaluationCompleted;

        /// <summary>Chuỗi animation của lượt vừa rồi đã chạy hết.</summary>
        public static event Action AnimationCompleted;

        /// <summary>
        /// Bàn có mục tiêu hợp lệ cho booster Nam châm không. Board đẩy cờ này sau mỗi
        /// lần settle (và lúc nạp màn) — nam châm CÓ LÚC bất lực: khi chỉ còn nhóm cha
        /// đang chờ nhóm con collapse thì không nhóm nào đủ 4 thẻ trên bàn để hút.
        ///
        /// Đẩy trạng thái thay vì hoàn lượt: BoosterManager trừ lượt TRƯỚC khi bàn kịp
        /// từ chối, mà lượt này người chơi mua bằng coin — để bấm hụt rồi mất lượt là
        /// mất tiền thật.
        /// </summary>
        public static event Action<bool> MagnetAvailabilityChanged;

        public static bool MagnetAvailable { get; private set; }

        public static void SetMagnetAvailable(bool available)
        {
            if (MagnetAvailable == available) return;
            MagnetAvailable = available;
            MagnetAvailabilityChanged?.Invoke(available);
        }

        public static void RaiseStarted(int levelIndex, int totalGroups)
            => Started?.Invoke(new LevelStartedEvent(levelIndex, totalGroups));

        public static void RaiseFinished(bool isWin, int levelIndex, int movesUsed)
            => Finished?.Invoke(new LevelResultEvent(isWin, levelIndex, movesUsed));

        public static void RaiseFirstInteraction()
            => FirstInteraction?.Invoke();

        public static void RaiseMoveCommitted(int movesUsed)
            => MoveCommitted?.Invoke(movesUsed);

        public static void RaiseEvaluationCompleted(bool isWin, bool isLose, bool hasPendingAnimation, int movesUsed, int groupsCleared)
            => EvaluationCompleted?.Invoke(new LevelEvaluationEvent(isWin, isLose, hasPendingAnimation, movesUsed, groupsCleared));

        public static void RaiseAnimationCompleted()
            => AnimationCompleted?.Invoke();

        // Event static sống sót qua lần Play kế tiếp khi Domain Reload bị tắt, khiến
        // người nghe cũ bị gọi lại và coin cộng hai lần. Xoá sạch lúc khởi động.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Started = null;
            Finished = null;
            FirstInteraction = null;
            MoveCommitted = null;
            EvaluationCompleted = null;
            AnimationCompleted = null;
            MagnetAvailabilityChanged = null;
            MagnetAvailable = false;
        }
    }
}
