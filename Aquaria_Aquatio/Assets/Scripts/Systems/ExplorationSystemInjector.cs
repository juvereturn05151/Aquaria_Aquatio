using UnityEngine;

public class ExplorationSystemInjector : MonoBehaviour
{
    [Header("GPSManager")]
    [SerializeField]
    private GPSManager gpsManager;

    [Header("Position Sources")]
    [SerializeField]
    private ExplorationPositionSourceSelector explorationPositionSourceSelector;
    [SerializeField] 
    private GPSPositionSource gpsPositionSource;
    [SerializeField] private EditorKeyboardPositionSource editorPositionSource;

    [Header("Controller")]
    [SerializeField]
    private EncounterEntryVirtualController encounterEntryVirtualController;
    [SerializeField]
    private ExplorationController explorationController;
    [SerializeField]
    private DeviceHeadingController deviceHeadingController;

    [Header("Systems")]
    [SerializeField]
    private CreatureSpawnManager creatureSpawnManager;
    [SerializeField] 
    private CreatureProximitySystem creatureProximitySystem;

    [Header("Debug")]
    [SerializeField]
    private ExplorationDebugPanel explorationDebugPanel;


}
