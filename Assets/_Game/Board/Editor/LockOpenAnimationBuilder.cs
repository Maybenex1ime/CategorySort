// Dựng LitMotionAnimation "mở hộp khoá nhóm" trên Lock Root. Số liệu timeline nằm ở
// LockOpen.figma-motion.json (Figma Group 547, node 373:1245 — plan
// docs/superpowers/plans/2026-09-27-lock-box-open-animation.md) và đi qua FigmaMotionImporter; tool này
// chỉ lo phần sửa cấu trúc một lần mà importer không làm.
// Mở Box.prefab, chọn Lock Root, Tools ▸ WordStack ▸ Build Lock Open Animation, Save prefab.
// Chỉnh số về sau: sửa JSON rồi Import lại qua Tools ▸ Figma Motion ▸ Importer — khỏi chạy lại tool này.
//
// Tool làm, đều Undo được: gỡ script mất; tách UpperLit ra ngang hàng Lit (Figma để hai lớp là anh
// em — JSON gọi chúng UpperLit[0] bên trái, UpperLit[1] bên phải); tách Middle thành Top/Bottom
// (texture đã cắt 2 sprite); rồi import JSON (ghi đè LitMotionAnimation, Parallel, không tự chạy).
using System.Collections.Generic;
using FigmaMotion.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    static class LockOpenAnimationBuilder
    {
        const string MiddleTexture = "Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png";
        const string TimelineJson = "Assets/_Game/Art/Blocker/Blocker_Box/LockOpen.figma-motion.json";

        [MenuItem("Tools/WordStack/Build Lock Open Animation")]
        static void Build()
        {
            var root = Selection.activeGameObject;
            if (root == null) { Debug.LogWarning("[LockOpen] Chọn Lock Root (trong Box.prefab) trước."); return; }

            var rt = root.transform;

            // Kiểm tra & chuẩn bị all validation trước khi mutate.
            if (!CollectBacksAndUppers(rt, out var backs, out var uppers)) return;

            var middle = rt.Find("Middle");
            if (middle == null) { Debug.LogWarning("[LockOpen] Không thấy con \"Middle\".", root); return; }

            // MiddleTexture chỉ cần cắt sẵn 2 sprite khi Middle CHƯA tách Top/Bottom (chạy tool
            // lần đầu) — chạy lại trên Middle đã tách rồi thì khỏi đòi hỏi texture nữa.
            bool hasTop = middle.Find("Top") != null, hasBottom = middle.Find("Bottom") != null;
            if (hasTop != hasBottom)
            {
                Debug.LogWarning("[LockOpen] Middle có đúng một trong hai con Top/Bottom — dở dang, kiểm tra lại prefab.", root);
                return;
            }
            Sprite[] middleSprites = null;
            if (!hasTop && !TryLoadMiddleSprites(out middleSprites)) return;

            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(TimelineJson);
            if (json == null) { Debug.LogWarning($"[LockOpen] Không thấy {TimelineJson}."); return; }

            // Validation passed; proceed with mutations.
            Undo.SetCurrentGroupName("Build Lock Open Animation");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RegisterCompleteObjectUndo(root, "Remove missing scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            FlattenUppers(rt, uppers);
            EnsureMiddleSplit(middle, middleSprites);

            // Cấu trúc đã đúng dạng JSON mô tả — giờ mới tra node được.
            var r = FigmaMotionImporter.Resolve(json.text, rt);
            if (!r.Ok)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogWarning("[LockOpen] JSON không khớp Lock Root, đã hoàn tác:\n  " + string.Join("\n  ", r.errors), root);
                return;
            }
            FigmaMotionImporter.Write(root, r.tracks, r.sequential);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[LockOpen] {r.tracks.Count} component trên {root.name}. Nhớ Save prefab.", root);
        }

        // Kiểm tra Lit/UpperLit tồn tại & số lượng khớp nhau.
        static bool CollectBacksAndUppers(Transform root, out List<Transform> backs, out List<Transform> uppers)
        {
            backs = new List<Transform>();
            uppers = new List<Transform>();
            foreach (Transform c in root)
            {
                if (c.name.StartsWith("UpperLit")) { uppers.Add(c); continue; }
                if (!c.name.StartsWith("Lit")) continue;
                backs.Add(c);
                foreach (Transform g in c) if (g.name.StartsWith("UpperLit")) uppers.Add(g);
            }
            if (backs.Count == 0 || backs.Count != uppers.Count)
            {
                Debug.LogWarning($"[LockOpen] Cần số Lit bằng số UpperLit, đang {backs.Count}/{uppers.Count}.", root);
                return false;
            }
            return true;
        }

        // Kiểm tra MiddleTexture cắt đúng 2 sprite. Dùng trong validation lẫn EnsureMiddleSplit.
        static bool TryLoadMiddleSprites(out Sprite[] sprites)
        {
            sprites = AssetDatabase.LoadAllAssetsAtPath(MiddleTexture).OfType<Sprite>()
                                   .OrderByDescending(x => x.rect.y).ToArray();
            if (sprites.Length != 2)
            {
                Debug.LogWarning($"[LockOpen] {MiddleTexture} cần cắt đúng 2 sprite (Sprite Editor ▸ Slice Automatic, pivot Center), đang có {sprites.Length}.");
                return false;
            }
            return true;
        }

        // Lớp nan (UpperLit) là con của tấm nền thì tách ra cùng cha, giữ vị trí world. Rồi xếp các
        // UpperLit theo x tăng dần trong số anh em — JSON gọi theo thứ tự đó: [0] trái, [1] phải.
        static void FlattenUppers(Transform root, List<Transform> uppers)
        {
            foreach (var u in uppers)
                if (u.parent != root) Undo.SetTransformParent(u, root, "Flatten shutter slats");

            var sorted = uppers.OrderBy(u => u.localPosition.x).ToList();
            var slots = uppers.Select(u => u.GetSiblingIndex()).OrderBy(i => i).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].GetSiblingIndex() == slots[i]) continue;
                Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Order shutter slats");
                sorted[i].SetSiblingIndex(slots[i]);
            }
        }

        // Middle cũ là một sprite Single pivot Center; sau khi cắt 2 sprite, đặt hai con Top/Bottom đúng
        // chỗ cũ: tâm rect so với tâm texture, chia PPU. Renderer của Middle tắt đi.
        // sprites phải đã được verify bằng TryLoadMiddleSprites (null khi Middle đã tách sẵn).
        static void EnsureMiddleSplit(Transform middle, Sprite[] sprites)
        {
            if (middle.Find("Top") != null && middle.Find("Bottom") != null) return;

            var src = middle.GetComponent<SpriteRenderer>();
            MakeBar(middle, "Top", sprites[0], src);
            MakeBar(middle, "Bottom", sprites[1], src);
            if (src != null) { Undo.RecordObject(src, "Split Middle"); src.enabled = false; }
        }

        static void MakeBar(Transform middle, string name, Sprite sprite, SpriteRenderer src)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Split Middle");
            go.transform.SetParent(middle, false);
            var tex = sprite.texture;
            var c = sprite.rect.center;
            go.transform.localPosition = new Vector3((c.x - tex.width * 0.5f) / sprite.pixelsPerUnit,
                                                     (c.y - tex.height * 0.5f) / sprite.pixelsPerUnit, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            if (src != null)
            {
                r.sortingLayerID = src.sortingLayerID;
                r.sortingOrder = src.sortingOrder;
                r.color = src.color;
                r.sharedMaterial = src.sharedMaterial;
                r.maskInteraction = src.maskInteraction;
            }
        }
    }
}
