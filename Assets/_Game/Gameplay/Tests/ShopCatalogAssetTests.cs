using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogosGame.Features.Currency;
using LogosGame.Features.Currency.Transactions;
using LogosGame.Features.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Canh dữ liệu shop: gói nào trong ShopProductIds mà thiếu trong catalog là nút mua
    /// báo UnknownProduct; gói trong catalog mà không có trong ShopProductIds là product id
    /// gõ tay chưa đăng ký; item lạ trong gói combo thì user trả tiền mà không nhận được gì.
    /// Tất cả đều chỉ lộ ra khi người chơi bấm.
    /// </summary>
    public sealed class ShopCatalogAssetTests
    {
        private const string ShopCatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
        private const string ProjectScopePath = "Assets/Prefabs/ProjectScope.prefab";

        // Đọc thẳng các hằng số: thêm gói mới vào ShopProductIds là test tự đòi có trong catalog.
        private static string[] ConstValues(System.Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .ToArray();

        [Test]
        public void MoiProductId_CoGoiHopLeTrongCatalog()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath + " — chạy WordStack ▸ Setup ▸ Build Shop.");

            string[] expected = ConstValues(typeof(ShopProductIds));
            List<string> inCatalog = catalog.CoinBundles.Select(b => b.ProductId).ToList();

            foreach (string id in expected)
                Assert.Contains(id, inCatalog, $"Catalog thiếu gói '{id}'.");
            foreach (string id in inCatalog)
                Assert.Contains(id, expected, $"Gói '{id}' chưa có trong ShopProductIds — thêm hằng số, và tạo sản phẩm trên console.");

            foreach (CoinBundleDefinition b in catalog.CoinBundles)
            {
                Assert.Greater(b.Coins, 0, $"'{b.ProductId}' có Coins <= 0 (gói combo cũng phải có coin).");
                Assert.IsFalse(string.IsNullOrEmpty(b.PriceLabelFallback), $"'{b.ProductId}' chưa có PriceLabelFallback.");
                Assert.IsNotNull(b.Icon, $"'{b.ProductId}' chưa gán Icon.");
            }
        }

        [Test]
        public void GoiCombo_ChiChuaItemHopLe()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath);

            foreach (CoinBundleDefinition b in catalog.CoinBundles)
            {
                if (b.Items == null) continue;
                foreach (ShopReward reward in b.Items)
                {
                    Assert.IsTrue(System.Enum.IsDefined(typeof(ResourceType), reward.Type),
                        $"'{b.ProductId}' có ResourceType lạ ({(int)reward.Type}) — enum bị đổi số?");
                    Assert.Greater(reward.Amount, 0, $"'{b.ProductId}' có '{reward.Type}' với Amount <= 0.");
                }
            }
        }

        [Test]
        public void MoiResourceType_DeuTraoDuoc()
        {
            // Thêm loại tài nguyên mới vào enum mà quên map sang ItemIds là gói có nó sẽ trao thiếu.
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.Coin) continue;
                Assert.Contains(type.ToItemId(), ConstValues(typeof(ItemIds)),
                    $"ResourceType.{type} chưa map sang ItemIds trong ResourceTypeExtensions.ToItemId.");
            }
        }

        [Test]
        public void ProjectScope_CoShopInstaller_TroDungCatalog()
        {
            GameObject scope = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectScopePath);
            Assert.IsNotNull(scope, "Không thấy " + ProjectScopePath);

            ShopInstaller installer = scope.GetComponentInChildren<ShopInstaller>(true);
            Assert.IsNotNull(installer, "ProjectScope chưa gắn ShopInstaller.");

            var so = new SerializedObject(installer);
            Object assigned = so.FindProperty("_shopCatalog").objectReferenceValue;
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath), assigned,
                "ShopInstaller._shopCatalog chưa trỏ tới SO_ShopCatalog.asset.");
        }
    }
}
