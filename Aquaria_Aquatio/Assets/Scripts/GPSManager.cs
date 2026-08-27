using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSManager : MonoBehaviour
{
    [SerializeField] private GPSWorldMovement gpsWorldMovement;

    public bool HasValidLocation { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentHorizontalAccuracy { get; private set; }
    public LocationServiceStatus CurrentStatus => Input.location.status;

    private void Awake()
    {
        InjectIntoMovement();
    }

    private void Reset()
    {
        gpsWorldMovement = FindAnyObjectByType<GPSWorldMovement>();
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

        while (
            Input.location.status == LocationServiceStatus.Initializing &&
            maxWait > 0
        )
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
        HasValidLocation = true;
    }

    private void InjectIntoMovement()
    {
        if (gpsWorldMovement != null)
        {
            gpsWorldMovement.SetGPSManager(this);
        }
    }

    private void OnDestroy()
    {
        Input.location.Stop();
    }
}
