/*
CreatureProximityState.cs

Purpose:
Defines the player-to-creature proximity bands used by exploration feedback and
encounter availability.

Responsibilities:
- Name the out-of-range, weak-signal, strong-signal, and encounter-ready states.
- Provide shared state values for proximity UI and creature presentation.

Architecture:
Small gameplay enum shared by CreatureProximitySystem, CreaturePresentation,
and debug UI.

Dependencies:
- None

Data Flow:
CreatureProximitySystem computes CreatureProximityState
    -> UI, presentation, and encounter flow read the result

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

public enum CreatureProximityState
{
    OutOfRange,
    WeakSignal,
    StrongSignal,
    EncounterReady
}
