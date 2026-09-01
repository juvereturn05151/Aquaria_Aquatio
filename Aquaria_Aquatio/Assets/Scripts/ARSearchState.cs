/*
ARSearchState.cs

Purpose:
Defines the lifecycle states for the AR creature search scene.

Responsibilities:
- Name the encounter startup, searching, visibility, and found states.
- Provide shared state values for AR search coordination and UI display.

Architecture:
Small reusable enum consumed by ARCreatureSearchController and ARSearchUIController.

Dependencies:
- None

Data Flow:
ARCreatureSearchController sets ARSearchState values
    -> ARSearchUIController displays matching player instructions

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

public enum ARSearchState
{
    Initializing,
    Searching,
    CreatureVisible,
    CreatureFound
}
