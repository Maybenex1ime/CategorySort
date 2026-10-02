// Inspector của SO_BoosterAnim: mọi field như cũ + khối Preview diễn thử hiệu ứng thẻ booster lên đối tượng
// đang chọn ngay trong Edit mode (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md Mục 5).
// Chỉ đổi scale/góc tạm thời: xong hoặc Stop là trả về như cũ, không ghi gì vào scene/prefab.
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    [CustomEditor(typeof(BoosterAnimSettings))]
    public class BoosterAnimSettingsEditor : UnityEditor.Editor
    {
        Transform previewTarget;
        Vector3 baseScale, baseEuler;
        BoosterTilePreview.Step[] steps;
        double startTime;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview hiệu ứng thẻ", EditorStyles.boldLabel);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Preview chỉ chạy ở Edit mode.", MessageType.Info);
                return;
            }
            var sel = Selection.activeTransform;
            if (sel == null && previewTarget == null)
            {
                EditorGUILayout.HelpBox("Chọn một thẻ trong scene (hoặc mở Tile.prefab) rồi bấm nút để xem. " +
                                        "Cỡ hiện tại của thẻ được coi là 1.", MessageType.Info);
                return;
            }

            var a = (BoosterAnimSettings)target;
            using (new EditorGUI.DisabledScope(previewTarget != null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Thẻ nam châm")) Begin(sel, BoosterTilePreview.MagnetTile(a));
                    if (GUILayout.Button("Thẻ cha")) Begin(sel, BoosterTilePreview.ParentTile(a));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Thẻ undo")) Begin(sel, BoosterTilePreview.UndoTile(a));
                    if (GUILayout.Button("Thẻ hiện ra")) Begin(sel, BoosterTilePreview.RevealTile(a));
                }
            }
            using (new EditorGUI.DisabledScope(previewTarget == null))
                if (GUILayout.Button("Stop")) End();
        }

        void OnDisable() { End(); }

        void Begin(Transform t, BoosterTilePreview.Step[] s)
        {
            if (t == null) return;
            End();
            previewTarget = t;
            baseScale = t.localScale;
            baseEuler = t.localEulerAngles;
            steps = s;
            startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        void Tick()
        {
            if (previewTarget == null) { End(); return; }
            bool running = BoosterTilePreview.Sample(steps, (float)(EditorApplication.timeSinceStartup - startTime),
                                                     out float scale, out float spin);
            previewTarget.localScale = baseScale * scale;
            previewTarget.localEulerAngles = baseEuler + new Vector3(0f, 0f, spin);
            SceneView.RepaintAll();
            if (!running) End();
        }

        // Trả scale/góc gốc. Gọi lại nhiều lần vô hại.
        void End()
        {
            EditorApplication.update -= Tick;
            if (previewTarget != null)
            {
                previewTarget.localScale = baseScale;
                previewTarget.localEulerAngles = baseEuler;
                SceneView.RepaintAll();
            }
            previewTarget = null;
            steps = null;
            Repaint();
        }
    }
}
