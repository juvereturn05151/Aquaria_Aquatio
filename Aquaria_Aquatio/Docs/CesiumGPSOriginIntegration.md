# Cesium GPS Origin Integration

## Purpose

`Exploration_05_Cesium` uses the existing GPS exploration stack for player motion and encounter logic. The Cesium integration should only align the `CesiumGeoreference` origin with the player's first valid GPS fix, then leave ongoing movement to the established East/North meter displacement system.

## Existing Systems Reused

- `GPSManager` owns Unity's `Input.location` service. It requests Android fine location permission, starts `Input.location`, and publishes the latest sample through `CurrentLatitude`, `CurrentLongitude`, `CurrentHorizontalAccuracy`, `CurrentTimestamp`, and `HasValidLocation`.
- `GPSPositionSource` receives `GPSManager` through `SetGPSManager`, filters samples, establishes the GPS origin, and exposes position through the `ExplorationPositionSource` API. GPS validity for consumers is reported by `IsReady`, while status and debug data are exposed through `GPSStatus`, `LastSampleResult`, accepted/rejected sample counts, and accuracy.
- `ExplorationController` reads `ExplorationPositionSource.DisplacementMeters`, maps East/North meters to Unity X/Z, and moves `worldRoot` while keeping the player marker centered.
- `CreatureProximitySystem` and `ExplorationEncounterEntry` keep encounter detection and encounter scene entry based on the existing East/North position source and proximity state.

## Adapter

`CesiumGPSOriginAdapter` is a small one-way bridge from `GPSPositionSource` to `CesiumGeoreference`.

It waits until the assigned `GPSPositionSource` reports `IsReady`, reads `CurrentLatitude` and `CurrentLongitude`, and calls:

```csharp
cesiumGeoreference.SetOriginLongitudeLatitudeHeight(
    longitude,
    latitude,
    originHeight
);
```

After the first successful initialization, it sets `originInitialized` and never updates the Cesium origin again. This avoids re-centering the Cesium world during play. Subsequent GPS updates continue through `GPSPositionSource` and `ExplorationController`, and encounter gameplay remains unchanged.

## Inspector Setup

In `Assets/Scenes/Exploration_05_Cesium.unity`, add `CesiumGPSOriginAdapter` to a scene object such as `Systems/GPSManager` or the GameObject that holds the `CesiumGeoreference`.

Assign:

- `Gps Position Source`: the existing `GPSManager` GameObject's `GPSPositionSource` component.
- `Cesium Georeference`: the existing scene `CesiumGeoreference` component.
- `Origin Height`: the desired Cesium ellipsoid height in meters. Use `0` unless the Cesium content requires a specific ellipsoid height.

Do not add another `GPSManager`, `GPSPositionSource`, or Unity `LocationService` user.

## Android Test

1. Open `Assets/Scenes/Exploration_05_Cesium.unity`.
2. Confirm the adapter references are assigned as described above.
3. Build and run on an Android device with location/GPS enabled.
4. Accept the fine location permission prompt.
5. Wait outdoors or near a window until GPS is running and the existing debug panel shows an accepted GPS origin/sample.
6. Confirm the Unity log contains `Cesium GPS origin initialized...`.
7. Move physically and confirm the player/world movement still follows the existing East/North displacement readouts.
8. Approach an existing creature target and confirm proximity/encounter prompts behave the same as before.
