// HUD world-space: thanh tiêu đề trên bàn, dòng hướng dẫn dưới bàn, 2 panel trạng thái.
// Bố cục phụ thuộc kích thước bàn nên controller gọi Layout() một lần mỗi level.
using UnityEngine;

namespace WordStack.Prototype
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] Transform headerBg;
        [SerializeField] TextMesh title;
        [SerializeField] TextMesh help;
        [SerializeField] GameObject winPanel;
        [SerializeField] TextMesh winTitle;
        [SerializeField] TextMesh winHint;
        [SerializeField] GameObject stuckPanel;
        [SerializeField] Transform stuckBg;
        [SerializeField] TextMesh stuckLabel;

        float cx, bottom, width = 6f;

        public void Layout(float centerX, float top, float boardBottom, float boardWidth)
        {
            cx = centerX;
            bottom = boardBottom;
            width = boardWidth;

            headerBg.localPosition = new Vector3(cx, top + 0.58f, 0f);
            headerBg.localScale = new Vector3(width, 0.55f, 1f);
            title.transform.localPosition = new Vector3(cx, top + 0.58f, 0f);

            help.transform.localPosition = new Vector3(cx, bottom - 0.45f, 0f);
            // R/N là phím dev bỏ qua AppFlow — không quảng cáo cho người chơi nữa.
            ViewText.Apply(help, "Drag a tile onto another stack", 0.85f, width);

            winPanel.transform.localPosition = new Vector3(cx, 0f, 0f);
            stuckPanel.transform.localPosition = new Vector3(cx, bottom - 1.15f, 0f);
            stuckBg.localScale = new Vector3(width * 0.9f, 0.7f, 1f);
        }

        public void Set(string levelTitle, int cleared, int total, int moves)
        {
            ViewText.Apply(title,
                levelTitle + "   ·   " + cleared + "/" + total + " groups   ·   " + moves + " moves",
                1.00f, width - 0.36f);
        }

        public void ShowWin()
        {
            HideAll();
            winPanel.SetActive(true);
            ViewText.Apply(winTitle, "Complete!", 3.0f, 6f);
            ViewText.Apply(winHint, "click / N for next level", 1.2f, 12f);
        }

        public void ShowStuck()
        {
            HideAll();
            stuckPanel.SetActive(true);
            ViewText.Apply(stuckLabel, "No moves left — click or press R to restart", 1.0f, width * 0.85f);
        }

        public void HideAll()
        {
            winPanel.SetActive(false);
            stuckPanel.SetActive(false);
        }
    }
}
