/*
AquariaUnionAnimation.cs

Purpose:
Builds and plays the short union celebration overlay after the AR encounter flow
has found both required creatures.

Responsibilities:
- Play an overlay UI that is authored in the scene.
- Animate marker positions, marker scale, and title visibility.
- Hide the overlay when the animation duration completes.

Architecture:
Scene-authored UI effect used by ARCreatureSearchController as a
presentation-only celebration step near the end of the AR search scene.

Dependencies:
- Canvas and CanvasGroup
- TextMeshProUGUI
- UnityEngine.UI.Image

Data Flow:
ARCreatureSearchController
    -> AquariaUnionAnimation.Play()
    -> Runtime overlay animation

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using TMPro;
using UnityEngine;

public class AquariaUnionAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private RectTransform aquariaMarker;
    [SerializeField] private RectTransform aquarioMarker;
    [SerializeField] private RectTransform unitedMarker;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Timing")]
    [SerializeField] private float duration = 2.4f;

    [Header("Debug Runtime")]
    [SerializeField] private bool playing;
    [SerializeField] private float timer;

    private void Awake()
    {
        SetOverlayVisible(false);
    }

    private void Update()
    {
        if (!playing)
        {
            return;
        }

        timer += Time.deltaTime;
        float progress = duration > 0f ? Mathf.Clamp01(timer / duration) : 1f;
        ApplyProgress(progress);

        if (progress >= 1f)
        {
            playing = false;
        }
    }

    public void Play()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogWarning(
                "AquariaUnionAnimation cannot play because the scene UI references are not assigned.",
                this
            );
            return;
        }

        timer = 0f;
        playing = true;
        SetOverlayVisible(true);
        ApplyProgress(0f);
    }

    private void ApplyProgress(float progress)
    {
        if (!HasRequiredReferences())
        {
            playing = false;
            return;
        }

        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        overlayGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(progress * 3f));
        aquariaMarker.anchoredPosition = Vector2.Lerp(new Vector2(-190f, 0f), Vector2.zero, easedProgress);
        aquarioMarker.anchoredPosition = Vector2.Lerp(new Vector2(190f, 0f), Vector2.zero, easedProgress);
        aquariaMarker.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, easedProgress);
        aquarioMarker.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, easedProgress);
        unitedMarker.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.45f, easedProgress);
        unitedMarker.gameObject.SetActive(progress > 0.35f);

        if (titleText != null)
        {
            titleText.text = progress >= 1f
                ? "Aquario united with Aquaria"
                : "Aquaria and Aquario are uniting";
        }
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayGroup == null)
        {
            return;
        }

        overlayGroup.gameObject.SetActive(visible);
        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = visible;
        overlayGroup.blocksRaycasts = visible;
    }

    private bool HasRequiredReferences()
    {
        return overlayGroup != null &&
            aquariaMarker != null &&
            aquarioMarker != null &&
            unitedMarker != null;
    }
}
