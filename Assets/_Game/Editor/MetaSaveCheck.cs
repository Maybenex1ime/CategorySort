using LogosMeta.Economy;
using LogosMeta.Progression;
using LogosSDK.Core.Events;
using LogosSDK.Save;
using Reflex.Core;
using UnityEditor;
using UnityEngine;
using WordStack.Contracts;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Nghiệm thu cả chuỗi meta bằng chính prefab root scope thật, không cần vào Play:
    /// dựng container y như Reflex làm lúc khởi động, đổi số coin, ghi đĩa, dựng lại
    /// container mới và đọc lại. Số phải sống sót — nếu không, một mắt xích nào đó
    /// (đăng ký domain, thứ tự Lazy/Eager, SaveAll) đã đứt.
    /// Chạy xong tự trả coin về giá trị ban đầu để không phá save đang chơi.
    /// </summary>
    internal static class MetaSaveCheck
    {
        private const string ScopePath = "Assets/Prefabs/ProjectScope.prefab";

        [MenuItem("WordStack/Test/Meta save round-trip")]
        private static void Run()
        {
            var scope = AssetDatabase.LoadAssetAtPath<ContainerScope>(ScopePath);
            if (scope == null)
            {
                Debug.LogError($"META CHECK FAIL: không thấy root scope tại {ScopePath}");
                return;
            }

            int original;
            const int delta = 37;

            using (var container = BuildContainer(scope))
            {
                var currency = container.Resolve<ICurrencyService>();
                original = currency.Coins.CurrentValue;
                currency.Add(delta);

                if (currency.Coins.CurrentValue != original + delta)
                {
                    Debug.LogError($"META CHECK FAIL: cộng coin trong bộ nhớ sai — " +
                                   $"mong {original + delta}, thật {currency.Coins.CurrentValue}");
                    return;
                }

                container.Resolve<ISaveManager>().SaveAll();
            }

            // Container thứ hai đọc lại từ đĩa — đây mới là phép thử thật.
            using (var container = BuildContainer(scope))
            {
                var currency = container.Resolve<ICurrencyService>();
                var reloaded = currency.Coins.CurrentValue;

                if (reloaded != original + delta)
                {
                    Debug.LogError($"META CHECK FAIL: coin không sống sót qua đĩa — " +
                                   $"mong {original + delta}, đọc lại được {reloaded}");
                    return;
                }

                var hearts = container.Resolve<IHeartService>();
                Debug.Log($"META CHECK OK — coin {original} → {reloaded} qua đĩa · " +
                          $"tim {hearts.Current.CurrentValue}");

                currency.SetCoins(original); // trả lại nguyên trạng
                container.Resolve<ISaveManager>().SaveAll();
            }
        }

        /// <summary>
        /// Kiểm luồng meta: bắn LevelResultEvent(thắng) lên bus và xem hai thứ có
        /// xảy ra không — CoinRewardService cộng coin, ProgressionService tăng màn.
        /// Không dựng được MetaSession (là MonoBehaviour) nên phần trừ tim gọi thẳng.
        /// Chạy xong trả mọi số về nguyên trạng.
        /// </summary>
        [MenuItem("WordStack/Test/Meta flow round-trip")]
        private static void RunFlow()
        {
            var scope = AssetDatabase.LoadAssetAtPath<ContainerScope>(ScopePath);
            if (scope == null)
            {
                Debug.LogError($"FLOW CHECK FAIL: không thấy root scope tại {ScopePath}");
                return;
            }

            using (var container = BuildContainer(scope))
            {
                var save = container.Resolve<ISaveManager>();
                var currency = container.Resolve<ICurrencyService>();
                var progression = container.Resolve<IProgressionService>();
                var hearts = container.Resolve<IHeartService>();
                // Resolve để ÉP DỰNG — constructor của nó mới là chỗ đăng ký nghe bus.
                var reward = container.Resolve<ICoinRewardService>();

                var coins0 = currency.Coins.CurrentValue;
                var level0 = save.Load<LevelProgressData>().CurrentLevel;
                var hearts0 = hearts.Current.CurrentValue;

                Bus.Global.Fire(new LevelResultEvent(true, level0, 12));

                var awarded = reward.LastAwardedAmount;
                if (awarded <= 0)
                {
                    Debug.LogError("FLOW CHECK FAIL: thắng màn mà không thưởng coin nào — " +
                                   "kiểm CoinsPerWin trên ProjectScope, hoặc CoinRewardService chưa được dựng.");
                    return;
                }
                if (currency.Coins.CurrentValue != coins0 + awarded)
                {
                    Debug.LogError($"FLOW CHECK FAIL: coin sai — mong {coins0 + awarded}, " +
                                   $"thật {currency.Coins.CurrentValue}");
                    return;
                }

                progression.ReportResult(true);
                var level1 = save.Load<LevelProgressData>().CurrentLevel;
                if (level1 != level0 + 1)
                {
                    Debug.LogError($"FLOW CHECK FAIL: tiến độ không tăng — mong {level0 + 1}, thật {level1}");
                    return;
                }

                if (hearts0 > 0)
                {
                    hearts.ConsumeOne();
                    if (hearts.Current.CurrentValue != hearts0 - 1)
                    {
                        Debug.LogError($"FLOW CHECK FAIL: tim không trừ — mong {hearts0 - 1}, " +
                                       $"thật {hearts.Current.CurrentValue}");
                        return;
                    }
                }

                Debug.Log($"FLOW CHECK OK — coin {coins0} → {currency.Coins.CurrentValue} (+{awarded}) · " +
                          $"màn {level0} → {level1} · tim {hearts0} → {hearts.Current.CurrentValue}");

                // trả nguyên trạng
                currency.SetCoins(coins0);
                var progress = save.Load<LevelProgressData>();
                progress.CurrentLevel = level0;
                save.Save(progress);
                hearts.SetHearts(hearts0);
                save.SaveAll();
            }
        }

        private static Container BuildContainer(ContainerScope scope)
        {
            var builder = new ContainerBuilder().SetName("MetaSaveCheck");
            scope.InstallBindings(builder);
            return builder.Build();
        }
    }
}
