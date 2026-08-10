using LogosSDK.Save;
using Reflex.Attributes;
using UnityEngine;

namespace WordStack.Meta
{
    /// <summary>
    /// Ghi mọi domain đang dirty xuống đĩa. Bắt buộc phải có: `ISaveManager.Save&lt;T&gt;()`
    /// chỉ đánh dấu dirty trong bộ nhớ, chỉ `SaveAll()` mới thật sự ghi file — thiếu nó
    /// thì số coin thay đổi trong phiên chơi sẽ mất sạch khi thoát.
    ///
    /// Component này phải sống TRONG SCENE, không đặt trên prefab root scope: Reflex
    /// không bao giờ instantiate prefab root scope (nó gọi thẳng InstallBindings trên
    /// asset), nên MonoBehaviour đặt ở đó sẽ không nhận được một callback vòng đời nào.
    /// </summary>
    public sealed class MetaSaveTrigger : MonoBehaviour
    {
        [Inject] private readonly ISaveManager _save;

        // Trên mobile OnApplicationQuit KHÔNG được bảo đảm gọi; pause mới là mốc đáng tin.
        private void OnApplicationPause(bool paused)
        {
            if (paused) _save?.SaveAll();
        }

        private void OnApplicationQuit()
        {
            _save?.SaveAll();
        }
    }
}
