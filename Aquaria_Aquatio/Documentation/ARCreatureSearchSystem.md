# AR Creature Search System

## Purpose

The AR creature search system turns the encounter scene into an active search loop instead of placing the creature directly in front of the player. The player enters the AR scene, waits for AR tracking, receives the instruction to find the creature, follows a 3D directional arrow, and finds the creature only after it remains visible through the AR camera for a short configured duration.

The AR search scene is intentionally isolated from GPS exploration code. Runtime scripts in `Encounter_01_ARSearch` must not depend on `EncounterSessionData`, `CreatureExplorationTarget`, `CreaturePresentation`, `CreatureProximitySystem`, GPS sources, or exploration scene controllers.

This milestone intentionally stops at discovery. It does not implement capture, combat, rewards, inventory, networking, or GPS movement inside the AR scene.

## Scene Hierarchy

The active prototype scene is `Assets/Scenes/Encounter_01_ARSearch.unity`.

Important scene objects:

- `AR Session`
  - Existing AR Foundation session object.
  - Contains `ARSession` and `ARInputManager`.
- `XR Origin`
  - Existing AR Foundation origin object.
  - Contains `XROrigin`, `ARPlaneManager`, and `ARRaycastManager`.
  - Owns `Camera Offset`, which contains the existing `AR Camera`.
  - `Camera Offset` also has `AREditorCameraInputController` for local editor testing.
- `AR Creature Root`
  - Parent transform for the spawned creature.
  - The creature remains in world space under this root, not under the camera.
  - The spawned prefab is `Assets/Prefabs/ARLookAroundCreature.prefab`, an AR-only prefab with no exploration components.
- `Systems`
  - Contains `ARCreatureSearchController`.
  - Contains `ARCreatureSpawner`.
  - Contains `ARCreatureVisibilityDetector`.
  - Contains `ARSearchUIController`.
- `ARSearchGuidance`
  - Contains `ARDirectionArrow`.
  - Child `DirectionArrow` is the editable placeholder 3D arrow model.
- `Canvas`
  - `SearchInstruction/InstructionText`
  - `FoundPanel/FoundText`
  - `DebugPanel/DebugText`
  - `ReturnButton`

The UI and arrow are real scene objects so designers can edit anchors, position, font size, colors, scale, materials, and hierarchy in the Unity Inspector.

## Runtime Flow

1. `ExplorationEncounterEntry` loads `Encounter_01_ARSearch` after the player reaches a creature and presses the encounter prompt.
2. `ARCreatureSearchController` starts in `Initializing`.
3. The controller waits until `ARSession.state == ARSessionState.SessionTracking`.
4. When tracking is ready, `ARCreatureSpawner` chooses a horizontal spawn position away from the camera's initial forward direction.
5. The spawned creature is assigned to `ARCreatureVisibilityDetector` and `ARDirectionArrow`.
6. The state becomes `Searching`, the UI says `Find the creature`, and the arrow becomes visible.
7. Each frame, the visibility detector checks the creature using `Camera.WorldToViewportPoint`.
8. If the creature enters the camera view, the state becomes `CreatureVisible`.
9. If it remains visible for `requiredVisibleTime`, the state becomes `CreatureFound`.
10. On found, the UI changes to `Creature Found!`, the arrow hides, and `OnCreatureFound` fires.

## State Machine

`ARSearchState` is an enum with four states:

- `Initializing`: AR tracking is not ready or the creature has not spawned.
- `Searching`: The creature has spawned in AR world space and the player is looking for it.
- `CreatureVisible`: The creature is currently inside the AR camera viewport, but has not remained visible long enough.
- `CreatureFound`: The creature has remained visible for the configured duration.

The enum keeps logic away from string comparisons and gives future encounter systems a clear place to branch.

## Script Responsibilities

`ARCreatureSearchController`

- Owns the overall search state.
- Waits for AR tracking before spawning.
- Connects the spawned creature to the visibility detector and arrow.
- Updates UI state.
- Exposes `OnCreatureSpawned`, `OnCreatureVisible`, and `OnCreatureFound`.
- Keeps scene return behavior via `ReturnToExploration`.

`ARCreatureSpawner`

- Owns creature prefab instantiation and spawn math.
- Reads the AR camera position and horizontal forward direction.
- Randomly chooses an allowed left or right spawn angle.
- Optionally uses plane height when enabled, but can fall back to camera-height horizontal placement so plane detection does not block this prototype.
- Disables exploration-only presentation behavior on the spawned creature so the AR creature renders normally.

`ARCreatureVisibilityDetector`

- Owns viewport-based visibility testing.
- Uses `Camera.WorldToViewportPoint` on a target transform.
- Tracks `visibleTimer`.
- Requires visibility to persist for `requiredVisibleTime`.

`ARDirectionArrow`

- Keeps the arrow object in a convenient camera-relative position.
- Points the arrow toward the creature's world-space position.
- Ignores vertical difference when calculating heading.
- Smooths rotation with `Quaternion.Slerp`.

`ARSearchUIController`

- Owns runtime text changes and panel visibility.
- Does not create UI objects.
- Leaves layout and styling editable in scene objects.

`AREditorCameraInputController`

- Adds local editor/test locomotion to the AR scene.
- Moves the camera offset with WASD.
- Rotates with right-mouse drag.
- Uses Q/E for vertical movement and Shift for sprint.
- Runs in the Unity Editor by default and is disabled in player builds unless `enableInPlayer` is explicitly enabled.

## Spawn Math

The spawner first projects the AR camera forward vector onto the horizontal plane:

```csharp
Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
```

This removes camera pitch so looking slightly up or down does not push the spawn far above or below the player.

It then chooses an angle magnitude between `minimumSpawnAngleFromForward` and `maximumSpawnAngleFromForward`, and randomly applies either left or right sign:

```csharp
float angleMagnitude = Random.Range(minimumSpawnAngleFromForward, maximumSpawnAngleFromForward);
float angleDirection = Random.value < 0.5f ? -1f : 1f;
float resolvedSpawnAngle = angleMagnitude * angleDirection;
```

With the default range of 60 to 160 degrees, the creature cannot start inside the forward cone from -60 to +60 degrees. That makes the player rotate or physically move before seeing it.

The horizontal direction is then multiplied by a random distance between `minimumSpawnDistance` and `maximumSpawnDistance`.

## Height Placement

The default prototype uses camera height plus `creatureHeightOffset`. This keeps the search mostly horizontal and avoids blocking the feature on AR plane detection.

`useDetectedPlaneHeight` can be enabled later. When enabled, the spawner tries to use a horizontal AR plane or raycast plane hit, then adds `creatureHeightOffset`.

## Adjusting Spawn In The AR World

Open `Assets/Scenes/Encounter_01_ARSearch.unity`, select the `Systems` GameObject, then edit the `ARCreatureSpawner` component.

The main spawn controls are:

- `Minimum Spawn Distance`: The closest distance, in meters, that the creature can spawn from the AR camera after tracking starts.
- `Maximum Spawn Distance`: The farthest distance, in meters, that the creature can spawn from the AR camera.
- `Minimum Spawn Angle From Forward`: The smallest horizontal angle away from the player's starting forward direction.
- `Maximum Spawn Angle From Forward`: The largest horizontal angle away from the player's starting forward direction.
- `Creature Height Offset`: How high the creature appears relative to the camera height or detected plane height.
- `Use Detected Plane Height`: When disabled, spawning uses camera height plus `Creature Height Offset`. When enabled, the spawner tries to place relative to a detected horizontal plane.
- `Allow Fallback Placement Without Plane`: When enabled, the creature can still spawn even if no AR plane has been detected.

For the current prototype, the default distance range is:

```text
Minimum Spawn Distance = 18
Maximum Spawn Distance = 30
```

This means the creature spawns between 18 and 30 meters away from the player. Make these numbers larger if the creature is still found too quickly. Make them smaller if testing becomes tedious.

The current angle range is:

```text
Minimum Spawn Angle From Forward = 60
Maximum Spawn Angle From Forward = 160
```

The spawner randomly chooses either the left or right side, then picks an angle inside that range. In practice, this means the creature can spawn from 60 to 160 degrees to the player's right, or from 60 to 160 degrees to the player's left. It will not spawn in the central forward cone.

Useful tuning presets:

```text
Easy Search
Minimum Spawn Distance = 6
Maximum Spawn Distance = 12
Minimum Spawn Angle From Forward = 45
Maximum Spawn Angle From Forward = 130
```

```text
Current Longer Search
Minimum Spawn Distance = 18
Maximum Spawn Distance = 30
Minimum Spawn Angle From Forward = 60
Maximum Spawn Angle From Forward = 160
```

```text
Hard Search
Minimum Spawn Distance = 30
Maximum Spawn Distance = 50
Minimum Spawn Angle From Forward = 90
Maximum Spawn Angle From Forward = 170
```

If the creature appears too high, lower `Creature Height Offset`. If it appears too low, raise it. A value around `0.5` keeps the creature slightly above the camera or plane reference for quick prototype testing.

For most prototype testing, leave `Use Detected Plane Height` disabled and `Allow Fallback Placement Without Plane` enabled. That keeps spawning reliable even when AR plane detection is slow or unavailable. Enable plane height later when the encounter needs the creature to feel grounded on real surfaces.

After changing these values in the scene Inspector, save the scene. If you rebuild the AR scene from the editor menu, update the matching defaults in `EncounterEntrySceneBuilder.BuildARSearchScene` too, otherwise the builder will overwrite the scene values the next time it regenerates `Encounter_01_ARSearch`.

## Arrow Direction Math

The arrow computes the creature direction from camera to target:

```csharp
Vector3 directionToCreature = targetTransform.position - arCamera.transform.position;
directionToCreature.y = 0f;
```

Ignoring `y` makes the arrow a horizontal compass-style guide. The arrow is positioned near the camera for readability, but its rotation is based on the creature's fixed world-space location. This means the arrow guides the player without moving or re-parenting the creature.

`arrowCameraOffset` controls where the arrow appears relative to the AR camera. `arrowRotationSmoothSpeed` controls how quickly it turns toward new headings.

## Visibility Detection

The detector uses:

```csharp
Vector3 viewport = arCamera.WorldToViewportPoint(creatureVisibilityTarget.position);
```

The creature is considered in view only when:

- `viewport.z > 0`
- `viewport.x` is within the configured horizontal viewport padding.
- `viewport.y` is within the configured vertical viewport padding.

The padding avoids marking the creature visible when it barely clips the edge of the screen.

Being in view for one frame is not enough. `visibleTimer` must reach `requiredVisibleTime`, which defaults to 0.75 seconds. This prevents accidental instant discovery as the phone sweeps past the creature.

## Inspector References

`ARCreatureSearchController`

- `AR Session`
- `AR Camera`
- `Creature Spawner`
- `Visibility Detector`
- `Direction Arrow`
- `UI Controller`
- `Return Scene Name`

`ARCreatureSpawner`

- `Plane Manager`
- `Raycast Manager`
- `Creature Prefab`
- `Creature Parent`
- `Minimum Spawn Distance`
- `Maximum Spawn Distance`
- `Minimum Spawn Angle From Forward`
- `Maximum Spawn Angle From Forward`
- `Creature Height Offset`
- `Use Detected Plane Height`
- `Allow Fallback Placement Without Plane`

`ARCreatureVisibilityDetector`

- `AR Camera`
- `Creature Visibility Target`
- `Required Visible Time`
- `Viewport Padding`

`ARDirectionArrow`

- `AR Camera`
- `Arrow Transform`
- `Target Transform`
- `Arrow Camera Offset`
- `Arrow Scale`
- `Arrow Rotation Smooth Speed`

`ARSearchUIController`

- `Instruction Text`
- `Found Panel`
- `Found Text`
- `Debug Panel`
- `Debug Text`
- Runtime text strings
- `Show Debug Panel`

## Tuning

The current prototype defaults to a far spawn range of 18 to 30 meters. Increase `minimumSpawnDistance` and `maximumSpawnDistance` further to make the player walk more, or reduce them if testing takes too long.

Increase `minimumSpawnAngleFromForward` to make the creature less likely to appear near the initial view. Keep it below `maximumSpawnAngleFromForward`.

Lower `maximumSpawnAngleFromForward` if the creature feels too often directly behind the player.

Adjust `creatureHeightOffset` if the creature appears too low or too high relative to the camera.

Increase `requiredVisibleTime` if discovery feels too easy. Decrease it if players struggle to complete the found condition.

Adjust `arrowCameraOffset` if the arrow blocks the creature or appears too close to screen center.

For editor testing, use WASD to move, hold right mouse and move the cursor to rotate, hold Shift to move faster, and use Q/E to move down/up. These controls move the simulated camera offset only; Android AR movement still comes from the tracked device pose.

## Replacing The Arrow Model

Replace the child object under `ARSearchGuidance/DirectionArrow` with a new mesh or prefab instance. Keep `ARDirectionArrow.arrowTransform` assigned to the transform that should rotate toward the creature.

The script assumes the arrow's local forward axis points in the direction it should indicate. If the art asset points along another axis, rotate the mesh child inside `DirectionArrow` until the visible arrow points along local +Z.

## Replacing Creature Prefabs

Assign a different prefab to `ARCreatureSpawner.creaturePrefab`. The prefab should render normally when instantiated in world space.

Do not assign `Assets/Prefabs/AquariaCreature.prefab` here. That prefab belongs to GPS exploration and contains exploration-side components. AR search should use `Assets/Prefabs/ARLookAroundCreature.prefab` or another AR-only prefab.

If the prefab has a better visibility target than its root pivot, create a child transform such as `VisibilityTarget` near the creature's visual center and assign that after extending the spawn hookup. The current prototype uses the spawned root transform.

## Modifying UI

Edit the existing Canvas hierarchy:

- Move or restyle `SearchInstruction`.
- Change typography on `InstructionText`.
- Restyle or reposition `FoundPanel`.
- Hide, resize, or restyle `DebugPanel`.

`ARSearchUIController` changes only text content and panel active state. Layout belongs to the scene.

## Debugging

The debug panel reports:

- AR search state
- AR tracking state
- tracking readiness
- creature position
- distance to creature
- horizontal angle to creature
- whether the creature is inside the camera viewport
- visible timer
- arrow target direction
- placement state

Disable `showDebugPanel` on `ARSearchUIController` for cleaner device testing.

## Known Limitations

- Tracking readiness currently uses `ARSession.state == ARSessionState.SessionTracking`.
- Plane detection is optional and disabled by default for prototype reliability.
- Visibility detection uses viewport bounds only; it does not account for occlusion or whether another object blocks the creature.
- The current visibility target is the spawned creature root.
- The placeholder arrow is primitive geometry meant to be replaced by art.

## Future Extension Points

- Add a dedicated visibility target transform on creature prefabs.
- Add arrow fade behavior when the creature is visible but not yet found.
- Subscribe capture, interaction, animation, reward, or dialogue systems to `OnCreatureFound`.
- Add distance-based arrow feedback such as pulsing or color changes.
- Replace fallback height placement with reliable plane placement once AR plane UX is stronger.
- Move search state into a reusable encounter state machine if future encounters need multiple phases.
