using UnityEngine;

public class EditorKeyboardPositionSource : ExplorationPositionSource
{
    [Header("Simulation")]
    [SerializeField] private bool simulationEnabled = true;
    [SerializeField] private float simulationSpeed = 3f;

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
}
