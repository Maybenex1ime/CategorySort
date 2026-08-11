using LogosMeta.Economy;
using LogosMeta.Progression;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using Reflex.Attributes;
using UnityEngine;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta
{
    /// <summary>
    /// Cầu nối giữa gameplay và tầng meta, đặt trong scene. Nghe hai sự kiện
    /// gameplay bắn ra và điều phối đúng như aquapark làm trong AppFlowContext /
    /// GameplayCoordinator: bắt đầu màn thì trừ tim, kết thúc thì ghi tiến độ.
    /// Coin do <see cref="CoinRewardService"/> lo — nó tự nghe cùng sự kiện.
    ///
    /// <see cref="_coinReward"/> được [Inject] chỉ để ÉP DỰNG service đó (nó đăng ký
    /// nghe bus trong constructor). Bỏ field này đi là coin lặng lẽ ngừng cộng.
    /// </summary>
    public sealed class MetaSession : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<MetaSession>();

        [Inject] private readonly IHeartService _hearts;
        [Inject] private readonly IProgressionService _progression;
        [Inject] private readonly ICoinRewardService _coinReward;

        private void Awake()
        {
            LevelSignals.Started += OnLevelStarted;
            LevelSignals.Finished += OnLevelResult;
        }

        private void OnDestroy()
        {
            LevelSignals.Started -= OnLevelStarted;
            LevelSignals.Finished -= OnLevelResult;
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (_hearts == null) return;

            // Khác aquapark một điểm CÓ CHỦ Ý: bên đó hết tim là chặn không cho vào
            // màn và bật popup "hết tim". WordStack chưa có popup nào, chặn ở đây
            // sẽ thành game đứng im không giải thích. Nên chỉ ghi log, chưa chặn.
            if (_hearts.Current.CurrentValue <= 0)
            {
                _logger.Warn("[MetaSession] Hết tim — aquapark sẽ chặn vào màn ở đây; " +
                             "WordStack cho chơi tiếp vì chưa có popup báo.");
                return;
            }

            _hearts.ConsumeOne();
        }

        private void OnLevelResult(LevelResultEvent evt)
        {
            // Chuyển tiếp lên bus TRƯỚC: CoinRewardService nghe ở đó, giữ nguyên
            // hình dạng aquapark (service nghe bus, không ai gọi thẳng nó).
            Bus.Global.Fire(evt);
            _progression?.ReportResult(evt.IsWin);
        }
    }
}
