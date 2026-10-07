using System.Collections.Generic;
using UnityEngine;

namespace WordStack.Board
{
    // Toán fit camera ortho: khung bàn (world) nằm trọn vùng trống (pixel) giữa HUD, tâm bàn
    // rơi đúng tâm vùng trống. Tách khỏi BoardController để test được không cần scene.
    public static class BoardFit
    {
        public static void FitOrtho(Rect board, Rect freePx, Vector2 screenPx, out Vector2 camPos, out float orthoSize)
        {
            float h = Mathf.Max(screenPx.y, 1f);
            if (freePx.width <= 0f || freePx.height <= 0f)
                freePx = new Rect(0f, 0f, Mathf.Max(screenPx.x, 1f), h);

            orthoSize = Mathf.Max(board.height * h / (2f * freePx.height), board.width * h / (2f * freePx.width));
            float worldPerPx = 2f * orthoSize / h;
            camPos = board.center - (freePx.center - screenPx / 2f) * worldPerPx;
        }

        // Blocker có tâm ở nửa trên đẩy mép trên xuống, nửa dưới đẩy mép dưới lên.
        public static Rect FreeArea(Rect safeArea, Vector2 screenPx, IEnumerable<Rect> blockersPx)
        {
            float top = safeArea.yMax, bottom = safeArea.yMin;
            foreach (var b in blockersPx)
            {
                if (b.center.y >= screenPx.y / 2f) top = Mathf.Min(top, b.yMin);
                else bottom = Mathf.Max(bottom, b.yMax);
            }
            return Rect.MinMaxRect(safeArea.xMin, bottom, safeArea.xMax, top);
        }
    }
}
