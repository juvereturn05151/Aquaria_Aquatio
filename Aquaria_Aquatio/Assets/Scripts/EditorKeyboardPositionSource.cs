/*
EditorKeyboardPositionSource.cs

Purpose:
Provides simulated exploration movement for testing in the Unity Editor
without requiring real GPS data.

Responsibilities:

* Read keyboard input using WASD.
* Accept optional virtual movement input from another system.
* Convert input into local East/North movement in meters.
* Move the simulated exploration position at a configurable speed.
* Expose simulated movement through the shared ExplorationPositionSource interface.
* Mark the position source as ready even when the simulated player is stationary.

Controls:
W -> Move North
S -> Move South
D -> Move East
A -> Move West

Coordinate Mapping:
East  -> Unity X axis
North -> Unity Z axis

Simulation:
simulationSpeed represents movement speed in meters per second.

Keyboard input and virtual input are combined, then clamped so diagonal
movement does not become faster than movement along a single axis.

Architecture:
EditorKeyboardPositionSource inherits from ExplorationPositionSource and acts
as a development/testing alternative to GPSPositionSource.

Gameplay systems should depend on ExplorationPositionSource rather than
directly depending on this class. This allows the same gameplay code to work
with either real GPS movement or simulated Editor movement.

Data Flow:
Keyboard / Virtual Input
-> EditorKeyboardPositionSource
-> ExplorationPositionSource
-> Exploration movement / gameplay systems

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
