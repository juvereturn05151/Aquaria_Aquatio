/*
CreaturePresentation.cs

Purpose:
Animates exploration creature feedback based on the player's current proximity
to a creature target.

Responsibilities:
- Resolve visual roots and renderers when explicit references are missing.
- Show signal-ring feedback for the nearest creature.
- Fade creature renderers according to signal mode and proximity.
- Animate bobbing, pulsing, and the encounter prompt.

Architecture:
Presentation component that polls CreatureProximitySystem and applies visual
state to one CreatureExplorationTarget. It does not calculate proximity itself.

Dependencies:
- CreatureExplorationTarget
- CreatureProximitySystem
- Renderer
- CanvasGroup

Data Flow:
CreatureProximitySystem nearest target/state/signal strength
    -> CreaturePresentation.Update()
    -> Creature visuals, signal effect, and prompt opacity

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class CreaturePresentation : MonoBehaviour
{
    private const bool SignalOnlyMode = true;

    [Header("State Source")]
    [SerializeField] private CreatureExplorationTarget target;
    [SerializeField] private CreatureProximitySystem proximitySystem;

    [Header("Visual References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform bobRoot;
    [SerializeField] private Transform pulseRoot;
    [SerializeField] private GameObject signalEffectRoot;
    [SerializeField] private CanvasGroup encounterPrompt;
    [SerializeField] private Renderer[] fadeRenderers;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float weakSignalVisibility = 0f;
    [SerializeField, Range(0f, 1f)] private float visibleRendererThreshold = 0.5f;

    [Header("Bobbing")]
    [SerializeField] private float bobHeight = 0.35f;
    [SerializeField] private float bobSpeed = 2.4f;

    [Header("Pulse")]
    [SerializeField] private float minimumPulseScale = 1.25f;
    [SerializeField] private float maximumPulseScale = 5.5f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float weakSignalPulseIntensity = 0.2f;
    [SerializeField] private float encounterPulseBoost = 0.85f;

    private Vector3 visualBaseScale = Vector3.one;
    private Vector3 bobBaseLocalPosition;
    private Vector3 pulseBaseScale = Vector3.one;
    private float currentVisibility;

    public void SetProximitySystem(CreatureProximitySystem system)
    {
        proximitySystem = system;
    }

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<CreatureExplorationTarget>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
        }

        if (bobRoot == null)
        {
            bobRoot = visualRoot;
        }

        if (pulseRoot == null)
        {
            pulseRoot = transform.Find("VisualRoot/SignalRing");
        }

        if (signalEffectRoot == null && pulseRoot != null)
        {
            signalEffectRoot = pulseRoot.gameObject;
        }

        if (fadeRenderers == null || fadeRenderers.Length == 0)
        {
            fadeRenderers = GetComponentsInChildren<Renderer>(true);
        }

        visualBaseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        bobBaseLocalPosition = bobRoot != null ? bobRoot.localPosition : Vector3.zero;
        pulseBaseScale = pulseRoot != null ? pulseRoot.localScale : Vector3.one;
        if (proximitySystem != null)
        {
            ApplyCreatureVisibility(0f, true);
        }
    }

    private void Update()
    {
        if (proximitySystem == null)
        {
            return;
        }

        CreatureProximityState state = GetPresentationState(out float signalStrength);
        float targetVisibility = GetTargetVisibility(state);
        float fadeSpeed = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;

        currentVisibility = Mathf.MoveTowards(
            currentVisibility,
            targetVisibility,
            fadeSpeed
        );

        ApplyCreatureVisibility(currentVisibility, false);
        UpdateBob(state, signalStrength);
        UpdatePulse(state, signalStrength);
        UpdateEncounterPrompt(state);
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

    private void ApplyCreatureVisibility(float visibility, bool force)
    {
        bool renderCreature =
            !SignalOnlyMode && (visibility >= visibleRendererThreshold || force);

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(true);
            visualRoot.localScale = visualBaseScale;
        }

        if (fadeRenderers == null)
        {
            return;
        }

        foreach (Renderer renderer in fadeRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (IsPulseRenderer(renderer))
            {
                continue;
            }

            renderer.enabled = renderCreature;
        }
    }

    private void UpdateBob(CreatureProximityState state, float signalStrength)
    {
        if (bobRoot == null)
        {
            return;
        }

        bool shouldBob =
            !SignalOnlyMode &&
            (
                state == CreatureProximityState.StrongSignal ||
                state == CreatureProximityState.EncounterReady
            );
        float bobAmount = shouldBob
            ? Mathf.Sin(Time.time * bobSpeed) * bobHeight * Mathf.Max(0.25f, signalStrength)
            : 0f;

        bobRoot.localPosition = bobBaseLocalPosition + Vector3.up * bobAmount;
    }

    private void UpdatePulse(CreatureProximityState state, float signalStrength)
    {
        if (pulseRoot == null)
        {
            return;
        }

        pulseRoot.gameObject.SetActive(true);

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
            SetPulseRenderersEnabled(false);
            pulseRoot.localScale = pulseBaseScale * minimumPulseScale;
            return;
        }

        SetPulseRenderersEnabled(true);

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
        float pulseScale = Mathf.Lerp(
            baselineScale,
            peakScale,
            pulse
        );
        pulseRoot.localScale = pulseBaseScale * pulseScale;
    }

    private bool IsPulseRenderer(Renderer renderer)
    {
        return pulseRoot != null && renderer.transform.IsChildOf(pulseRoot);
    }

    private void SetPulseRenderersEnabled(bool enabled)
    {
        if (pulseRoot == null)
        {
            return;
        }

        Renderer[] pulseRenderers = pulseRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in pulseRenderers)
        {
            renderer.enabled = enabled;
        }
    }

    private void UpdateEncounterPrompt(CreatureProximityState state)
    {
        if (encounterPrompt == null)
        {
            return;
        }

        bool ready = !SignalOnlyMode &&
            state == CreatureProximityState.EncounterReady;
        encounterPrompt.alpha = ready ? 1f : 0f;
        encounterPrompt.interactable = ready;
        encounterPrompt.blocksRaycasts = ready;
        encounterPrompt.gameObject.SetActive(ready);
    }
}
