public static class EncounterSessionData
{
    private const int DefaultAquarioCatchCount = 3;

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
