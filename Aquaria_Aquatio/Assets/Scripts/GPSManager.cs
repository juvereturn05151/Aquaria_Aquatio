/* 
GPSManager.cs
E-mail: juvereturn@gmail.com
Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.

Purpose:
This component owns access to Unity's GPS/location service.

Responsibilities:
- Request Android location permission.
- Start and stop Unity's LocationService.
- Wait for GPS initialization to complete.
- Read the latest GPS sample.
- Expose latitude, longitude, accuracy, timestamp, and service status.
- Provide itself to GPSPositionSource.

Architecture Notes:
GPSManager acts as the low-level GPS service layer.
Other gameplay systems should avoid directly calling Input.location when possible.
Instead, they should obtain location information through GPSManager or through
higher-level abstractions such as GPSPositionSource.

GPSPositionSource is required on the same GameObject because GPSManager
injects itself into that component.
*/

using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[RequireComponent(typeof(GPSPositionSource))]
public class GPSManager : MonoBehaviour
{
    [SerializeField] 
    private GPSPositionSource gpsPositionSource;

    public bool HasValidLocation { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentHorizontalAccuracy { get; private set; }
    public double CurrentTimestamp { get; private set; }
    public LocationServiceStatus CurrentStatus => Input.location.status;

    private void Awake()
    {
        InjectIntoMovement();
    }

    private IEnumerator Start()
    {
        InjectIntoMovement();

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            yield return null;
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;

        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            yield break;
        }
    }

    private void Update()
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            return;
        }

        LocationInfo location = Input.location.lastData;

        CurrentLatitude = location.latitude;
        CurrentLongitude = location.longitude;
        CurrentHorizontalAccuracy = location.horizontalAccuracy;
        CurrentTimestamp = location.timestamp;
        HasValidLocation = true;
    }

    private void InjectIntoMovement()
    {
        if (gpsPositionSource != null)
        {
            gpsPositionSource.SetGPSManager(this);
        }
    }

    private void OnDestroy()
    {
        Input.location.Stop();
    }
}
