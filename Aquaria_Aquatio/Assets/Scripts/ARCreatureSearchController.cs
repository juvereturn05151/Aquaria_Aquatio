/*
ARCreatureSearchController.cs

Purpose:
Coordinates the AR creature search encounter from scene startup through finding
the selected creature and returning to exploration.

Responsibilities:
- Read the selected creature from EncounterSessionData.
- Wait for AR tracking before spawning the encounter creature.
- Drive the AR search state and UI feedback.
- Tick creature visibility detection and direction arrow targeting.
- Register found creatures and trigger the union animation when complete.
- Load the configured return scene after the encounter result.

Architecture:
Scene-level AR encounter coordinator for Encounter_01_ARSearch. It connects AR
tracking, creature spawning, visibility gameplay, UI, session state, and scene
transition behavior.

Dependencies:
- ARSession
- ARCreatureSpawner
- ARCreatureVisibilityDetector
- ARDirectionArrow
- ARSearchUIController
- AquariaUnionAnimation
- EncounterSessionData
- UnityEngine.SceneManagement.SceneManager

Events / Data Flow:
EncounterSessionData selected creature
    -> ARCreatureSpawner
    -> ARCreatureVisibilityDetector / ARDirectionArrow / ARSearchUIController
    -> EncounterSessionData.RegisterCreatureFound()
    -> Return scene load

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;

public class ARCreatureSearchController : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private Camera arCamera;

    [Header("Search Components")]
    [SerializeField] private ARCreatureSpawner creatureSpawner;
    [SerializeField] private ARCreatureVisibilityDetector visibilityDetector;
    [SerializeField] private ARDirectionArrow directionArrow;
    [SerializeField] private ARSearchUIController uiController;
    [SerializeField] private AquariaUnionAnimation unionAnimation;

    [Header("Scene")]
    [SerializeField] private string returnSceneName = "Exploration_04_EncounterEntry";

    [Header("Encounter Flow")]
    [SerializeField] private float returnDelayAfterFound = 2.5f;

    [Header("Creature Animation")]
    [SerializeField] private string caughtAnimatorParameter = "Caught";

    [Header("Debug Runtime")]
    [SerializeField] private ARSearchState searchState = ARSearchState.Initializing;
    [SerializeField] private Transform spawnedCreature;
    [SerializeField] private bool trackingReady;
    [SerializeField] private CreatureType selectedCreatureType;
    [SerializeField] private float returnTimer;
    [SerializeField] private string activeCameraName;
    [SerializeField] private string activeCameraParentName;

    public UnityEvent<Transform> OnCreatureSpawned = new();
    public UnityEvent<Transform> OnCreatureVisible = new();
    public UnityEvent<Transform> OnCreatureFound = new();

    public ARSearchState SearchState => searchState;
    public Transform SpawnedCreature => spawnedCreature;

    private bool creatureVisibleEventSent;
    private int caughtAnimatorParameterHash;

    private void Reset()
    {
        arSession = FindAnyObjectByType<ARSession>();
        arCamera = Camera.main;
        creatureSpawner = GetComponent<ARCreatureSpawner>();
        visibilityDetector = GetComponent<ARCreatureVisibilityDetector>();
        directionArrow = FindAnyObjectByType<ARDirectionArrow>();
        uiController = FindAnyObjectByType<ARSearchUIController>();
        unionAnimation = FindAnyObjectByType<AquariaUnionAnimation>();
    }

    private void Awake()
    {
        caughtAnimatorParameterHash = Animator.StringToHash(caughtAnimatorParameter);
        ResolveActiveARCamera();
        searchState = ARSearchState.Initializing;
        selectedCreatureType = EncounterSessionData.HasSelectedCreature
            ? EncounterSessionData.SelectedCreatureType
            : EncounterSessionData.CurrentSignalCreature;

        if (unionAnimation == null)
        {
            unionAnimation = FindAnyObjectByType<AquariaUnionAnimation>(FindObjectsInactive.Include);
        }

        if (unionAnimation == null)
        {
            Debug.LogWarning(
                "ARCreatureSearchController has no AquariaUnionAnimation assigned. " +
                "Add the union UI to the scene and assign it in the Inspector.",
                this
            );
        }

        if (creatureSpawner != null)
        {
            creatureSpawner.SelectedCreatureType = selectedCreatureType;
        }

        if (uiController != null)
        {
            uiController.SetCreatureContext(selectedCreatureType);
            uiController.SetState(searchState);
        }

        if (directionArrow != null)
        {
            directionArrow.SetVisible(false);
        }
    }

    private void Update()
    {
        ResolveActiveARCamera();
        trackingReady = ARSession.state == ARSessionState.SessionTracking;

        if (searchState == ARSearchState.Initializing)
        {
            TryBeginSearch();
        }
        else if (
            searchState == ARSearchState.Searching ||
            searchState == ARSearchState.MoveCloser ||
            searchState == ARSearchState.CreatureVisible
        )
        {
            UpdateSearch();
        }
        else if (
            searchState == ARSearchState.CreatureFound &&
            !EncounterSessionData.AquariaAquarioUnited
        )
        {
            returnTimer += Time.deltaTime;

            if (returnTimer >= returnDelayAfterFound)
            {
                ReturnToPreviousScene();
            }
        }

        UpdateDebugText();
    }

    public void ReturnToPreviousScene()
    {
        SceneManager.LoadScene(returnSceneName);
    }

    private void TryBeginSearch()
    {
        if (!trackingReady || arCamera == null || creatureSpawner == null)
        {
            return;
        }

        spawnedCreature = creatureSpawner.Spawn(arCamera);

        if (spawnedCreature == null)
        {
            return;
        }

        SetSpawnedCreatureCaught(false);

        if (visibilityDetector != null)
        {
            visibilityDetector.ARCamera = arCamera;
            visibilityDetector.PlayerViewpoint = arCamera.transform;
            visibilityDetector.CreatureVisibilityTarget = spawnedCreature;
        }

        if (directionArrow != null)
        {
            directionArrow.ARCamera = arCamera;
            directionArrow.TargetTransform = spawnedCreature;
            directionArrow.SetVisible(true);
        }

        OnCreatureSpawned.Invoke(spawnedCreature);
        SetState(ARSearchState.Searching);
    }

    private void UpdateSearch()
    {
        if (visibilityDetector == null || spawnedCreature == null)
        {
            return;
        }

        bool found = visibilityDetector.TickVisibility();

        if (found)
        {
            SetState(ARSearchState.CreatureFound);
            SetSpawnedCreatureCaught(true);
            EncounterSessionData.RegisterCreatureFound(selectedCreatureType);
            returnTimer = 0f;

            if (uiController != null)
            {
                uiController.SetFoundMessage(EncounterSessionData.LastEncounterMessage);
            }

            OnCreatureFound.Invoke(spawnedCreature);

            if (directionArrow != null)
            {
                directionArrow.SetVisible(false);
            }

            if (EncounterSessionData.AquariaAquarioUnited && unionAnimation != null)
            {
                unionAnimation.Play();
            }

            return;
        }

        if (visibilityDetector.IsLookingAtCreature && !visibilityDetector.IsCloseEnough)
        {
            creatureVisibleEventSent = false;
            SetState(ARSearchState.MoveCloser);

            if (uiController != null)
            {
                uiController.SetMoveCloserInstruction(
                    visibilityDetector.DistanceToCreature,
                    visibilityDetector.RequiredDistance,
                    visibilityDetector.ShowDebugDistance
                );
            }
        }
        else if (visibilityDetector.IsLookingAtCreature)
        {
            if (!creatureVisibleEventSent)
            {
                creatureVisibleEventSent = true;
                OnCreatureVisible.Invoke(spawnedCreature);
            }

            SetState(ARSearchState.CreatureVisible);
        }
        else
        {
            creatureVisibleEventSent = false;
            SetState(ARSearchState.Searching);
        }
    }

    private void SetState(ARSearchState nextState)
    {
        if (searchState == nextState)
        {
            return;
        }

        searchState = nextState;

        if (uiController != null)
        {
            uiController.SetState(searchState);
        }
    }

    private void UpdateDebugText()
    {
        if (uiController == null)
        {
            return;
        }

        Vector3 creaturePosition = spawnedCreature != null ? spawnedCreature.position : Vector3.zero;
        float distance = directionArrow != null ? directionArrow.DistanceToTarget : 0f;
        float angle = directionArrow != null ? directionArrow.HorizontalAngleToTarget : 0f;
        bool inView = visibilityDetector != null && visibilityDetector.CreatureInCameraView;
        bool closeEnough = visibilityDetector != null && visibilityDetector.IsCloseEnough;
        float detectionDistance =
            visibilityDetector != null ? visibilityDetector.DistanceToCreature : 0f;
        float requiredDistance =
            visibilityDetector != null ? visibilityDetector.RequiredDistance : 0f;
        float visibleTimer = visibilityDetector != null ? visibilityDetector.VisibleTimer : 0f;
        Vector3 arrowDirection =
            directionArrow != null ? directionArrow.ArrowTargetDirection : Vector3.zero;
        string placementState =
            creatureSpawner != null ? creatureSpawner.PlacementState : "No spawner";

        uiController.SetDebugText(
            $"AR Search State: {searchState}\n" +
            $"Selected Creature: {selectedCreatureType}\n" +
            $"Aquaria Found: {EncounterSessionData.AquariaFound}\n" +
            $"Aquario Count To Catch: {EncounterSessionData.AquarioCountToCatch}\n" +
            $"United: {EncounterSessionData.AquariaAquarioUnited}\n" +
            $"AR tracking state: {ARSession.state}\n" +
            $"Tracking ready: {trackingReady}\n" +
            $"Camera Transform Name: {activeCameraName}\n" +
            $"Camera Parent Name: {activeCameraParentName}\n" +
            $"Camera Position: {FormatVector(arCamera != null ? arCamera.transform.position : Vector3.zero)}\n" +
            $"Camera Rotation: {FormatVector(arCamera != null ? arCamera.transform.eulerAngles : Vector3.zero)}\n" +
            $"Camera Forward: {FormatVector(arCamera != null ? arCamera.transform.forward : Vector3.zero)}\n" +
            $"Creature Position: {creaturePosition:F2}\n" +
            $"World Direction To Creature: {FormatVector(GetWorldDirectionToCreature())}\n" +
            $"Camera-Local Direction To Creature: {FormatVector(GetCameraLocalDirectionToCreature())}\n" +
            $"Distance To Creature: {distance:F2} m\n" +
            $"Detection Distance: {detectionDistance:F2} m\n" +
            $"Required Distance: {requiredDistance:F2} m\n" +
            $"Horizontal Angle To Creature: {angle:F1}\n" +
            $"Creature In Camera View: {inView}\n" +
            $"Close Enough: {closeEnough}\n" +
            $"Visible Timer: {visibleTimer:F2}\n" +
            $"Arrow Target Direction: {arrowDirection:F2}\n" +
            $"Placement: {placementState}"
        );
    }

    private void ResolveActiveARCamera()
    {
        Camera resolvedCamera = GetXROriginCamera();

        if (resolvedCamera == null)
        {
            resolvedCamera = GetARCameraManagerCamera();
        }

        if (resolvedCamera == null)
        {
            resolvedCamera = Camera.main;
        }

        if (resolvedCamera == null || arCamera == resolvedCamera)
        {
            UpdateActiveCameraDebugNames();
            return;
        }

        arCamera = resolvedCamera;
        UpdateRuntimeCameraReferences();
        UpdateActiveCameraDebugNames();
    }

    private Camera GetXROriginCamera()
    {
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        return xrOrigin != null ? xrOrigin.Camera : null;
    }

    private static Camera GetARCameraManagerCamera()
    {
        ARCameraManager cameraManager = FindAnyObjectByType<ARCameraManager>();
        return cameraManager != null ? cameraManager.GetComponent<Camera>() : null;
    }

    private void UpdateRuntimeCameraReferences()
    {
        if (arCamera == null)
        {
            return;
        }

        if (visibilityDetector != null)
        {
            visibilityDetector.ARCamera = arCamera;
            visibilityDetector.PlayerViewpoint = arCamera.transform;
        }

        if (directionArrow != null)
        {
            directionArrow.ARCamera = arCamera;
        }

        if (spawnedCreature != null)
        {
            EncounterCreatureLookAtPlayer lookAtPlayer =
                spawnedCreature.GetComponent<EncounterCreatureLookAtPlayer>();

            if (lookAtPlayer != null)
            {
                lookAtPlayer.Target = arCamera.transform;
            }
        }
    }

    private void UpdateActiveCameraDebugNames()
    {
        Transform cameraTransform = arCamera != null ? arCamera.transform : null;
        activeCameraName = cameraTransform != null ? cameraTransform.name : "None";
        activeCameraParentName =
            cameraTransform != null && cameraTransform.parent != null
                ? cameraTransform.parent.name
                : "None";
    }

    private Vector3 GetWorldDirectionToCreature()
    {
        return arCamera != null && spawnedCreature != null
            ? spawnedCreature.position - arCamera.transform.position
            : Vector3.zero;
    }

    private Vector3 GetCameraLocalDirectionToCreature()
    {
        return arCamera != null && spawnedCreature != null
            ? arCamera.transform.InverseTransformDirection(GetWorldDirectionToCreature())
            : Vector3.zero;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private void SetSpawnedCreatureCaught(bool caught)
    {
        if (spawnedCreature == null || string.IsNullOrWhiteSpace(caughtAnimatorParameter))
        {
            return;
        }

        Animator animator = spawnedCreature.GetComponentInChildren<Animator>(true);

        if (animator == null || !AnimatorHasBool(animator, caughtAnimatorParameterHash))
        {
            return;
        }

        animator.SetBool(caughtAnimatorParameterHash, caught);
    }

    private static bool AnimatorHasBool(Animator animator, int parameterHash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }
}
