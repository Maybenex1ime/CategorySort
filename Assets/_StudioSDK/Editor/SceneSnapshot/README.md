# Scene Snapshot Tool

Editor-only tool that "freezes" the live state of the active scene during Play Mode
into a real `.unity` asset on disk that you can re-open and edit in Edit Mode.

## Location

`Assets/_StudioSDK/Editor/SceneSnapshot/` — Editor asmdef (`LogosSDK.Editor.SceneSnapshot`),
runtime test helpers under `Tests/Runtime/`.

## Usage

### Window
`Tools/Logos/Scene Snapshot` opens the snapshot window. Configure output path,
toggle options & post-processors, hit **Snapshot Now** while in Play Mode.

### One-shot menu
`Tools/Logos/Take Snapshot` — snapshots the active scene to
`Assets/_Snapshots/<SceneName>_<timestamp>.unity` with default options.
Pings the new asset on success. No dialog. Disabled outside Play Mode.

### API
```csharp
using LogosGameLab.Editor.SceneSnapshot;

var result = SceneSnapshotter.SnapshotActiveScene(
    "Assets/_Snapshots/MyScene.unity",
    SnapshotOptions.Default);

if (result.Success) {
    Debug.Log($"Saved {result.GameObjectCount} GOs to {result.SavedPath}");
}
```

## How it works (Unity 6 specific)

In Unity 6, `EditorSceneManager.NewScene` and `EditorSceneManager.SaveScene` are
**blocked during Play Mode**. To work around this the tool uses a two-phase flow:

**Phase 1 — during Play Mode (when you click Take Snapshot):**
1. A staging scene is created via `SceneManager.CreateScene` (runtime API,
   allowed in play mode).
2. Each filtered root is `Object.Instantiate`-d and parented under a single
   `SnapshotRoot` GameObject in the staging scene. Originals stay untouched
   in the live scene.
3. Post-processors run on the cloned hierarchy.
4. `SnapshotRoot` is saved as a **temporary prefab** via `PrefabUtility.SaveAsPrefabAsset`
   (this *does* work in play mode), at `<scenePath>.snapshot.prefab`.
5. The staging scene is unloaded.
6. A pending entry `(prefabPath, scenePath)` is recorded in `SessionState`.

**Phase 2 — when you exit Play Mode (automatic):**
1. `PendingSnapshotMaterializer` (`[InitializeOnLoad]`) listens for
   `PlayModeStateChange.EnteredEditMode`.
2. For each pending entry: a new empty scene is created additively, the prefab
   is instantiated and unpacked completely, the `SnapshotRoot` container is
   flattened (its children become real scene roots), and the scene is saved
   to the requested `.unity` path.
3. The temp prefab is deleted; the new scene asset is pinged in the Project
   window.

The user-facing contract is unchanged: you ask for a `.unity`, you get a
`.unity`. There is just a small delay until you exit Play Mode.

## Post-processors

Each implements `ISnapshotPostProcessor`. All run by default; toggle in the window.

| Processor | What it does |
|-----------|--------------|
| `MissingScriptProcessor` | Strips MonoBehaviours with missing scripts (so the saved scene loads clean). |
| `DOTweenKillProcessor` | Kills tweens whose target is anything in the cloned hierarchy — prevents ghost tweens persisting in the runtime tween manager. |
| `ReflexClearProcessor` | Nulls fields of type `Reflex.Container` / `Reflex.Scope` and any field marked `[Inject]`. The container is runtime-only. |
| `R3ClearProcessor` | Nulls `IDisposable`, `Subject<T>`, `ReactiveProperty<T>`, `DisposableBag`, `CompositeDisposable` fields. Subscriptions don't survive serialization. |
| `AddressablesClearProcessor` | Resets `AsyncOperationHandle` fields. Handles are runtime-only and a serialized handle would point at a freed asset on next load. |

The Reflex/R3/DOTween/Addressables processors use **reflection** to find the
relevant types — the asmdef does not hard-reference these libraries, so the tool
keeps working if you swap them out.

You can extend with your own processor: implement `ISnapshotPostProcessor`, pass
a custom list to `SnapshotActiveScene(path, options, processors)`.

## Options

```csharp
public class SnapshotOptions {
    public bool includeDontDestroyOnLoad;     // default false
    public bool stripMissingScripts;          // default true
    public bool clearReflexContainerRefs;     // default true
    public bool clearR3References;            // default true
    public bool killDOTweenTweens;            // default true
    public bool clearAddressablesHandles;     // default true
    public Predicate<GameObject> rootFilter;  // optional per-root filter
    public HideFlags excludedHideFlags;       // default DontSave | HideAndDontSave
}
```

## Caveats

- **Prefab links are preserved as-is.** If a prefab instance is overridden at
  runtime (e.g. spawned via Addressables), the saved scene keeps the override
  pattern but you may see prefab-modification clutter in the Inspector.
- **Cross-scene references are not resolved.** Components holding references to
  GameObjects in other loaded scenes will log a warning; the reference will be
  null when you re-open the snapshot.
- **Reflex containers are NOT re-bound.** When you re-open the snapshot in Edit
  Mode (or play it), you must run your normal `ProjectScope` / `SceneScope`
  bootstrap. Snapshots don't try to re-bind DI graphs.
- **Generated meshes / textures** (`Mesh`/`Texture2D` created at runtime via
  `new`) are NOT serialized — Unity can't persist runtime-only assets without an
  explicit `AssetDatabase.CreateAsset` step. The MeshFilter/Renderer reference
  becomes null in the saved scene. Materials that reference project assets are
  fine.
- **Particle systems** and other components with internal runtime state will
  reset on re-open — they are restored to their initial serialized state.

## Acceptance test scene

Generate the test scene by running:
**`Tools/Logos/Scene Snapshot Tests/Create Test Scene`**

This writes `Assets/_StudioSDK/Editor/SceneSnapshot/Tests/SnapshotTestScene.unity`
containing a single `Spawner` GameObject with `SnapshotTestSpawner` (spawns 50
randomly-positioned cubes + a DOTween rotation tween on the first cube).

To verify the acceptance criteria:

1. Open `SnapshotTestScene`, press Play.
2. Run `Tools/Logos/Take Snapshot` — you'll see a `.snapshot.prefab` appear.
3. **Exit Play Mode** — the materializer fires automatically and you'll see
   the final `.unity` scene appear in `Assets/_Snapshots/`, with the temp
   prefab deleted.
4. Double-click the generated `_Snapshots/<...>.unity`.
5. Confirm: 50 cubes at the same positions, no console errors, no ghost tween.

## Anti-patterns / known limits

- Do not call `SnapshotActiveScene` from a hot Update loop — it dirties the
  editor scene list and runs `EditorSceneManager.SaveScene` synchronously.
- Do not include `DontDestroyOnLoad` if your project keeps long-lived audio
  sources or ads SDK GameObjects there — the snapshot will pull them in.
