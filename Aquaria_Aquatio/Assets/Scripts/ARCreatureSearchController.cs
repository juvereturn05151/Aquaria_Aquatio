// Used by scene: Assets/Scenes/Encounter_01_ARSearch.unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

    [Header("Scene")]
    [SerializeField] private string returnSceneName = "Exploration_04_EncounterEntry";

    [Header("Debug Runtime")]
    [SerializeField] private ARSearchState searchState = ARSearchState.Initializing;
    [SerializeField] private Transform spawnedCreature;
    [SerializeField] private bool trackingReady;

    public UnityEvent<Transform> OnCreatureSpawned = new();
    public UnityEvent<Transform> OnCreatureVisible = new();
    public UnityEvent<Transform> OnCreatureFound = new();

    public ARSearchState SearchState => searchState;
    public Transform SpawnedCreature => spawnedCreature;

    private bool creatureVisibleEventSent;

    private void Reset()
    {
        arSession = FindAnyObjectByType<ARSession>();
        arCamera = Camera.main;
        creatureSpawner = GetComponent<ARCreatureSpawner>();
        visibilityDetector = GetComponent<ARCreatureVisibilityDetector>();
        directionArrow = FindAnyObjectByType<ARDirectionArrow>();
        uiController = FindAnyObjectByType<ARSearchUIController>();
    }

    private void Awake()
    {
        searchState = ARSearchState.Initializing;

        if (uiController != null)
        {
            uiController.SetState(searchState);
        }

        if (directionArrow != null)
        {
            directionArrow.SetVisible(false);
        }
    }

    private void Update()
    {
        trackingReady = ARSession.state == ARSessionState.SessionTracking;

        if (searchState == ARSearchState.Initializing)
        {
            TryBeginSearch();
        }
        else if (
            searchState == ARSearchState.Searching ||
            searchState == ARSearchState.CreatureVisible
        )
        {
            UpdateSearch();
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

        if (visibilityDetector != null)
        {
            visibilityDetector.ARCamera = arCamera;
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
            OnCreatureFound.Invoke(spawnedCreature);

            if (directionArrow != null)
            {
                directionArrow.SetVisible(false);
            }

            return;
        }

        if (visibilityDetector.CreatureInCameraView)
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
        float visibleTimer = visibilityDetector != null ? visibilityDetector.VisibleTimer : 0f;
        Vector3 arrowDirection =
            directionArrow != null ? directionArrow.ArrowTargetDirection : Vector3.zero;
        string placementState =
            creatureSpawner != null ? creatureSpawner.PlacementState : "No spawner";

        uiController.SetDebugText(
            $"AR Search State: {searchState}\n" +
            $"AR tracking state: {ARSession.state}\n" +
            $"Tracking ready: {trackingReady}\n" +
            $"Creature Position: {creaturePosition:F2}\n" +
            $"Distance To Creature: {distance:F2} m\n" +
            $"Horizontal Angle To Creature: {angle:F1}\n" +
            $"Creature In Camera View: {inView}\n" +
            $"Visible Timer: {visibleTimer:F2}\n" +
            $"Arrow Target Direction: {arrowDirection:F2}\n" +
            $"Placement: {placementState}"
        );
    }
}
