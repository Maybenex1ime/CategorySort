using LogosGame.Features.Currency.Transactions;
using LogosGame.Features.Shop;
using LogosMeta.Economy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Canh dữ liệu shop: gói nào trong ShopProductIds mà thiếu trong catalog là nút mua
    /// báo UnknownProduct; mã item mà không có trong SO_TransactionCatalog là tab Item
    /// lặng lẽ thiếu ô. Cả hai đều chỉ lộ ra khi người chơi bấm.
    /// </summary>
    public sealed class ShopCatalogAssetTests
    {
        private const string ShopCatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
        private const string TransactionCatalogPath = "Assets/_Game/Content/SO_TransactionCatalog.asset";
        private const string ProjectScopePath = "Assets/Prefabs/ProjectScope.prefab";

        private static readonly string[] ExpectedProducts =
        {
            ShopProductIds.Coins1000, ShopProductIds.Coins5000, ShopProductIds.Coins10000,
            ShopProductIds.Coins25000, ShopProductIds.Coins50000, ShopProductIds.Coins100000,
        };

        [Test]
        public void MoiProductId_CoGoiHopLeTrongCatalog()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath + " — chạy WordStack ▸ Setup ▸ Build Shop.");

            foreach (string id in ExpectedProducts)
            {
                CoinBundleDefinition? found = null;
                foreach (CoinBundleDefinition b in catalog.CoinBundles)
                    if (b.ProductId == id) found = b;

                Assert.IsTrue(found.HasValue, $"Catalog thiếu gói '{id}'.");
                Assert.Greater(found.Value.Coins, 0, $"'{id}' có Coins <= 0.");
                Assert.IsFalse(string.IsNullOrEmpty(found.Value.PriceLabelFallback), $"'{id}' chưa có PriceLabelFallback.");
                Assert.IsNotNull(found.Value.Icon, $"'{id}' chưa gán Icon.");
            }

            Assert.AreEqual(ExpectedProducts.Length, catalog.CoinBundles.Count,
                "Catalog có gói ngoài ShopProductIds — thêm const vào ShopProductIds hoặc xoá gói.");
        }

        [Test]
        public void MoiMaItem_CoTrongTransactionCatalog()
        {
            ShopCatalog shop = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            TransactionCatalog tx = AssetDatabase.LoadAssetAtPath<TransactionCatalog>(TransactionCatalogPath);
            Assert.IsNotNull(shop, "Chưa có " + ShopCatalogPath);
            Assert.IsNotNull(tx, "Chưa có " + TransactionCatalogPath);
            Assert.Greater(shop.ItemTransactionIds.Count, 0, "Tab Item đang trống.");

            foreach (string id in shop.ItemTransactionIds)
                Assert.IsTrue(tx.TryGet(id, out TransactionDefinition _), $"'{id}' không có trong SO_TransactionCatalog.");
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
