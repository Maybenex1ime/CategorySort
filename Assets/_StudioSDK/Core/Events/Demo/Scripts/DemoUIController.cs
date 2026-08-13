using System.Collections.Generic;
using LogosSDK.Core.Events;
using TMPro;
using UnityEngine;

namespace EventBus.Demo
{
    /// <summary>
    /// Demo UI controller that listens to events and updates the UI
    /// </summary>
    public class DemoUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text gameStateText;
        [SerializeField] private TMP_Text eventLogText;
        
        private int score = 0;
        private List<string> eventLog = new List<string>();
        private const int MaxLogEntries = 10;
        
        private void OnEnable()
        {
            // Subscribe to events
            Bus.Global.On<PlayerDamagedEvent>(OnPlayerDamaged);
            Bus.Global.On<ItemCollectedEvent>(OnItemCollected);
            Bus.Global.On<GameStateChangedEvent>(OnGameStateChanged);
            
            // Initialize UI
            UpdateHealthUI(100);
            UpdateScoreUI();
            UpdateGameStateUI(GameState.Playing);
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            Bus.Global.Off<PlayerDamagedEvent>(OnPlayerDamaged);
            Bus.Global.Off<ItemCollectedEvent>(OnItemCollected);
            Bus.Global.Off<GameStateChangedEvent>(OnGameStateChanged);
        }
        
        // Event handlers
        
        // This handler will be called first due to the EventOrder attribute
        [EventOrder(-10)]
        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            string logMessage = $"Damage: {evt.DamageAmount} from {evt.DamageSource}";
            AddLogEntry(logMessage);
            
            // Get the health directly from the event
            // We'll calculate the new health based on the damage
            // This is a simplified approach for the demo
            int currentHealth = 0;
            
            // Try to parse the current health from the UI if available
            if (healthText != null && healthText.text.StartsWith("Health: "))
            {
                string healthStr = healthText.text.Substring("Health: ".Length);
                if (int.TryParse(healthStr, out int parsedHealth))
                {
                    currentHealth = parsedHealth;
                }
                else
                {
                    currentHealth = 100; // Default value if parsing fails
                }
            }
            else
            {
                currentHealth = 100; // Default value if text is not available
            }
            
            // Calculate new health after damage
            int newHealth = Mathf.Max(0, currentHealth - evt.DamageAmount);
            
            UpdateHealthUI(newHealth);
        }
        
        private void OnItemCollected(ItemCollectedEvent evt)
        {
            score += evt.ItemValue;
            UpdateScoreUI();
            
            string logMessage = $"Collected: {evt.ItemName} (+{evt.ItemValue} points)";
            AddLogEntry(logMessage);
        }
        
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdateGameStateUI(evt.NewState);
            
            string logMessage = $"Game State: {evt.NewState}";
            AddLogEntry(logMessage);
        }
        
        // UI update methods
        
        private void UpdateHealthUI(int health)
        {
            if (healthText != null)
            {
                healthText.text = $"Health: {health}";
                
                // Change color based on health
                if (health < 30)
                {
                    healthText.color = Color.red;
                }
                else if (health < 60)
                {
                    healthText.color = Color.yellow;
                }
                else
                {
                    healthText.color = Color.green;
                }
            }
        }
        
        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }
        
        private void UpdateGameStateUI(GameState state)
        {
            if (gameStateText != null)
            {
                gameStateText.text = $"State: {state}";
                
                // Change color based on state
                switch (state)
                {
                    case GameState.Playing:
                        gameStateText.color = Color.green;
                        break;
                    case GameState.Paused:
                        gameStateText.color = Color.yellow;
                        break;
                    case GameState.GameOver:
                        gameStateText.color = Color.red;
                        break;
                    default:
                        gameStateText.color = Color.white;
                        break;
                }
            }
        }
        
        private void AddLogEntry(string entry)
        {
            eventLog.Add(entry);
            
            // Keep only the most recent entries
            if (eventLog.Count > MaxLogEntries)
            {
                eventLog.RemoveAt(0);
            }
            
            // Update the log text
            if (eventLogText != null)
            {
                eventLogText.text = string.Join("\n", eventLog);
            }
        }
    }
} 