using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AREncounterPlacementController : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Camera arCamera;

    [Header("Creature")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform creatureParent;

    [Header("Placement")]
    [SerializeField] private bool useDebugPlacement = true;
    [SerializeField] private float debugSpawnYaw = 120f;
    [SerializeField] private float debugSpawnDistance = 3f;
    [SerializeField] private float minSpawnDistance = 2f;
    [SerializeField] private float maxSpawnDistance = 4f;
    [SerializeField] private float minSpawnYawOffset = 70f;
    [SerializeField] private float maxSpawnYawOffset = 160f;
    [SerializeField] private float heightOffset;
    [SerializeField] private bool allowFallbackPlacementWithoutPlane = true;

    [Header("Scene")]
    [SerializeField] private string returnSceneName = "Exploration_04_EncounterEntry";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Debug Runtime")]
    [SerializeField] private bool trackingReady;
    [SerializeField] private bool planeFound;
    [SerializeField] private bool creaturePlaced;
    [SerializeField] private float resolvedSpawnYaw;
    [SerializeField] private float resolvedSpawnDistance;
    [SerializeField] private int planeCount;
    [SerializeField] private string placementState = "Waiting";

    private readonly List<ARRaycastHit> raycastHits = new();
    private GameObject spawnedCreature;

    public bool CreaturePlaced => creaturePlaced;

    private void Reset()
    {
        arSession = FindAnyObjectByType<ARSession>();
        planeManager = FindAnyObjectByType<ARPlaneManager>();
        raycastManager = FindAnyObjectByType<ARRaycastManager>();
        arCamera = Camera.main;
    }

    private void Update()
    {
        trackingReady = ARSession.state == ARSessionState.SessionTracking;
        planeCount = CountPlanes();
        planeFound = planeCount > 0;

        if (!creaturePlaced)
        {
            TryPlaceCreature();
        }

        UpdateText();
    }

    public void ReturnToExploration()
    {
        EncounterSessionData.Clear();
        SceneManager.LoadScene(returnSceneName);
    }

    private void TryPlaceCreature()
    {
        if (creaturePrefab == null || arCamera == null)
        {
            placementState = "Missing creature prefab or AR camera";
            return;
        }

        if (!trackingReady)
        {
            placementState = "Move phone to scan the area";
            return;
        }

        ResolveSpawnSettings();

        Vector3 targetPosition = CalculateSpawnPosition();

        if (TryFindPlaneHeight(targetPosition, out float planeHeight))
        {
            targetPosition.y = planeHeight + heightOffset;
            placementState = "Placed on detected plane";
        }
        else if (allowFallbackPlacementWithoutPlane)
        {
            targetPosition.y += heightOffset;
            placementState = "Placed with fallback debug offset";
        }
        else
        {
            placementState = "Move phone to find a horizontal plane";
            return;
        }

        spawnedCreature = Instantiate(
            creaturePrefab,
            targetPosition,
            Quaternion.LookRotation(arCamera.transform.position - targetPosition, Vector3.up),
            creatureParent
        );
        spawnedCreature.name = EncounterSessionData.HasSelectedCreature
            ? $"{EncounterSessionData.SelectedCreatureType}_ARSearchTarget"
            : "Aquaria_ARSearchTarget";
        DisableExplorationPresentation(spawnedCreature);
        creaturePlaced = true;
    }

    private void DisableExplorationPresentation(GameObject creature)
    {
        CreaturePresentation presentation = creature.GetComponent<CreaturePresentation>();

        if (presentation != null)
        {
            presentation.enabled = false;
        }

        foreach (Renderer renderer in creature.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }
    }

    private void ResolveSpawnSettings()
    {
        if (useDebugPlacement)
        {
            resolvedSpawnYaw = debugSpawnYaw;
            resolvedSpawnDistance = debugSpawnDistance;
            return;
        }

        float yawMagnitude = UnityEngine.Random.Range(minSpawnYawOffset, maxSpawnYawOffset);
        float yawDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        resolvedSpawnYaw = yawMagnitude * yawDirection;
        resolvedSpawnDistance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);

        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = Vector3.forward;
        }

        Vector3 spawnDirection =
            Quaternion.AngleAxis(resolvedSpawnYaw, Vector3.up) * flatForward.normalized;
        return arCamera.transform.position + spawnDirection.normalized * resolvedSpawnDistance;
    }

    private bool TryFindPlaneHeight(Vector3 targetPosition, out float planeHeight)
    {
        planeHeight = 0f;

        if (planeManager != null)
        {
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (
                    plane == null ||
                    plane.alignment != PlaneAlignment.HorizontalUp
                )
                {
                    continue;
                }

                planeHeight = plane.transform.position.y;
                return true;
            }
        }

        if (raycastManager != null && arCamera != null)
        {
            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

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

    private int CountPlanes()
    {
        if (planeManager == null)
        {
            return 0;
        }

        int count = 0;

        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane != null)
            {
                count++;
            }
        }

        return count;
    }

    private void UpdateText()
    {
        if (instructionText != null)
        {
            instructionText.text = creaturePlaced
                ? "Look around to find the creature"
                : "Move phone to scan the area";
        }

        if (debugText == null)
        {
            return;
        }

        debugText.text =
            $"AR tracking state: {ARSession.state}\n" +
            $"Plane found/count: {planeFound} / {planeCount}\n" +
            $"Creature placed: {creaturePlaced}\n" +
            $"Spawn yaw: {resolvedSpawnYaw:F1}\n" +
            $"Spawn distance: {resolvedSpawnDistance:F1}\n" +
            $"Placement: {placementState}";
    }
}
