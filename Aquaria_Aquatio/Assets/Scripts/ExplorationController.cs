using UnityEngine;

public class ExplorationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private Transform worldRoot;
    [SerializeField] 
    private Transform playerMarker;
    [SerializeField] 
    private Transform followCamera;

    [Header("Movement")]
    [SerializeField] 
    private float movementScale = 1f;
    [SerializeField] 
    private float smoothingSpeed = 3f;
    [SerializeField] 
    private bool keepPlayerMarkerCentered = true;
    [SerializeField] 
    private bool movePlayerMarkerFromDisplacement;
    [SerializeField] 
    private Vector3 followCameraOffset = new Vector3(0f, 13f, -10f);
    [SerializeField] 
    private bool debugMovementLogging;
    [SerializeField] 
    private float debugLogDistanceThreshold = 0.5f;

    [Header("Debug Runtime")]
    [SerializeField] 
    private Vector3 gpsDisplacement;
    [SerializeField] 
    private Vector3 worldRootTargetPosition;
    [SerializeField] 
    private Vector3 worldRootCurrentPosition;
    [SerializeField] 
    private Vector3 playerTargetPosition;

    private Vector3 lastLoggedDisplacement;
    private bool hasLoggedMovement;

    private ExplorationPositionSource positionSource;
    public ExplorationPositionSource PositionSource => positionSource;
    public Vector3 GPSDisplacement => gpsDisplacement;
    public Vector3 WorldRootTargetPosition => worldRootTargetPosition;
    public Vector3 WorldRootCurrentPosition => worldRootCurrentPosition;
    public float MovementScale => movementScale;

    public void SetPositionSource(ExplorationPositionSource source)
    {
        positionSource = source;
    }

    private void Update()
    {
        if (positionSource == null || !positionSource.IsReady)
        {
            return;
        }

        gpsDisplacement = positionSource.DisplacementMeters * movementScale;

        if (movePlayerMarkerFromDisplacement)
        {
            MovePlayerMarkerFromDisplacement();
            FollowPlayerWithCamera();
            LogMovementChangeIfNeeded();
            return;
        }

        KeepPlayerCentered();

        if (worldRoot == null)
        {
            return;
        }

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
        LogMovementChangeIfNeeded();
    }

    private void MovePlayerMarkerFromDisplacement()
    {
        if (playerMarker == null)
        {
            return;
        }

        playerTargetPosition = new Vector3(
            gpsDisplacement.x,
            playerMarker.position.y,
            gpsDisplacement.z
        );

        float lerpAmount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        playerMarker.position = Vector3.Lerp(
            playerMarker.position,
            playerTargetPosition,
            lerpAmount
        );
    }

    private void FollowPlayerWithCamera()
    {
        if (followCamera == null || playerMarker == null)
        {
            return;
        }

        followCamera.position = playerMarker.position + followCameraOffset;
    }

    private void KeepPlayerCentered()
    {
        if (!keepPlayerMarkerCentered || playerMarker == null)
        {
            return;
        }

        playerMarker.position = new Vector3(0f, playerMarker.position.y, 0f);
    }

    private void LogMovementChangeIfNeeded()
    {
        if (!debugMovementLogging)
        {
            return;
        }

        if (
            hasLoggedMovement &&
            Vector3.Distance(lastLoggedDisplacement, gpsDisplacement) < debugLogDistanceThreshold
        )
        {
            return;
        }

        hasLoggedMovement = true;
        lastLoggedDisplacement = gpsDisplacement;

        Vector3 playerPosition = playerMarker != null ? playerMarker.position : Vector3.zero;
        Vector3 cameraPosition = followCamera != null ? followCamera.position : Vector3.zero;

        Debug.Log(
            $"Exploration movement: east={positionSource.EastMeters:F2}, north={positionSource.NorthMeters:F2}, " +
            $"target={playerTargetPosition}, player={playerPosition}, camera={cameraPosition}"
        );
    }
}
