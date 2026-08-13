using UnityEngine;

namespace LogosGame.Features.Gameplay.Views
{
    public sealed class GameplayReadyOverlayView : MonoBehaviour
    {
        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
