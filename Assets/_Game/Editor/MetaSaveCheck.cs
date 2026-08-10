using LogosMeta.Economy;
using LogosSDK.Save;
using Reflex.Core;
using UnityEditor;
using UnityEngine;

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

        private static Container BuildContainer(ContainerScope scope)
        {
            var builder = new ContainerBuilder().SetName("MetaSaveCheck");
            scope.InstallBindings(builder);
            return builder.Build();
        }
    }
}
