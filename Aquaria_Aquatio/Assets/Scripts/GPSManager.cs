/*
GPSManager.cs

Purpose:
Owns access to Unity's device location service and exposes the latest GPS sample.

Responsibilities:
- Request Android fine-location permission.
- Start Unity LocationService and wait for initialization.
- Copy the latest running location sample into public read-only properties.
- Expose latitude, longitude, horizontal accuracy, timestamp, and service status.
- Stop the Unity LocationService when destroyed.

Architecture:
Low-level GPS service component. GPSPositionSource reads this component after it
is supplied by ExplorationSystemInjector.

Dependencies:
- UnityEngine.Input.location
- UnityEngine.LocationService
- UnityEngine.Android.Permission on Android builds

Data Flow:
Unity Location Service
    -> GPSManager.Update()
    -> GPSPositionSource

Editor / Runtime:
Android permission code is wrapped in UNITY_ANDROID. In the editor, GPS access
depends on Unity's location service availability.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSManager : MonoBehaviour
{
    public bool HasValidLocation { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentHorizontalAccuracy { get; private set; }
    public double CurrentTimestamp { get; private set; }
    public LocationServiceStatus CurrentStatus => Input.location.status;

    private IEnumerator Start()
    {
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

    private void OnDestroy()
    {
        Input.location.Stop();
    }
}
