// Used by scene: Assets/Scenes/Encounter_01_ARSearch.unity
using TMPro;
using UnityEngine;

public class ARSearchUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private GameObject foundPanel;
    [SerializeField] private TextMeshProUGUI foundText;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Text")]
    [SerializeField] private string initializingInstruction = "Move phone to scan the area";
    [SerializeField] private string searchingInstruction = "Find the creature";
    [SerializeField] private string visibleInstruction = "Keep the creature in view";
    [SerializeField] private string foundInstruction = "Creature Found!";

    [Header("Debug")]
    [SerializeField] private bool showDebugPanel = true;

    public void SetCreatureContext(CreatureType creatureType)
    {
        searchingInstruction = $"Look around for {creatureType}";
        visibleInstruction = $"Keep {creatureType} in view";
        foundInstruction = $"{creatureType} Found!";

        if (instructionText != null)
        {
            instructionText.text = searchingInstruction;
        }
    }

    public void SetFoundMessage(string value)
    {
        foundInstruction = value;

        if (instructionText != null)
        {
            instructionText.text = foundInstruction;
        }

        if (foundText != null)
        {
            foundText.text = foundInstruction;
        }
    }

    public void SetState(ARSearchState state)
    {
        if (instructionText != null)
        {
            instructionText.text = state switch
            {
                ARSearchState.Initializing => initializingInstruction,
                ARSearchState.Searching => searchingInstruction,
                ARSearchState.CreatureVisible => visibleInstruction,
                ARSearchState.CreatureFound => foundInstruction,
                _ => searchingInstruction,
            };
        }

        if (foundPanel != null)
        {
            foundPanel.SetActive(state == ARSearchState.CreatureFound);
        }

        if (foundText != null)
        {
            foundText.text = foundInstruction;
        }
    }

    public void SetDebugText(string value)
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugPanel);
        }

        if (debugText != null)
        {
            debugText.text = value;
        }
    }
}
