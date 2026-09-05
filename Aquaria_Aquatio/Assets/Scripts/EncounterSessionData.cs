/*
EncounterSessionData.cs

Purpose:
Stores the selected AR encounter creature and simple two-creature progression
state across scene loads.

Responsibilities:
- Remember which CreatureType should be searched for in the AR scene.
- Track whether Aquaria has been found and how many Aquario encounters succeeded.
- Determine which creature is currently searchable.
- Record found creatures and union completion.
- Reset or clear session/progression values for testing and replay.

Architecture:
Static runtime session store shared by exploration and AR scenes. It avoids
scene object references and survives scene transitions through static fields.

Dependencies:
- CreatureType

Data Flow:
ExplorationEncounterFlow selects a creature
    -> EncounterSessionData
    -> ARCreatureSearchController reads selection
    -> ARCreatureSearchController registers found result
    -> Exploration proximity filtering uses progression state

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

public static class EncounterSessionData
{
    private const int DefaultAquarioCatchCount = 2;

    public static CreatureType SelectedCreatureType { get; private set; } = CreatureType.Aquaria;
    public static bool HasSelectedCreature { get; private set; }
    public static bool ProgressionStarted { get; private set; }
    public static bool AquariaFound { get; private set; }
    public static int AquarioCountToCatch { get; private set; } = DefaultAquarioCatchCount;
    public static bool AquariaAquarioUnited { get; private set; }
    public static string LastEncounterMessage { get; private set; } =
        "Find Aquaria's signal first.";

    public static CreatureType CurrentSignalCreature
    {
        get
        {
            if (!AquariaFound)
            {
                return CreatureType.Aquaria;
            }

            return CreatureType.Aquario;
        }
    }

    public static void SetSelectedCreature(CreatureType creatureType)
    {
        ProgressionStarted = true;
        SelectedCreatureType = creatureType;
        HasSelectedCreature = true;
    }

    public static void EnsureProgressionStarted(int aquarioCountToCatch = DefaultAquarioCatchCount)
    {
        if (ProgressionStarted)
        {
            return;
        }

        ResetProgression(aquarioCountToCatch);
        ProgressionStarted = true;
    }

    public static bool CanSearchFor(CreatureType creatureType)
    {
        if (AquariaAquarioUnited)
        {
            return false;
        }

        return creatureType == CurrentSignalCreature;
    }

    public static void RegisterCreatureFound(CreatureType creatureType)
    {
        SetSelectedCreature(creatureType);

        if (creatureType == CreatureType.Aquaria)
        {
            AquariaFound = true;
            LastEncounterMessage =
                "Aquaria found. Aquaria says: please look for Aquario's signal.";
            return;
        }

        if (creatureType == CreatureType.Aquario && AquarioCountToCatch > 0)
        {
            AquarioCountToCatch--;
        }

        if (AquarioCountToCatch <= 0)
        {
            AquarioCountToCatch = 0;
            AquariaAquarioUnited = true;
            LastEncounterMessage = "Aquario found. Aquario unites with Aquaria.";
        }
        else
        {
            LastEncounterMessage =
                $"Aquario found. {AquarioCountToCatch} Aquario catches left.";
        }
    }

    public static void Clear()
    {
        HasSelectedCreature = false;
    }

    public static void ResetProgression(int aquarioCountToCatch = DefaultAquarioCatchCount)
    {
        SelectedCreatureType = CreatureType.Aquaria;
        HasSelectedCreature = false;
        ProgressionStarted = false;
        AquariaFound = false;
        AquarioCountToCatch = aquarioCountToCatch >= 0
            ? aquarioCountToCatch
            : DefaultAquarioCatchCount;
        AquariaAquarioUnited = false;
        LastEncounterMessage = "Find Aquaria's signal first.";
    }
}
