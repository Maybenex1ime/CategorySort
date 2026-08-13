using System.Threading.Tasks;
using LogosSDK.Core.Events;
using UnityEngine;

namespace EventBus.Demo
{
    /// <summary>
    /// Demonstrates how to use async event handlers
    /// </summary>
    public class DemoAsyncHandler : MonoBehaviour
    {
        private void OnEnable()
        {
            // Subscribe to events with async handlers
            Bus.Global.OnAsync<ItemCollectedEvent>(OnItemCollectedAsync);
            Bus.Global.OnAsync<GameStateChangedEvent>(OnGameStateChangedAsync);
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            Bus.Global.OffAsync<ItemCollectedEvent>(OnItemCollectedAsync);
            Bus.Global.OffAsync<GameStateChangedEvent>(OnGameStateChangedAsync);
        }
        
        // Async event handlers
        
        private async ValueTask OnItemCollectedAsync(ItemCollectedEvent evt)
        {
            Debug.Log($"[ASYNC] Starting to process item collection: {evt.ItemName}");
            
            // Simulate some async processing
            await Task.Delay(1000);
            
            Debug.Log($"[ASYNC] Finished processing item collection: {evt.ItemName} worth {evt.ItemValue} points");
            
            // You could perform additional async operations here, like:
            // - Loading data from a database
            // - Making network requests
            // - Performing complex calculations off the main thread
        }
        
        private async ValueTask OnGameStateChangedAsync(GameStateChangedEvent evt)
        {
            Debug.Log($"[ASYNC] Game state changing to: {evt.NewState}");
            
            // Simulate different processing times based on the state
            int delayTime = evt.NewState switch
            {
                GameState.MainMenu => 500,
                GameState.Playing => 1000,
                GameState.Paused => 300,
                GameState.GameOver => 2000,
                _ => 500
            };
            
            await Task.Delay(delayTime);
            
            Debug.Log($"[ASYNC] Game state change to {evt.NewState} processed");
        }
    }
} 