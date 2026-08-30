using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AquariaUnionAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;

    [Header("Timing")]
    [SerializeField] private float duration = 2.4f;

    [Header("Debug Runtime")]
    [SerializeField] private bool playing;
    [SerializeField] private float timer;

    private CanvasGroup overlayGroup;
    private RectTransform aquariaMarker;
    private RectTransform aquarioMarker;
    private RectTransform unitedMarker;
    private TextMeshProUGUI titleText;

    private void Awake()
    {
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        EnsureOverlay();
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

        if (progress >= 1f)
        {
            playing = false;
        }
    }

    public void Play()
    {
        EnsureOverlay();
        timer = 0f;
        playing = true;
        SetOverlayVisible(true);
    }

    private void EnsureOverlay()
    {
        if (overlayGroup != null || canvas == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("AquariaUnionOverlay");
        overlayObject.transform.SetParent(canvas.transform);

        RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;

        Image background = overlayObject.AddComponent<Image>();
        background.color = new Color(0f, 0.1f, 0.16f, 0.82f);

        overlayGroup = overlayObject.AddComponent<CanvasGroup>();

        GameObject titleObject = new GameObject("UnionTitle");
        titleObject.transform.SetParent(overlayObject.transform);
        titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "Aquaria and Aquario are uniting";
        titleText.fontSize = 38f;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 170f);
        titleRect.sizeDelta = new Vector2(720f, 96f);

        aquariaMarker = CreateMarker(
            overlayObject.transform,
            "AquariaMarker",
            new Color(0f, 0.75f, 0.95f, 1f),
            new Vector2(-190f, 0f),
            132f
        );
        aquarioMarker = CreateMarker(
            overlayObject.transform,
            "AquarioMarker",
            new Color(0.95f, 0.56f, 0.18f, 1f),
            new Vector2(190f, 0f),
            132f
        );
        unitedMarker = CreateMarker(
            overlayObject.transform,
            "UnitedMarker",
            new Color(0.8f, 1f, 0.85f, 0.95f),
            Vector2.zero,
            150f
        );
        unitedMarker.gameObject.SetActive(false);
    }

    private static RectTransform CreateMarker(
        Transform parent,
        string name,
        Color color,
        Vector2 anchoredPosition,
        float size
    )
    {
        GameObject markerObject = new GameObject(name);
        markerObject.transform.SetParent(parent);
        RectTransform rectTransform = markerObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(size, size);

        Image image = markerObject.AddComponent<Image>();
        image.color = color;

        return rectTransform;
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
}
