using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Build Android một phát từ menu WordStack ▸ Build. Thứ tự: đăng ký level + prefab UI vào Addressables
    /// (hai tool sẵn có — level mới không đăng ký là build chạy rơi về lv-001), build nội dung Addressables
    /// cho Android, rồi BuildPlayer ra Build/Android/ (thư mục đã .gitignore).
    /// Phải Switch Platform sang Android trước — build chéo là Unity import lại toàn bộ asset.
    /// Chạy trong Editor vì batchmode trên máy này bị chặn; không có đường CLI.
    /// </summary>
    public static class AndroidBuild
    {
        private const string OutDir = "Build/Android";

        [MenuItem("WordStack/Build/Android APK (dev)")]
        public static void ApkDev() => Build(aab: false, dev: true);

        [MenuItem("WordStack/Build/Android APK (release)")]
        public static void ApkRelease() => Build(aab: false, dev: false);

        [MenuItem("WordStack/Build/Android AAB (release, lên store)")]
        public static void AabRelease() => Build(aab: true, dev: false);

        private static void Build(bool aab, bool dev)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("[AndroidBuild] Build Settings đang không phải Android — Switch Platform trước.");
                return;
            }

            LevelCatalogBuilder.Build();
            UiAddressablesBuilder.Build();

            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                Debug.LogError("[AndroidBuild] Build Addressables thất bại: " + content.Error);
                return;
            }

            EditorUserBuildSettings.buildAppBundle = aab;
            Directory.CreateDirectory(OutDir);
            string file = Path.Combine(OutDir,
                PlayerSettings.productName + "-" + PlayerSettings.bundleVersion + (dev ? "-dev" : "") + (aab ? ".aab" : ".apk"));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = file,
                target = BuildTarget.Android,
                options = dev ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[AndroidBuild] Xong: {file} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:F0}s).");
            else
                Debug.LogError($"[AndroidBuild] {s.result}: {s.totalErrors} lỗi — xem Console.");
        }
    }
}
