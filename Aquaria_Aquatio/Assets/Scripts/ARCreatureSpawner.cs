// Used by scene: Assets/Scenes/Encounter_01_ARSearch.unity
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCreatureSpawner : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Creature")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform creatureParent;
    [SerializeField] private string spawnedCreatureName = "ARLookAroundCreature";
    [SerializeField] private CreatureType selectedCreatureType = CreatureType.Aquaria;

    [Header("Spawn Placement")]
    [SerializeField] private float minimumSpawnDistance = 18f;
    [SerializeField] private float maximumSpawnDistance = 30f;
    [SerializeField] private float minimumSpawnAngleFromForward = 60f;
    [SerializeField] private float maximumSpawnAngleFromForward = 160f;
    [SerializeField] private float creatureHeightOffset = 0.5f;
    [SerializeField] private bool useDetectedPlaneHeight;
    [SerializeField] private bool allowFallbackPlacementWithoutPlane = true;

    [Header("Debug Runtime")]
    [SerializeField] private Vector3 lastSpawnPosition;
    [SerializeField] private float resolvedSpawnAngle;
    [SerializeField] private float resolvedSpawnDistance;
    [SerializeField] private string placementState = "Waiting";

    public UnityEvent<Transform> OnCreatureSpawned = new();

    private readonly List<ARRaycastHit> raycastHits = new();

    public Vector3 LastSpawnPosition => lastSpawnPosition;
    public float ResolvedSpawnAngle => resolvedSpawnAngle;
    public float ResolvedSpawnDistance => resolvedSpawnDistance;
    public string PlacementState => placementState;
    public CreatureType SelectedCreatureType
    {
        get => selectedCreatureType;
        set => selectedCreatureType = value;
    }

    private void Reset()
    {
        planeManager = FindAnyObjectByType<ARPlaneManager>();
        raycastManager = FindAnyObjectByType<ARRaycastManager>();
    }

    public bool CanSpawn(out string reason)
    {
        if (creaturePrefab == null)
        {
            reason = "Missing creature prefab";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public Transform Spawn(Camera arCamera)
    {
        if (arCamera == null)
        {
            placementState = "Missing AR camera";
            return null;
        }

        if (!CanSpawn(out string reason))
        {
            placementState = reason;
            return null;
        }

        Vector3 spawnPosition = CalculateSpawnPosition(arCamera.transform);

        if (useDetectedPlaneHeight && TryFindPlaneHeight(spawnPosition, out float planeHeight))
        {
            spawnPosition.y = planeHeight + creatureHeightOffset;
            placementState = "Placed on detected plane";
        }
        else if (!useDetectedPlaneHeight || allowFallbackPlacementWithoutPlane)
        {
            spawnPosition.y = arCamera.transform.position.y + creatureHeightOffset;
            placementState = "Placed with horizontal search offset";
        }
        else
        {
            placementState = "Waiting for horizontal plane";
            return null;
        }

        Quaternion lookAtCamera = Quaternion.LookRotation(
            arCamera.transform.position - spawnPosition,
            Vector3.up
        );
        GameObject spawnedCreature = Instantiate(
            creaturePrefab,
            spawnPosition,
            lookAtCamera,
            creatureParent
        );

        spawnedCreature.name = $"{spawnedCreatureName}_{selectedCreatureType}";
        lastSpawnPosition = spawnPosition;
        OnCreatureSpawned.Invoke(spawnedCreature.transform);
        return spawnedCreature.transform;
    }

    private Vector3 CalculateSpawnPosition(Transform cameraTransform)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = Vector3.forward;
        }

        float angleMagnitude = Random.Range(
            minimumSpawnAngleFromForward,
            maximumSpawnAngleFromForward
        );
        float angleDirection = Random.value < 0.5f ? -1f : 1f;
        resolvedSpawnAngle = angleMagnitude * angleDirection;
        resolvedSpawnDistance = Random.Range(minimumSpawnDistance, maximumSpawnDistance);

        Vector3 spawnDirection =
            Quaternion.AngleAxis(resolvedSpawnAngle, Vector3.up) * flatForward.normalized;
        return cameraTransform.position + spawnDirection.normalized * resolvedSpawnDistance;
    }

    private bool TryFindPlaneHeight(Vector3 targetPosition, out float planeHeight)
    {
        planeHeight = 0f;

        if (planeManager != null)
        {
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    continue;
                }

                planeHeight = plane.transform.position.y;
                return true;
            }
        }

        if (raycastManager != null)
        {
            Vector2 screenPoint = new(Screen.width * 0.5f, Screen.height * 0.5f);

            if (
                raycastManager.Raycast(
                    screenPoint,
                    raycastHits,
                    TrackableType.PlaneWithinPolygon
                ) &&
                raycastHits.Count > 0
            )
            {
                planeHeight = raycastHits[0].pose.position.y;
                return true;
            }
        }

        return false;
    }
}
