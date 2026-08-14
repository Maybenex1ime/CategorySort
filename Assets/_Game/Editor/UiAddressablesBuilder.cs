using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Đảm bảo mọi prefab popup/screen là Addressable với address = TÊN FILE
    /// (= tên class — UIManager nạp bằng type.Name; sai address là lỗi runtime
    /// chứ không phải lỗi compile). Chạy sau mỗi lần thêm popup mới.
    /// KHÔNG quét cả _Shared/Prefab gốc: UIManager/GamePlayUIRoot là scene object,
    /// không nạp qua Addressables.
    /// </summary>
    public static class UiAddressablesBuilder
    {
        private static readonly string[] PrefabRoots =
        {
            "Assets/_Shared/Prefab/Popup",
            "Assets/_Shared/Prefab/Screen",
        };

        // public để gọi được qua MCP bridge (nó chặn System.Reflection).
        [MenuItem("WordStack/Build UI Addressables")]
        public static void Build()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[UiAddressablesBuilder] Addressables chưa khởi tạo — mở Window > Asset Management > Addressables > Groups một lần.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", PrefabRoots);
            Array.Sort(guids, (a, b) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            int touched = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string address = System.IO.Path.GetFileNameWithoutExtension(path);

                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry != null && entry.address == address) continue;

                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = address;
                touched++;
                Debug.Log($"[UiAddressablesBuilder] '{path}' → address '{address}'.");
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiAddressablesBuilder] Xong: {guids.Length} prefab, {touched} entry thêm/sửa, group '{settings.DefaultGroup.Name}'.");
        }
    }
}
