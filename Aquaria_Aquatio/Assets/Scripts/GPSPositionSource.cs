/*
GPSPositionSource.cs

Purpose:
Provides exploration movement data using real GPS coordinates.

Responsibilities:

Receive GPS data from GPSManager.
Establish the first valid GPS position as the local exploration origin.
Convert latitude/longitude changes into local East/North displacement in meters.
Reject unreliable GPS samples using configurable filtering rules.
Smooth accepted GPS movement before exposing it to gameplay systems.
Track the latest GPS coordinates and accepted movement target.
Update the shared ExplorationPositionSource state used by downstream gameplay systems.

Filtering:
A GPS sample may be rejected when:

The coordinate is invalid.
Horizontal accuracy is worse than the configured maximum.
Movement is smaller than the minimum movement threshold.
Movement is larger than the maximum accepted jump distance.

Smoothing:
Accepted GPS samples are stored as target positions.
The displayed exploration position gradually moves toward the latest accepted target.

When accuracy-weighted smoothing is enabled, poor GPS accuracy reduces the
smoothing speed so noisy GPS samples have less immediate influence on movement.

Coordinate Mapping:
Latitude change -> North/South movement
Longitude change -> East/West movement

Local exploration coordinates are exposed as:
East -> Unity X axis
North -> Unity Z axis

Architecture:
GPSPositionSource inherits from ExplorationPositionSource and implements the
real-device GPS version of the exploration position system.

GPSManager is injected through SetGPSManager().
GPSPositionSource does not start, stop, or request permissions for the device GPS;
those responsibilities belong to GPSManager.

Downstream gameplay systems should depend on ExplorationPositionSource rather
than directly depending on GPSPositionSource. This allows the same gameplay
systems to work with GPS, simulated, or other position-source implementations.

Data Flow:
Unity Location Service
-> GPSManager
-> GPSPositionSource
-> ExplorationPositionSource
-> Exploration movement / gameplay systems

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class GPSPositionSource : ExplorationPositionSource
{
    [Header("GPS Filtering")]
    [SerializeField] 
    private float maximumHorizontalAccuracy = 20f;
    [SerializeField] 
    private float minimumMovementDistance = 2.5f;
    [SerializeField] 
    private float maximumAcceptedJumpDistance = 100f;

    [Header("GPS Smoothing")]
    [SerializeField] 
    private float gpsSmoothingSpeed = 2f;
    [SerializeField] 
    private bool useAccuracyWeightedSmoothing = true;
    [SerializeField] 
    private float poorAccuracySmoothingMultiplier = 0.35f;

    [Header("GPS Debug Runtime")]
    private bool hasOrigin;
    private double originLatitude;
    private double originLongitude;
    private double currentLatitude;
    private double currentLongitude;
    private Vector2 targetDisplacementMeters;
    private Vector2 smoothedDisplacementMeters;

    private GPSManager gpsManager;
    private double previousAcceptedLatitude;
    private double previousAcceptedLongitude;
    private double lastProcessedTimestamp = -1.0;
    private bool hasAcceptedTarget;

    public override double OriginLatitude => originLatitude;
    public override double OriginLongitude => originLongitude;
    public override double CurrentLatitude => currentLatitude;
    public override double CurrentLongitude => currentLongitude;
    public override float HorizontalAccuracy => gpsManager != null
        ? gpsManager.CurrentHorizontalAccuracy
        : 0f;
    public override LocationServiceStatus GPSStatus => gpsManager != null
        ? gpsManager.CurrentStatus
        : LocationServiceStatus.Stopped;

    public void SetGPSManager(GPSManager manager)
    {
        gpsManager = manager;
    }

    private void Update()
    {
        if (gpsManager == null || !gpsManager.HasValidLocation)
        {
            return;
        }

        if (gpsManager.CurrentTimestamp != lastProcessedTimestamp)
        {
            ProcessGpsSample();
        }

        SmoothTowardAcceptedGpsTarget();
    }

    private void ProcessGpsSample()
    {
        lastProcessedTimestamp = gpsManager.CurrentTimestamp;
        currentLatitude = gpsManager.CurrentLatitude;
        currentLongitude = gpsManager.CurrentLongitude;

        if (!IsValidCoordinate(currentLatitude, currentLongitude))
        {
            RejectPosition("Rejected: invalid GPS coordinate");
            return;
        }

        if (!hasOrigin)
        {
            SetOrigin(currentLatitude, currentLongitude);
            targetDisplacementMeters = Vector2.zero;
            smoothedDisplacementMeters = Vector2.zero;
            hasAcceptedTarget = true;
            AcceptPosition(0f, 0f, "Accepted origin");
            return;
        }

        Vector2 displacementFromOrigin = CalculateDisplacementMeters(
            originLatitude,
            originLongitude,
            currentLatitude,
            currentLongitude
        );

        if (!ShouldAcceptCurrentGpsPosition(out string rejectionReason))
        {
            RejectPosition(rejectionReason);
            return;
        }

        previousAcceptedLatitude = currentLatitude;
        previousAcceptedLongitude = currentLongitude;
        targetDisplacementMeters = displacementFromOrigin;
        hasAcceptedTarget = true;
        acceptedSamples++;
        lastSampleResult = "Accepted GPS target";
    }

    private void SetOrigin(double latitude, double longitude)
    {
        originLatitude = latitude;
        originLongitude = longitude;
        previousAcceptedLatitude = latitude;
        previousAcceptedLongitude = longitude;
        hasOrigin = true;
    }

    private void SmoothTowardAcceptedGpsTarget()
    {
        if (!hasAcceptedTarget)
        {
            return;
        }

        float smoothingSpeed = Mathf.Max(0f, gpsSmoothingSpeed);

        if (useAccuracyWeightedSmoothing)
        {
            float accuracyRatio = maximumHorizontalAccuracy > 0f
                ? Mathf.Clamp01(HorizontalAccuracy / maximumHorizontalAccuracy)
                : 0f;
            smoothingSpeed *= Mathf.Lerp(1f, poorAccuracySmoothingMultiplier, accuracyRatio);
        }

        float smoothingAmount = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
        smoothedDisplacementMeters = Vector2.Lerp(
            smoothedDisplacementMeters,
            targetDisplacementMeters,
            smoothingAmount
        );

        UpdatePosition(
            smoothedDisplacementMeters.x,
            smoothedDisplacementMeters.y,
            lastSampleResult
        );
    }

    private bool ShouldAcceptCurrentGpsPosition(out string rejectionReason)
    {
        rejectionReason = string.Empty;

        if (maximumHorizontalAccuracy > 0f && gpsManager.CurrentHorizontalAccuracy > maximumHorizontalAccuracy)
        {
            rejectionReason = "Rejected: horizontal accuracy too low";
            return false;
        }

        Vector2 movementFromPreviousAccepted = CalculateDisplacementMeters(
            previousAcceptedLatitude,
            previousAcceptedLongitude,
            currentLatitude,
            currentLongitude
        );

        float movementDistance = movementFromPreviousAccepted.magnitude;

        if (movementDistance < minimumMovementDistance)
        {
            rejectionReason = "Rejected: movement below minimum distance";
            return false;
        }

        if (maximumAcceptedJumpDistance > 0f && movementDistance > maximumAcceptedJumpDistance)
        {
            rejectionReason = "Rejected: GPS jump too large";
            return false;
        }

        return true;
    }

    private Vector2 CalculateDisplacementMeters(double fromLatitude, double fromLongitude, double toLatitude,double toLongitude)
    {
        const double metersPerDegree = 111320.0;

        double originLatitudeRadians = originLatitude * Mathf.Deg2Rad;
        double north = (toLatitude - fromLatitude) * metersPerDegree;
        double east = (toLongitude - fromLongitude) * metersPerDegree * System.Math.Cos(originLatitudeRadians);

        return new Vector2((float)east, (float)north);
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
