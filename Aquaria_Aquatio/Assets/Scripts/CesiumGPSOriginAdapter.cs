using CesiumForUnity;
using UnityEngine;

[DisallowMultipleComponent]
public class CesiumGPSOriginAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GPSPositionSource gpsPositionSource;
    [SerializeField] private CesiumGeoreference cesiumGeoreference;

    [Header("Origin")]
    [SerializeField] private double originHeight;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    [Header("Debug Runtime")]
    [SerializeField] private bool originInitialized;
    [SerializeField] private double initializedLatitude;
    [SerializeField] private double initializedLongitude;

    public bool OriginInitialized => originInitialized;
    public double InitializedLatitude => initializedLatitude;
    public double InitializedLongitude => initializedLongitude;

    private void Reset()
    {
        gpsPositionSource = FindAnyObjectByType<GPSPositionSource>();
        cesiumGeoreference = FindAnyObjectByType<CesiumGeoreference>();
    }

    private void Update()
    {
        if (originInitialized || gpsPositionSource == null || cesiumGeoreference == null)
        {
            return;
        }

        if (!gpsPositionSource.IsReady)
        {
            return;
        }

        double latitude = gpsPositionSource.CurrentLatitude;
        double longitude = gpsPositionSource.CurrentLongitude;

        if (!IsValidCoordinate(latitude, longitude))
        {
            return;
        }

        cesiumGeoreference.SetOriginLongitudeLatitudeHeight(
            longitude,
            latitude,
            originHeight
        );

        initializedLatitude = latitude;
        initializedLongitude = longitude;
        originInitialized = true;

        if (debugLogging)
        {
            Debug.Log(
                $"Cesium GPS origin initialized at latitude {latitude:F7}, longitude {longitude:F7}, height {originHeight:F2}."
            );
        }
    }

    private bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude >= -90.0 &&
            latitude <= 90.0 &&
            longitude >= -180.0 &&
            longitude <= 180.0 &&
            (latitude != 0.0 || longitude != 0.0);
    }
}
