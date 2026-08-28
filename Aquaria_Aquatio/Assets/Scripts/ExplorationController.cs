using UnityEngine;

public class ExplorationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplorationPositionSource positionSource;
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform playerMarker;

    [Header("Movement")]
    [SerializeField] private float movementScale = 1f;
    [SerializeField] private float smoothingSpeed = 3f;
    [SerializeField] private bool keepPlayerMarkerCentered = true;

    [Header("Debug Runtime")]
    [SerializeField] private Vector3 gpsDisplacement;
    [SerializeField] private Vector3 worldRootTargetPosition;
    [SerializeField] private Vector3 worldRootCurrentPosition;

    public ExplorationPositionSource PositionSource => positionSource;
    public Vector3 GPSDisplacement => gpsDisplacement;
    public Vector3 WorldRootTargetPosition => worldRootTargetPosition;
    public Vector3 WorldRootCurrentPosition => worldRootCurrentPosition;
    public float MovementScale => movementScale;

    public void SetPositionSource(ExplorationPositionSource source)
    {
        positionSource = source;
    }

    private void Reset()
    {
        positionSource = FindAnyObjectByType<ExplorationPositionSource>();
    }

    private void Update()
    {
        KeepPlayerCentered();

        if (positionSource == null || worldRoot == null || !positionSource.IsReady)
        {
            return;
        }

        gpsDisplacement = positionSource.DisplacementMeters * movementScale;
        worldRootTargetPosition = new Vector3(
            -gpsDisplacement.x,
            worldRoot.position.y,
            -gpsDisplacement.z
        );

        float lerpAmount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        worldRoot.position = Vector3.Lerp(
            worldRoot.position,
            worldRootTargetPosition,
            lerpAmount
        );

        worldRootCurrentPosition = worldRoot.position;
    }

    private void KeepPlayerCentered()
    {
        if (!keepPlayerMarkerCentered || playerMarker == null)
        {
            return;
        }

        playerMarker.position = new Vector3(0f, playerMarker.position.y, 0f);
    }
}
