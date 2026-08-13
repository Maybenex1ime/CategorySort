# Creating the EventBus Demo Scene

Since the provided `EventBusDemo.unity` scene is empty, follow these steps to create a working demo scene from scratch.

## Step 1: Create a New Scene

1. Open Unity and navigate to your project
2. Go to `File > New Scene` and select the `Basic (Built-in)` template
3. Save the scene as `EventBusDemo` in the `Assets/Core/Utils/EventBus/Demo/Scenes` folder (replacing the empty file)

## Step 2: Set Up the Main Camera and Light

1. Select the Main Camera in the Hierarchy
2. In the Inspector, set its position to `(0, 1, -10)`
3. Select the Directional Light in the Hierarchy
4. Set its rotation to `(50, -30, 0)` for better lighting

## Step 3: Create the UI

1. Create a Canvas:
   - Right-click in the Hierarchy > UI > Canvas
   - In the Canvas component, set Render Mode to "Screen Space - Overlay"
   - Add a Canvas Scaler component (if not already present)
   - Set UI Scale Mode to "Scale With Screen Size" with Reference Resolution `1920 x 1080`

2. Create a Panel for the background:
   - Right-click on Canvas > UI > Panel
   - Set its color to a semi-transparent black: `R: 0, G: 0, B: 0, A: 0.8`
   - Set its Rect Transform to stretch in all directions with 20px padding

3. Create TextMeshPro Text elements as children of the Panel:
   - **Health Text**:
     - Right-click on Panel > UI > TextMeshPro - Text
     - Set Text to "Health: 100"
     - Set Color to green `(0, 1, 0, 1)`
     - Position in top-left corner
     - Rename to "HealthText"

   - **Score Text**:
     - Right-click on Panel > UI > TextMeshPro - Text
     - Set Text to "Score: 0"
     - Position in top-right corner
     - Rename to "ScoreText"

   - **Game State Text**:
     - Right-click on Panel > UI > TextMeshPro - Text
     - Set Text to "State: Playing"
     - Set Color to green `(0, 1, 0, 1)`
     - Position in top-center
     - Rename to "GameStateText"

   - **Event Log Text**:
     - Right-click on Panel > UI > TextMeshPro - Text
     - Clear the Text field (empty)
     - Position in bottom-left, make it larger
     - Set Vertical Overflow to "Overflow"
     - Rename to "EventLogText"

   - **Controls Text**:
     - Right-click on Panel > UI > TextMeshPro - Text
     - Set Text to:
       ```
       Controls:
       D - Take Damage
       C - Collect Item
       P - Toggle Pause
       R - Restart Game
       ```
     - Position in bottom-right corner
     - Rename to "ControlsText"

## Step 4: Create Game Objects with Components

1. Create the DemoManager:
   - Right-click in Hierarchy > Create Empty
   - Rename to "DemoManager"
   - Add component: `DemoSceneSetup`

2. Create the Player:
   - Right-click in Hierarchy > Create Empty
   - Rename to "Player"
   - Add component: `DemoPlayerController`

3. Create the UIManager:
   - Right-click in Hierarchy > Create Empty
   - Rename to "UIManager"
   - Add component: `DemoUIController`

4. Create the AsyncHandler:
   - Right-click in Hierarchy > Create Empty
   - Rename to "AsyncHandler"
   - Add component: `DemoAsyncHandler`

## Step 5: Set Up References

1. Select the DemoManager GameObject
2. In the Inspector, in the DemoSceneSetup component:
   - Drag the HealthText to the Health Text field
   - Drag the ScoreText to the Score Text field
   - Drag the GameStateText to the Game State Text field
   - Drag the EventLogText to the Event Log Text field
   - Drag the ControlsText to the Controls Text field
   - Drag the Player GameObject to the Player Controller field
   - Drag the UIManager GameObject to the UI Controller field
   - Drag the AsyncHandler GameObject to the Async Handler field

3. Select the UIManager GameObject
4. In the Inspector, in the DemoUIController component:
   - Drag the HealthText to the Health Text field
   - Drag the ScoreText to the Score Text field
   - Drag the GameStateText to the Game State Text field
   - Drag the EventLogText to the Event Log Text field

## Step 6: Test the Scene

1. Save the scene
2. Press Play to test
3. Use the keyboard controls to interact:
   - `D` to take damage
   - `C` to collect an item
   - `P` to toggle pause
   - `R` to restart the game

## Troubleshooting

If the scene doesn't work as expected:

1. **Missing Scripts**: Make sure all scripts are properly compiled. Check the console for errors.

2. **Namespace Issues**: Ensure you're using the correct namespace in your scripts:
   ```csharp
   namespace EventBus.Demo.Core.Utils.EventBus.Demo.Scripts
   ```

3. **Assembly References**: Make sure the EventBus.Demo.asmdef file references both the EventBus.Core and Unity.TextMeshPro assemblies.

4. **UI References**: Double-check that all UI elements are properly referenced in the DemoUIController.

5. **TextMeshPro Import**: If you get errors about TextMeshPro, make sure the TextMeshPro package is imported in your project via the Package Manager (Window > Package Manager > TextMeshPro).

6. **Console Errors**: Check the console for any runtime errors that might be occurring.

## Next Steps

Once your scene is working, explore the code to understand how the EventBus system works:

1. See how events are defined in `DemoEvents.cs`
2. Observe how events are published in `DemoPlayerController.cs`
3. Learn how to subscribe to events in `DemoUIController.cs`
4. Explore async event handling in `DemoAsyncHandler.cs` 