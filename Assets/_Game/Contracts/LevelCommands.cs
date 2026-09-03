using System;
using UnityEngine;

namespace WordStack.Contracts
{
    // Chiều ngược lại của LevelSignals: meta → gameplay.
    // Giữ C# thuần vì assembly Contracts cố ý không tham chiếu gì (xem ghi chú
    // trong LevelEvents.cs về compilecheck.sh).
    //
    // Không có người nghe thì không xảy ra gì — gameplay chạy độc lập được.

    /// <summary>
    /// AppFlow (qua BoardInitializer) đưa NỘI DUNG level cho gameplay.
    /// Board không còn tự nạp từ Resources — nó nhận JSON của đúng một màn,
    /// kèm chỉ số màn để báo ngược qua LevelSignals.
    /// </summary>
    public static class LevelCommands
    {
        public static event Action<int, string> LoadRequested;

        public static void RequestLoad(int levelIndex, string levelJson)
            => LoadRequested?.Invoke(levelIndex, levelJson);

        /// <summary>
        /// Chặn input của bàn khi UI meta (popup settings...) đang mở. Cần thiết vì
        /// BoardController đọc Pointer.current trực tiếp — uGUI KHÔNG chặn hộ nó,
        /// bấm lên popup mà không gate là kéo thẻ phía sau.
        /// </summary>
        public static bool InputBlocked { get; private set; }

        public static void SetInputBlocked(bool blocked) => InputBlocked = blocked;

        /// <summary>
        /// Booster Nam châm: tầng meta xin bàn hút trọn một nhóm.
        ///
        /// Phải đi đường này chứ KHÔNG phải Bus.Global: assembly WordStack.Board chỉ
        /// tham chiếu WordStack.Contracts + Unity.InputSystem, giữ vậy để
        /// compilecheck.sh compile được target `game` bằng ref set 4.7.1-api.
        /// MetaSession là chỗ bắc cầu từ BoosterActivatedEvent sang đây.
        /// </summary>
        public static event Action MagnetRequested;

        public static void RequestMagnet() => MagnetRequested?.Invoke();

        /// <summary>
        /// Booster Shuffle. Cùng lý do với MagnetRequested: assembly WordStack.Board chỉ
        /// tham chiếu WordStack.Contracts nên bàn không nghe Bus.Global được.
        /// </summary>
        public static event Action ShuffleRequested;

        public static void RequestShuffle() => ShuffleRequested?.Invoke();

        // Event static sống sót qua lần Play kế tiếp khi Domain Reload tắt —
        // cùng lý do với LevelSignals.ResetStaticState.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            LoadRequested = null;
            MagnetRequested = null;
            ShuffleRequested = null;
            InputBlocked = false;
        }
    }
}
