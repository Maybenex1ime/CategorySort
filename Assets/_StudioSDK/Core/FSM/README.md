# Unity FSM System - API Documentation

## Overview
A flexible, type-safe Finite State Machine system for Unity that supports event-driven state transitions, automatic Unity lifecycle integration, and clean separation of concerns.

## Core Architecture

### Key Interfaces

#### `ITrigger`
```csharp
public interface ITrigger { }
```
Base interface for all events that can trigger state transitions.

#### `IState<TState, TTrigger>`
```csharp
public interface IState<out TState, in TTrigger> 
    where TState : IState<TState, TTrigger> 
    where TTrigger : TTrigger
{
    void OnEnter();           // Called when entering this state
    void OnExit();            // Called when exiting this state
    void OnUpdate();          // Called every frame while active
    void OnFixedUpdate();     // Called every fixed update while active
    TState HandleTrigger(TTrigger data); // Process triggers and return next state
}
```

#### `IStateMachineCore`
```csharp
public interface IStateMachineCore
{
    void OnUpdate();
    void OnFixedUpdate();
}
```

## Core Classes

### `StateMachine<TState, TTrigger>`
Main state machine implementation with automatic Unity integration.

**Constructor:**
```csharp
public StateMachine(GameObject gameObject)
```
- Automatically adds `StateMachineRunner` component to GameObject
- Integrates with Unity's Update/FixedUpdate lifecycle

**Key Methods:**
```csharp
public void Initialize(TState initialState)           // Set starting state
public bool Trigger(TTrigger data)           // Send event to current state
public TState CurrentState { get; }                  // Get current active state
```

**Properties:**
- `CurrentState`: Returns the currently active state
- Thread-safe transition handling with `_isTransitioning` flag

### `StateBase<TState, TTrigger>`
Abstract base class for implementing states with trigger handling.

**Key Features:**
```csharp
protected readonly Dictionary<Type, Func<TTrigger, TState>> TriggerHandlers;

protected void RegisterTriggerHandler<T>(Func<T, TState> handler) where T : TTrigger
public virtual TState HandleTrigger(TTrigger data)
```

**Usage Pattern:**
```csharp
public class MyState : StateBase<IMyState, IMyTrigger>, IMyState
{
    public MyState()
    {
        RegisterTriggerHandler<SpecificTrigger>(OnSpecificTrigger);
    }
    
    private IMyState OnSpecificTrigger(SpecificTrigger trigger)
    {
        return new NextState();
    }
}
```

### `StateMachineFactory`
Factory for creating and initializing state machines.

```csharp
public static StateMachine<TState, TTrigger> Create<TState, TTrigger>(
    GameObject gameObject, 
    TState initialState)
    where TState : IState<TState, TTrigger>
    where TTrigger : ITrigger
```

### `StateMachineRunner`
MonoBehaviour component that bridges Unity lifecycle to state machine.

**Auto-attached to GameObject when StateMachine is created**
- Handles Update() and FixedUpdate() calls
- Manages component lifecycle

## Implementation Patterns

### 1. Define Trigger Types
```csharp
public interface IMyTrigger : ITrigger { }

public class JumpTrigger : IMyTrigger { }

public class MoveTrigger : IMyTrigger 
{
    public Vector3 Direction { get; }
    public MoveEvent(Vector3 direction) => Direction = direction;
}
```

### 2. Define State Interface
```csharp
public interface IMyState : IState<IMyState, IMyTrigger> { }
```

### 3. Implement States
```csharp
public class IdleState : StateBase<IMyState, IMyTrigger>, IMyState
{
    private readonly MyController _controller;
    
    public IdleState(MyController controller)
    {
        _controller = controller;
        RegisterTriggerHandler<JumpTrigger>(OnJump);
        RegisterTriggerHandler<MoveTrigger>(OnMove);
    }
    
    public override void OnEnter()
    {
        Debug.Log("Entering Idle State");
        _controller.StopMovement();
    }
    
    public override void OnUpdate()
    {
        // Idle-specific update logic
    }
    
    private IMyState OnJump(JumpTrigger trigger) => new JumpState(_controller);
    private IMyState OnMove(MoveTrigger trigger) => new MoveState(_controller, trigger.Direction);
}
```

### 4. Create State Machine
```csharp
public class MyController : MonoBehaviour
{
    private StateMachine<IMyState, IMyTrigger> _stateMachine;
    
    private void Awake()
    {
        _stateMachine = StateMachineFactory.Create<IMyState, IMyTrigger>(
            gameObject, 
            new IdleState(this)
        );
    }
    
    public void Trigger(IMyTrigger data)
    {
        _stateMachine.Trigger(data);
    }
}
```

## Key Features

### Event-Driven Architecture
- States register handlers for specific trigger types
- Type-safe trigger handling with generic constraints
- Automatic trigger routing based on type

### Unity Integration
- Automatic MonoBehaviour lifecycle management
- GameObject-scoped state machine instances
- Update/FixedUpdate integration without manual calls

### Thread Safety
- Transition locking prevents race conditions
- Safe event triggering during transitions

### Type Safety
- Generic constraints ensure compile-time type checking
- Interface-based design for flexibility
- Clear separation between triggers and states

## Best Practices

### State Design
- Keep states focused and single-purpose
- Pass dependencies through constructor
- Use composition over inheritance when possible

### Trigger Design
- Make triggers immutable with readonly properties
- Include all necessary data in trigger constructors
- Use descriptive trigger names (VerbTrigger pattern)

### Controller Integration
- Keep state machine reference in main controller
- Expose Trigger method for external systems
- Use dependency injection for shared components

### Performance Considerations
- Trigger handlers are cached in Dictionary for O(1) lookup
- Minimize allocations in OnUpdate/OnFixedUpdate
- Consider object pooling for frequently created states

## File Structure
```
Core/FSM/Scripts
├── ITrigger.cs                    // Base trigger interface
├── IState.cs                      // State interface definition
├── IAsyncState.cs                 // Async extension for App Flow states
├── IStateMachineCore.cs           // Core state machine interface
├── StateMachine.cs                // Main state machine implementation
├── StateBase.cs                   // Abstract state base class
├── StateMachineFactory.cs         // Factory for creating state machines
└── StateMachineRunner.cs          // Unity MonoBehaviour bridge
```

## Namespace
`LogosSDK.Core.FSM` — part of the `LogosSDK.Core` assembly (no separate asmdef).

## Two-Tier FSM (per puzzle-architecture.md)

**App Flow** (Boot → Splash → MainMenu → Gameplay → Result):
- States implement both `IState<TState, TTrigger>` and `IAsyncState`
- Use `StateMachineFactory.CreateAsync()` and `TriggerAsync()`
- `IAsyncState.OnEnterAsync/OnExitAsync` awaited automatically

**Gameplay Flow** (Ready → Playing → Evaluating → Win/Lose):
- States implement only `IState<TState, TTrigger>` (sync)
- Use `StateMachineFactory.Create()` and `Trigger()`

## Dependencies
- Unity 6 or higher (Awaitable for async support)
- No external dependencies for core system
- Part of LogosSDK.Core assembly
- Demo uses Unity's built-in Animator and Rigidbody components