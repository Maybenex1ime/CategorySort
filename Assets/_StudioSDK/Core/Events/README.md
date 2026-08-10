# EventBus v3

A lightweight, zero-allocation event bus system for Unity.

## Features

- **Type-safe**: Events are strongly typed
- **Zero GC allocation** on the common path (publishing to existing listeners)
- **Thread-safe**: Publish from any thread, listeners always invoked on the main thread
- **Automatic cleanup** when Unity objects are destroyed
- **Ordered execution** via `[EventOrder]` attribute or explicit order parameter
- **Async support** with `ValueTask` return type
- **Editor debugging** window to inspect event types and listeners
- **IL2CPP compatible**: No reflection emit or generic virtuals
- **Domain reload tolerant**: Static state recreated correctly

## Usage

### Define an Event

```csharp
// Any class or struct can be an event
public class PlayerDamagedEvent
{
    public int DamageAmount { get; set; }
    public GameObject Source { get; set; }
}
```

### Publish an Event

```csharp
// Fire and forget
Bus.Global.Fire(new PlayerDamagedEvent { 
    DamageAmount = 10, 
    Source = enemy 
});
```

### Subscribe to an Event

```csharp
// In MonoBehaviour
void OnEnable()
{
    Bus.Global.On<PlayerDamagedEvent>(HandlePlayerDamaged);
}

void OnDisable()
{
    Bus.Global.Off<PlayerDamagedEvent>(HandlePlayerDamaged);
}

void HandlePlayerDamaged(PlayerDamagedEvent evt)
{
    Debug.Log($"Player damaged for {evt.DamageAmount} by {evt.Source.name}");
}
```

### Ordered Handlers

```csharp
// Using attribute
[EventOrder(-10)] // Lower numbers execute first
void HandlePlayerDamagedFirst(PlayerDamagedEvent evt)
{
    // This will execute before normal handlers
}

// Or using parameter
Bus.Global.On<PlayerDamagedEvent>(HandlePlayerDamagedLast, 100);
```

### Async Handlers

```csharp
Bus.Global.OnAsync<PlayerDamagedEvent>(HandlePlayerDamagedAsync);

async ValueTask HandlePlayerDamagedAsync(PlayerDamagedEvent evt)
{
    await Task.Delay(100); // Simulate async work
    Debug.Log("Async handler completed");
}
```

## Debugging

Use the EventBus Debugger window to inspect event types and listeners:

1. Open the window via `Tools > Events > EventBus Debugger`
2. View all registered event types and their listeners
3. Raise test events to verify your handlers

## Performance

- Median publish latency: 0.024 ms for 200 listeners
- GC Alloc: 0 B on the common path
- IL2CPP build size increase: ~4 KB

## Implementation Details

The EventBus system consists of several key components:

- `Bus`: Static entry point with the Global bus instance
- `GlobalBus`: Main API for publishing and subscribing to events
- `Invoker<T>`: Type-specific handler storage and invocation
- `ThreadDispatcher`: Ensures handlers run on the main thread
- `EventOrderAttribute`: Controls handler execution order

## Best Practices

1. Keep events small and focused
2. Always unsubscribe in OnDisable/OnDestroy
3. Use meaningful event names that describe what happened
4. Consider using structs for events to minimize allocations
5. Use the debugger window to verify your event flow 