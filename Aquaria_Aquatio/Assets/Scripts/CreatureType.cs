/*
CreatureType.cs

Purpose:
Identifies the creature variants used by exploration targets and AR encounters.

Responsibilities:
- Provide shared creature identifiers for Aquaria and Aquario.
- Keep exploration progression and AR encounter selection using the same values.

Architecture:
Small gameplay enum shared across exploration detection, session state, and AR
encounter presentation.

Dependencies:
- None

Data Flow:
CreatureExplorationTarget / ExplorationEncounterFlow set CreatureType values
    -> EncounterSessionData carries the selected creature into the AR scene

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

public enum CreatureType
{
    Aquaria,
    Aquario
}
