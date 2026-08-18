using System;
using System.Collections.Generic;
using System.IO;
using LogosGame.Features.Gameplay.Content;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Quét level JSON → đánh dấu Addressable (address = "id" trong JSON) → điền
    /// SO_LevelCatalog. Port ý tưởng LevelAssetRegistrar bên aquapark: catalog là
    /// file SINH RA, không điền tay — thêm màn mới chỉ cần chạy lại menu này.
    ///
    /// JSON vẫn giữ "difficulty" làm nguồn SEED lúc edit-time; runtime không đọc
    /// nó nữa (parser đã bỏ), chỉ đọc catalog. Muốn đổi độ khó: sửa JSON rồi
    /// chạy lại tool, hoặc sửa thẳng catalog.
    /// </summary>
    public static class LevelCatalogBuilder
    {
        private const string LevelsFolder = "Assets/_Game/Content/Levels";
        private const string CatalogPath = "Assets/_Game/Content/SO_LevelCatalog.asset";

        // JsonUtility bỏ qua field lạ nên DTO chỉ cần mấy field cần seed.
        // moves khuyết trong JSON → 0 → runtime rơi về mặc định GameplayStartContext.
        [Serializable]
        private sealed class LevelDto
        {
            public string id;
            public string title;
            public string difficulty;
            public int moves;
        }

        [MenuItem("WordStack/Build Level Catalog")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(LevelsFolder))
            {
                Debug.LogError($"[LevelCatalogBuilder] Không thấy '{LevelsFolder}'. " +
                               "Level JSON phải nằm đúng thư mục đó — file trong Resources không " +
                               "đánh Addressable được.");
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LevelCatalogBuilder] Addressables chưa khởi tạo — mở Window > Asset Management > Addressables > Groups một lần.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { LevelsFolder });
            Array.Sort(guids, (a, b) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            if (guids.Length == 0)
            {
                Debug.LogError($"[LevelCatalogBuilder] '{LevelsFolder}' không có TextAsset nào.");
                return;
            }

            LevelCatalog catalog = LoadOrCreateCatalog();
            var rows = new List<(string id, string title, LevelDifficultyProxy diff, int moves)>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) continue;

                LevelDto dto = JsonUtility.FromJson<LevelDto>(asset.text);
                if (dto == null || string.IsNullOrEmpty(dto.id))
                {
                    Debug.LogWarning($"[LevelCatalogBuilder] Bỏ qua '{path}' — thiếu \"id\".");
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = dto.id;   // BoardInitializer nạp bằng đúng chuỗi này

                if (dto.moves < 0)
                {
                    Debug.LogWarning($"[LevelCatalogBuilder] '{path}': moves = {dto.moves} âm — dùng mặc định.");
                    dto.moves = 0;
                }

                rows.Add((dto.id, string.IsNullOrEmpty(dto.title) ? dto.id : dto.title,
                          ParseDifficulty(dto.difficulty, path), dto.moves));
            }

            WriteEntries(catalog, rows);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LevelCatalogBuilder] Xong: {rows.Count} level → '{CatalogPath}', address = id, group '{settings.DefaultGroup.Name}'.");
        }

        private static LevelCatalog LoadOrCreateCatalog()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log($"[LevelCatalogBuilder] Tạo mới '{CatalogPath}' — nhớ kéo vào ProgressionInstaller trên ProjectScope prefab.");
            return catalog;
        }

        // Ghi qua SerializedObject vì _entries là private — cùng cách LevelAssetRegistrar làm.
        private static void WriteEntries(LevelCatalog catalog, List<(string id, string title, LevelDifficultyProxy diff, int moves)> rows)
        {
            var so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = rows.Count;

            for (int i = 0; i < rows.Count; i++)
            {
                SerializedProperty e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("LevelId").stringValue = rows[i].id;
                e.FindPropertyRelative("AddressKey").stringValue = rows[i].id;
                e.FindPropertyRelative("DisplayName").stringValue = rows[i].title;
                e.FindPropertyRelative("Difficulty").enumValueIndex = (int)rows[i].diff;
                e.FindPropertyRelative("Moves").intValue = rows[i].moves;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        // Trùng giá trị WordStack.Contracts.LevelDifficulty — enum riêng để khỏi kéo
        // Contracts vào chữ ký tool; enumValueIndex chỉ cần đúng số.
        private enum LevelDifficultyProxy { Normal = 0, Hard = 1, Crazy = 2 }

        private static LevelDifficultyProxy ParseDifficulty(string raw, string path)
        {
            switch (raw)
            {
                case null:
                case "": return LevelDifficultyProxy.Normal;
                case "Normal": return LevelDifficultyProxy.Normal;
                case "Hard": return LevelDifficultyProxy.Hard;
                case "Crazy": return LevelDifficultyProxy.Crazy;
                default:
                    Debug.LogWarning($"[LevelCatalogBuilder] '{path}': difficulty '{raw}' lạ — dùng Normal.");
                    return LevelDifficultyProxy.Normal;
            }
        }
    }
}
