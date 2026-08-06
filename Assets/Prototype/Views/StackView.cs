// Một vị trí trên lưới: hộp trên cùng + các lớp hộp ẩn "lấp ló" bên dưới.
// Lớp lấp ló dựng sẵn trong prefab (offset/màu chỉnh bằng mắt), code chỉ bật/tắt.
using UnityEngine;

namespace WordStack.Prototype
{
    public class StackView : MonoBehaviour
    {
        [SerializeField] Transform boxAnchor;
        [SerializeField] GameObject[] peekLayers = new GameObject[3];
        [SerializeField] TextMesh overflow;

        public Transform BoxAnchor { get { return boxAnchor; } }

        public void ShowDepth(int hidden)
        {
            for (int i = 0; i < peekLayers.Length; i++)
                peekLayers[i].SetActive(i < hidden);

            bool over = hidden > peekLayers.Length;
            overflow.gameObject.SetActive(over);
            if (over) ViewText.Apply(overflow, "+" + hidden, 1.0f, 1.2f);
        }
    }
}
