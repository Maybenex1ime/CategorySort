using LogosSDK.Core.Events;
using UnityEngine;

namespace EventBus.Demo
{
    /// <summary>
    /// Demo player controller that fires events based on user input
    /// </summary>
    public class DemoPlayerController : MonoBehaviour
    {
        [SerializeField] private int health = 100;
        [SerializeField] private string[] collectibleItems = { "Coin", "Gem", "Potion", "Key" };
        
        private void Update()
        {
            // Press D to take damage
            if (Input.GetKeyDown(KeyCode.D))
            {
                TakeDamage(Random.Range(5, 20), "Enemy");
            }
            
            // Press C to collect an item
            if (Input.GetKeyDown(KeyCode.C))
            {
                CollectRandomItem();
            }
            
            // Press P to toggle pause
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
            
            // Press R to restart game
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        private void TakeDamage(int amount, string source)
        {
            health -= amount;
            Debug.Log($"Player took {amount} damage from {source}. Health: {health}");
            
            // Fire the event
            Bus.Global.Fire(new PlayerDamagedEvent
            {
                DamageAmount = amount,
                DamageSource = source
            });
            
            // Check for game over
            if (health <= 0)
            {
                health = 0;
                Bus.Global.Fire(new GameStateChangedEvent { NewState = GameState.GameOver });
            }
        }

        private void CollectRandomItem()
        {
            string itemName = collectibleItems[Random.Range(0, collectibleItems.Length)];
            int itemValue = Random.Range(1, 100);
            
            Debug.Log($"Player collected {itemName} worth {itemValue} points");
            
            // Fire the event
            Bus.Global.Fire(new ItemCollectedEvent
            {
                ItemName = itemName,
                ItemValue = itemValue
            });
        }

        private void TogglePause()
        {
            bool isPaused = Time.timeScale < 0.01f;
            
            if (isPaused)
            {
                Time.timeScale = 1.0f;
                Bus.Global.Fire(new GameStateChangedEvent { NewState = GameState.Playing });
                Debug.Log("Game resumed");
            }
            else
            {
                Time.timeScale = 0.0f;
                Bus.Global.Fire(new GameStateChangedEvent { NewState = GameState.Paused });
                Debug.Log("Game paused");
            }
        }

        private void RestartGame()
        {
            health = 100;
            Time.timeScale = 1.0f;
            Bus.Global.Fire(new GameStateChangedEvent { NewState = GameState.Playing });
            Debug.Log("Game restarted");
        }
    }
} 