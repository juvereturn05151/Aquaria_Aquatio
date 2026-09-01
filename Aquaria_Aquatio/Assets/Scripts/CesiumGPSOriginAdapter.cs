/*
CesiumGPSOriginAdapter.cs

Purpose:
Initializes the CesiumGeoreference origin from the first valid real GPS
coordinate reported by the active GPS position source.

Responsibilities:
- Receive GPSPositionSource and CesiumGeoreference references from the injector.
- Wait until GPSPositionSource is ready and has a valid coordinate.
- Set Cesium longitude, latitude, and height once.
- Optionally log the initialized Cesium origin.

Architecture:
Exploration scene adapter between gameplay GPS data and Cesium map rendering.
It keeps Cesium origin setup separate from player movement and encounter rules.

Dependencies:
- GPSPositionSource
- CesiumGeoreference
- ExplorationSystemInjector

Data Flow:
GPSManager
    -> GPSPositionSource
    -> CesiumGPSOriginAdapter
    -> CesiumGeoreference.SetOriginLongitudeLatitudeHeight()

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using CesiumForUnity;
using UnityEngine;

[DisallowMultipleComponent]
public class CesiumGPSOriginAdapter : MonoBehaviour
{
    [Header("Origin")]
    [SerializeField] 
    private double originHeight;

    [Header("Debug")]
    [SerializeField]
    private bool debugLogging = true;

    [Header("Debug Runtime")]
    [SerializeField]
    private bool originInitialized;
    [SerializeField] 
    private double initializedLatitude;
    [SerializeField] 
    private double initializedLongitude;

    private GPSPositionSource gpsPositionSource;
    private CesiumGeoreference cesiumGeoreference;

    public bool OriginInitialized => originInitialized;
    public double InitializedLatitude => initializedLatitude;
    public double InitializedLongitude => initializedLongitude;

    public void Initialize(ExplorationSystemInjector explorationSystemInjector) 
    {
        gpsPositionSource = explorationSystemInjector.GPSPositionSource;
        cesiumGeoreference = explorationSystemInjector.CesiumGeoreference;
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
