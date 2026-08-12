# EventBus Demo

This demo showcases the EventBus system, a lightweight, zero-allocation event bus for Unity.

## Contents

- **Scripts**: C# scripts demonstrating EventBus usage
- **Documentation**: Step-by-step tutorial and setup instructions

## Getting Started

The demo scene needs to be set up from scratch:

1. Follow the detailed instructions in `Documentation/CreateDemoScene.md` to create the demo scene
2. Once set up, use the following controls to interact with the demo:
   - `D` - Take damage
   - `C` - Collect item
   - `P` - Toggle pause
   - `R` - Restart game

## Documentation

For detailed instructions, see:
- `Documentation/CreateDemoScene.md` - Step-by-step guide to create the demo scene
- `Documentation/EventBusDemo_Tutorial.md` - Learn how the EventBus system works
- `Documentation/SceneSetupInstructions.md` - Additional scene setup information

## Features Demonstrated

- Basic event publishing and subscribing
- Ordered event handling with `[EventOrder]` attribute
- Asynchronous event handling with `ValueTask`
- Automatic cleanup when objects are destroyed

## Components

- **DemoPlayerController**: Fires events based on user input
- **DemoUIController**: Updates UI based on events
- **DemoAsyncHandler**: Demonstrates asynchronous event handling
- **DemoSceneSetup**: Sets up the demo scene

## Events

- **PlayerDamagedEvent**: Fired when the player takes damage
- **ItemCollectedEvent**: Fired when the player collects an item
- **GameStateChangedEvent**: Fired when the game state changes

## Next Steps

After exploring this demo, check out the main EventBus documentation in `Assets/Core/Utils/EventBus/README.md` for more advanced usage and best practices. 