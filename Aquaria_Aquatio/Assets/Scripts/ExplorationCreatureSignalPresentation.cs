/*
ExplorationCreatureSignalPresentation.cs

Purpose:
Controls only the visible signal for GPS/map exploration creature targets.

Responsibilities:
- Read proximity state from CreatureProximitySystem for this exploration target.
- Show, hide, and pulse the designer-editable SignalVisual hierarchy.
- Keep encounter prompt visibility in sync with encounter readiness when assigned.

Intentionally not responsible for:
- Encounter/AR creature visuals.
- Creature body rendering.
- Look-at-player behavior.
- GPS, spawning, scene transitions, or encounter progression.

Intended use:
Attach to Exploration creature prefabs only, such as AquariaCreature_Exploration
and AquarioCreature_Exploration.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class ExplorationCreatureSignalPresentation : MonoBehaviour
{
    [Header("State Source")]
    [SerializeField] private CreatureExplorationTarget target;
    [SerializeField] private CreatureProximitySystem proximitySystem;

    [Header("Signal References")]
    [SerializeField] private Transform signalVisualRoot;
    [SerializeField] private Transform pulseRoot;
    [SerializeField] private GameObject signalEffectRoot;
    [SerializeField] private CanvasGroup encounterPrompt;
    [SerializeField] private Renderer[] signalRenderers;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float weakSignalVisibility = 1f;

    [Header("Pulse")]
    [SerializeField] private float minimumPulseScale = 0.68f;
    [SerializeField] private float maximumPulseScale = 3.93f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float weakSignalPulseIntensity = 0.2f;
    [SerializeField] private float encounterPulseBoost = 0.85f;

    private Vector3 pulseBaseScale = Vector3.one;
    private float currentVisibility;

    public void SetProximitySystem(CreatureProximitySystem system)
    {
        proximitySystem = system;
    }

    private void Awake()
    {
        ResolveReferences();
        currentVisibility = 0f;
        ApplySignalVisibility(0f);
    }

    private void Update()
    {
        if (proximitySystem == null)
        {
            ApplySignalVisibility(0f);
            UpdateEncounterPrompt(CreatureProximityState.OutOfRange);
            return;
        }

        CreatureProximityState state = GetPresentationState(out float signalStrength);
        float targetVisibility = GetTargetVisibility(state);
        float fadeSpeed = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;

        currentVisibility = Mathf.MoveTowards(currentVisibility, targetVisibility, fadeSpeed);

        ApplySignalVisibility(currentVisibility);
        UpdatePulse(state, signalStrength);
        UpdateEncounterPrompt(state);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = GetComponent<CreatureExplorationTarget>();
        }

        if (signalVisualRoot == null)
        {
            signalVisualRoot = transform.Find("SignalVisual");
        }

        if (pulseRoot == null && signalVisualRoot != null)
        {
            pulseRoot = signalVisualRoot.Find("SignalRing");
        }

        if (signalEffectRoot == null && pulseRoot != null)
        {
            signalEffectRoot = pulseRoot.gameObject;
        }

        if (signalRenderers == null || signalRenderers.Length == 0)
        {
            signalRenderers = signalVisualRoot != null
                ? signalVisualRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
        }

        pulseBaseScale = pulseRoot != null ? pulseRoot.localScale : Vector3.one;
    }

    private CreatureProximityState GetPresentationState(out float signalStrength)
    {
        signalStrength = 0f;

        if (
            proximitySystem == null ||
            target == null ||
            proximitySystem.NearestCreature != target
        )
        {
            return CreatureProximityState.OutOfRange;
        }

        signalStrength = proximitySystem.SignalStrength;
        return proximitySystem.ProximityState;
    }

    private float GetTargetVisibility(CreatureProximityState state)
    {
        return state switch
        {
            CreatureProximityState.EncounterReady => 1f,
            CreatureProximityState.StrongSignal => 1f,
            CreatureProximityState.WeakSignal => weakSignalVisibility,
            _ => 0f,
        };
    }

    private void ApplySignalVisibility(float visibility)
    {
        bool visible = visibility > 0.01f;

        if (signalVisualRoot != null)
        {
            signalVisualRoot.gameObject.SetActive(visible);
        }

        if (signalRenderers == null)
        {
            return;
        }

        foreach (Renderer renderer in signalRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void UpdatePulse(CreatureProximityState state, float signalStrength)
    {
        if (pulseRoot == null)
        {
            return;
        }

        float distanceIntensity = state switch
        {
            CreatureProximityState.EncounterReady => Mathf.Clamp01(signalStrength + encounterPulseBoost),
            CreatureProximityState.StrongSignal => signalStrength,
            CreatureProximityState.WeakSignal => Mathf.Max(signalStrength, weakSignalPulseIntensity),
            _ => 0f,
        };

        bool showPulse = distanceIntensity > 0.01f;

        if (signalEffectRoot != null)
        {
            signalEffectRoot.SetActive(showPulse);
        }

        if (!showPulse)
        {
            pulseRoot.localScale = pulseBaseScale * minimumPulseScale;
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        float baselineScale = Mathf.Lerp(
            minimumPulseScale,
            maximumPulseScale * 0.45f,
            distanceIntensity
        );
        float peakScale = Mathf.Lerp(
            minimumPulseScale,
            maximumPulseScale,
            distanceIntensity
        );
        float pulseScale = Mathf.Lerp(baselineScale, peakScale, pulse);
        pulseRoot.localScale = pulseBaseScale * pulseScale;
    }

    private void UpdateEncounterPrompt(CreatureProximityState state)
    {
        if (encounterPrompt == null)
        {
            return;
        }

        bool ready = state == CreatureProximityState.EncounterReady;
        encounterPrompt.alpha = ready ? 1f : 0f;
        encounterPrompt.interactable = ready;
        encounterPrompt.blocksRaycasts = ready;
        encounterPrompt.gameObject.SetActive(ready);
    }
}
