using TMPro;
using UnityEngine;

public class GPSWorldMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerMarker;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Movement")]
    [SerializeField] private float movementScale = 1f;
    [SerializeField] private float smoothingSpeed = 5f;
    [SerializeField] private float minimumMovementDistance = 1.5f;

    [Header("Debug Runtime")]
    [SerializeField] private bool hasOrigin;
    [SerializeField] private double originLatitude;
    [SerializeField] private double originLongitude;
    [SerializeField] private double currentLatitude;
    [SerializeField] private double currentLongitude;
    [SerializeField] private float eastMeters;
    [SerializeField] private float northMeters;
    [SerializeField] private float totalDistanceFromOrigin;
    [SerializeField] private Vector3 resultingUnityPosition;

    private GPSManager gpsManager;
    private bool hasAcceptedGpsPosition;
    private double previousAcceptedLatitude;
    private double previousAcceptedLongitude;
    private Vector3 targetUnityPosition;

    public bool HasOrigin => hasOrigin;
    public double OriginLatitude => originLatitude;
    public double OriginLongitude => originLongitude;
    public double CurrentLatitude => currentLatitude;
    public double CurrentLongitude => currentLongitude;
    public float EastMeters => eastMeters;
    public float NorthMeters => northMeters;
    public float TotalDistanceFromOrigin => totalDistanceFromOrigin;
    public Vector3 ResultingUnityPosition => resultingUnityPosition;

    public void SetGPSManager(GPSManager manager)
    {
        gpsManager = manager;
    }

    private void Update()
    {
        if (gpsManager == null || playerMarker == null || !gpsManager.HasValidLocation)
        {
            UpdateDebugText();
            return;
        }

        currentLatitude = gpsManager.CurrentLatitude;
        currentLongitude = gpsManager.CurrentLongitude;

        if (!hasOrigin)
        {
            SetOrigin(currentLatitude, currentLongitude);
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

        if (ShouldAcceptCurrentGpsPosition())
        {
            targetUnityPosition = new Vector3(
                eastMeters * movementScale,
                playerMarker.position.y,
                northMeters * movementScale
            );

            previousAcceptedLatitude = currentLatitude;
            previousAcceptedLongitude = currentLongitude;
            hasAcceptedGpsPosition = true;
        }

        float lerpAmount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        playerMarker.position = Vector3.Lerp(
            playerMarker.position,
            targetUnityPosition,
            lerpAmount
        );

        resultingUnityPosition = playerMarker.position;
        UpdateDebugText();
    }

    private void SetOrigin(double latitude, double longitude)
    {
        originLatitude = latitude;
        originLongitude = longitude;
        previousAcceptedLatitude = latitude;
        previousAcceptedLongitude = longitude;
        targetUnityPosition = new Vector3(0f, playerMarker.position.y, 0f);
        resultingUnityPosition = playerMarker.position;
        hasAcceptedGpsPosition = true;
        hasOrigin = true;
    }

    private bool ShouldAcceptCurrentGpsPosition()
    {
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

        return movementFromPreviousAccepted.magnitude >= minimumMovementDistance;
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
            $"East: {eastMeters:F2} m\n" +
            $"North: {northMeters:F2} m\n" +
            $"Distance From Origin: {totalDistanceFromOrigin:F2} m\n" +
            $"Unity Position: {resultingUnityPosition}";
    }
}
