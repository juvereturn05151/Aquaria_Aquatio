/*
ExplorationController.cs

Purpose:
Moves the exploration player marker or world root from East/North displacement
reported by the active position source.

Responsibilities:
- Receive an ExplorationPositionSource from the source selector.
- Convert East/North meters into Unity X/Z movement.
- Smooth movement toward the latest displacement target.
- Support either moving the player marker or keeping the player centered and
  moving the world root.
- Optionally make the camera follow the player marker.

Architecture:
Exploration movement adapter between GPS/simulated position data and scene
Transforms. It does not own GPS sampling or creature encounter rules.

Dependencies:
- ExplorationPositionSource
- World root Transform
- Player marker Transform
- Optional follow Camera

Data Flow:
ExplorationPositionSource.DisplacementMeters
    -> ExplorationController.Update()
    -> PlayerMarker or WorldRoot Transform movement

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Camera Swipe")]
    [SerializeField]
    private bool enableSwipeCameraRotation = true;
    [SerializeField]
    private float swipeYawSensitivity = 0.18f;
    [SerializeField]
    private float swipePitchSensitivity = 0.12f;
    [SerializeField]
    private float minCameraPitch = 25f;
    [SerializeField]
    private float maxCameraPitch = 75f;
    [SerializeField]
    private float cameraLookAtHeight = 1.25f;
    [SerializeField]
    private bool ignoreTouchesOverUI = true;
    [SerializeField]
    private bool allowMouseSwipeInEditor = true;

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
    private int activeSwipeFingerId = -1;
    private Vector2 lastSwipePosition;
    private float cameraYaw;
    private float cameraPitch;
    private float cameraDistance;
    private bool cameraOrbitInitialized;

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
        ProcessCameraSwipeInput();

        if (positionSource == null || !positionSource.IsReady)
        {
            FollowPlayerWithCamera();
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

        UpdateCameraOffsetFromOrbit();
        followCamera.position = playerMarker.position + followCameraOffset;

        Vector3 lookTarget = playerMarker.position + Vector3.up * cameraLookAtHeight;
        Vector3 lookDirection = lookTarget - followCamera.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            followCamera.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private void ProcessCameraSwipeInput()
    {
        if (!enableSwipeCameraRotation)
        {
            return;
        }

        if (Input.touchSupported && Input.touchCount > 0)
        {
            ProcessTouchSwipeInput();
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowMouseSwipeInEditor)
        {
            ProcessMouseSwipeInput();
        }
#endif
    }

    private void ProcessTouchSwipeInput()
    {
        if (activeSwipeFingerId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch candidate = Input.GetTouch(i);
                if (candidate.phase != TouchPhase.Began || IsTouchOverUI(candidate.fingerId))
                {
                    continue;
                }

                activeSwipeFingerId = candidate.fingerId;
                lastSwipePosition = candidate.position;
                InitializeCameraOrbitIfNeeded();
                break;
            }
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != activeSwipeFingerId)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                RotateCameraFromSwipeDelta(touch.position - lastSwipePosition);
                lastSwipePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Stationary)
            {
                lastSwipePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                activeSwipeFingerId = -1;
            }

            return;
        }

        activeSwipeFingerId = -1;
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void ProcessMouseSwipeInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsTouchOverUI(-1))
            {
                return;
            }

            lastSwipePosition = Input.mousePosition;
            InitializeCameraOrbitIfNeeded();
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        Vector2 currentPosition = Input.mousePosition;
        RotateCameraFromSwipeDelta(currentPosition - lastSwipePosition);
        lastSwipePosition = currentPosition;
    }
#endif

    private void RotateCameraFromSwipeDelta(Vector2 delta)
    {
        InitializeCameraOrbitIfNeeded();

        cameraYaw += delta.x * swipeYawSensitivity;
        cameraPitch = Mathf.Clamp(
            cameraPitch - delta.y * swipePitchSensitivity,
            minCameraPitch,
            maxCameraPitch
        );

        cameraOrbitInitialized = true;
    }

    private void InitializeCameraOrbitIfNeeded()
    {
        if (cameraOrbitInitialized)
        {
            return;
        }

        Vector3 offset = followCameraOffset;
        cameraDistance = Mathf.Max(offset.magnitude, 0.1f);
        Vector3 flatOffset = new Vector3(offset.x, 0f, offset.z);

        cameraYaw = flatOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(flatOffset.x, flatOffset.z) * Mathf.Rad2Deg
            : 180f;

        cameraPitch = Mathf.Asin(Mathf.Clamp(offset.y / cameraDistance, -1f, 1f)) * Mathf.Rad2Deg;
        cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
        cameraOrbitInitialized = true;
    }

    private void UpdateCameraOffsetFromOrbit()
    {
        InitializeCameraOrbitIfNeeded();

        float pitchRadians = cameraPitch * Mathf.Deg2Rad;
        float yawRadians = cameraYaw * Mathf.Deg2Rad;
        float horizontalDistance = Mathf.Cos(pitchRadians) * cameraDistance;

        followCameraOffset = new Vector3(
            Mathf.Sin(yawRadians) * horizontalDistance,
            Mathf.Sin(pitchRadians) * cameraDistance,
            Mathf.Cos(yawRadians) * horizontalDistance
        );
    }

    private bool IsTouchOverUI(int fingerId)
    {
        if (!ignoreTouchesOverUI || EventSystem.current == null)
        {
            return false;
        }

        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
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
