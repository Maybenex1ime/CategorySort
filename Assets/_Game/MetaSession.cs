using BoosterModule;
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
    /// Cầu nối giữa gameplay và tầng meta, đặt trong scene.
    ///
    /// Mô hình tim (đổi 2026-08: theo aquapark): VÀO màn miễn phí, tim chỉ trừ khi
    /// THUA (ở đây) hoặc RESTART giữa màn (AppFlowContext, chỗ bấm nút). Cửa chặn
    /// hết-tim nằm ở AppFlow (RunGatedByHearts → NoHeartsPopup), không nằm ở đây.
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
            LevelSignals.Finished += OnLevelResult;
            Bus.Global.On<BoosterActivatedEvent>(OnBoosterActivated);
        }

        private void OnDestroy()
        {
            LevelSignals.Finished -= OnLevelResult;
            Bus.Global.Off<BoosterActivatedEvent>(OnBoosterActivated);
        }

        private void OnLevelResult(LevelResultEvent evt)
        {
            // Thua mất 1 tim. Finished bắn đúng một lần mỗi màn (cờ resultReported
            // phía board) nên không trừ lặp; ép-thua từ cheat cũng đi qua đây.
            if (!evt.IsWin && _hearts != null)
            {
                _hearts.ConsumeOne();
                _logger.Info($"[MetaSession] Thua màn {evt.LevelIndex} → -1 tim (còn {_hearts.Current.CurrentValue}).");
            }

            // Chuyển tiếp lên bus TRƯỚC progression: CoinRewardService nghe ở đó,
            // giữ nguyên hình dạng aquapark (service nghe bus, không ai gọi thẳng nó).
            Bus.Global.Fire(evt);
            _progression?.ReportResult(evt.IsWin);
        }

        /// <summary>
        /// Chiều meta → gameplay cho booster. Bàn KHÔNG nghe Bus.Global được: assembly
        /// WordStack.Board chỉ tham chiếu WordStack.Contracts + Unity.InputSystem, giữ
        /// vậy để compilecheck.sh compile được target `game`. Nên cầu bắc ở đây, đối
        /// xứng với chiều LevelSignals.Finished → Bus.Global ở trên.
        /// </summary>
        private void OnBoosterActivated(BoosterActivatedEvent evt)
        {
            if (evt.Id == BoosterId.Magnet) LevelCommands.RequestMagnet();
        }
    }
}
