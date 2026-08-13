using TMPro;
using UnityEngine;

namespace EventBus.Demo
{
    /// <summary>
    /// Sets up the demo scene with necessary UI elements and components
    /// </summary>
    public class DemoSceneSetup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text gameStateText;
        [SerializeField] private TMP_Text eventLogText;
        [SerializeField] private TMP_Text controlsText;
        
        [Header("Components")]
        [SerializeField] private DemoPlayerController playerController;
        [SerializeField] private DemoUIController uiController;
        [SerializeField] private DemoAsyncHandler asyncHandler;
        
        private void Start()
        {
            if (controlsText != null)
            {
                // Set up the controls text
                controlsText.text = "Controls:\n" +
                                   "D - Take Damage\n" +
                                   "C - Collect Item\n" +
                                   "P - Toggle Pause\n" +
                                   "R - Restart Game";
            }
            
            // Set up references
            if (uiController != null)
            {
                // Use reflection to set the UI references
                var healthTextField = uiController.GetType().GetField("healthText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var scoreTextField = uiController.GetType().GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gameStateTextField = uiController.GetType().GetField("gameStateText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var eventLogTextField = uiController.GetType().GetField("eventLogText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (healthTextField != null) healthTextField.SetValue(uiController, healthText);
                if (scoreTextField != null) scoreTextField.SetValue(uiController, scoreText);
                if (gameStateTextField != null) gameStateTextField.SetValue(uiController, gameStateText);
                if (eventLogTextField != null) eventLogTextField.SetValue(uiController, eventLogText);
            }
        }
    }
} 