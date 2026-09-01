/*
GPSPositionSource.cs

Purpose:
Provides exploration position data by converting real GPS coordinates into local
East/North displacement.

Responsibilities:
- Receive GPSManager from ExplorationSystemInjector.
- Establish the first valid GPS coordinate as the local origin.
- Convert latitude/longitude changes into East/North meters.
- Reject invalid, inaccurate, too-small, or too-large GPS movement samples.
- Smooth accepted GPS targets before updating shared position state.
- Expose current/origin GPS coordinates and horizontal accuracy.

Architecture:
Real-device ExplorationPositionSource implementation. It reads GPS samples but
leaves permission and LocationService lifetime management to GPSManager.

Dependencies:
- ExplorationPositionSource
- GPSManager
- ExplorationSystemInjector

Data Flow:
Unity Location Service
    -> GPSManager
    -> GPSPositionSource.ProcessGpsSample()
    -> ExplorationPositionSource displacement
    -> Exploration movement, Cesium origin, and proximity systems

Editor / Runtime:
Intended for device GPS. ExplorationPositionSourceSelector may disable it in the
Unity Editor when keyboard simulation is selected.

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

    public void Initialize(ExplorationSystemInjector explorationSystemInjector)
    {
        gpsManager = explorationSystemInjector.GPSManager;
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
