/*
ExplorationSystemInjector.cs

Purpose:
Centralizes scene reference wiring for the exploration gameplay systems.

Responsibilities:
- Hold serialized references to GPS, position, movement, heading, creature, map,
  virtual controller, and debug components.
- Expose those references through read-only properties.
- Call Initialize() on systems that need scene references during Awake.

Architecture:
Scene composition helper for exploration scenes. It keeps Inspector wiring in
one place, but it is currently a required startup dependency for several systems.

Dependencies:
- GPSManager
- GPSPositionSource
- EditorKeyboardPositionSource
- ExplorationPositionSourceSelector
- EncounterEntryVirtualController
- ExplorationController
- DeviceHeadingController
- CreatureSpawnManager
- CreatureProximitySystem
- CesiumGeoreference
- CesiumGPSOriginAdapter
- ExplorationDebugPanel

Data Flow:
Serialized scene references
    -> ExplorationSystemInjector.Awake()
    -> Initialize() calls on dependent gameplay systems

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using CesiumForUnity;
using UnityEngine;

public class ExplorationSystemInjector : MonoBehaviour
{
    [Header("GPSManager")]
    [SerializeField]
    private GPSManager gpsManager;
    public GPSManager GPSManager => gpsManager;

    [Header("Position Sources")]
    [SerializeField]
    private ExplorationPositionSourceSelector explorationPositionSourceSelector;
    public ExplorationPositionSourceSelector ExplorationPositionSourceSelector => explorationPositionSourceSelector;
    [SerializeField] 
    private GPSPositionSource gpsPositionSource;
    public GPSPositionSource GPSPositionSource => gpsPositionSource;
    [SerializeField] 
    private EditorKeyboardPositionSource editorKeyboardPositionSource;
    public EditorKeyboardPositionSource EditorKeyboardPositionSource => editorKeyboardPositionSource;

    [Header("Controller")]
    [SerializeField]
    private EncounterEntryVirtualController encounterEntryVirtualController;
    public EncounterEntryVirtualController EncounterEntryVirtualController => encounterEntryVirtualController;
    [SerializeField]
    private ExplorationController explorationController;
    public ExplorationController ExplorationController => explorationController;
    [SerializeField]
    private DeviceHeadingController deviceHeadingController;
    public DeviceHeadingController DeviceHeadingController => deviceHeadingController;

    [Header("Systems")]
    [SerializeField]
    private CreatureSpawnManager creatureSpawnManager;
    public CreatureSpawnManager CreatureSpawnManager => creatureSpawnManager;
    [SerializeField] 
    private CreatureProximitySystem creatureProximitySystem;
    public CreatureProximitySystem CreatureProximitySystem => creatureProximitySystem;

    [Header("Geo-Location")]
    [SerializeField]
    private CesiumGeoreference cesiumGeoreference;
    public CesiumGeoreference CesiumGeoreference => cesiumGeoreference;
    [SerializeField]
    private CesiumGPSOriginAdapter cesiumGPSOriginAdapter;

    [Header("Debug")]
    [SerializeField]
    private ExplorationDebugPanel explorationDebugPanel;
    public ExplorationDebugPanel ExplorationDebugPanel => explorationDebugPanel;

    private void Awake()
    {
        gpsPositionSource.Initialize(this);
        explorationPositionSourceSelector.Initialize(this);
        encounterEntryVirtualController.Initialize(this);
        creatureProximitySystem.Initialize(this);
        cesiumGPSOriginAdapter.Initialize(this);
        explorationDebugPanel.Initialize(this);
    }
}
