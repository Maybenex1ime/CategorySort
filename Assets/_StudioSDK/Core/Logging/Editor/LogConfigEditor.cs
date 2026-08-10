#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LogosSDK.Core.Logging
{
    /// <summary>
    /// Editor window for viewing and modifying runtime and asset logging configuration.
    /// </summary>
    public class LogControlWindow : EditorWindow
    {
        /// <summary>
        /// Resources path where the LogConfig asset is expected.
        /// </summary>
        private const string ConfigPath = "LogConfig"; // Path inside Resources folder

        [MenuItem("Window/Core/Log Control")]
        public static void ShowWindow()
        {
            var window = GetWindow<LogControlWindow>("Log Control");
            window.minSize = new Vector2(300, 250);
            window.Show();
        }

        private void OnGUI()
        {
            LogConfigSO currentConfig = ResolveConfig();

            if (!currentConfig)
            {
                DrawNoConfigWarning();
                return;
            }

            DrawHeader(currentConfig);

            EditorGUILayout.Space(10);

            DrawMainControls(currentConfig);

            EditorGUILayout.Space(10);

            DrawQuickProfiles(currentConfig);

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Resolves the active logging configuration.
        /// </summary>
        private LogConfigSO ResolveConfig()
        {
            return Resources.Load<LogConfigSO>(ConfigPath);
        }

        /// <summary>
        /// Draws current logging status and mode (Editor vs Runtime).
        /// </summary>
        private void DrawHeader(LogConfigSO config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Log System Controller", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("MODE: RUNTIME (Editing Live Instance)", EditorStyles.miniBoldLabel);
            }
            else
            {
                GUI.color = new Color(0.7f, 0.7f, 1f); // Light blue
                EditorGUILayout.LabelField("MODE: EDITOR (Editing Asset File)", EditorStyles.miniBoldLabel);
            }
            GUI.color = Color.white;

            EditorGUILayout.Separator();

            string status = config.LoggingEnabled ? "ACTIVE" : "DISABLED";
            GUI.color = config.LoggingEnabled ? Color.green : Color.red;
            EditorGUILayout.LabelField($"Current Status: {status}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Renders primary logging configuration controls.
        /// </summary>
        private void DrawMainControls(LogConfigSO config)
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            bool newEnabled = EditorGUILayout.Toggle("Logging Enabled", config.LoggingEnabled);

            LogLevel newLevel = (LogLevel)EditorGUILayout.EnumPopup("Min Log Level", config.MinimumLogLevel);

            bool newConsole = EditorGUILayout.Toggle("Log To Console", config.LogToConsole);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Log Settings");
                config.LoggingEnabled = newEnabled;
                config.MinimumLogLevel = newLevel;
                config.LogToConsole = newConsole;
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(config);
                }
                config.NotifyConfigChanged();
            }
        }

        /// <summary>
        /// Displays preset buttons for fast configuration switching.
        /// </summary>
        private void DrawQuickProfiles(LogConfigSO config)
        {
            GUILayout.Label("Quick Profiles", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("DEVELOPMENT\n(Debug + All)", GUILayout.Height(40)))
            {
                config.SetDevelopmentMode();
            }

            if (GUILayout.Button("PRODUCTION\n(Warning Only)", GUILayout.Height(40)))
            {
                config.SetProductionMode();
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("MUTE ALL", GUILayout.Height(25)))
            {
                config.DisableAllLogging();
            }
        }

        /// <summary>
        /// Shows a warning UI when no LogConfig asset is found.
        /// </summary>
        private void DrawNoConfigWarning()
        {
            EditorGUILayout.HelpBox($"Could not find LogConfigSO at 'Resources/{ConfigPath}'.", MessageType.Error);
            if (GUILayout.Button("Create Config Asset"))
            {
                CreateConfigAsset();
            }
        }

        /// <summary>
        /// Creates a default LogConfig asset inside the Resources folder.
        /// </summary>
        private void CreateConfigAsset()
        {
            if (!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
            }

            var asset = CreateInstance<LogConfigSO>();
            AssetDatabase.CreateAsset(asset, $"Assets/Resources/{ConfigPath}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
