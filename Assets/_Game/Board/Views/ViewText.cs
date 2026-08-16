// Chữ dùng chung cho TileView / StackView.
//
// Font là dynamic font của HĐH (TextMesh, không TMP — xem Q2 trong view-prefabs.md): prefab
// không tham chiếu được nên BoardController tạo lúc Awake rồi gán vào đây. Static vì cả bàn
// chỉ có một font; đổi thành field tiêm vào từng view khi nào có bàn thứ hai.
using UnityEngine;

namespace WordStack.Board
{
    public static class ViewText
    {
        // Bề rộng trung bình một ký tự = CharW × size. Đo THẬT trong Play mode bằng
        // Renderer.bounds trên TextMesh; lấy giá trị LỚN nhất đo được để chữ không bao giờ tràn.
        public const float CharW = 0.085f;

        public static Font Font;

        // maxWidth: co chữ cho vừa bề rộng cho phép. Chỉ CO, không bao giờ phóng to.
        // KHÔNG tự ngắt dòng: ngắt ở mọi khoảng trắng biến câu HUD thành cột dọc một từ
        // mỗi dòng. Chuỗi dài thì co chữ, không xé câu.
        public static void Apply(TextMesh tm, string text, float size, float maxWidth)
        {
            if (tm == null) return;
            if (Font != null && tm.font != Font)
            {
                tm.font = Font;
                var mr = tm.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = Font.material;
            }
            float fit = maxWidth / Mathf.Max(text.Length * CharW, 0.0001f);
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.021f * Mathf.Min(size, fit);
        }

        public static void Order(TextMesh tm, int order)
        {
            var mr = tm.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = order;
        }
    }
}
