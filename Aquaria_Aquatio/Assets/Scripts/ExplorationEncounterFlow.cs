using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationEncounterFlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CreatureProximitySystem proximitySystem;

    [Header("Scene")]
    [SerializeField] private string encounterSceneName = "Encounter_01_ARSearch";

    [Header("Encounter Flow")]
    [SerializeField] private int aquarioCountToCatch = 3;

    [Header("Debug Runtime")]
    [SerializeField] private bool encounterReady;
    [SerializeField] private CreatureType selectedCreatureType;
    [SerializeField] private string encounterFlowMessage;

    public bool EncounterReady => encounterReady;
    public CreatureType SelectedCreatureType => selectedCreatureType;
    public string EncounterFlowMessage => encounterFlowMessage;
    public CreatureType CurrentSignalCreature => EncounterSessionData.CurrentSignalCreature;
    public bool AquariaAquarioUnited => EncounterSessionData.AquariaAquarioUnited;

    private void Reset()
    {
        proximitySystem = FindAnyObjectByType<CreatureProximitySystem>();
    }

    private void Awake()
    {
        ResolveOptionalReferences();
        EncounterSessionData.EnsureProgressionStarted(aquarioCountToCatch);
        selectedCreatureType = EncounterSessionData.CurrentSignalCreature;
        encounterFlowMessage = EncounterSessionData.LastEncounterMessage;
    }

    private void Update()
    {
        RefreshEncounterState();
    }

    public void Configure(
        CreatureProximitySystem proximity,
        string sceneName,
        int aquarioCatchCount
    )
    {
        proximitySystem = proximity;

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            encounterSceneName = sceneName;
        }

        aquarioCountToCatch = aquarioCatchCount;
        EncounterSessionData.EnsureProgressionStarted(aquarioCountToCatch);
        RefreshEncounterState();
    }

    private void ResolveOptionalReferences()
    {
        if (proximitySystem == null)
        {
            proximitySystem = FindAnyObjectByType<CreatureProximitySystem>();
        }
    }

    public bool TryBeginEncounter()
    {
        ResolveOptionalReferences();
        RefreshEncounterState();

        if (!encounterReady || proximitySystem == null || proximitySystem.NearestCreature == null)
        {
            return false;
        }

        CreatureType creatureType = proximitySystem.NearestCreature.CreatureType;

        if (!EncounterSessionData.CanSearchFor(creatureType))
        {
            return false;
        }

        EncounterSessionData.SetSelectedCreature(creatureType);
        SceneManager.LoadScene(encounterSceneName);
        return true;
    }

    public void RefreshEncounterState()
    {
        ResolveOptionalReferences();

        CreatureExplorationTarget nearestCreature =
            proximitySystem != null ? proximitySystem.NearestCreature : null;

        encounterReady =
            proximitySystem != null &&
            nearestCreature != null &&
            proximitySystem.ProximityState == CreatureProximityState.EncounterReady &&
            EncounterSessionData.CanSearchFor(nearestCreature.CreatureType);

        selectedCreatureType = encounterReady
            ? nearestCreature.CreatureType
            : EncounterSessionData.CurrentSignalCreature;

        encounterFlowMessage = EncounterSessionData.LastEncounterMessage;
    }
}
