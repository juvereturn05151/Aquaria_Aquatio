/*
ExplorationDebugPanel.cs

Purpose:
Displays live exploration diagnostics for GPS/simulation, movement, heading,
and creature proximity.

Responsibilities:
- Receive debug data sources from ExplorationSystemInjector.
- Toggle the debug panel root based on the showDebug flag.
- Build a combined multiline debug readout.
- Update individual TextMeshPro scene fields for GPS, displacement, heading,
  nearest creature, signal strength, and encounter state.

Architecture:
Runtime debug UI component for exploration scenes. It reads existing systems
through polling and does not drive gameplay state.

Dependencies:
- ExplorationSystemInjector
- ExplorationPositionSourceSelector
- ExplorationPositionSource
- ExplorationController
- DeviceHeadingController
- CreatureProximitySystem
- TextMeshProUGUI

Data Flow:
Exploration systems expose public state
    -> ExplorationDebugPanel.Update()
    -> Debug panel and scene text fields

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using TMPro;
using UnityEngine;

public class ExplorationDebugPanel : MonoBehaviour
{
    public bool ShowDebug => showDebug;

    [SerializeField] 
    private bool showDebug = true;
    [SerializeField] 
    private GameObject debugPanelRoot;
    [SerializeField] 
    private TextMeshProUGUI debugText;
    [SerializeField] 
    private TextMeshProUGUI gpsSimulationStatusText;
    [SerializeField] 
    private TextMeshProUGUI currentDisplacementText;
    [SerializeField] 
    private TextMeshProUGUI eastDisplacementText;
    [SerializeField] 
    private TextMeshProUGUI northDisplacementText;
    [SerializeField] 
    private TextMeshProUGUI headingText;
    [SerializeField] 
    private TextMeshProUGUI nearestCreatureText;
    [SerializeField] 
    private TextMeshProUGUI nearestCreatureDistanceText;
    [SerializeField] 
    private TextMeshProUGUI proximityStateText;
    [SerializeField] 
    private TextMeshProUGUI signalStrengthText;
    [SerializeField] 
    private TextMeshProUGUI encounterStateText;

    private ExplorationPositionSourceSelector positionSourceSelector;
    private ExplorationPositionSource positionSource;
    private ExplorationController explorationController;
    private DeviceHeadingController headingController;
    private CreatureProximitySystem proximitySystem;

    public void Initialize(ExplorationSystemInjector explorationSystemInjector) 
    {
        positionSourceSelector = explorationSystemInjector.ExplorationPositionSourceSelector;
        positionSource = explorationSystemInjector.GPSPositionSource;
        explorationController = explorationSystemInjector.ExplorationController;
        headingController = explorationSystemInjector.DeviceHeadingController;
        proximitySystem = explorationSystemInjector.CreatureProximitySystem;
    }

    private void Update()
    {
        if (debugPanelRoot != null && debugPanelRoot.activeSelf != showDebug)
        {
            debugPanelRoot.SetActive(showDebug);
        }

        if (!showDebug || debugText == null)
        {
            UpdateSceneFields();
            return;
        }

        debugText.text = BuildDebugText();
        UpdateSceneFields();
    }

    private string BuildDebugText()
    {
        ExplorationPositionSource activePositionSource =
            positionSourceSelector != null &&
            positionSourceSelector.ActivePositionSource != null
                ? positionSourceSelector.ActivePositionSource
                : positionSource;

        string gpsStatus = activePositionSource != null
            ? activePositionSource.GPSStatus.ToString()
            : "No position source";

        string nearestCreature = proximitySystem != null &&
            proximitySystem.NearestCreature != null
            ? proximitySystem.NearestCreature.CreatureType.ToString()
            : "None";

        return
            "[GPS]\n" +
            $"Status: {gpsStatus}\n" +
            $"Current: {FormatLatLon(activePositionSource?.CurrentLatitude ?? 0.0, activePositionSource?.CurrentLongitude ?? 0.0)}\n" +
            $"Origin: {FormatLatLon(activePositionSource?.OriginLatitude ?? 0.0, activePositionSource?.OriginLongitude ?? 0.0)}\n" +
            $"Accuracy: {(activePositionSource != null ? activePositionSource.HorizontalAccuracy : 0f):F1} m\n" +
            $"Samples: {(activePositionSource != null ? activePositionSource.AcceptedSamples : 0)}/{(activePositionSource != null ? activePositionSource.RejectedSamples : 0)} {activePositionSource?.LastSampleResult}\n\n" +
            "[Movement]\n" +
            $"East/North: {(activePositionSource != null ? activePositionSource.EastMeters : 0f):F2}, {(activePositionSource != null ? activePositionSource.NorthMeters : 0f):F2} m\n" +
            $"Total: {(activePositionSource != null ? activePositionSource.TotalDistanceMeters : 0f):F2} m\n" +
            $"World Target: {(explorationController != null ? explorationController.WorldRootTargetPosition : Vector3.zero)}\n" +
            $"World Current: {(explorationController != null ? explorationController.WorldRootCurrentPosition : Vector3.zero)}\n\n" +
            "[Compass]\n" +
            $"Raw: {(headingController != null ? headingController.RawHeading : 0f):F1}\n" +
            $"Smoothed: {(headingController != null ? headingController.SmoothedHeading : 0f):F1}\n" +
            $"State: {(headingController != null ? headingController.HeadingState : "None")}\n\n" +
            "[Exploration]\n" +
            $"Nearest: {nearestCreature}\n" +
            $"Distance: {(proximitySystem != null ? proximitySystem.NearestCreatureDistance : 0f):F1} m\n" +
            $"State: {(proximitySystem != null ? proximitySystem.ProximityState : CreatureProximityState.OutOfRange)}\n" +
            $"Signal: {(proximitySystem != null ? proximitySystem.SignalStrength : 0f):F2}\n" +
            $"Encounter: {(proximitySystem != null ? proximitySystem.EncounterState : "None")}";
    }

    private string FormatLatLon(double latitude, double longitude)
    {
        return $"{latitude:F6}, {longitude:F6}";
    }

    private void UpdateSceneFields()
    {
        ExplorationPositionSource activePositionSource =
            positionSourceSelector != null &&
            positionSourceSelector.ActivePositionSource != null
                ? positionSourceSelector.ActivePositionSource
                : positionSource;

        SetText(
            gpsSimulationStatusText,
            activePositionSource != null
                ? $"GPS/simulation status: {activePositionSource.GPSStatus} ({activePositionSource.LastSampleResult})"
                : "GPS/simulation status: No position source"
        );

        Vector3 displacement = activePositionSource != null
            ? activePositionSource.DisplacementMeters
            : Vector3.zero;

        SetText(currentDisplacementText, $"Current displacement: {displacement}");
        SetText(
            eastDisplacementText,
            $"East displacement: {(activePositionSource != null ? activePositionSource.EastMeters : 0f):F2} m"
        );
        SetText(
            northDisplacementText,
            $"North displacement: {(activePositionSource != null ? activePositionSource.NorthMeters : 0f):F2} m"
        );
        SetText(
            headingText,
            $"Heading: {(headingController != null ? headingController.SmoothedHeading : 0f):F1} ({(headingController != null ? headingController.HeadingState : "None")})"
        );
        SetText(
            nearestCreatureText,
            $"Nearest creature: {(proximitySystem != null && proximitySystem.NearestCreature != null ? proximitySystem.NearestCreature.CreatureType.ToString() : "None")}"
        );
        SetText(
            nearestCreatureDistanceText,
            $"Distance to nearest creature: {(proximitySystem != null ? proximitySystem.NearestCreatureDistance : 0f):F1} m"
        );
        SetText(
            proximityStateText,
            $"Proximity state: {(proximitySystem != null ? proximitySystem.ProximityState : CreatureProximityState.OutOfRange)}"
        );
        SetText(
            signalStrengthText,
            $"Signal strength: {(proximitySystem != null ? proximitySystem.SignalStrength : 0f):F2}"
        );
        SetText(
            encounterStateText,
            $"Encounter state: {(proximitySystem != null ? proximitySystem.EncounterState : "None")}"
        );
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
