/*
CreatureExplorationTarget.cs

Purpose:
Marks a creature's exploration-world location and type for proximity checks.

Responsibilities:
- Store the creature type represented by this target.
- Optionally apply a debug East/North/height position in edit or play mode.
- Accept runtime placement from CreatureSpawnManager.
- Expose the target's local world position to proximity systems.
- Allow builder scripts to configure the target data.

Architecture:
Lightweight data component placed on creature target GameObjects or prefabs.
CreatureProximitySystem reads these targets through CreatureSpawnManager.

Dependencies:
- CreatureType
- Transform

Data Flow:
Scene or editor builder configuration
    -> CreatureExplorationTarget
    -> CreatureSpawnManager target list
    -> CreatureProximitySystem distance checks

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class CreatureExplorationTarget : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private CreatureType creatureType;

    [Header("Position")]
    [SerializeField] private bool useDebugPosition = true;
    [SerializeField] private float debugEast;
    [SerializeField] private float debugNorth = 12f;
    [SerializeField] private float height = 0.6f;

    [Header("Debug Runtime")]
    [SerializeField] private Vector2 localEastNorthMeters;

    public CreatureType CreatureType => creatureType;
    public bool UseDebugPosition => useDebugPosition;
    public float DebugEast => debugEast;
    public float DebugNorth => debugNorth;
    public Vector2 LocalEastNorthMeters => localEastNorthMeters;
    public Vector3 LocalWorldPosition => transform.localPosition;

    public void Configure(
        CreatureType type,
        bool useDebug,
        float east,
        float north,
        float targetHeight
    )
    {
        creatureType = type;
        useDebugPosition = useDebug;
        debugEast = east;
        debugNorth = north;
        height = targetHeight;
        ApplyDebugPositionIfEnabled();
    }

    public void SetRuntimePosition(float east, float north, float targetHeight)
    {
        useDebugPosition = false;
        height = targetHeight;
        transform.localPosition = new Vector3(east, height, north);
        localEastNorthMeters = new Vector2(east, north);
    }

    private void Awake()
    {
        ApplyDebugPositionIfEnabled();
    }

    private void OnValidate()
    {
        ApplyDebugPositionIfEnabled();
    }

    public void ApplyDebugPositionIfEnabled()
    {
        if (useDebugPosition)
        {
            transform.localPosition = new Vector3(debugEast, height, debugNorth);
        }

        localEastNorthMeters = new Vector2(
            transform.localPosition.x,
            transform.localPosition.z
        );
    }
}
