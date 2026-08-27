using TMPro;
using UnityEngine;

public class GPSWorldMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform playerMarker;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Movement")]
    [SerializeField] private float movementScale = 1f;
    [SerializeField] private float smoothingSpeed = 5f;
    [SerializeField] private float minimumMovementDistance = 1.5f;
    [SerializeField] private float maximumHorizontalAccuracy = 25f;

    [Header("Debug Runtime")]
    [SerializeField] private bool hasOrigin;
    [SerializeField] private double originLatitude;
    [SerializeField] private double originLongitude;
    [SerializeField] private double currentLatitude;
    [SerializeField] private double currentLongitude;
    [SerializeField] private float eastMeters;
    [SerializeField] private float northMeters;
    [SerializeField] private float totalDistanceFromOrigin;
    [SerializeField] private Vector3 gpsDisplacement;
    [SerializeField] private Vector3 worldRootTargetPosition;
    [SerializeField] private Vector3 worldRootCurrentPosition;
    [SerializeField] private int acceptedSamples;
    [SerializeField] private int rejectedSamples;
    [SerializeField] private string lastSampleResult;

    private GPSManager gpsManager;
    private bool hasAcceptedGpsPosition;
    private double previousAcceptedLatitude;
    private double previousAcceptedLongitude;
    private double lastProcessedTimestamp = -1.0;

    public bool HasOrigin => hasOrigin;
    public double OriginLatitude => originLatitude;
    public double OriginLongitude => originLongitude;
    public double CurrentLatitude => currentLatitude;
    public double CurrentLongitude => currentLongitude;
    public float EastMeters => eastMeters;
    public float NorthMeters => northMeters;
    public float TotalDistanceFromOrigin => totalDistanceFromOrigin;
    public Vector3 GPSDisplacement => gpsDisplacement;
    public Vector3 WorldRootTargetPosition => worldRootTargetPosition;
    public Vector3 WorldRootCurrentPosition => worldRootCurrentPosition;
    public int AcceptedSamples => acceptedSamples;
    public int RejectedSamples => rejectedSamples;

    public void SetGPSManager(GPSManager manager)
    {
        gpsManager = manager;
    }

    private void Update()
    {
        KeepPlayerMarkerCentered();

        if (gpsManager == null || worldRoot == null || !gpsManager.HasValidLocation)
        {
            UpdateDebugText();
            return;
        }

        if (gpsManager.CurrentTimestamp != lastProcessedTimestamp)
        {
            ProcessGpsSample();
        }

        SmoothWorldRoot();
        UpdateDebugText();
    }

    private void ProcessGpsSample()
    {
        lastProcessedTimestamp = gpsManager.CurrentTimestamp;
        currentLatitude = gpsManager.CurrentLatitude;
        currentLongitude = gpsManager.CurrentLongitude;

        if (!hasOrigin)
        {
            SetOrigin(currentLatitude, currentLongitude);
            lastSampleResult = "Accepted origin";
            return;
        }

        Vector2 displacementFromOrigin = CalculateDisplacementMeters(
            originLatitude,
            originLongitude,
            currentLatitude,
            currentLongitude
        );

        eastMeters = displacementFromOrigin.x;
        northMeters = displacementFromOrigin.y;
        totalDistanceFromOrigin = displacementFromOrigin.magnitude;
        gpsDisplacement = new Vector3(
            eastMeters * movementScale,
            0f,
            northMeters * movementScale
        );

        if (ShouldAcceptCurrentGpsPosition(out string rejectionReason))
        {
            worldRootTargetPosition = new Vector3(
                -gpsDisplacement.x,
                worldRoot.position.y,
                -gpsDisplacement.z
            );

            previousAcceptedLatitude = currentLatitude;
            previousAcceptedLongitude = currentLongitude;
            hasAcceptedGpsPosition = true;
            acceptedSamples++;
            lastSampleResult = "Accepted";
        }
        else
        {
            rejectedSamples++;
            lastSampleResult = $"Rejected: {rejectionReason}";
        }
    }

    private void SmoothWorldRoot()
    {
        float lerpAmount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        worldRoot.position = Vector3.Lerp(
            worldRoot.position,
            worldRootTargetPosition,
            lerpAmount
        );

        worldRootCurrentPosition = worldRoot.position;
    }

    private void SetOrigin(double latitude, double longitude)
    {
        originLatitude = latitude;
        originLongitude = longitude;
        previousAcceptedLatitude = latitude;
        previousAcceptedLongitude = longitude;
        gpsDisplacement = Vector3.zero;
        worldRootTargetPosition = new Vector3(0f, worldRoot.position.y, 0f);
        worldRootCurrentPosition = worldRoot.position;
        hasAcceptedGpsPosition = true;
        hasOrigin = true;
        acceptedSamples++;
    }

    private bool ShouldAcceptCurrentGpsPosition(out string rejectionReason)
    {
        rejectionReason = string.Empty;

        if (
            maximumHorizontalAccuracy > 0f &&
            gpsManager.CurrentHorizontalAccuracy > maximumHorizontalAccuracy
        )
        {
            rejectionReason = "Horizontal accuracy too low";
            return false;
        }

        if (!hasAcceptedGpsPosition)
        {
            return true;
        }

        Vector2 movementFromPreviousAccepted = CalculateDisplacementMeters(
            previousAcceptedLatitude,
            previousAcceptedLongitude,
            currentLatitude,
            currentLongitude
        );

        if (movementFromPreviousAccepted.magnitude < minimumMovementDistance)
        {
            rejectionReason = "Movement below minimum distance";
            return false;
        }

        return true;
    }

    private Vector2 CalculateDisplacementMeters(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude
    )
    {
        const double metersPerDegree = 111320.0;

        double originLatitudeRadians = originLatitude * Mathf.Deg2Rad;
        double north = (toLatitude - fromLatitude) * metersPerDegree;
        double east = (toLongitude - fromLongitude) *
            metersPerDegree *
            System.Math.Cos(originLatitudeRadians);

        return new Vector2((float)east, (float)north);
    }

    private void KeepPlayerMarkerCentered()
    {
        if (playerMarker == null)
        {
            return;
        }

        playerMarker.position = new Vector3(0f, playerMarker.position.y, 0f);
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
        {
            return;
        }

        string status = gpsManager != null
            ? gpsManager.CurrentStatus.ToString()
            : "No GPSManager assigned";

        debugText.text =
            $"GPS Status: {status}\n" +
            $"Origin Lat/Lon: {originLatitude:F6}, {originLongitude:F6}\n" +
            $"Current Lat/Lon: {currentLatitude:F6}, {currentLongitude:F6}\n" +
            $"Accuracy: {(gpsManager != null ? gpsManager.CurrentHorizontalAccuracy : 0f):F1} m\n" +
            $"East: {eastMeters:F2} m\n" +
            $"North: {northMeters:F2} m\n" +
            $"Distance From Origin: {totalDistanceFromOrigin:F2} m\n" +
            $"GPS Displacement: {gpsDisplacement}\n" +
            $"WorldRoot Target: {worldRootTargetPosition}\n" +
            $"WorldRoot Current: {worldRootCurrentPosition}\n" +
            $"Accepted/Rejected: {acceptedSamples}/{rejectedSamples}\n" +
            $"Last Sample: {lastSampleResult}";
    }
}
