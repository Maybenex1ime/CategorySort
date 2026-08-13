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
    /// AppFlow ra lệnh cho gameplay nạp một màn. BoardController là bên nghe.
    /// </summary>
    public static class LevelCommands
    {
        public static event Action<int> LoadRequested;

        public static void RequestLoad(int levelIndex) => LoadRequested?.Invoke(levelIndex);

        // Event static sống sót qua lần Play kế tiếp khi Domain Reload tắt —
        // cùng lý do với LevelSignals.ResetStaticState.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            LoadRequested = null;
        }
    }
}
