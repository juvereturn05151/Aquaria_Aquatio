/*
ARCreatureSpawner.cs

Purpose:
Places the encounter creature into the AR scene once tracking is ready.

Responsibilities:
- Choose a spawn direction and distance relative to the AR camera.
- Prefer detected AR plane height when plane placement is enabled.
- Fall back to a horizontal camera-relative spawn position.
- Instantiate the creature prefab and notify listeners.

Architecture:
Reusable AR scene helper owned by the encounter flow. It focuses only on spawn
placement and prefab instantiation.

Dependencies:
- Camera
- ARPlaneManager
- ARRaycastManager
- Creature prefab GameObject

Events / Data Flow:
ARCreatureSearchController
    -> SpawnCreature()
    -> OnCreatureSpawned event
    -> Visibility and direction systems receive the spawned Transform

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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
    [SerializeField] private GameObject aquariaCreaturePrefab;
    [SerializeField] private GameObject aquarioCreaturePrefab;
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
        if (GetPrefabForSelectedCreature() == null)
        {
            reason = $"Missing prefab for {selectedCreatureType}";
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
        GameObject selectedPrefab = GetPrefabForSelectedCreature();
        GameObject spawnedCreature = Instantiate(
            selectedPrefab,
            spawnPosition,
            lookAtCamera,
            creatureParent
        );
        EncounterCreatureLookAtPlayer lookAtPlayer =
            spawnedCreature.GetComponent<EncounterCreatureLookAtPlayer>();

        if (lookAtPlayer != null)
        {
            lookAtPlayer.Target = arCamera.transform;
        }

        spawnedCreature.name = string.IsNullOrWhiteSpace(spawnedCreatureName)
            ? selectedCreatureType.ToString()
            : $"{spawnedCreatureName}_{selectedCreatureType}";
        lastSpawnPosition = spawnPosition;
        OnCreatureSpawned.Invoke(spawnedCreature.transform);
        return spawnedCreature.transform;
    }

    private GameObject GetPrefabForSelectedCreature()
    {
        return selectedCreatureType switch
        {
            CreatureType.Aquaria => aquariaCreaturePrefab != null
                ? aquariaCreaturePrefab
                : creaturePrefab,
            CreatureType.Aquario => aquarioCreaturePrefab != null
                ? aquarioCreaturePrefab
                : creaturePrefab,
            _ => creaturePrefab,
        };
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
