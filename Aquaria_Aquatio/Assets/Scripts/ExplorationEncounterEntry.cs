using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ExplorationEncounterFlow))]
public class ExplorationEncounterEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplorationEncounterFlow encounterFlow;
    [SerializeField] private CanvasGroup encounterPrompt;
    [SerializeField] private Button encounterButton;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptBackground;
    [SerializeField] private RectTransform promptRectTransform;

    [Header("Prompt Presentation")]
    [SerializeField] private bool applyDefaultPromptStyle = true;
    [SerializeField] private Vector2 visibleAnchoredPosition = new(0f, 140f);
    [SerializeField] private Vector2 promptSize = new(620f, 128f);
    [SerializeField] private Color readyBackgroundColor = new(0f, 0.72f, 0.9f, 0.95f);
    [SerializeField] private Color readyTextColor = Color.white;

    [Header("Debug Runtime")]
    [SerializeField] private bool encounterReady;
    [SerializeField] private CreatureType selectedCreatureType;
    [SerializeField] private string encounterFlowMessage;

    public bool EncounterReady => encounterReady;

    private void Reset()
    {
        encounterFlow = GetComponent<ExplorationEncounterFlow>();
        encounterButton = GetComponentInChildren<Button>(true);
        encounterPrompt = GetComponentInChildren<CanvasGroup>(true);
        promptText = GetComponentInChildren<TextMeshProUGUI>(true);
        promptBackground = GetComponent<Image>();
        promptRectTransform = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        ResolveOptionalReferences();
        ApplyPromptStyle();

        if (encounterButton != null)
        {
            encounterButton.onClick.AddListener(BeginEncounter);
        }

        UpdatePrompt(false);
    }

    private void OnDestroy()
    {
        if (encounterButton != null)
        {
            encounterButton.onClick.RemoveListener(BeginEncounter);
        }
    }

    private void ResolveOptionalReferences()
    {
        if (encounterFlow == null)
        {
            encounterFlow = EnsureEncounterFlow();
        }

        if (encounterPrompt == null)
        {
            encounterPrompt = GetComponentInChildren<CanvasGroup>(true);
        }

        if (encounterButton == null)
        {
            encounterButton = GetComponentInChildren<Button>(true);
        }

        if (promptText == null)
        {
            promptText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (promptBackground == null)
        {
            promptBackground = GetComponent<Image>();
        }

        if (promptRectTransform == null)
        {
            promptRectTransform = GetComponent<RectTransform>();
        }
    }

    private ExplorationEncounterFlow EnsureEncounterFlow()
    {
        return GetComponent<ExplorationEncounterFlow>() ??
            gameObject.AddComponent<ExplorationEncounterFlow>();
    }

    private void ApplyPromptStyle()
    {
        if (!applyDefaultPromptStyle)
        {
            return;
        }

        if (promptRectTransform != null)
        {
            promptRectTransform.anchorMin = new Vector2(0.5f, 0f);
            promptRectTransform.anchorMax = new Vector2(0.5f, 0f);
            promptRectTransform.pivot = new Vector2(0.5f, 0f);
            promptRectTransform.anchoredPosition = visibleAnchoredPosition;
            promptRectTransform.sizeDelta = promptSize;
            promptRectTransform.localScale = Vector3.one;
        }

        if (promptBackground != null)
        {
            promptBackground.color = readyBackgroundColor;
        }

        if (encounterButton != null)
        {
            ColorBlock colors = encounterButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.65f, 0.9f, 1f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
            encounterButton.colors = colors;
        }

        if (promptText != null)
        {
            promptText.fontSize = 34f;
            promptText.color = readyTextColor;
            promptText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void Update()
    {
        if (encounterFlow != null)
        {
            encounterReady = encounterFlow.EncounterReady;
            selectedCreatureType = encounterFlow.SelectedCreatureType;
            encounterFlowMessage = encounterFlow.EncounterFlowMessage;
        }

        UpdatePrompt(encounterReady);
    }

    public void BeginEncounter()
    {
        if (encounterFlow == null)
        {
            return;
        }

        encounterFlow.TryBeginEncounter();
    }

    private void UpdatePrompt(bool visible)
    {
        if (encounterPrompt != null)
        {
            encounterPrompt.alpha = visible ? 1f : 0f;
            encounterPrompt.interactable = visible;
            encounterPrompt.blocksRaycasts = visible;
        }

        if (encounterButton != null)
        {
            encounterButton.interactable = visible;
        }

        if (promptText != null)
        {
            promptText.text = visible
                ? $"START AR ENCOUNTER - {selectedCreatureType}"
                : encounterFlow.AquariaAquarioUnited
                    ? "Aquaria and Aquario United"
                    : $"Find {encounterFlow.CurrentSignalCreature}'s signal";
        }
    }
}
