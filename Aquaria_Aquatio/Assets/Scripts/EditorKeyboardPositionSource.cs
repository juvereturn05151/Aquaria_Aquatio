/*
EditorKeyboardPositionSource.cs

Purpose:
Provides simulated exploration position data from keyboard and virtual movement
input.

Responsibilities:
- Read WASD input for local East/North movement.
- Accept optional virtual movement input from EncounterEntryVirtualController.
- Clamp combined movement so diagonal input is not faster.
- Advance simulated displacement at the configured meters-per-second speed.
- Mark the position source ready even before movement begins.

Architecture:
Concrete ExplorationPositionSource implementation for development and editor
testing. ExplorationPositionSourceSelector can choose it instead of GPS.

Dependencies:
- ExplorationPositionSource
- UnityEngine.Input

Data Flow:
Keyboard / virtual input
    -> EditorKeyboardPositionSource
    -> ExplorationController and CreatureProximitySystem

Editor / Runtime:
Intended for Unity Editor simulation, but the component itself can run in any
scene where it is enabled.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class EditorKeyboardPositionSource : ExplorationPositionSource
{
    [Header("Simulation")]
    [SerializeField] 
    private bool simulationEnabled = true;
    [SerializeField] 
    private float simulationSpeed = 3f;

    private Vector2 virtualMoveInput;

    public bool SimulationEnabled
    {
        get => simulationEnabled;
        set => simulationEnabled = value;
    }

    private void Update()
    {
        if (!simulationEnabled)
        {
            return;
        }

        Vector2 input = Vector2.zero;

        if (IsPressed(KeyCode.W))
        {
            input.y += 1f;
        }

        if (IsPressed(KeyCode.S))
        {
            input.y -= 1f;
        }

        if (IsPressed(KeyCode.D))
        {
            input.x += 1f;
        }

        if (IsPressed(KeyCode.A))
        {
            input.x -= 1f;
        }

        input += virtualMoveInput;

        if (input.sqrMagnitude <= 0f)
        {
            if (!isReady)
            {
                isReady = true;
                lastSampleResult = "Simulation ready";
            }

            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        eastMeters += input.x * simulationSpeed * Time.deltaTime;
        northMeters += input.y * simulationSpeed * Time.deltaTime;
        AcceptPosition(eastMeters, northMeters, "Accepted simulation");
    }

    private bool IsPressed(KeyCode keyCode)
    {
        return Input.GetKey(keyCode);
    }

    public void SetVirtualMoveInput(Vector2 input)
    {
        virtualMoveInput = Vector2.ClampMagnitude(input, 1f);
    }
}
