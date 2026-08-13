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

        /// <summary>Đọc từ field "difficulty" của file level. HUD dùng để chọn sprite khung.</summary>
        public LevelDifficulty Difficulty { get; }

        public LevelStartedEvent(int levelIndex, LevelDifficulty difficulty)
        {
            LevelIndex = levelIndex;
            Difficulty = difficulty;
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

        public static void RaiseStarted(int levelIndex, LevelDifficulty difficulty)
            => Started?.Invoke(new LevelStartedEvent(levelIndex, difficulty));

        public static void RaiseFinished(bool isWin, int levelIndex, int movesUsed)
            => Finished?.Invoke(new LevelResultEvent(isWin, levelIndex, movesUsed));

        // Event static sống sót qua lần Play kế tiếp khi Domain Reload bị tắt, khiến
        // người nghe cũ bị gọi lại và coin cộng hai lần. Xoá sạch lúc khởi động.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Started = null;
            Finished = null;
        }
    }
}
