// ============================================================================
// Chạy một chuỗi nước đi lên bàn đang Play, chờ cascade lắng giữa mỗi nước, rồi báo kết quả.
// Menu: WordStack ▸ Test ▸ Play 4 moves on lv-001
//
// Vì sao không mô phỏng chuột: Editor CHẶN sự kiện pointer bơm bằng code khi Game View không có
// focus, mà tự động hoá thì không giữ được focus. Driver này gọi BoardController.DebugMove —
// vào đúng chỗ Drop() đi tiếp sau khi domain nhận nước đi.
//
// Cái nó KHÔNG kiểm: hit-test vùng thả, ghost đuổi con trỏ, hover, feel. Mấy thứ đó phải kéo tay.
// ============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WordStack.Prototype
{
    public static class BoardTestDriver
    {
        // lv-001: Dog + Cat sang hộp trống, rồi Apple + banana sang hộp phải trên
        // => 4 fruit trong một hộp => CLEAR.
        static readonly int[,] Lv001 = { { 1, 2, 3 }, { 1, 3, 3 }, { 0, 0, 1 }, { 0, 1, 1 } };

        static BoardController board;
        static int[,] moves;
        static int index;
        static double waitUntil;
        static List<string> log = new List<string>();

        [MenuItem("WordStack/Test/Play 4 moves on lv-001")]
        public static void RunLv001() { Run(Lv001); }

        public static void Run(int[,] seq)
        {
            board = Object.FindFirstObjectByType<BoardController>();
            if (board == null) { Debug.LogError("BoardTestDriver: chưa Play, hoặc scene không có BoardController."); return; }
            moves = seq;
            index = 0;
            waitUntil = 0;
            log.Clear();
            log.Add("bat dau: " + board.DebugStatus);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            if (board == null) { Finish("BoardController biến mất (thoát Play?)"); return; }
            if (board.DebugBusy) return;                       // đang cascade, chờ

            if (index >= moves.GetLength(0)) { Finish(null); return; }

            int from = moves[index, 0], slot = moves[index, 1], to = moves[index, 2];
            bool ok = board.DebugMove(from, slot, to);
            log.Add("nuoc " + index + ": stack " + from + " slot " + slot + " -> stack " + to +
                    (ok ? "  OK" : "  TU CHOI") + " | " + board.DebugStatus);
            index++;
            waitUntil = EditorApplication.timeSinceStartup + 1.5;   // chờ tween + cascade
        }

        static void Finish(string err)
        {
            EditorApplication.update -= Tick;
            if (err != null) log.Add("LOI: " + err);
            else log.Add("ket thuc: " + board.DebugStatus);
            Shot();
            System.IO.File.WriteAllLines(Application.dataPath + "/../Temp/testdrive.txt", log);
            Debug.Log("BoardTestDriver:\n" + string.Join("\n", log));
        }

        static void Shot()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rt = new RenderTexture(1000, 700, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1000, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1000, 700), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(Application.dataPath + "/../Temp/testdrive.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
