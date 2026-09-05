/*
AquariaExplorationUI.cs

Purpose:
Presentation-only controller for the editable Aquaria exploration HUD prefab.
*/

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AquariaExplorationUI : MonoBehaviour
{
    [Header("Gameplay Data")]
    [SerializeField] private ExplorationSystemInjector explorationSystemInjector;
    [SerializeField] private ExplorationPositionSourceSelector positionSourceSelector;
    [SerializeField] private ExplorationController explorationController;
    [SerializeField] private DeviceHeadingController headingController;
    [SerializeField] private CreatureProximitySystem proximitySystem;
    [SerializeField] private ExplorationEncounterFlow encounterFlow;

    [Header("Status")]
    [SerializeField] private TMP_Text explorationTitleText;
    [SerializeField] private TMP_Text locationStatusText;
    [SerializeField] private TMP_Text signalStatusText;

    [Header("Creature Info")]
    [SerializeField] private TMP_Text creatureNameText;
    [SerializeField] private TMP_Text miniCreatureNameText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text miniDistanceText;
    [SerializeField] private TMP_Text directionText;
    [SerializeField] private TMP_Text flavorText;

    [Header("Signal Strength")]
    [SerializeField] private Image signalProgressFill;
    [SerializeField] private TMP_Text signalProgressText;
    [SerializeField] private RectTransform signalIndicator;
    [SerializeField] private Image signalOuterRing;
    [SerializeField] private Image signalInnerRing;
    [SerializeField] private float farSignalAlpha = 0.32f;
    [SerializeField] private float nearSignalAlpha = 1f;
    [SerializeField] private float encounterPulseScale = 1.08f;

    [Header("Radar")]
    [SerializeField] private RectTransform radarCreatureMarker;
    [SerializeField] private RectTransform radarPlayerArrow;
    [SerializeField] private float radarRadius = 76f;

    [Header("Direction")]
    [SerializeField] private RectTransform creatureDirectionArrow;

    [Header("Icons")]
    [SerializeField] private Image creatureIcon;
    [SerializeField] private Image miniCreatureIcon;
    [SerializeField] private Sprite aquariaIcon;
    [SerializeField] private Sprite aquarioIcon;
    [SerializeField] private Sprite unknownCreatureIcon;

    [Header("Text")]
    [SerializeField] private string explorationTitle = "AQUARIA EXPLORATION";
    [SerializeField] private string defaultCreatureName = "Unknown Creature";
    [SerializeField] private string noSignalText = "Searching for signal...";
    [SerializeField] private string weakSignalText = "Move closer to investigate.";
    [SerializeField] private string strongSignalText = "Signal is getting stronger.";
    [SerializeField] private string encounterReadyText = "Encounter available.";

    [Header("Events")]
    [SerializeField] private UnityEvent onBackPressed = new();
    [SerializeField] private UnityEvent onMenuPressed = new();

    private ExplorationPositionSource activePositionSource;

    public UnityEvent OnBackPressed => onBackPressed;
    public UnityEvent OnMenuPressed => onMenuPressed;

    public void Initialize(ExplorationSystemInjector injector)
    {
        explorationSystemInjector = injector;

        if (injector == null)
        {
            return;
        }

        positionSourceSelector = injector.ExplorationPositionSourceSelector;
        explorationController = injector.ExplorationController;
        headingController = injector.DeviceHeadingController;
        proximitySystem = injector.CreatureProximitySystem;
        activePositionSource = positionSourceSelector != null
            ? positionSourceSelector.ActivePositionSource
            : injector.GPSPositionSource;
    }

    public void SetEncounterFlow(ExplorationEncounterFlow flow)
    {
        encounterFlow = flow;
    }

    public void PressBack()
    {
        onBackPressed.Invoke();
    }

    public void PressMenu()
    {
        onMenuPressed.Invoke();
    }

    private void Update()
    {
        RefreshActivePositionSource();
        Refresh();
    }

    private void RefreshActivePositionSource()
    {
        if (positionSourceSelector != null)
        {
            activePositionSource = positionSourceSelector.ActivePositionSource;
        }
    }

    private void Refresh()
    {
        CreatureExplorationTarget nearestCreature = proximitySystem != null
            ? proximitySystem.NearestCreature
            : null;

        float distance = proximitySystem != null ? proximitySystem.NearestCreatureDistance : 0f;
        float signal = GetSignalStrength();
        bool hasSignal = nearestCreature != null && proximitySystem.ProximityState != CreatureProximityState.OutOfRange;
        bool encounterReady = IsEncounterReady(nearestCreature);
        CreatureType creatureType = nearestCreature != null
            ? nearestCreature.CreatureType
            : EncounterSessionData.CurrentSignalCreature;

        SetText(explorationTitleText, explorationTitle);
        SetText(locationStatusText, GetLocationStatus());
        SetText(signalStatusText, GetSignalStatus(hasSignal, encounterReady));
        SetText(creatureNameText, encounterReady ? creatureType.ToString() : defaultCreatureName);
        SetText(miniCreatureNameText, encounterReady ? creatureType.ToString() : "???");
        SetText(distanceText, nearestCreature != null ? $"{distance:F0} m" : "-- m");
        SetText(miniDistanceText, nearestCreature != null ? $"{distance:F0} m" : "-- m");
        SetText(directionText, nearestCreature != null ? GetCardinalDirection(GetCreatureWorldBearing(nearestCreature)) : "--");
        SetText(signalProgressText, $"{Mathf.RoundToInt(signal * 100f)}%");
        SetText(flavorText, GetFlavorText(hasSignal, encounterReady));

        if (signalProgressFill != null)
        {
            signalProgressFill.fillAmount = signal;
        }

        ApplyCreatureIcon(creatureType, encounterReady);
        UpdateRadar(nearestCreature);
        UpdateDirectionArrow(nearestCreature);
        UpdateSignalIndicator(signal, encounterReady);
    }

    private float GetSignalStrength()
    {
        if (proximitySystem == null)
        {
            return 0f;
        }

        if (proximitySystem.ProximityState == CreatureProximityState.EncounterReady)
        {
            return 1f;
        }

        return Mathf.Clamp01(proximitySystem.SignalStrength);
    }

    private bool IsEncounterReady(CreatureExplorationTarget nearestCreature)
    {
        if (encounterFlow != null)
        {
            return encounterFlow.EncounterReady;
        }

        return proximitySystem != null &&
            nearestCreature != null &&
            proximitySystem.ProximityState == CreatureProximityState.EncounterReady;
    }

    private string GetLocationStatus()
    {
        if (activePositionSource == null)
        {
            return "LOCATION: WAITING";
        }

        if (!activePositionSource.IsReady)
        {
            return $"LOCATION: {activePositionSource.GPSStatus}";
        }

        if (headingController != null && headingController.CompassEnabled)
        {
            return "LOCATION: READY";
        }

        return "LOCATION: SIM";
    }

    private string GetSignalStatus(bool hasSignal, bool encounterReady)
    {
        if (encounterReady)
        {
            return "SIGNAL FOUND";
        }

        if (proximitySystem == null)
        {
            return "SIGNAL WAITING";
        }

        return hasSignal ? proximitySystem.ProximityState.ToString().ToUpperInvariant() : "SIGNAL SEARCH";
    }

    private string GetFlavorText(bool hasSignal, bool encounterReady)
    {
        if (encounterReady)
        {
            return encounterReadyText;
        }

        if (!hasSignal)
        {
            return noSignalText;
        }

        return proximitySystem.ProximityState == CreatureProximityState.StrongSignal
            ? strongSignalText
            : weakSignalText;
    }

    private void UpdateRadar(CreatureExplorationTarget nearestCreature)
    {
        float signedAngle = nearestCreature != null ? GetCreatureRelativeBearing(nearestCreature) : 0f;

        if (radarPlayerArrow != null)
        {
            radarPlayerArrow.localRotation = Quaternion.Euler(0f, 0f, -GetHeading());
        }

        if (radarCreatureMarker == null)
        {
            return;
        }

        if (nearestCreature == null)
        {
            radarCreatureMarker.anchoredPosition = Vector2.zero;
            return;
        }

        float signal = GetSignalStrength();
        float markerDistance = Mathf.Lerp(radarRadius, 16f, signal);
        float radians = signedAngle * Mathf.Deg2Rad;
        radarCreatureMarker.anchoredPosition = new Vector2(
            Mathf.Sin(radians) * markerDistance,
            Mathf.Cos(radians) * markerDistance
        );
    }

    private void UpdateDirectionArrow(CreatureExplorationTarget nearestCreature)
    {
        if (creatureDirectionArrow == null)
        {
            return;
        }

        float signedAngle = nearestCreature != null ? GetCreatureRelativeBearing(nearestCreature) : 0f;
        creatureDirectionArrow.localRotation = Quaternion.Euler(0f, 0f, -signedAngle);
    }

    private void UpdateSignalIndicator(float signal, bool encounterReady)
    {
        float alpha = Mathf.Lerp(farSignalAlpha, nearSignalAlpha, signal);
        SetImageAlpha(signalOuterRing, alpha);
        SetImageAlpha(signalInnerRing, Mathf.Lerp(0.45f, 1f, signal));

        if (signalIndicator == null)
        {
            return;
        }

        float pulse = encounterReady
            ? Mathf.Lerp(1f, encounterPulseScale, (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f)
            : Mathf.Lerp(0.92f, 1.02f, signal);

        signalIndicator.localScale = new Vector3(pulse, pulse, 1f);
    }

    private float GetCreatureRelativeBearing(CreatureExplorationTarget target)
    {
        return Mathf.DeltaAngle(GetHeading(), GetCreatureWorldBearing(target));
    }

    private float GetCreatureWorldBearing(CreatureExplorationTarget target)
    {
        Vector2 playerPosition = activePositionSource != null
            ? new Vector2(activePositionSource.EastMeters, activePositionSource.NorthMeters)
            : Vector2.zero;
        Vector3 targetPosition = target.LocalWorldPosition;
        Vector2 direction = new Vector2(targetPosition.x, targetPosition.z) - playerPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return GetHeading();
        }

        return Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
    }

    private float GetHeading()
    {
        return headingController != null ? headingController.SmoothedHeading : 0f;
    }

    private string GetCardinalDirection(float bearing)
    {
        string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int index = Mathf.RoundToInt(Mathf.Repeat(bearing, 360f) / 45f) % directions.Length;
        return directions[index];
    }

    private void ApplyCreatureIcon(CreatureType creatureType, bool revealCreature)
    {
        Sprite icon = unknownCreatureIcon;

        if (revealCreature)
        {
            icon = creatureType == CreatureType.Aquaria ? aquariaIcon : aquarioIcon;
        }

        if (creatureIcon != null)
        {
            creatureIcon.sprite = icon;
        }

        if (miniCreatureIcon != null)
        {
            miniCreatureIcon.sprite = icon;
        }
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
