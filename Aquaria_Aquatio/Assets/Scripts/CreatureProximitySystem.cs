using TMPro;
using UnityEngine;

public class CreatureProximitySystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplorationPositionSource positionSource;
    [SerializeField] private CreatureSpawnManager spawnManager;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI signalText;
    [SerializeField] private TextMeshProUGUI creatureNearbyText;
    [SerializeField] private TextMeshProUGUI encounterStatusText;

    [Header("Distance Bands")]
    [SerializeField] private float encounterDistance = 3f;
    [SerializeField] private float veryCloseDistance = 7f;
    [SerializeField] private float closeDistance = 15f;
    [SerializeField] private float mediumDistance = 30f;

    [Header("Debug Runtime")]
    [SerializeField] private CreatureExplorationTarget nearestCreature;
    [SerializeField] private float nearestCreatureDistance;
    [SerializeField] private CreatureProximityState proximityState;
    [SerializeField] private float signalStrength;
    [SerializeField] private string encounterState = "None";

    public CreatureExplorationTarget NearestCreature => nearestCreature;
    public float NearestCreatureDistance => nearestCreatureDistance;
    public CreatureProximityState ProximityState => proximityState;
    public float SignalStrength => signalStrength;
    public string EncounterState => encounterState;

    public void SetPositionSource(ExplorationPositionSource source)
    {
        positionSource = source;
    }

    private void Update()
    {
        if (positionSource == null || spawnManager == null || !positionSource.IsReady)
        {
            proximityState = CreatureProximityState.None;
            UpdateFeedbackText();
            return;
        }

        UpdateNearestCreature();
        UpdateProximityState();
        UpdateFeedbackText();
    }

    private void UpdateNearestCreature()
    {
        nearestCreature = null;
        nearestCreatureDistance = float.PositiveInfinity;

        Vector2 playerPosition = new Vector2(
            positionSource.EastMeters,
            positionSource.NorthMeters
        );

        foreach (CreatureExplorationTarget target in spawnManager.Targets)
        {
            if (target == null)
            {
                continue;
            }

            Vector3 targetPosition = target.LocalWorldPosition;
            float distance = Vector2.Distance(
                playerPosition,
                new Vector2(targetPosition.x, targetPosition.z)
            );

            if (distance < nearestCreatureDistance)
            {
                nearestCreatureDistance = distance;
                nearestCreature = target;
            }
        }
    }

    private void UpdateProximityState()
    {
        if (nearestCreature == null)
        {
            proximityState = CreatureProximityState.None;
            signalStrength = 0f;
            return;
        }

        float discoveryRadius = Mathf.Max(nearestCreature.DiscoveryRadius, veryCloseDistance);
        signalStrength = Mathf.Clamp01(1f - nearestCreatureDistance / mediumDistance);

        if (nearestCreatureDistance <= discoveryRadius)
        {
            nearestCreature.MarkDiscovered();
        }

        if (nearestCreatureDistance <= Mathf.Min(encounterDistance, nearestCreature.EncounterRadius))
        {
            proximityState = CreatureProximityState.Encounter;

            if (nearestCreature.TryStartEncounter())
            {
                encounterState = $"{nearestCreature.CreatureType} Encounter Ready";
                spawnManager.NotifyEncounterStarted(nearestCreature);
            }

            return;
        }

        if (nearestCreatureDistance <= veryCloseDistance)
        {
            proximityState = CreatureProximityState.VeryClose;
        }
        else if (nearestCreatureDistance <= closeDistance)
        {
            proximityState = CreatureProximityState.Close;
        }
        else if (nearestCreatureDistance <= mediumDistance)
        {
            proximityState = CreatureProximityState.Medium;
        }
        else
        {
            proximityState = CreatureProximityState.Far;
        }
    }

    private void UpdateFeedbackText()
    {
        if (feedbackText == null)
        {
            UpdateOptionalSceneTexts();
            return;
        }

        feedbackText.text = GetFeedbackMessage();
        UpdateOptionalSceneTexts();
    }

    private void UpdateOptionalSceneTexts()
    {
        if (signalText != null)
        {
            signalText.text = nearestCreature == null
                ? "Signal: None"
                : proximityState switch
                {
                    CreatureProximityState.Encounter => "Signal: Encounter",
                    CreatureProximityState.VeryClose => "Signal: Very Strong",
                    CreatureProximityState.Close => "Signal: Strong",
                    CreatureProximityState.Medium => "Signal: Medium",
                    CreatureProximityState.Far => "Signal: Weak",
                    _ => "Signal: None",
                };
        }

        if (creatureNearbyText != null)
        {
            creatureNearbyText.text =
                nearestCreature != null && proximityState != CreatureProximityState.Far
                    ? "Creature Nearby"
                    : "No Creature Nearby";
        }

        if (encounterStatusText != null)
        {
            encounterStatusText.text = proximityState == CreatureProximityState.Encounter
                ? $"{nearestCreature.CreatureType} Encounter Ready"
                : "No Encounter";
        }
    }

    private string GetFeedbackMessage()
    {
        if (nearestCreature == null)
        {
            return "Creature Signal: None";
        }

        return proximityState switch
        {
            CreatureProximityState.Encounter => $"{nearestCreature.CreatureType} Encounter Ready",
            CreatureProximityState.VeryClose => "Creature Signal: Very Strong",
            CreatureProximityState.Close => "Creature Signal: Strong",
            CreatureProximityState.Medium => "Creature Signal: Medium",
            CreatureProximityState.Far => "Creature Signal: Weak",
            _ => "Creature Signal: None",
        };
    }
}
