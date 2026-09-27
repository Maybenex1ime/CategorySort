using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Chặn build phát hành lỡ dùng StubIAPService (luôn "mua thành công" = phát coin miễn phí).
    /// Chỉ chạy khi define RELEASE_BUILD bật — pipeline phát hành bật define này rồi chạy test.
    /// </summary>
    public sealed class ShopReleaseConfigTests
    {
        private const string ProjectScopePath = "Assets/Prefabs/ProjectScope.prefab";

        [Test]
        public void BuildPhatHanh_PhaiDungStoreThat()
        {
#if !RELEASE_BUILD
            Assert.Ignore("Chỉ kiểm khi define RELEASE_BUILD bật.");
#else
            GameObject scope = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectScopePath);
            Assert.IsNotNull(scope, "Không thấy " + ProjectScopePath);

            ShopInstaller installer = scope.GetComponentInChildren<ShopInstaller>(true);
            Assert.IsNotNull(installer, "ProjectScope chưa gắn ShopInstaller — shop không có store.");
            Assert.IsTrue(installer.UseRealStore,
                "ShopInstaller._useRealStore đang TẮT: build này sẽ phát coin miễn phí qua StubIAPService.");
#endif
        }
    }
}
