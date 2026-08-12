#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public sealed class SceneSnapshotWindow : EditorWindow
    {
        private const string DefaultFolder = "Assets/_Snapshots";

        [SerializeField] private string _outputPath;
        [SerializeField] private SnapshotOptions _options = SnapshotOptions.Default;
        private bool[] _processorEnabled;
        private SnapshotResult _lastResult;
        private Vector2 _warningsScroll;

        [MenuItem("Tools/Logos/Scene Snapshot", priority = 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<SceneSnapshotWindow>("Scene Snapshot");
            win.minSize = new Vector2(380, 420);
            win.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_outputPath)) _outputPath = BuildDefaultPath();
            var processors = SceneSnapshotter.AvailableProcessors;
            if (_processorEnabled == null || _processorEnabled.Length != processors.Count)
            {
                _processorEnabled = new bool[processors.Count];
                for (int i = 0; i < _processorEnabled.Length; i++) _processorEnabled[i] = true;
            }
        }

        private static string BuildDefaultPath()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Untitled";
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{DefaultFolder}/{sceneName}_{ts}.unity";
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _outputPath = EditorGUILayout.TextField(_outputPath);
                if (GUILayout.Button("…", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.SaveFilePanelInProject(
                        "Snapshot Target",
                        System.IO.Path.GetFileNameWithoutExtension(_outputPath),
                        "unity",
                        "Pick a path inside the project Assets folder.");
                    if (!string.IsNullOrEmpty(picked)) _outputPath = picked;
                }
                if (GUILayout.Button("Reset", GUILayout.Width(50))) _outputPath = BuildDefaultPath();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            _options.includeDontDestroyOnLoad = EditorGUILayout.Toggle("Include DontDestroyOnLoad", _options.includeDontDestroyOnLoad);
            _options.stripMissingScripts = EditorGUILayout.Toggle("Strip Missing Scripts", _options.stripMissingScripts);
            _options.clearReflexContainerRefs = EditorGUILayout.Toggle("Clear Reflex Container Refs", _options.clearReflexContainerRefs);
            _options.clearR3References = EditorGUILayout.Toggle("Clear R3 References", _options.clearR3References);
            _options.killDOTweenTweens = EditorGUILayout.Toggle("Kill DOTween Tweens", _options.killDOTweenTweens);
            _options.clearAddressablesHandles = EditorGUILayout.Toggle("Clear Addressables Handles", _options.clearAddressablesHandles);
            _options.excludedHideFlags = (HideFlags)EditorGUILayout.EnumFlagsField("Exclude HideFlags", _options.excludedHideFlags);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Post-Processors", EditorStyles.boldLabel);
            var processors = SceneSnapshotter.AvailableProcessors;
            for (int i = 0; i < processors.Count; i++)
            {
                _processorEnabled[i] = EditorGUILayout.ToggleLeft(processors[i].Name, _processorEnabled[i]);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button(Application.isPlaying ? "Snapshot Now" : "Snapshot Now (Play Mode required)",
                        GUILayout.Height(32)))
                {
                    DoSnapshot();
                }
            }

            DrawResult();
        }

        private void DoSnapshot()
        {
            var processors = SceneSnapshotter.AvailableProcessors;
            var active = new List<ISnapshotPostProcessor>();
            for (int i = 0; i < processors.Count; i++)
                if (_processorEnabled[i]) active.Add(processors[i]);

            _lastResult = SceneSnapshotter.SnapshotActiveScene(_outputPath, _options, active);
            Repaint();
        }

        private void DrawResult()
        {
            if (_lastResult == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            if (!_lastResult.Success)
            {
                EditorGUILayout.HelpBox(_lastResult.ErrorMessage ?? "Snapshot failed.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Saved To", _lastResult.SavedPath);
            if (_lastResult.PendingMaterialize)
            {
                EditorGUILayout.HelpBox(
                    "Snapshot was staged as a temporary prefab. The .unity scene will " +
                    "be created automatically when you exit Play Mode.",
                    MessageType.Info);
            }
            EditorGUILayout.LabelField("Roots / GameObjects", $"{_lastResult.RootCount} / {_lastResult.GameObjectCount}");
            EditorGUILayout.LabelField("Stripped Missing Scripts", _lastResult.StrippedMissingScripts.ToString());
            EditorGUILayout.LabelField("Cleared References", _lastResult.ClearedReferences.ToString());
            EditorGUILayout.LabelField("Warnings", _lastResult.Warnings.Count.ToString());

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping in Project"))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(_lastResult.SavedPath);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
                if (GUILayout.Button("Open in Edit Mode"))
                {
                    if (Application.isPlaying)
                    {
                        bool ok = EditorUtility.DisplayDialog(
                            "Exit Play Mode?",
                            "Opening the snapshot scene requires exiting Play Mode. Continue?",
                            "Exit Play & Open",
                            "Cancel");
                        if (!ok) return;
                        EditorApplication.isPlaying = false;
                        EditorApplication.delayCall += () => OpenSnapshotSceneDeferred(_lastResult.SavedPath);
                    }
                    else
                    {
                        OpenSnapshotSceneDeferred(_lastResult.SavedPath);
                    }
                }
            }

            if (_lastResult.Warnings.Count > 0)
            {
                EditorGUILayout.LabelField("Warnings:", EditorStyles.miniBoldLabel);
                using (var s = new EditorGUILayout.ScrollViewScope(_warningsScroll, GUILayout.Height(120)))
                {
                    _warningsScroll = s.scrollPosition;
                    foreach (var w in _lastResult.Warnings)
                        EditorGUILayout.LabelField(w, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private static void OpenSnapshotSceneDeferred(string path)
        {
            if (Application.isPlaying)
            {
                EditorApplication.delayCall += () => OpenSnapshotSceneDeferred(path);
                return;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
#endif
