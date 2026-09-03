/*
CreatureProximitySystem.cs

Purpose:
Detects the nearest searchable exploration creature and determines whether an
encounter is available.

Responsibilities:
- Receive the active ExplorationPositionSource and CreatureSpawnManager.
- Compare player East/North displacement against creature target positions.
- Filter targets using EncounterSessionData progression rules when enabled.
- Compute nearest target, distance, signal strength, and proximity state.
- Notify CreatureSpawnManager when a creature enters encounter range.
- Update assigned exploration feedback text fields.

Architecture:
Exploration gameplay system that currently mixes proximity rules with simple UI
text updates. ExplorationEncounterFlow reads its encounter state to start AR.

Dependencies:
- ExplorationPositionSource
- CreatureSpawnManager
- CreatureExplorationTarget
- EncounterSessionData
- TextMeshProUGUI

Events / Data Flow:
ExplorationPositionSource displacement
    -> CreatureProximitySystem.Update()
    -> CreatureSpawnManager.SpawnTargetsNearPlayer()
    -> CreatureSpawnManager.NotifyEncounterReady()
    -> ExplorationEncounterFlow / UI / ExplorationCreatureSignalPresentation

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using TMPro;
using UnityEngine;

public class CreatureProximitySystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI signalText;
    [SerializeField] private TextMeshProUGUI creatureNearbyText;
    [SerializeField] private TextMeshProUGUI encounterStatusText;

    [Header("Encounter Flow")]
    [SerializeField] private bool useEncounterFlowFilter = true;

    [Header("Proximity Thresholds")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float strongSignalRange = 12f;
    [SerializeField] private float encounterRange = 3f;

    [Header("Debug Runtime")]
    [SerializeField] private CreatureExplorationTarget nearestCreature;
    [SerializeField] private float nearestCreatureDistance;
    [SerializeField] private CreatureProximityState proximityState = CreatureProximityState.OutOfRange;
    [SerializeField] private float signalStrength;
    [SerializeField] private string encounterState = "None";

    private CreatureExplorationTarget lastEncounterReadyTarget;
    private ExplorationPositionSource positionSource;
    private CreatureSpawnManager spawnManager;

    public float DetectionRange => detectionRange;
    public float StrongSignalRange => strongSignalRange;
    public float EncounterRange => encounterRange;
    public CreatureExplorationTarget NearestCreature => nearestCreature;
    public float NearestCreatureDistance => nearestCreatureDistance;
    public CreatureProximityState ProximityState => proximityState;
    public float SignalStrength => signalStrength;
    public string EncounterState => encounterState;

    public void Initialize(ExplorationSystemInjector explorationSystemInjector)
    {
        spawnManager = explorationSystemInjector.CreatureSpawnManager;
    }

    public void SetPositionSource(ExplorationPositionSource source)
    {
        positionSource = source;
    }

    private void Update()
    {
        if (positionSource == null || spawnManager == null || !positionSource.IsReady)
        {
            nearestCreature = null;
            nearestCreatureDistance = 0f;
            proximityState = CreatureProximityState.OutOfRange;
            signalStrength = 0f;
            encounterState = "Waiting for player position";
            UpdateFeedbackText();
            return;
        }

        Vector2 playerPosition = GetPlayerPosition();
        spawnManager.SpawnTargetsNearPlayer(playerPosition, this);
        UpdateNearestCreature(playerPosition);
        UpdateProximityState();
        UpdateFeedbackText();
    }

    private Vector2 GetPlayerPosition()
    {
        return new Vector2(
            positionSource.EastMeters,
            positionSource.NorthMeters
        );
    }

    private void UpdateNearestCreature(Vector2 playerPosition)
    {
        nearestCreature = null;
        nearestCreatureDistance = float.PositiveInfinity;

        foreach (CreatureExplorationTarget target in spawnManager.Targets)
        {
            if (target == null)
            {
                continue;
            }

            if (useEncounterFlowFilter && !EncounterSessionData.CanSearchFor(target.CreatureType))
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
            proximityState = CreatureProximityState.OutOfRange;
            signalStrength = 0f;
            encounterState = EncounterSessionData.AquariaAquarioUnited
                ? "Aquaria and Aquario United"
                : $"Search for {EncounterSessionData.CurrentSignalCreature}";
            return;
        }

        float safeDetectionRange = Mathf.Max(0.01f, detectionRange);
        float safeEncounterRange = Mathf.Clamp(encounterRange, 0f, safeDetectionRange);
        float safeStrongSignalRange = Mathf.Clamp(
            strongSignalRange,
            safeEncounterRange,
            safeDetectionRange
        );

        signalStrength = Mathf.Clamp01(1f - nearestCreatureDistance / safeDetectionRange);

        if (nearestCreatureDistance <= safeEncounterRange)
        {
            proximityState = CreatureProximityState.EncounterReady;
            encounterState = $"{nearestCreature.CreatureType} Encounter Ready";

            if (lastEncounterReadyTarget != nearestCreature)
            {
                lastEncounterReadyTarget = nearestCreature;
                spawnManager.NotifyEncounterReady(nearestCreature);
            }

            return;
        }

        encounterState = "No Encounter";
        lastEncounterReadyTarget = null;

        if (nearestCreatureDistance <= safeStrongSignalRange)
        {
            proximityState = CreatureProximityState.StrongSignal;
        }
        else if (nearestCreatureDistance <= safeDetectionRange)
        {
            proximityState = CreatureProximityState.WeakSignal;
        }
        else
        {
            proximityState = CreatureProximityState.OutOfRange;
            signalStrength = 0f;
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
                    CreatureProximityState.EncounterReady => "Signal: Encounter Ready",
                    CreatureProximityState.StrongSignal => "Signal: Strong",
                    CreatureProximityState.WeakSignal => "Signal: Weak",
                    CreatureProximityState.OutOfRange => "Signal: Out of Range",
                    _ => "Signal: None",
                };
        }

        if (creatureNearbyText != null)
        {
            creatureNearbyText.text =
                nearestCreature != null && proximityState != CreatureProximityState.OutOfRange
                    ? $"{nearestCreature.CreatureType} Signal Nearby"
                    : "No Creature Nearby";
        }

        if (encounterStatusText != null)
        {
            encounterStatusText.text = proximityState == CreatureProximityState.EncounterReady
                ? $"{nearestCreature.CreatureType} Encounter Ready"
                : "No Encounter";
        }
    }

    private string GetFeedbackMessage()
    {
        if (nearestCreature == null)
        {
            return EncounterSessionData.AquariaAquarioUnited
                ? "Aquaria and Aquario are united"
                : $"Creature Signal: Search for {EncounterSessionData.CurrentSignalCreature}";
        }

        return proximityState switch
        {
            CreatureProximityState.EncounterReady => $"{nearestCreature.CreatureType} Encounter Ready",
            CreatureProximityState.StrongSignal => $"{nearestCreature.CreatureType} Signal: Strong",
            CreatureProximityState.WeakSignal => $"{nearestCreature.CreatureType} Signal: Weak",
            CreatureProximityState.OutOfRange => "Creature Signal: Out of Range",
            _ => "Creature Signal: None",
        };
    }
}
