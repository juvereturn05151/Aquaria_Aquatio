using TMPro;
using UnityEngine;

public class DeviceHeadingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Heading")]
    [SerializeField] private bool enableCompassOnStart = true;
    [SerializeField] private bool preferTrueHeading = true;
    [SerializeField] private float headingSmoothingSpeed = 6f;

    [Header("Editor Simulation")]
    [SerializeField] private bool editorSimulationEnabled = true;
    [SerializeField] private float simulationTurnSpeed = 90f;

    [Header("Debug Runtime")]
    [SerializeField] private bool compassEnabled;
    [SerializeField] private float rawHeading;
    [SerializeField] private float smoothedHeading;
    [SerializeField] private float headingAccuracy;
    [SerializeField] private string headingState = "Waiting";

    public bool CompassEnabled => compassEnabled;
    public float RawHeading => rawHeading;
    public float SmoothedHeading => smoothedHeading;
    public float HeadingAccuracy => headingAccuracy;
    public string HeadingState => headingState;

    private bool hasHeading;

    private void Start()
    {
        if (enableCompassOnStart)
        {
            Input.compass.enabled = true;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (editorSimulationEnabled)
        {
            UpdateEditorSimulation();
        }
        else
#endif
        {
            UpdateCompassHeading();
        }

        SmoothHeading();
        RotatePlayerVisual();
        UpdateDebugText();
    }

    private void UpdateCompassHeading()
    {
        compassEnabled = Input.compass.enabled;

        if (!compassEnabled)
        {
            headingState = "Compass disabled";
            return;
        }

        headingAccuracy = Input.compass.headingAccuracy;

        float trueHeading = Input.compass.trueHeading;
        float magneticHeading = Input.compass.magneticHeading;

        rawHeading = preferTrueHeading && trueHeading > 0f
            ? trueHeading
            : magneticHeading;

        hasHeading = true;
        headingState = headingAccuracy < 0f
            ? "Compass accuracy unavailable"
            : "Compass running";
    }

    private void UpdateEditorSimulation()
    {
        compassEnabled = true;
        headingAccuracy = 0f;

        float turnInput = 0f;

        if (IsPressed(KeyCode.Q))
        {
            turnInput -= 1f;
        }

        if (IsPressed(KeyCode.E))
        {
            turnInput += 1f;
        }

        rawHeading = Mathf.Repeat(
            rawHeading + turnInput * simulationTurnSpeed * Time.deltaTime,
            360f
        );

        hasHeading = true;
        headingState = "Editor simulation";
    }

    private bool IsPressed(KeyCode keyCode)
    {
        return Input.GetKey(keyCode);
    }

    private void SmoothHeading()
    {
        if (!hasHeading)
        {
            return;
        }

        smoothedHeading = Mathf.LerpAngle(
            smoothedHeading,
            rawHeading,
            Mathf.Clamp01(headingSmoothingSpeed * Time.deltaTime)
        );
    }

    private void RotatePlayerVisual()
    {
        if (playerVisual == null || !hasHeading)
        {
            return;
        }

        playerVisual.localRotation = Quaternion.Euler(0f, smoothedHeading, 0f);
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
        {
            return;
        }

        debugText.text =
            $"Compass: {headingState}\n" +
            $"Raw Heading: {rawHeading:F1}\n" +
            $"Smoothed Heading: {smoothedHeading:F1}\n" +
            $"Heading Accuracy: {headingAccuracy:F1}";
    }
}
