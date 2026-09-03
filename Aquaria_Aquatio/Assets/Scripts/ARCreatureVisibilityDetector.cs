/*
ARCreatureVisibilityDetector.cs

Purpose:
Determines whether the spawned AR creature has remained visible in the camera
view and close enough to the player long enough to count as found.

Responsibilities:
- Track the player viewpoint Transform.
- Track the current target Transform.
- Project the target into camera viewport space.
- Measure horizontal player-to-creature distance.
- Accumulate visible time while the target is in view and close enough.
- Reset visibility progress when no target is assigned or the target leaves view.

Architecture:
Small reusable AR gameplay rule component. ARCreatureSearchController polls it
each frame through TickVisibility().

Dependencies:
- Camera
- Player viewpoint Transform assigned at runtime
- Target Transform assigned at runtime

Data Flow:
Spawned creature Transform and AR camera/player viewpoint
    -> CreatureVisibilityTarget / PlayerViewpoint
    -> TickVisibility()
    -> ARCreatureSearchController found-state decision

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class ARCreatureVisibilityDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform playerViewpoint;
    [SerializeField] private Transform creatureVisibilityTarget;

    [Header("Visibility")]
    [SerializeField] private float requiredVisibleTime = 0.75f;
    [SerializeField] private Vector2 viewportPadding = new(0.05f, 0.05f);

    [Header("Distance")]
    [SerializeField] private float requiredDistance = 2f;
    [SerializeField] private bool showDebugDistance;

    [Header("Debug Runtime")]
    [SerializeField] private bool creatureInCameraView;
    [SerializeField] private bool closeEnough;
    [SerializeField] private float distanceToCreature;
    [SerializeField] private float visibleTimer;
    [SerializeField] private Vector3 lastViewportPosition;

    public Camera ARCamera
    {
        get => arCamera;
        set
        {
            arCamera = value;

            if (playerViewpoint == null && arCamera != null)
            {
                playerViewpoint = arCamera.transform;
            }
        }
    }

    public Transform PlayerViewpoint
    {
        get => playerViewpoint;
        set
        {
            playerViewpoint = value;
            ResetTimer();
        }
    }

    public Transform CreatureVisibilityTarget
    {
        get => creatureVisibilityTarget;
        set
        {
            creatureVisibilityTarget = value;
            ResetTimer();
        }
    }

    public bool CreatureInCameraView => creatureInCameraView;
    public bool IsLookingAtCreature => creatureInCameraView;
    public bool IsCloseEnough => closeEnough;
    public float DistanceToCreature => distanceToCreature;
    public float RequiredDistance => requiredDistance;
    public bool ShowDebugDistance => showDebugDistance;
    public float VisibleTimer => visibleTimer;
    public float RequiredVisibleTime => requiredVisibleTime;
    public Vector3 LastViewportPosition => lastViewportPosition;

    private void Reset()
    {
        arCamera = Camera.main;
        playerViewpoint = arCamera != null ? arCamera.transform : null;
    }

    public bool TickVisibility()
    {
        creatureInCameraView = IsTargetInCameraView();
        closeEnough = IsTargetCloseEnough();

        if (creatureInCameraView && closeEnough)
        {
            visibleTimer += Time.deltaTime;
        }
        else
        {
            visibleTimer = 0f;
        }

        return visibleTimer >= requiredVisibleTime;
    }

    public void ResetTimer()
    {
        visibleTimer = 0f;
        creatureInCameraView = false;
        closeEnough = false;
        distanceToCreature = 0f;
        lastViewportPosition = Vector3.zero;
    }

    private bool IsTargetInCameraView()
    {
        if (arCamera == null || creatureVisibilityTarget == null)
        {
            return false;
        }

        lastViewportPosition = arCamera.WorldToViewportPoint(creatureVisibilityTarget.position);
        return
            lastViewportPosition.z > 0f &&
            lastViewportPosition.x > viewportPadding.x &&
            lastViewportPosition.x < 1f - viewportPadding.x &&
            lastViewportPosition.y > viewportPadding.y &&
            lastViewportPosition.y < 1f - viewportPadding.y;
    }

    private bool IsTargetCloseEnough()
    {
        Transform viewpoint = playerViewpoint != null
            ? playerViewpoint
            : arCamera != null
                ? arCamera.transform
                : null;

        if (viewpoint == null || creatureVisibilityTarget == null)
        {
            distanceToCreature = 0f;
            return false;
        }

        Vector3 playerPosition = viewpoint.position;
        Vector3 creaturePosition = creatureVisibilityTarget.position;
        playerPosition.y = 0f;
        creaturePosition.y = 0f;
        distanceToCreature = Vector3.Distance(playerPosition, creaturePosition);
        return distanceToCreature <= requiredDistance;
    }
}
