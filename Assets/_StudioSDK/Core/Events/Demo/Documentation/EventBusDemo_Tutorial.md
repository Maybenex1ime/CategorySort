# EventBus Demo Tutorial

This tutorial will guide you through the EventBus demo, showing you how to use the EventBus system in your Unity projects.

## Overview

The EventBus system provides a decoupled way for different parts of your game to communicate with each other. Instead of direct references between components, they can communicate through events.

This demo showcases:
- Basic event publishing and subscribing
- Ordered event handling
- Asynchronous event handling
- Automatic cleanup when objects are destroyed

## Step 1: Open the Demo Scene

1. Navigate to `Assets/Core/Utils/EventBus/Demo/Scenes`
2. Open the `EventBusDemo` scene

## Step 2: Explore the Scene Hierarchy

The scene contains:
- A `DemoManager` GameObject with the `DemoSceneSetup` component
- A `Player` GameObject with the `DemoPlayerController` component
- A `UIManager` GameObject with the `DemoUIController` component
- An `AsyncHandler` GameObject with the `DemoAsyncHandler` component
- Various UI elements for displaying game state

## Step 3: Run the Demo

1. Press Play in the Unity Editor
2. Use the following controls to interact with the demo:
   - Press `D` to make the player take damage
   - Press `C` to collect a random item
   - Press `P` to toggle pause state
   - Press `R` to restart the game

3. Observe how events flow through the system:
   - The `DemoPlayerController` fires events when actions occur
   - The `DemoUIController` listens for these events and updates the UI
   - The `DemoAsyncHandler` processes events asynchronously

## Step 4: Examine the Code

### Event Definitions

Open `DemoEvents.cs` to see how events are defined:

```csharp
public class PlayerDamagedEvent
{
    public int DamageAmount { get; set; }
    public string DamageSource { get; set; }
}
```

Events can be any class or struct. They typically contain data relevant to the event.

### Publishing Events

Open `DemoPlayerController.cs` to see how events are published:

```csharp
Bus.Global.Fire(new PlayerDamagedEvent
{
    DamageAmount = amount,
    DamageSource = source
});
```

Events are published using `Bus.Global.Fire()` with an instance of the event class.

### Subscribing to Events

Open `DemoUIController.cs` to see how to subscribe to events:

```csharp
private void OnEnable()
{
    // Subscribe to events
    Bus.Global.On<PlayerDamagedEvent>(OnPlayerDamaged);
    Bus.Global.On<ItemCollectedEvent>(OnItemCollected);
    Bus.Global.On<GameStateChangedEvent>(OnGameStateChanged);
}
```

Subscribe in `OnEnable()` and unsubscribe in `OnDisable()` to ensure proper cleanup.

### Ordered Event Handling

The `OnPlayerDamaged` method in `DemoUIController.cs` uses the `[EventOrder]` attribute:

```csharp
[EventOrder(-10)]
private void OnPlayerDamaged(PlayerDamagedEvent evt)
{
    // This handler will be called first
}
```

Lower order values are executed first.

### Asynchronous Event Handling

Open `DemoAsyncHandler.cs` to see how to handle events asynchronously:

```csharp
private async ValueTask OnItemCollectedAsync(ItemCollectedEvent evt)
{
    Debug.Log($"[ASYNC] Starting to process item collection: {evt.ItemName}");
    
    // Simulate some async processing
    await Task.Delay(1000);
    
    Debug.Log($"[ASYNC] Finished processing item collection: {evt.ItemName}");
}
```

Async handlers return `ValueTask` and can use `await` for asynchronous operations.

## Step 5: Try Modifying the Demo

Here are some ideas to try:

1. Add a new event type (e.g., `LevelCompletedEvent`)
2. Add a new component that listens for existing events
3. Modify the `DemoPlayerController` to fire events on additional key presses
4. Add UI elements to display more information about events

## Step 6: Using EventBus in Your Own Projects

To use EventBus in your own projects:

1. Define your event classes (any class or struct can be an event)
2. Fire events using `Bus.Global.Fire(new YourEventType())`
3. Subscribe to events using `Bus.Global.On<YourEventType>(YourHandlerMethod)`
4. Unsubscribe when done using `Bus.Global.Off<YourEventType>(YourHandlerMethod)`
5. For async handling, use `Bus.Global.OnAsync<YourEventType>(YourAsyncHandlerMethod)`

## Best Practices

1. Keep events small and focused
2. Always unsubscribe in `OnDisable()` or `OnDestroy()`
3. Use meaningful event names that describe what happened
4. Consider using structs for events to minimize allocations
5. Use the EventBus debugger window (`Tools > Events > EventBus Debugger`) to verify your event flow

## Conclusion

The EventBus system provides a powerful way to decouple your game components while maintaining clear communication paths. By using events instead of direct references, you can create more maintainable and extensible code.

For more information, see the main EventBus documentation in `Assets/Core/Utils/EventBus/README.md`. 