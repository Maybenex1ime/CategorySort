// Một vị trí trên lưới: hộp trên cùng + các lớp Under Tray "lấp ló" bên dưới.
// Lớp lấp ló dựng sẵn trong prefab (offset/sprite chỉnh bằng mắt), code chỉ bật/tắt.
// Sâu hơn số lớp author sẵn thì hiện tối đa — không còn text "+N".
using UnityEngine;

namespace WordStack.Board
{
    public class StackView : MonoBehaviour
    {
        [SerializeField] Transform boxAnchor;
        [SerializeField] GameObject[] peekLayers = new GameObject[4];
        // Tile nhỏ ở mép Under Tray trên cùng — báo hộp ngay dưới đang chứa mấy thẻ.
        [SerializeField] GameObject[] nextTileMarkers = new GameObject[4];
        [Tooltip("Tile Holder mang các tile nhỏ ở trên — lộ hộp mới thì nó tách ra trượt xuống (BoardController.RevealBox). Trống = Peek1/TileMarkerHolder")]
        [SerializeField] Transform nextHolder;

        public Transform BoxAnchor { get { return boxAnchor; } }
        public GameObject[] NextTileMarkers { get { return nextTileMarkers; } }
        public Transform NextHolder
        {
            get
            {
                if (nextHolder == null && peekLayers.Length > 0 && peekLayers[0] != null)
                    nextHolder = peekLayers[0].transform.Find("TileMarkerHolder");
                return nextHolder;
            }
        }

        public void ShowDepth(int hidden, int tilesInNext)
        {
            for (int i = 0; i < peekLayers.Length; i++)
                peekLayers[i].SetActive(i < hidden);

            for (int i = 0; i < nextTileMarkers.Length; i++)
                if (nextTileMarkers[i] != null)
                    nextTileMarkers[i].SetActive(hidden > 0 && i < tilesInNext);
        }
    }
}
