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
    /// AppFlow (qua BoardInitializerView) đưa NỘI DUNG level cho gameplay.
    /// Board không còn tự nạp từ Resources — nó nhận JSON của đúng một màn,
    /// kèm chỉ số màn để báo ngược qua LevelSignals.
    /// </summary>
    public static class LevelCommands
    {
        public static event Action<int, string> LoadRequested;

        public static void RequestLoad(int levelIndex, string levelJson)
            => LoadRequested?.Invoke(levelIndex, levelJson);

        // Event static sống sót qua lần Play kế tiếp khi Domain Reload tắt —
        // cùng lý do với LevelSignals.ResetStaticState.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            LoadRequested = null;
        }
    }
}
