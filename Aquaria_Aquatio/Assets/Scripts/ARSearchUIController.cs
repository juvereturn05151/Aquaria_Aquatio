/*
ARSearchUIController.cs

Purpose:
Updates the on-screen AR encounter instructions, found message, and optional
debug text.

Responsibilities:
- Store creature-specific instruction text for the current encounter.
- Show instructions for each ARSearchState.
- Toggle the found panel when the creature is discovered.
- Write optional debug text from the AR search controller.

Architecture:
Presentation component for Encounter_01_ARSearch. It does not decide gameplay
state; it reflects state passed in by ARCreatureSearchController.

Dependencies:
- TextMeshProUGUI
- Found panel GameObject
- ARSearchState

Data Flow:
ARCreatureSearchController
    -> SetCreatureContext() / SetState() / SetFoundMessage() / SetDebugText()
    -> AR scene UI

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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
