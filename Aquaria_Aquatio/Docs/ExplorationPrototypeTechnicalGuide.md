# Exploration Prototype Technical Guide

This document explains how the current exploration prototype works, how the Unity scene is intended to be assembled, how movement and creature proximity flow through the system, and where to tune the experience when testing outdoors or in AR.

## High-Level Goal

The prototype creates a Pokemon GO-style exploration loop:

1. Get player movement from either GPS or editor keyboard simulation.
2. Convert movement into local east/north displacement in meters.
3. Keep the visible player marker centered.
4. Move `WorldRoot` opposite the player displacement.
5. Check distance from the player displacement to placed creature targets.
6. Update designer-editable UI text with debug, signal, and encounter state.

The important design choice is that the player marker does not travel through the scene. Instead, the world moves around the centered player.

```text
GPS or WASD
  -> ExplorationPositionSource
  -> ExplorationPositionSourceSelector
  -> ExplorationController
  -> WorldRoot moves opposite displacement
  -> CreatureProximitySystem checks targets
  -> Canvas UI updates
```

## Scene Structure

The setup scene is intended to be generated as:

```text
ExplorationPrototype_Setup
├── Main Camera
├── Directional Light
├── Systems
│   ├── GPSManager
│   ├── ExplorationController
│   ├── DeviceHeadingController
│   ├── CreatureSpawnManager
│   ├── CreatureProximitySystem
│   └── DebugManager
├── PlayerMarker
│   └── PlayerVisual
│       └── ForwardNose
├── WorldRoot
│   ├── Ground
│   ├── TestRoad
│   ├── TestRoadCrossing
│   ├── LandmarkA
│   ├── LandmarkB
│   ├── LandmarkC
│   ├── LandmarkD
│   └── CreatureTargets
│       ├── AquariaTarget
│       └── AquarioTarget
├── Canvas
│   ├── ExplorationFeedback
│   ├── DebugPanel
│   └── EncounterStatus
└── EventSystem
```

`WorldRoot` contains only exploration environment objects. `PlayerMarker`, `Canvas`, `Systems`, `EventSystem`, and `Main Camera` stay outside `WorldRoot`.

## Core Runtime Classes

### ExplorationPositionSource

File: `Assets/Scripts/ExplorationPositionSource.cs`

This is the base class for anything that can provide player displacement. It stores:

- `eastMeters`
- `northMeters`
- `totalDistanceMeters`
- `isReady`
- accepted/rejected sample counts
- the latest sample status string

The rest of the exploration system talks to this base class instead of caring whether movement came from GPS or keyboard simulation.

Important methods:

- `AcceptPosition(float east, float north, string result)` updates the position and increments the accepted sample count.
- `RejectPosition(string reason)` increments the rejected sample count and records why the sample was rejected.
- `UpdatePosition(float east, float north, string result)` updates the exposed displacement without counting it as a new GPS sample. GPS smoothing uses this so frame-by-frame smoothing does not inflate accepted GPS sample counts.

### GPSManager

File: `Assets/Scripts/GPSManager.cs`

`GPSManager` talks to Unity's mobile location service:

- Requests Android fine location permission.
- Starts `Input.location`.
- Waits for location initialization.
- Reads `Input.location.lastData` when GPS is running.
- Stores current latitude, longitude, horizontal accuracy, and timestamp.

It does not move the world directly. It feeds `GPSPositionSource`.

Because this code uses `Input.location`, the project must use Unity's legacy `UnityEngine.Input` API for Android builds.

### GPSPositionSource

File: `Assets/Scripts/GPSPositionSource.cs`

`GPSPositionSource` converts GPS latitude/longitude into local displacement in meters.

It does four jobs:

1. Establish an origin coordinate on the first valid GPS sample.
2. Convert later coordinates into east/north meter displacement from that origin.
3. Reject noisy or unreasonable GPS samples.
4. Smooth accepted GPS target displacement before exposing it to the movement system.

Filtering fields:

- `maximumHorizontalAccuracy`: rejects GPS samples worse than this accuracy. Current default: `20`.
- `minimumMovementDistance`: ignores movement smaller than this distance from the last accepted GPS point. Current default: `2.5`.
- `maximumAcceptedJumpDistance`: rejects very large GPS jumps. Current default: `100`.

Smoothing fields:

- `gpsSmoothingSpeed`: controls how quickly smoothed position catches up to accepted GPS target displacement. Current default: `2`.
- `useAccuracyWeightedSmoothing`: when enabled, poor GPS accuracy slows smoothing.
- `poorAccuracySmoothingMultiplier`: multiplier applied when accuracy is near the maximum acceptable accuracy. Current default: `0.35`.

The smoothing flow is:

```text
Raw GPS sample
  -> valid coordinate check
  -> accuracy/distance/jump filters
  -> targetDisplacementMeters
  -> smoothedDisplacementMeters
  -> UpdatePosition()
```

The smoothing equation is:

```csharp
float smoothingAmount = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
smoothedDisplacementMeters = Vector2.Lerp(
    smoothedDisplacementMeters,
    targetDisplacementMeters,
    smoothingAmount
);
```

Lower `gpsSmoothingSpeed` feels steadier but laggier. Higher values feel more responsive but can reveal more GPS jitter.

### EditorKeyboardPositionSource

File: `Assets/Scripts/EditorKeyboardPositionSource.cs`

This is the editor-only movement simulator. It updates east/north displacement using keyboard input:

- `W`: north
- `S`: south
- `A`: west
- `D`: east

It reads keyboard state through legacy `Input.GetKey`, matching the Android-first project input setting used by GPS and compass.

The important point is that keyboard simulation writes to the same displacement fields as GPS. This means the editor simulator feeds the same exploration pipeline instead of becoming a separate movement implementation.

### ExplorationPositionSourceSelector

File: `Assets/Scripts/ExplorationPositionSourceSelector.cs`

This selects the active movement provider.

In the Unity Editor:

```text
EditorKeyboardPositionSource
```

On device:

```text
GPSPositionSource
```

After selecting the active source, it assigns that source to:

- `ExplorationController`
- `CreatureProximitySystem`

This is the handoff that keeps GPS and simulation using the same downstream movement and proximity logic.

### ExplorationController

File: `Assets/Scripts/ExplorationController.cs`

`ExplorationController` turns player displacement into visible world movement.

It reads:

```text
positionSource.DisplacementMeters
```

Then it moves `WorldRoot` in the opposite direction:

```text
Player displacement: +10m north
WorldRoot movement:  -10m north
```

This keeps the player visually centered while the road, landmarks, and creature targets move relative to the player.

Important fields:

- `positionSource`: selected GPS or keyboard movement source.
- `worldRoot`: parent transform for exploration environment objects.
- `playerMarker`: centered visible player marker.
- `movementScale`: multiplier for displacement.
- `smoothingSpeed`: world movement interpolation speed. Current default: `3`.
- `keepPlayerMarkerCentered`: keeps `PlayerMarker` locked near `(0, y, 0)`.

### DeviceHeadingController

File: `Assets/Scripts/DeviceHeadingController.cs`

This controls player facing direction.

In the Unity Editor:

- `Q`: rotate heading left
- `E`: rotate heading right

On device, it can read compass heading from:

```csharp
Input.compass
```

It rotates the assigned `playerVisual` transform rather than the whole `PlayerMarker`. This lets the visual face a heading while the marker stays centered.

Important fields:

- `playerVisual`: the transform that rotates.
- `enableCompassOnStart`: enables `Input.compass` on device.
- `preferTrueHeading`: uses true heading when available.
- `headingSmoothingSpeed`: smooths heading rotation.
- `editorSimulationEnabled`: enables `Q/E` heading simulation in the Editor.
- `simulationTurnSpeed`: heading turn speed in degrees per second.

### CreatureSpawnManager

File: `Assets/Scripts/CreatureSpawnManager.cs`

Despite the name, this setup uses `CreatureSpawnManager` mostly as a creature target registry.

It stores:

- `targetsRoot`: the transform containing placed creature targets.
- `targets`: the collected list of `CreatureExplorationTarget` components.

On awake, it can collect all targets under `targetsRoot`.

When a creature encounter starts, it logs the encounter and fires:

```csharp
OnCreatureEncounterStarted
```

### CreatureExplorationTarget

File: `Assets/Scripts/CreatureExplorationTarget.cs`

This is the data component for each placed creature target.

It stores:

- `creatureType`: `Aquaria` or `Aquario`.
- `discoveryRadius`: distance where the creature is considered discovered.
- `encounterRadius`: distance where the encounter is ready.
- `discovered`: runtime discovered state.
- `encounterStarted`: runtime encounter state.

The setup target defaults are:

- Discovery radius: `30`
- Encounter radius: `3`

### CreatureProximitySystem

File: `Assets/Scripts/CreatureProximitySystem.cs`

This checks the active player displacement against all collected creature targets.

It calculates:

- nearest creature
- distance to nearest creature
- proximity state
- signal strength
- encounter state

The player position is taken from the active `ExplorationPositionSource`:

```text
playerPosition = (eastMeters, northMeters)
```

Each creature target is checked using its local `x/z` position under `CreatureTargets`.

Proximity bands:

- `Encounter`: within encounter distance.
- `VeryClose`: within very close distance.
- `Close`: within close distance.
- `Medium`: within medium distance.
- `Far`: outside medium distance.
- `None`: no available target or no ready position source.

When the nearest creature enters encounter range, the system calls:

```csharp
nearestCreature.TryStartEncounter()
spawnManager.NotifyEncounterStarted(nearestCreature)
```

It also updates scene-created UI text references:

- feedback text
- signal text
- creature nearby text
- encounter status text

### ExplorationDebugPanel

File: `Assets/Scripts/ExplorationDebugPanel.cs`

This is the runtime debug readout.

It can update one consolidated `debugText` block and also individual designer-editable TextMeshPro fields for:

- GPS/simulation status
- current displacement
- east displacement
- north displacement
- heading
- nearest creature
- distance to nearest creature
- proximity state
- signal strength
- encounter state

The individual fields make it easier for a designer to rearrange and style the UI without changing gameplay scripts.

## UI Architecture

The UI is scene-based.

Scripts do not create the runtime UI hierarchy. They only update assigned `TextMeshProUGUI` references.

The intended Canvas groups are:

### ExplorationFeedback

Contains player-facing signal text:

- `FeedbackText`
- `SignalText`
- `CreatureNearbyText`

Updated by:

- `CreatureProximitySystem`

### DebugPanel

Contains detailed test/debug fields:

- `DebugText`
- `GPSSimulationStatus`
- `CurrentDisplacement`
- `EastDisplacement`
- `NorthDisplacement`
- `Heading`
- `NearestCreature`
- `DistanceToNearestCreature`
- `ProximityState`
- `SignalStrength`
- `EncounterState`

Updated by:

- `ExplorationDebugPanel`

### EncounterStatus

Contains encounter state text:

- `EncounterStatusText`

Updated by:

- `CreatureProximitySystem`

## Input Backend Notes

The project uses Unity's legacy Input Manager path.

The project setting is:

```yaml
activeInputHandler: 0
```

That means `Input Manager (Old)`.

This matters because Android GPS and compass code uses `Input.location` and `Input.compass`. Unity warns that `Both` input handling is not supported on Android, so the Android-first choice is the legacy Input Manager only.

## Testing Flow

### Editor Test

1. Open the setup scene.
2. Press Play.
3. Click the Game view so it has keyboard focus.
4. Use `WASD` to simulate movement.
5. Use `Q/E` to simulate heading.
6. Watch east/north displacement in the debug panel.
7. Confirm `WorldRoot` moves while `PlayerMarker` stays centered.
8. Move toward `AquariaTarget` or `AquarioTarget`.
9. Confirm signal and encounter UI updates.

### Android Outdoor Test

1. Build to an ARCore-capable Android phone.
2. Grant location permission.
3. Test outdoors with clear sky when possible.
4. Watch horizontal accuracy in the debug panel.
5. Walk slowly in a straight line.
6. Observe whether movement feels too laggy or too twitchy.
7. Tune smoothing and filtering values.

Avoid judging GPS quality indoors or very close to buildings. Indoor GPS and wall-reflected GPS signals are often too noisy for meter-level movement.

## Tuning Guide

### Movement Is Too Jumpy

Try:

- Lower `gpsSmoothingSpeed` from `2` to `1` or `1.5`.
- Raise `minimumMovementDistance` from `2.5` to `3` or `4`.
- Lower `maximumHorizontalAccuracy` from `20` to `15`.
- Lower `ExplorationController.smoothingSpeed` from `3` to `2`.

Tradeoff: movement becomes smoother but slower to respond.

### Movement Is Too Laggy

Try:

- Raise `gpsSmoothingSpeed` from `2` to `3` or `4`.
- Lower `minimumMovementDistance` from `2.5` to `1.5` or `2`.
- Raise `ExplorationController.smoothingSpeed` from `3` to `4` or `5`.

Tradeoff: movement responds faster but can show more GPS noise.

### GPS Rarely Moves

Check:

- Android location permission was granted.
- Android location accuracy is enabled.
- `maximumHorizontalAccuracy` is not too strict.
- Horizontal accuracy in the debug panel is below the configured threshold.
- You are outdoors with open sky.

### Encounter Does Not Trigger

Check:

- `CreatureSpawnManager.targetsRoot` points to `CreatureTargets`.
- Creature targets have `CreatureExplorationTarget`.
- `CreatureSpawnManager` collected targets.
- `CreatureProximitySystem.positionSource` is assigned.
- `CreatureProximitySystem.spawnManager` is assigned.
- Target `encounterRadius` is large enough for testing.

## Improvement Roadmap

### 1. Add Better GPS Filtering

Current smoothing is exponential smoothing. It is simple and useful, but not as strong as a full location filter.

Next improvements:

- Accuracy-aware minimum movement distance.
- Speed gate to reject vehicle-like movement.
- Direction persistence before accepting small drift.
- Kalman filter for position and velocity.

### 2. Blend AR Tracking With GPS

For Pokemon GO-style AR, GPS should usually provide broad outdoor position while AR tracking provides smooth local device movement.

Recommended flow:

```text
GPS = coarse global outdoor position
AR camera pose = smooth local motion
final exploration displacement = filtered GPS + local AR offset
```

This helps around small spaces because AR camera tracking is much smoother than GPS over short distances.

### 3. Add Recenter/Reset Origin UX

Add a button or debug command to reset the GPS origin at the player's current location.

This is useful when:

- GPS starts with a bad origin.
- The player enters the test area and wants to recalibrate.
- AR tracking and GPS drift disagree too much.

### 4. Improve Creature Spawning

The current setup uses manually placed test targets.

Future options:

- Spawn targets around the current GPS origin.
- Use weighted random placement.
- Prevent spawns too close to the player.
- Snap targets to known map/path data.
- Persist discovered creatures.

### 5. Use Map or VPS Data

Pokemon GO-style smoothness often depends on more than GPS:

- road/path snapping
- map constraints
- visual positioning
- server-side location data
- sensor fusion

For a polished outdoor game, GPS alone is usually not enough.

## Tutorials and References

### Unity AR Foundation

- Unity AR Foundation overview: https://docs.unity.cn/Packages/com.unity.xr.arfoundation%402.1/manual/index.html
- Unity AR Foundation scene setup concepts: https://docs.unity.cn/Packages/com.unity.xr.arfoundation%402.2/manual/index.html
- Google ARCore Unity AR Foundation codelab: https://codelabs.developers.google.com/arcore-unity-ar-foundation

These are useful for understanding AR sessions, world tracking, AR camera setup, plane detection, raycasts, and how ARFoundation turns device tracking into Unity transforms.

### Unity Input

- Unity legacy Input documentation: https://docs.unity.cn/430/Documentation/Manual/Input.html

This is the input path currently used for Android GPS, compass, editor keyboard simulation, and the default UI event module.

### Location-Based AR and GPS

- Unity AR+GPS Location documentation: https://docs.unity-ar-gps-location.com/
- AR+GPS routes and navigation docs: https://docs.unity-ar-gps-location.com/routes/
- Niantic Location Drift Mitigation: https://www.nianticspatial.com/docs/nsdk/3.17.0/how-to/vps/location_manager/index.html
- Pokemon GO GPS troubleshooting: https://niantic.helpshift.com/hc/en/6-pokemon-go/faq/2520-gps-troubleshooting-guide/

These are directly relevant to GPS limitations, drift, outdoor AR, and techniques like temporal fusion and smoothing location updates.

### GPS Smoothing and Filtering

- Expo activity tracker GPS filtering article: https://expo.dev/blog/how-to-build-a-resilient-activity-tracker-with-expo
- Mobile location Kalman filtering paper: https://pmc.ncbi.nlm.nih.gov/articles/PMC3274025/

These are not Unity-specific, but the concepts map well to `GPSPositionSource`: accuracy gates, speed gates, drift rejection, and Kalman-style filtering.

## Current Known Limitations

- GPS movement will not feel smooth indoors.
- Compass heading can be noisy near electronics or magnetic interference.
- Current smoothing reduces jitter but adds lag.
- Creature targets are manually placed in local scene coordinates.
- The prototype does not yet blend AR camera pose with GPS displacement.
- There is no recenter button yet.
- There is no map/path snapping yet.

## Recommended Next Technical Step

The highest-impact next feature is an AR/GPS blended position source:

```text
GPSPositionSource
  + AR camera local movement offset
  -> SmoothedExplorationPositionSource
  -> ExplorationController
  -> CreatureProximitySystem
```

That would let outdoor GPS place the player broadly while ARFoundation handles smooth local walking movement during an AR session.
