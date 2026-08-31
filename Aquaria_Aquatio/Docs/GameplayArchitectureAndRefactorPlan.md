# Gameplay Architecture And Refactor Plan

Review date: 2026-08-31

Scope: documentation-only review of the current Unity Android GPS + Cesium + encounter project. No gameplay code was refactored.

## Current Architecture

The current gameplay is built as a chain of concrete scene MonoBehaviours. GPS/editor position sources produce east/north displacement. Exploration consumes that displacement to move the player marker or world. Creature proximity polls that same displacement and exposes encounter readiness. Encounter entry writes the selected creature to static session state and loads the AR scene. The AR scene reads that static state, runs a small search state machine, then records progression and returns to exploration.

Simple actual-class diagram:

```text
GPSManager
-> GPSPositionSource
-> ExplorationPositionSourceSelector
-> ExplorationController
-> PlayerMarker / WorldRoot / Main Camera

GPSPositionSource
-> CesiumGPSOriginAdapter
-> CesiumGeoreference
-> Cesium World Terrain / Cesium OSM Buildings

EditorKeyboardPositionSource
-> ExplorationPositionSourceSelector
-> ExplorationController

ExplorationPositionSourceSelector
-> CreatureProximitySystem
-> CreatureSpawnManager
-> CreatureExplorationTarget

CreatureProximitySystem
-> ExplorationEncounterEntry
-> EncounterSessionData
-> Encounter_01_ARSearch

EncounterSessionData
-> ARCreatureSearchController
-> ARCreatureSpawner
-> ARCreatureVisibilityDetector
-> ARDirectionArrow
-> ARSearchUIController
-> EncounterSessionData.RegisterCreatureFound
-> Exploration_04_EncounterEntry
```

### Major Classes

| Class | Responsibility | Key dependencies | Notes |
| --- | --- | --- | --- |
| `GPSManager` | Requests Android fine location permission, starts/stops Unity `Input.location`, stores latest raw GPS sample. | `GPSPositionSource`, Unity `Input.location`, Android permissions. | Owns device GPS lifecycle. Injects itself into `GPSPositionSource` in `Awake` and `Start`. |
| `ExplorationPositionSource` | Abstract base for east/north displacement, readiness, sample counts, GPS metadata, and `PositionAccepted` event. | None. | Reusable abstraction, but the event is not currently consumed by gameplay scripts. |
| `GPSPositionSource` | Filters raw GPS, validates coordinates, establishes local GPS origin, converts lat/lon delta to east/north meters, smooths accepted displacement. | `GPSManager`. | Contains GPS validity, origin, conversion, filtering, and smoothing in one class. |
| `EditorKeyboardPositionSource` | Editor simulation source using WASD plus virtual movement input. | Unity `Input`, optional `EncounterEntryVirtualController`. | Good substitute implementation of `ExplorationPositionSource`. |
| `ExplorationPositionSourceSelector` | Chooses editor simulation in editor or GPS on device, enables one source, injects active source into exploration and proximity. | `GPSPositionSource`, `EditorKeyboardPositionSource`, `ExplorationController`, `CreatureProximitySystem`. | Scene composition helper; direct concrete dependencies are acceptable but could become brittle. |
| `ExplorationController` | Converts active displacement into scene movement. Either keeps player centered and moves `WorldRoot`, or moves `PlayerMarker` and follows it with camera. | `ExplorationPositionSource`, `WorldRoot`, `PlayerMarker`, optional `followCamera`. | Reusable movement adapter, but it owns two movement modes and camera follow. |
| `CesiumGPSOriginAdapter` | Once GPS source is ready and has a valid coordinate, sets `CesiumGeoreference` origin to GPS lon/lat/height. | `GPSPositionSource`, `CesiumGeoreference`. | Cesium-specific bridge. Tied to GPS source, not abstract position source, because it needs lat/lon. |
| `DeviceHeadingController` | Reads/simulates compass heading and rotates `PlayerVisual`. | Unity `Input.compass`, `PlayerVisual`, optional debug text. | Combines input, smoothing, visual rotation, and debug UI. |
| `CreatureSpawnManager` | Collects `CreatureExplorationTarget` children and creates missing Aquaria/Aquario runtime targets if absent. Raises encounter-ready event. | `CreatureExplorationTarget`, `Resources` materials. | Runtime target creation is useful for prototypes but mixes scene data and spawning policy. |
| `CreatureExplorationTarget` | Stores creature identity and debug east/north placement. Applies debug local position. | `CreatureType`. | Target position is read from `transform.localPosition`. |
| `CreatureProximitySystem` | Polls active player east/north against spawn manager targets, filters by encounter progression, computes nearest target, proximity state, signal strength, and several UI text fields. | `ExplorationPositionSource`, `CreatureSpawnManager`, `EncounterSessionData`, TMP UI. | High responsibility: gameplay rules, progression filtering, target scanning, event notification trigger, and UI. |
| `CreaturePresentation` | Updates exploration creature visibility, pulse, bob, and optional encounter prompt from proximity state. | `CreatureExplorationTarget`, `CreatureProximitySystem`. | Present on creature prefabs, but prefab `proximitySystem` references are null and there is no scene-level wiring found in inspected scenes. |
| `ExplorationEncounterEntry` | Shows/hides encounter prompt, initializes progression, checks encounter availability, handles button click, writes selected creature, loads AR scene. | `CreatureProximitySystem`, `EncounterSessionData`, Unity UI, `SceneManager`. | Mixes UI presentation, progression setup, readiness rules, and scene transition. |
| `EncounterSessionData` | Static cross-scene encounter/progression state. Tracks selected creature, Aquaria found, Aquario remaining count, and union state. | `CreatureType`. | Simple and effective bridge, but global static state needs reset discipline in tests and scene entry. |
| `ARCreatureSearchController` | AR encounter state machine. Waits for tracking, spawns creature, wires detector/arrow, handles visible/found states, registers progression, returns to exploration. | AR Foundation, `ARCreatureSpawner`, `ARCreatureVisibilityDetector`, `ARDirectionArrow`, `ARSearchUIController`, `AquariaUnionAnimation`, `EncounterSessionData`, `SceneManager`. | Central AR orchestrator with several responsibilities. |
| `ARCreatureSpawner` | Spawns AR creature at random distance/angle, optionally using detected plane height. | AR Foundation plane/raycast managers, creature prefab. | Random spawn math is embedded; scene uses fallback placement without plane. |
| `ARCreatureVisibilityDetector` | Checks if target stays inside camera viewport for required duration. | AR camera, target transform. | Clean, testable behavior if camera/target can be set up in tests. |
| `ARDirectionArrow` | Places arrow near AR camera and rotates it toward spawned creature. | AR camera, target transform. | View helper with `LateUpdate` polling. |
| `ARSearchUIController` | Updates AR instruction, found panel, and debug text. | TMP UI. | UI-only, reasonably scoped. |
| `AquariaUnionAnimation` | Builds and plays a runtime overlay animation when Aquaria/Aquario unite. | `Canvas`, TMP, UI Images. | Creates UI procedurally in code; useful prototype, but not designer-friendly. |
| `EncounterEntryVirtualController` | On-screen `OnGUI` editor/mobile-style controls for simulated exploration movement and heading. | `EditorKeyboardPositionSource`, `DeviceHeadingController`. | Debug/input helper, scene-specific. |
| `AREditorCameraInputController` | Editor/player camera movement and mouse look for AR testing. | Camera transform, Unity `Input`. | Debug helper for AR scene. |
| `GPSWorldMovement` | Obsolete subclass of `ExplorationController`. | `ExplorationController`. | Marked obsolete and only found in `GPS_Test.unity`. |

### Data Flow

1. `GPSManager` starts `Input.location`, then copies `Input.location.lastData` into `CurrentLatitude`, `CurrentLongitude`, accuracy, timestamp, and `HasValidLocation`.
2. `GPSPositionSource.Update` polls `GPSManager`. New timestamps are validated, filtered by accuracy/min movement/max jump, converted to east/north meters, and smoothed into the base `ExplorationPositionSource` fields.
3. `ExplorationPositionSourceSelector.Awake` selects either `EditorKeyboardPositionSource` in the editor or `GPSPositionSource` on device, enables only that source, and assigns it to `ExplorationController` and `CreatureProximitySystem`.
4. `ExplorationController.Update` reads `positionSource.DisplacementMeters` and applies it to either `WorldRoot` or `PlayerMarker`, depending on scene settings.
5. `CesiumGPSOriginAdapter.Update` polls `GPSPositionSource.IsReady`; once valid, it calls `CesiumGeoreference.SetOriginLongitudeLatitudeHeight` and then stops updating itself by setting `originInitialized`.
6. `CreatureProximitySystem.Update` polls active east/north displacement, compares it with `CreatureSpawnManager.Targets`, filters targets through `EncounterSessionData.CanSearchFor`, computes proximity state, updates UI text, and tells `CreatureSpawnManager` when an encounter first becomes ready for a target.
7. `ExplorationEncounterEntry.Update` polls `CreatureProximitySystem`, shows the prompt when `EncounterReady`, and on button click stores the selected creature in `EncounterSessionData` before loading `Encounter_01_ARSearch`.
8. `ARCreatureSearchController.Awake` reads `EncounterSessionData` to determine the selected creature. In `Update`, it waits for AR tracking, spawns the creature, checks camera visibility, registers the creature as found, and returns to exploration after a delay unless the union state is reached.

### Events And Polling

Current gameplay is primarily `Update` polling.

Events/callbacks currently present:

- `ExplorationPositionSource.PositionAccepted` is invoked by `AcceptPosition`, but no gameplay subscriptions were found.
- `CreatureSpawnManager.OnCreatureEncounterReady` is invoked by `NotifyEncounterReady`, but no subscriptions were found.
- `ExplorationEncounterEntry` uses `Button.onClick.AddListener(BeginEncounter)` and removes it in `OnDestroy`.
- `ARCreatureSearchController` exposes `UnityEvent<Transform>` for spawned, visible, and found creature events.
- `ARCreatureSpawner` exposes `OnCreatureSpawned`.

Update/LateUpdate polling currently present:

- `GPSManager.Update` polls `Input.location`.
- `GPSPositionSource.Update` polls `GPSManager`.
- `EditorKeyboardPositionSource.Update` polls keyboard/virtual input.
- `ExplorationController.Update` polls active source.
- `CesiumGPSOriginAdapter.Update` waits until origin can be initialized.
- `DeviceHeadingController.Update` polls compass/editor input.
- `CreatureProximitySystem.Update` polls position and targets.
- `CreaturePresentation.Update` polls proximity state.
- `ExplorationEncounterEntry.Update` polls proximity state.
- `ARCreatureSearchController.Update` runs the AR state machine.
- `ARCreatureVisibilityDetector.TickVisibility` is called from the AR search update.
- `ARDirectionArrow.LateUpdate` polls camera/target transforms.

### Scene-Specific Vs Reusable

Reusable systems:

- `ExplorationPositionSource`, `GPSPositionSource`, `EditorKeyboardPositionSource`
- `ExplorationController`
- `CreatureExplorationTarget`, `CreatureProximityState`, `CreatureType`
- `ARCreatureVisibilityDetector`, `ARDirectionArrow`, `ARSearchUIController`

Scene composition / scene-specific systems:

- `ExplorationPositionSourceSelector`
- `CesiumGPSOriginAdapter`
- `ExplorationEncounterEntry`
- `ARCreatureSearchController`
- `EncounterEntryVirtualController`
- `AREditorCameraInputController`
- `AquariaUnionAnimation`

Prototype/editor generation code:

- `Assets/Scripts/Editor/ExplorationPrototypeSceneBuilder.cs`
- `Assets/Scripts/Editor/ExplorationCreatureFeedbackSceneBuilder.cs`
- `Assets/Scripts/Editor/EncounterEntrySceneBuilder.cs`

Classes with too many responsibilities:

- `CreatureProximitySystem`: proximity rules, progression filtering, runtime event triggering, debug state, and UI text.
- `ExplorationEncounterEntry`: UI styling, prompt state, progression initialization, encounter eligibility, selected creature assignment, and scene loading.
- `ARCreatureSearchController`: AR orchestration, state machine, session progression, UI/debug formatting, scene loading, and union animation trigger.
- `GPSPositionSource`: source adaptation, coordinate validation, origin, conversion, filtering, smoothing, and debug state.
- `DeviceHeadingController`: compass input, editor simulation, smoothing, player visual rotation, and debug UI.

## Scene Script Map

This map was extracted from the scene YAML and script GUIDs.

| Scene | GameObject | Script | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| `Exploration_05_Cesium` | `GPSManager` | `GPSManager` | `GPSPositionSource` on same GameObject | Starts Android GPS and publishes latest raw sample. |
| `Exploration_05_Cesium` | `GPSManager` | `GPSPositionSource` | Set by `GPSManager`; selected by `ExplorationPositionSourceSelector` | Filters/smooths GPS into east/north displacement. |
| `Exploration_05_Cesium` | `GPSManager` | `EditorKeyboardPositionSource` | `EncounterEntryVirtualController` virtual input | Editor position simulation. |
| `Exploration_05_Cesium` | `GPSManager` | `ExplorationPositionSourceSelector` | `GPSPositionSource`, `EditorKeyboardPositionSource`, `ExplorationController`, `CreatureProximitySystem` | Chooses active position source. |
| `Exploration_05_Cesium` | `ExplorationController` | `ExplorationController` | `GPSManager`, `WorldRoot`, `PlayerMarker`, `Main Camera` | Moves player marker from displacement and follows with camera. `keepPlayerMarkerCentered=0`, `movePlayerMarkerFromDisplacement=1`. |
| `Exploration_05_Cesium` | `CesiumIntegration` | `CesiumGPSOriginAdapter` | `GPSManager` `GPSPositionSource`, `CesiumGeoreference` | Initializes Cesium georeference once from GPS origin. |
| `Exploration_05_Cesium` | `CesiumGeoreference` | `CesiumGeoreference`, `CesiumCameraManager` | Cesium runtime | Geospatial map origin/camera integration. |
| `Exploration_05_Cesium` | `Cesium World Terrain` | `Cesium3DTileset`, `CesiumIonRasterOverlay` | Cesium ion server asset | Terrain rendering. |
| `Exploration_05_Cesium` | `Cesium OSM Buildings` | `Cesium3DTileset` | Cesium ion server asset | Building tiles rendering. |
| `Exploration_05_Cesium` | `DynamicCamera` | `CesiumFlyToController`, `CesiumOriginShift`, `CesiumGlobeAnchor`, `CesiumCameraController` | Cesium runtime | Cesium camera/globe behavior. |
| `Exploration_05_Cesium` | `CreatureSpawnManager` | `CreatureSpawnManager` | `CreatureTargets` | Collects/creates exploration targets. |
| `Exploration_05_Cesium` | `CreatureProximitySystem` | `CreatureProximitySystem` | `GPSManager`, `CreatureSpawnManager`, feedback/signal/nearby/status TMP fields | Detects nearest valid target and encounter readiness. |
| `Exploration_05_Cesium` | `EncounterEntryPrompt` | `ExplorationEncounterEntry` | `CreatureProximitySystem`, prompt `CanvasGroup`, button, prompt text | Shows start AR encounter prompt and loads AR scene. |
| `Exploration_05_Cesium` | `DeviceHeadingController` | `DeviceHeadingController` | `PlayerVisual`; `debugText` null | Rotates player visual by compass/editor heading. |
| `Exploration_05_Cesium` | `Systems` | `EncounterEntryVirtualController` | `GPSManager` editor source, `DeviceHeadingController` | OnGUI virtual movement/turning input. |
| `Exploration_05_Cesium` | `DebugManager` | `ExplorationDebugPanel` | Position selector/source, exploration, heading, proximity, debug TMP fields | Runtime debug panel. |
| `Exploration_04_EncounterEntry` | `GPSManager` | `GPSManager`, `GPSPositionSource`, `EditorKeyboardPositionSource`, `ExplorationPositionSourceSelector` | Same source chain as Cesium scene | GPS/editor displacement source. |
| `Exploration_04_EncounterEntry` | `ExplorationController` | `ExplorationController` | `GPSManager`, `WorldRoot`, `PlayerMarker` | Keeps player centered and moves `WorldRoot`. `keepPlayerMarkerCentered=1`; no `followCamera`. |
| `Exploration_04_EncounterEntry` | `CreatureSpawnManager` | `CreatureSpawnManager` | `CreatureTargets` | Collects/creates targets. |
| `Exploration_04_EncounterEntry` | `CreatureProximitySystem` | `CreatureProximitySystem` | `GPSManager`, `CreatureSpawnManager`, feedback/signal/nearby/status TMP fields | Detects nearest valid target and encounter readiness. |
| `Exploration_04_EncounterEntry` | `EncounterEntryPrompt` | `ExplorationEncounterEntry` | `CreatureProximitySystem`, prompt `CanvasGroup`, button, prompt text | Start AR encounter prompt. |
| `Exploration_04_EncounterEntry` | `DeviceHeadingController` | `DeviceHeadingController` | `PlayerVisual`; `debugText` null | Rotates player visual. |
| `Exploration_04_EncounterEntry` | `Systems` | `EncounterEntryVirtualController` | `GPSManager` editor source, `DeviceHeadingController` | OnGUI virtual controls. |
| `Exploration_04_EncounterEntry` | `DebugManager` | `ExplorationDebugPanel` | Same debug fields as Cesium scene | Runtime debug panel. |
| `Exploration_03_CreatureFeedback` | `GPSManager` | `GPSManager`, `GPSPositionSource`, `EditorKeyboardPositionSource`, `ExplorationPositionSourceSelector` | Position chain | GPS/editor displacement source. |
| `Exploration_03_CreatureFeedback` | `ExplorationController` | `ExplorationController` | `GPSManager`, `WorldRoot`, `PlayerMarker` | Exploration movement without encounter entry. |
| `Exploration_03_CreatureFeedback` | `CreatureSpawnManager` | `CreatureSpawnManager` | `CreatureTargets` | Target collection/creation. |
| `Exploration_03_CreatureFeedback` | `CreatureProximitySystem` | `CreatureProximitySystem` | Position source, spawn manager, UI fields | Signal/proximity feedback. |
| `Exploration_03_CreatureFeedback` | `DeviceHeadingController`, `DebugManager` | `DeviceHeadingController`, `ExplorationDebugPanel` | Player visual/debug fields | Heading/debug. |
| `Exploration_02_CreatureDetection` | Same as `Exploration_03_CreatureFeedback` | Same GPS, exploration, heading, spawn, proximity, debug scripts | Same core references | Earlier detection scene, no encounter prompt and no Cesium integration. |
| `Encounter_01_ARSearch` | `Systems` | `ARCreatureSearchController` | `AR Session`, `AR Camera`, `ARCreatureSpawner`, `ARCreatureVisibilityDetector`, `ARDirectionArrow`, `ARSearchUIController`; `spawnedCreature` runtime null | AR search state machine and scene return. |
| `Encounter_01_ARSearch` | `Systems` | `ARCreatureSpawner` | `XR Origin` plane/raycast managers, `ARLookAroundCreature` prefab, `AR Creature Root` | Spawns selected creature at random offset. |
| `Encounter_01_ARSearch` | `Systems` | `ARCreatureVisibilityDetector` | `AR Camera`; target is runtime null until spawned | Checks viewport visibility duration. |
| `Encounter_01_ARSearch` | `Systems` | `ARSearchUIController` | Instruction/found/debug TMP fields | AR instruction/debug/found UI. |
| `Encounter_01_ARSearch` | `ARSearchGuidance` | `ARDirectionArrow` | `AR Camera`, `DirectionArrow`; target runtime null until spawned | Points toward spawned creature. |
| `Encounter_01_ARSearch` | `Camera Offset` | `AREditorCameraInputController` | `Camera Offset`, `AR Camera` | Editor AR navigation. |
| `Encounter_01_ARSearch` | `XR Origin` | `ARRaycastManager`, `ARPlaneManager` | AR Foundation | Plane/raycast support. |
| `Encounter_01_ARSearch` | `AR Session` | `ARSession`, `ARInputManager` | AR Foundation | AR session lifecycle. |

### Scene Differences And Inspector Notes

- `Exploration_05_Cesium` is the only enabled scene in `ProjectSettings/EditorBuildSettings.asset`; `Encounter_01_ARSearch` is present but disabled in build settings. Since `ExplorationEncounterEntry` loads by scene name, Android builds need the AR scene included/enabled or otherwise included through build configuration.
- `ARCreatureSearchController.returnSceneName` is `Exploration_04_EncounterEntry`, not `Exploration_05_Cesium`. From the current enabled scene, the player can enter AR from Cesium but will return to the previous non-Cesium exploration scene.
- `Exploration_05_Cesium` adds Cesium world terrain, OSM buildings, `CesiumGeoreference`, `CesiumGPSOriginAdapter`, and Cesium camera components. These are absent from `Exploration_04_EncounterEntry`, `03`, and `02`.
- `Exploration_05_Cesium` sets `ExplorationController` to `movePlayerMarkerFromDisplacement=1` and references `Main Camera`. `Exploration_04_EncounterEntry` keeps the marker centered and moves `WorldRoot`.
- `Exploration_04_EncounterEntry` adds `ExplorationEncounterEntry` and `EncounterEntryVirtualController` compared with `Exploration_03_CreatureFeedback`.
- `Exploration_03_CreatureFeedback` and `Exploration_02_CreatureDetection` have the same core GPS/exploration/proximity/debug scripts but no encounter prompt and no Cesium adapter.
- Serialized runtime fields intentionally appear null: `CreatureProximitySystem.nearestCreature`, `ARCreatureSearchController.spawnedCreature`, `ARCreatureVisibilityDetector.creatureVisibilityTarget`, and `ARDirectionArrow.targetTransform`.
- Potential missing optional references: `DeviceHeadingController.debugText` is null in exploration scenes. This is safe because `ExplorationDebugPanel` handles debug UI elsewhere.
- Potentially fragile nulls: `ExplorationEncounterEntry.promptBackground` and `promptRectTransform` are null in `Exploration_04_EncounterEntry` and `Exploration_05_Cesium`. `Awake` resolves them from `GetComponent<Image>()` and `GetComponent<RectTransform>()`; if the script GameObject lacks `Image`, background styling is silently skipped. The prompt still works through `CanvasGroup`, `Button`, and text references.
- Creature prefabs `AquariaCreature` and `AquarioCreature` include `CreatureExplorationTarget` and `CreaturePresentation`, but `CreaturePresentation.proximitySystem` is null in the prefab. I did not find scene-level wiring of that field in the inspected scenes, so prefab presentation may not react unless some editor builder or runtime code assigns it elsewhere.
- `GPSWorldMovement` is obsolete and only found in `GPS_Test.unity`.
- No duplicate major gameplay components were found in the inspected production scenes. `ExplorationPrototype.unity` contains two `CreatureExplorationTarget` components, which may be intentional prototype content.

## Full Gameplay Flow

1. Scene starts.
   - `GPSManager.Awake` injects itself into `GPSPositionSource`.
   - `ExplorationPositionSourceSelector.Awake` selects editor source in editor or GPS source on device, enables only the active source, and injects it into `ExplorationController` and `CreatureProximitySystem`.
   - `CreatureSpawnManager.Awake` collects target children and creates missing Aquaria/Aquario debug targets if configured.
   - `ExplorationEncounterEntry.Awake` initializes encounter progression with `EncounterSessionData.EnsureProgressionStarted`, wires button click, styles prompt, and hides it.

2. GPS initialization.
   - `GPSManager.Start` requests Android fine location permission, waits for authorization, checks `Input.location.isEnabledByUser`, starts `Input.location`, waits up to 20 seconds for initialization, and exits early on timeout/failure.

3. Valid GPS sample.
   - `GPSManager.Update` copies `Input.location.lastData` while status is `Running`, then sets `HasValidLocation=true`.
   - `GPSPositionSource.Update` notices a new timestamp and processes the sample.

4. Local origin.
   - First valid GPS coordinate in `GPSPositionSource.ProcessGpsSample` becomes `originLatitude`/`originLongitude`.
   - It accepts `(east=0,north=0)` and marks the source ready.

5. Cesium initialization.
   - `CesiumGPSOriginAdapter.Update` waits for `GPSPositionSource.IsReady`.
   - It reads `CurrentLatitude`/`CurrentLongitude`, validates them, and calls `CesiumGeoreference.SetOriginLongitudeLatitudeHeight(longitude, latitude, originHeight)`.
   - It records `originInitialized=true`, so initialization happens once.

6. East/North movement.
   - Later `GPSPositionSource` samples are converted with an approximate meters-per-degree calculation using origin latitude cosine for east distance.
   - Samples are rejected by invalid coordinate, poor accuracy, too-small movement, or too-large jump.
   - Accepted targets are smoothed toward `smoothedDisplacementMeters`, and the base position fields are updated.
   - In editor, `EditorKeyboardPositionSource.Update` directly increments east/north from WASD or virtual controls.

7. Player movement.
   - `ExplorationController.Update` reads the active source displacement.
   - In `Exploration_05_Cesium`, it moves `PlayerMarker` from displacement and updates `Main Camera` using `followCameraOffset`.
   - In `Exploration_04_EncounterEntry`, it keeps `PlayerMarker` centered and moves `WorldRoot` opposite the displacement.

8. Creature proximity.
   - `CreatureProximitySystem.Update` reads active east/north from the position source.
   - It scans `CreatureSpawnManager.Targets`, ignores targets not allowed by `EncounterSessionData.CanSearchFor`, computes nearest target distance, sets weak/strong/ready state, updates signal values and UI texts.

9. Encounter available.
   - If nearest allowed target is within `encounterRange` of 3 meters, `CreatureProximitySystem` sets `EncounterReady` and calls `spawnManager.NotifyEncounterReady` once per ready target.
   - `ExplorationEncounterEntry.Update` separately polls the same proximity state and checks `EncounterSessionData.CanSearchFor`.

10. Encounter button.
   - When ready, `ExplorationEncounterEntry` shows the prompt and enables the button.
   - Button click calls `BeginEncounter`, writes `EncounterSessionData.SetSelectedCreature`, and loads `Encounter_01_ARSearch`.

11. AR scene.
   - `ARCreatureSearchController.Awake` chooses `EncounterSessionData.SelectedCreatureType` if set, otherwise `CurrentSignalCreature`.
   - It assigns the type to `ARCreatureSpawner` and updates `ARSearchUIController`.
   - In `Update`, once `ARSession.state == SessionTracking`, it asks `ARCreatureSpawner` to spawn the prefab.
   - It wires the spawned transform into `ARCreatureVisibilityDetector` and `ARDirectionArrow`.
   - `ARCreatureVisibilityDetector.TickVisibility` returns true after the creature remains inside the padded viewport for 0.75 seconds.
   - On found, `ARCreatureSearchController` calls `EncounterSessionData.RegisterCreatureFound`, hides the arrow, shows found UI, optionally plays union animation, and returns to `Exploration_04_EncounterEntry` after 2.5 seconds unless united.

## Software Engineering Review

| Class/File | Problem | Severity | Recommended fix |
| --- | --- | --- | --- |
| `Assets/Scripts/ARCreatureSearchController.cs` | Returns to hard-coded `Exploration_04_EncounterEntry` while current enabled/advanced scene is `Exploration_05_Cesium`. This can break the GPS + Cesium loop after AR. | High | Make return scene configurable per entry or store origin scene in `EncounterSessionData` when starting encounter. First minimal fix: set scene reference/value consistently in `Exploration_05_Cesium` flow. |
| `ProjectSettings/EditorBuildSettings.asset` | Only `Exploration_05_Cesium` is enabled; the loaded AR scene is disabled. Name-based `LoadScene` requires included build scenes. | High | Enable `Encounter_01_ARSearch` and the intended return scene in build settings, or verify addressable/other inclusion if used. |
| `Assets/Scripts/CreatureProximitySystem.cs` | Single Responsibility violation: proximity scan, encounter progression filter, signal strength, encounter-ready notification, and UI text updates are all in one class. | Medium | Extract UI updates first into a small presenter or let existing UI scripts read a proximity model. Keep proximity calculation unchanged during first pass. |
| `Assets/Scripts/ExplorationEncounterEntry.cs` | Mixes prompt styling, UI state, progression initialization, eligibility checks, selected creature assignment, and scene transition. | Medium | Split scene transition/progression from prompt presentation after tests exist. Minimal first step: introduce a small method/data property for current encounter candidate. |
| `Assets/Scripts/GPSPositionSource.cs` and `Assets/Scripts/CesiumGPSOriginAdapter.cs` | Coordinate validation is duplicated; GPS conversion/filtering/smoothing are embedded in a MonoBehaviour, making edit-mode tests harder. | Medium | Extract static coordinate utilities and east/north conversion after locking behavior with tests. |
| `Assets/Scripts/CreatureSpawnManager.cs` | Runtime creation of missing flow targets hides scene data issues and mixes prototype fallback with normal scene target management. | Medium | Keep fallback for now but make it explicit per scene/profile, then move target definitions to serialized scene/prefab data. |
| `Assets/Scripts/CreaturePresentation.cs` | Prefabs have `proximitySystem=null`; no scene wiring found. If presentations are expected to react, they will idle. | Medium | Decide whether `CreaturePresentation` is still active. If yes, wire it from `CreatureSpawnManager` or a scene installer; if no, remove from active prefabs later. |
| `Assets/Scripts/ARCreatureSearchController.cs` | Centralized AR orchestration includes spawning, visibility state, progression updates, debug string composition, union trigger, and scene loading. | Medium | Keep the state machine but delegate debug text and return scene selection later. |
| `Assets/Scripts/ExplorationPositionSourceSelector.cs` | Hidden scene composition happens in `Awake`; order relative to `GPSManager.Awake` is implicit. Currently works because both direct references are serialized and injection is repeated in `GPSManager.Start`. | Low | Add lightweight validation/logging for missing required references; later move composition into a named scene setup component if needed. |
| `Assets/Scripts/DeviceHeadingController.cs` | Compass/editor input, smoothing, player visual rotation, and debug text are coupled. | Low | Leave until heading behavior changes. Later extract only if tests or alternate heading consumers appear. |
| `Assets/Scripts/ExplorationController.cs` | Two movement modes and camera follow are in one class. Scene flags determine behavior. | Low | Preserve both modes for now. Later separate camera follow if Cesium exploration becomes the only target flow. |
| `Assets/Scripts/EncounterSessionData.cs` | Static mutable state bridges scenes. Simple, but tests and scene restarts can inherit state unless explicitly reset. | Medium | Add reset calls in test setup and consider a tiny serializable session object only after behavior is covered. |
| `Assets/Scripts/AquariaUnionAnimation.cs` | Runtime procedural UI makes layout/styling harder to inspect and localize. | Low | Convert to prefab/presenter later if union sequence becomes production UI. |
| `Assets/Scripts/GPSWorldMovement.cs` | Obsolete compatibility class remains in project and is used by `GPS_Test.unity`. | Low | Keep until `GPS_Test` is retired or updated; do not delete during GPS refactor. |
| `Assets/Scripts/Editor/*.cs` | Scene builder scripts contain many `FindAnyObjectByType`/`GameObject.Find` calls and generated-scene assumptions. | Low | Treat as editor tooling/prototype generation. Clean only after runtime architecture stabilizes. |

Other observations:

- There are several `FindAnyObjectByType` calls in `Reset` methods, which is normal editor convenience. Runtime `FindAnyObjectByType` exists in `ARCreatureSearchController.Awake`, `AquariaUnionAnimation.Awake`, and `EncounterEntryVirtualController.Awake`; these are hidden dependencies worth reducing gradually.
- Magic numbers are visible in GPS thresholds, proximity ranges, AR spawn distances, visibility time, return delay, and UI dimensions. Most are serialized, which is acceptable; repeated coordinate constants should become shared utility constants when conversion tests are added.
- The current architecture is not overengineered. The main problem is not too many systems; it is a few central classes doing too much while scenes encode different behavior through flags.

## Target Architecture

Keep the architecture simple and Unity-friendly:

```text
Device GPS / Editor Simulation
-> ExplorationPositionSource
-> ExplorationMovementController
-> Player / World / Camera

GPSPositionSource
-> GeoCoordinateUtility
-> CesiumOriginInitializer
-> CesiumGeoreference

CreatureTargetRegistry
-> CreatureProximitySystem
-> CreatureSignalPresenter
-> EncounterEntryPresenter
-> EncounterSceneLoader

EncounterSessionData
-> ARSearchController
-> ARSpawner / ARVisibilityDetector / ARDirectionArrow / ARSearchUI
```

Suggested boundaries:

- Position source layer: raw GPS/editor input, validity, origin, east/north displacement.
- Movement layer: applies displacement to player/world/camera.
- Map layer: initializes Cesium from GPS origin only.
- Encounter rules layer: nearest creature and readiness, no UI text and no scene load.
- UI/presentation layer: debug panel, signal labels, encounter prompt.
- Scene flow layer: selected creature and scene transition.
- AR layer: state machine, spawn, visibility, arrow, UI, return flow.

Avoid:

- DI frameworks.
- Service locators.
- A broad event bus.
- ECS.
- Rewriting the GPS/Cesium/player/encounter flow before tests exist.

## Staged Refactor Plan

| Stage | Files affected | Goal | Risk | How to test |
| --- | --- | --- | --- | --- |
| 1. Document and build-scene alignment | `ProjectSettings/EditorBuildSettings.asset`, scenes only if approved | Ensure `Encounter_01_ARSearch` and intended return exploration scene are included and return path matches Cesium flow. | Low to Medium | Build/run in editor and Android; start from `Exploration_05_Cesium`, enter AR, return to expected scene. |
| 2. Add edit-mode tests around pure behavior seams | New test files; no runtime behavior changes | Cover coordinate validity/conversion expectations, encounter radius, session progression. | Low | Unity Test Runner edit-mode tests. |
| 3. Extract coordinate utility | `GPSPositionSource.cs`, `CesiumGPSOriginAdapter.cs`, new utility file | Remove duplicated coordinate validation and make east/north conversion testable. | Low | Existing GPS simulation and edit-mode conversion tests. |
| 4. Split proximity UI from proximity rules | `CreatureProximitySystem.cs`, new presenter or reuse debug/UI script, affected scenes | Keep proximity calculation in one class; move TMP text updates out. | Medium | Editor simulation: weak, strong, ready, out-of-range text and state still match. |
| 5. Clarify encounter entry flow | `ExplorationEncounterEntry.cs`, `EncounterSessionData.cs`, scenes | Separate prompt display from selected-creature/session/scene loading. Store return scene or entry scene intentionally. | Medium | Enter encounter for Aquaria, return, then search Aquario progression. |
| 6. Make target data explicit | `CreatureSpawnManager.cs`, creature prefabs/scenes | Reduce hidden runtime creation by serializing required targets or introducing a small target config. | Medium | Load exploration scenes with and without target children; verify no duplicate or missing targets. |
| 7. Trim runtime object finds | `ARCreatureSearchController.cs`, `AquariaUnionAnimation.cs`, `EncounterEntryVirtualController.cs`, scenes | Replace runtime `FindAnyObjectByType` fallback with validated serialized references where practical. | Low | Scene validation plus manual AR search in editor/device. |
| 8. AR controller cleanup | `ARCreatureSearchController.cs`, possible debug presenter | Keep state machine intact while moving debug string/return-scene policy out. | Medium | AR tracking, spawn, visibility found, found UI, union behavior, return delay. |

Recommended first refactor: fix scene-flow consistency before changing code structure. Specifically, decide whether `Encounter_01_ARSearch` should return to `Exploration_05_Cesium` and ensure both scenes are included in build settings. This is small, reversible, and protects the working end-to-end loop.

## Testing Strategy

### Editor Tests

- GPS validity:
  - Invalid lat/lon outside valid ranges are rejected.
  - `(0,0)` is rejected.
  - Poor horizontal accuracy is rejected when over threshold.
- East/North conversion:
  - North movement from latitude delta is positive north meters.
  - East movement from longitude delta is positive east meters.
  - East movement scales by cosine of origin latitude.
- Player movement:
  - With `movePlayerMarkerFromDisplacement=true`, `PlayerMarker` moves toward `(east,north)` and camera follows if assigned.
  - With centered mode, `PlayerMarker` remains centered and `WorldRoot` moves opposite displacement.
- Cesium initializes once:
  - Once `CesiumGPSOriginAdapter` sees valid GPS ready state, it sets origin and never sets it again for later samples.
- Encounter radius:
  - Distances above detection range are out of range.
  - Distances inside detection/strong/encounter thresholds produce weak/strong/ready.
- Encounter availability:
  - Before Aquaria is found, Aquario is filtered out.
  - After Aquaria is registered found, Aquario becomes searchable.
  - Once united, no creatures are searchable.
- Scene transitions:
  - `ExplorationEncounterEntry.BeginEncounter` only loads when ready and allowed.
  - Selected creature is written before loading AR scene.
  - AR found flow registers the creature and returns to the intended exploration scene.

Implementation note: many tests become easier after extracting coordinate/proximity/session utilities. Until then, use lightweight play-mode tests with test GameObjects and serialized references.

### Android Device Tests

- Permission flow:
  - First install asks for fine location permission.
  - Denied permission fails gracefully.
  - Location disabled exits without stuck UI.
- GPS acquisition:
  - Valid location sets origin once.
  - Accuracy threshold behaves acceptably outdoors.
  - Walking more than `minimumMovementDistance` updates displacement.
  - Large jumps are rejected.
- Cesium:
  - Cesium georeference initializes after first valid GPS.
  - Terrain/buildings render near the device origin.
  - Origin does not reset mid-session.
- Movement:
  - Player marker or camera movement matches real walking direction closely enough for encounter approach.
- Encounter:
  - Creature signal appears at correct approximate distance bands.
  - Encounter prompt appears inside 3 meters.
  - Button loads AR scene on device build.
- AR:
  - AR session reaches tracking.
  - Creature spawns at expected distance/angle.
  - Direction arrow points toward spawned creature.
  - Holding creature in view for 0.75 seconds registers found.
  - Return scene is correct after found.
  - Aquaria -> Aquario -> union progression works across multiple encounters.

## Final Summary

### Current Architecture

- `GPSManager` owns Unity GPS startup and raw latest sample state.
- `GPSPositionSource` converts valid GPS samples into smoothed east/north displacement.
- `EditorKeyboardPositionSource` provides the same displacement contract for editor simulation.
- `ExplorationPositionSourceSelector` chooses the active source and injects it into movement and proximity.
- `ExplorationController` moves either `WorldRoot` or `PlayerMarker`/camera from displacement.
- `CesiumGPSOriginAdapter` initializes `CesiumGeoreference` once from valid GPS lat/lon.
- `CreatureSpawnManager`, `CreatureExplorationTarget`, and `CreatureProximitySystem` provide signal and encounter readiness.
- `ExplorationEncounterEntry` shows the encounter button, writes `EncounterSessionData`, and loads AR.
- `ARCreatureSearchController` orchestrates AR spawn, arrow, visibility detection, found state, progression, and return.

### Scene Assembly Map

- `Exploration_05_Cesium`: active build scene; contains GPS/editor source stack, Cesium terrain/buildings/georeference/origin adapter, player-marker movement mode, creature proximity, encounter prompt, debug panel, heading, and virtual controls.
- `Exploration_04_EncounterEntry`: previous exploration scene; same GPS/proximity/encounter flow but no Cesium, and movement keeps player centered while moving `WorldRoot`.
- `Exploration_03_CreatureFeedback` and `Exploration_02_CreatureDetection`: earlier exploration scenes; same GPS/proximity/debug backbone without encounter prompt or Cesium.
- `Encounter_01_ARSearch`: AR scene; contains AR session/origin, search controller, spawner, visibility detector, direction arrow, UI controller, editor camera controls, and AR creature root.

### Top 5 Problems

1. AR return scene currently points to `Exploration_04_EncounterEntry`, not the Cesium scene.
2. `Encounter_01_ARSearch` is disabled in build settings even though exploration loads it by name.
3. `CreatureProximitySystem` mixes gameplay rules, progression filtering, event trigger, and UI text.
4. `ExplorationEncounterEntry` mixes prompt UI, progression setup, eligibility, selected creature state, and scene loading.
5. GPS validation/conversion logic is duplicated or embedded in MonoBehaviours, making tests harder.

### Recommended First Refactor

First small change: align scene flow and build settings. Decide the intended return target after AR, include `Encounter_01_ARSearch` plus the intended return exploration scene in build settings, and verify the full GPS -> Cesium -> movement -> encounter -> AR -> return loop before restructuring classes.

### Do Not Change Yet

- Do not change GPS permission/startup behavior.
- Do not change GPS filtering thresholds or smoothing.
- Do not change east/north displacement signs until tests lock them.
- Do not change Cesium georeference initialization timing.
- Do not change exploration movement mode behavior in `Exploration_05_Cesium`.
- Do not change encounter radius/progression rules.
- Do not change AR spawn/visibility/found flow.

