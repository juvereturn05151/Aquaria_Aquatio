public static class EncounterSessionData
{
    public static CreatureType SelectedCreatureType { get; private set; } = CreatureType.Aquaria;
    public static bool HasSelectedCreature { get; private set; }

    public static void SetSelectedCreature(CreatureType creatureType)
    {
        SelectedCreatureType = creatureType;
        HasSelectedCreature = true;
    }

    public static void Clear()
    {
        HasSelectedCreature = false;
    }
}
