/*
ExplorationPrototypeSceneBuilder.cs

Purpose:
Builds early exploration prototype and creature-detection scenes from Unity
Editor menu commands.

Responsibilities:
- Create prototype scene cameras, lighting, ground, roads, landmarks, player marker, and UI.
- Create or reuse creature target prefabs and materials.
- Add GPS, simulated position, movement, heading, spawn, proximity, and debug systems.
- Wire serialized references for the generated scene objects.
- Configure tuning values for GPS filtering, movement, heading, and proximity.
- Add generated scenes to Unity build settings.

Architecture:
Editor-only scene generation utility for exploration prototypes. It creates
runtime scene objects and serialized references, but is not itself runtime code.

Dependencies:
- UnityEditor and UnityEditor.SceneManagement
- TextMeshProUGUI and UnityEngine.UI
- GPSManager and ExplorationPositionSource implementations
- ExplorationController and DeviceHeadingController
- CreatureSpawnManager and CreatureProximitySystem
- ExplorationDebugPanel

Data Flow:
Unity Editor menu item
    -> Generated prototype/detection scenes and prefabs
    -> Runtime exploration systems run from serialized references

Editor / Runtime:
Located under Assets/Scripts/Editor and depends on UnityEditor APIs, so it is
editor-only and excluded from runtime builds.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ExplorationPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ExplorationPrototype_Setup.unity";
    private const string CreatureDetectionScenePath = "Assets/Scenes/Exploration_02_CreatureDetection.unity";
    private const string AquariaCreaturePrefabPath = "Assets/Prefabs/AquariaCreature.prefab";
    private const string AquarioCreaturePrefabPath = "Assets/Prefabs/AquarioCreature.prefab";

    [MenuItem("Aquaria/Build Exploration Prototype Setup Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        Material groundMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Ground.mat",
            new Color(0.16f, 0.38f, 0.28f)
        );
        Material roadMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Road.mat",
            new Color(0.22f, 0.22f, 0.24f)
        );
        Material landmarkMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Landmark.mat",
            new Color(0.1f, 0.45f, 0.9f)
        );
        Material aquariaMaterial = CreateMaterial(
            "Assets/Resources/Aquaria_Target.mat",
            new Color(0.0f, 0.75f, 0.95f)
        );
        Material aquarioMaterial = CreateMaterial(
            "Assets/Resources/Aquario_Target.mat",
            new Color(0.95f, 0.56f, 0.18f)
        );

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 12f, -8f);
        cameraObject.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        camera.clearFlags = CameraClearFlags.Skybox;

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject systems = new GameObject("Systems");
        GameObject gpsSystem = CreateChild(systems, "GPSManager");
        GPSManager gpsManager = gpsSystem.AddComponent<GPSManager>();
        GPSPositionSource gpsPositionSource = gpsSystem.AddComponent<GPSPositionSource>();
        EditorKeyboardPositionSource editorPositionSource =
            gpsSystem.AddComponent<EditorKeyboardPositionSource>();
        ExplorationPositionSourceSelector sourceSelector =
            gpsSystem.AddComponent<ExplorationPositionSourceSelector>();

        GameObject explorationSystem = CreateChild(systems, "ExplorationController");
        ExplorationController explorationController =
            explorationSystem.AddComponent<ExplorationController>();

        GameObject headingSystem = CreateChild(systems, "DeviceHeadingController");
        DeviceHeadingController headingController =
            headingSystem.AddComponent<DeviceHeadingController>();

        GameObject spawnSystem = CreateChild(systems, "CreatureSpawnManager");
        CreatureSpawnManager spawnManager = spawnSystem.AddComponent<CreatureSpawnManager>();

        GameObject proximitySystemObject = CreateChild(systems, "CreatureProximitySystem");
        CreatureProximitySystem proximitySystem =
            proximitySystemObject.AddComponent<CreatureProximitySystem>();

        GameObject debugSystem = CreateChild(systems, "DebugManager");
        ExplorationDebugPanel debugPanel = debugSystem.AddComponent<ExplorationDebugPanel>();

        GameObject playerMarker = new GameObject("PlayerMarker");
        playerMarker.transform.position = new Vector3(0f, 1f, 0f);

        GameObject playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerVisual.name = "PlayerVisual";
        playerVisual.transform.SetParent(playerMarker.transform);
        playerVisual.transform.localPosition = Vector3.zero;
        playerVisual.transform.localRotation = Quaternion.identity;
        playerVisual.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        Object.DestroyImmediate(playerVisual.GetComponent<Collider>());

        GameObject forwardNose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forwardNose.name = "ForwardNose";
        forwardNose.transform.SetParent(playerVisual.transform);
        forwardNose.transform.localPosition = new Vector3(0f, 0.25f, 0.65f);
        forwardNose.transform.localScale = new Vector3(0.25f, 0.2f, 0.6f);
        Object.DestroyImmediate(forwardNose.GetComponent<Collider>());

        GameObject worldRoot = new GameObject("WorldRoot");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(worldRoot.transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(7f, 1f, 7f);
        SetMaterial(ground, groundMaterial);

        CreateCube(
            "TestRoad",
            worldRoot.transform,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(3f, 0.08f, 70f),
            roadMaterial
        );
        CreateCube(
            "TestRoadCrossing",
            worldRoot.transform,
            new Vector3(0f, 0.04f, 0f),
            new Vector3(70f, 0.08f, 3f),
            roadMaterial
        );

        CreateCube(
            "LandmarkA",
            worldRoot.transform,
            new Vector3(10f, 1f, 0f),
            new Vector3(1.5f, 2f, 1.5f),
            landmarkMaterial
        );
        CreateCube(
            "LandmarkB",
            worldRoot.transform,
            new Vector3(-10f, 1f, 10f),
            new Vector3(1.5f, 2f, 1.5f),
            landmarkMaterial
        );
        CreateCube(
            "LandmarkC",
            worldRoot.transform,
            new Vector3(0f, 1f, 20f),
            new Vector3(1.5f, 2f, 1.5f),
            landmarkMaterial
        );
        CreateCube(
            "LandmarkD",
            worldRoot.transform,
            new Vector3(20f, 1f, -10f),
            new Vector3(1.5f, 2f, 1.5f),
            landmarkMaterial
        );

        GameObject creatureTargets = CreateChild(worldRoot, "CreatureTargets");
        CreatureExplorationTarget aquariaTarget = CreateCreatureTarget(
            "AquariaTarget",
            creatureTargets.transform,
            CreatureType.Aquaria,
            new Vector3(0f, 0.6f, 15f),
            aquariaMaterial
        );
        CreatureExplorationTarget aquarioTarget = CreateCreatureTarget(
            "AquarioTarget",
            creatureTargets.transform,
            CreatureType.Aquario,
            new Vector3(15f, 0.6f, 5f),
            aquarioMaterial
        );

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject feedbackGroup = CreatePanel(
            "ExplorationFeedback",
            canvasObject.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(520f, 170f)
        );
        TextMeshProUGUI feedbackText = CreateText(
            "FeedbackText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, 42f),
            28f,
            "Creature Signal: None"
        );
        TextMeshProUGUI signalText = CreateText(
            "SignalText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -66f),
            new Vector2(-32f, 36f),
            24f,
            "Signal: Weak"
        );
        TextMeshProUGUI creatureNearbyText = CreateText(
            "CreatureNearbyText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -108f),
            new Vector2(-32f, 36f),
            24f,
            "No Creature Nearby"
        );

        GameObject debugPanelRoot = CreatePanel(
            "DebugPanel",
            canvasObject.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(24f, -220f),
            new Vector2(580f, -260f)
        );
        TextMeshProUGUI debugText = CreateText(
            "DebugText",
            debugPanelRoot.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, -292f),
            20f,
            "Debug ready"
        );

        TextMeshProUGUI gpsStatusText = CreateDebugField(debugPanelRoot.transform, "GPSSimulationStatus", 12f, "GPS/simulation status: Waiting");
        TextMeshProUGUI currentDisplacementText = CreateDebugField(debugPanelRoot.transform, "CurrentDisplacement", 42f, "Current displacement: (0, 0, 0)");
        TextMeshProUGUI eastDisplacementText = CreateDebugField(debugPanelRoot.transform, "EastDisplacement", 72f, "East displacement: 0.00 m");
        TextMeshProUGUI northDisplacementText = CreateDebugField(debugPanelRoot.transform, "NorthDisplacement", 102f, "North displacement: 0.00 m");
        TextMeshProUGUI headingText = CreateDebugField(debugPanelRoot.transform, "Heading", 132f, "Heading: 0.0");
        TextMeshProUGUI nearestCreatureText = CreateDebugField(debugPanelRoot.transform, "NearestCreature", 162f, "Nearest creature: None");
        TextMeshProUGUI distanceText = CreateDebugField(debugPanelRoot.transform, "DistanceToNearestCreature", 192f, "Distance to nearest creature: 0.0 m");
        TextMeshProUGUI proximityStateText = CreateDebugField(debugPanelRoot.transform, "ProximityState", 222f, "Proximity state: None");
        TextMeshProUGUI signalStrengthText = CreateDebugField(debugPanelRoot.transform, "SignalStrength", 252f, "Signal strength: 0.00");
        TextMeshProUGUI encounterStateText = CreateDebugField(debugPanelRoot.transform, "EncounterState", 282f, "Encounter state: None");

        GameObject encounterGroup = CreatePanel(
            "EncounterStatus",
            canvasObject.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            new Vector2(430f, 96f)
        );
        RectTransform encounterRect = encounterGroup.GetComponent<RectTransform>();
        encounterRect.pivot = new Vector2(1f, 1f);
        TextMeshProUGUI encounterStatusText = CreateText(
            "EncounterStatusText",
            encounterGroup.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, -32f),
            28f,
            "No Encounter"
        );

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        SetObjectReference(gpsManager, "gpsPositionSource", gpsPositionSource);
        SetObjectReference(sourceSelector, "gpsPositionSource", gpsPositionSource);
        SetObjectReference(sourceSelector, "editorPositionSource", editorPositionSource);
        SetObjectReference(sourceSelector, "explorationController", explorationController);
        SetObjectReference(sourceSelector, "proximitySystem", proximitySystem);
        SetObjectReference(explorationController, "positionSource", editorPositionSource);
        SetObjectReference(explorationController, "worldRoot", worldRoot.transform);
        SetObjectReference(explorationController, "playerMarker", playerMarker.transform);
        SetObjectReference(headingController, "playerVisual", playerVisual.transform);
        SetObjectReference(spawnManager, "targetsRoot", creatureTargets.transform);
        SetObjectReference(proximitySystem, "positionSource", editorPositionSource);
        SetObjectReference(proximitySystem, "spawnManager", spawnManager);
        SetObjectReference(proximitySystem, "feedbackText", feedbackText);
        SetObjectReference(proximitySystem, "signalText", signalText);
        SetObjectReference(proximitySystem, "creatureNearbyText", creatureNearbyText);
        SetObjectReference(proximitySystem, "encounterStatusText", encounterStatusText);
        SetObjectReference(debugPanel, "debugPanelRoot", debugPanelRoot);
        SetObjectReference(debugPanel, "debugText", debugText);
        SetObjectReference(debugPanel, "positionSourceSelector", sourceSelector);
        SetObjectReference(debugPanel, "positionSource", editorPositionSource);
        SetObjectReference(debugPanel, "explorationController", explorationController);
        SetObjectReference(debugPanel, "headingController", headingController);
        SetObjectReference(debugPanel, "proximitySystem", proximitySystem);
        SetObjectReference(debugPanel, "gpsSimulationStatusText", gpsStatusText);
        SetObjectReference(debugPanel, "currentDisplacementText", currentDisplacementText);
        SetObjectReference(debugPanel, "eastDisplacementText", eastDisplacementText);
        SetObjectReference(debugPanel, "northDisplacementText", northDisplacementText);
        SetObjectReference(debugPanel, "headingText", headingText);
        SetObjectReference(debugPanel, "nearestCreatureText", nearestCreatureText);
        SetObjectReference(debugPanel, "nearestCreatureDistanceText", distanceText);
        SetObjectReference(debugPanel, "proximityStateText", proximityStateText);
        SetObjectReference(debugPanel, "signalStrengthText", signalStrengthText);
        SetObjectReference(debugPanel, "encounterStateText", encounterStateText);

        SetFloat(gpsPositionSource, "maximumHorizontalAccuracy", 20f);
        SetFloat(gpsPositionSource, "minimumMovementDistance", 2.5f);
        SetFloat(gpsPositionSource, "gpsSmoothingSpeed", 2f);
        SetBool(gpsPositionSource, "useAccuracyWeightedSmoothing", true);
        SetFloat(gpsPositionSource, "poorAccuracySmoothingMultiplier", 0.35f);
        SetBool(editorPositionSource, "simulationEnabled", true);
        SetFloat(editorPositionSource, "simulationSpeed", 3f);
        SetFloat(explorationController, "smoothingSpeed", 3f);
        SetBool(headingController, "editorSimulationEnabled", true);
        SetFloat(headingController, "simulationTurnSpeed", 90f);
        SetFloat(proximitySystem, "detectionRange", 30f);
        SetFloat(proximitySystem, "strongSignalRange", 12f);
        SetFloat(proximitySystem, "encounterRange", 3f);
        SetBool(aquariaTarget, "useDebugPosition", false);
        SetBool(aquarioTarget, "useDebugPosition", false);

        spawnManager.CollectTargets();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Aquaria/Build Exploration 02 Creature Detection Scene")]
    public static void BuildCreatureDetectionScene()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        Material groundMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Ground.mat",
            new Color(0.16f, 0.38f, 0.28f)
        );
        Material roadMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Road.mat",
            new Color(0.22f, 0.22f, 0.24f)
        );
        Material landmarkMaterial = CreateMaterial(
            "Assets/Resources/Exploration_Landmark.mat",
            new Color(0.1f, 0.45f, 0.9f)
        );
        Material aquariaMaterial = CreateMaterial(
            "Assets/Resources/Aquaria_Target.mat",
            new Color(0.0f, 0.75f, 0.95f)
        );
        Material aquarioMaterial = CreateMaterial(
            "Assets/Resources/Aquario_Target.mat",
            new Color(0.95f, 0.56f, 0.18f)
        );

        GameObject aquariaPrefab = CreateAquariaCreaturePrefab(aquariaMaterial);
        GameObject aquarioPrefab = CreateCreaturePrefab(
            AquarioCreaturePrefabPath,
            "AquarioCreature",
            CreatureType.Aquario,
            aquarioMaterial,
            18f,
            0f
        );

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 14f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        camera.clearFlags = CameraClearFlags.Skybox;

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject systems = new GameObject("Systems");
        GameObject gpsSystem = CreateChild(systems, "GPSManager");
        GPSManager gpsManager = gpsSystem.AddComponent<GPSManager>();
        GPSPositionSource gpsPositionSource = gpsSystem.AddComponent<GPSPositionSource>();
        EditorKeyboardPositionSource editorPositionSource =
            gpsSystem.AddComponent<EditorKeyboardPositionSource>();
        ExplorationPositionSourceSelector sourceSelector =
            gpsSystem.AddComponent<ExplorationPositionSourceSelector>();

        GameObject explorationSystem = CreateChild(systems, "ExplorationController");
        ExplorationController explorationController =
            explorationSystem.AddComponent<ExplorationController>();

        GameObject headingSystem = CreateChild(systems, "DeviceHeadingController");
        DeviceHeadingController headingController =
            headingSystem.AddComponent<DeviceHeadingController>();

        GameObject spawnSystem = CreateChild(systems, "CreatureSpawnManager");
        CreatureSpawnManager spawnManager = spawnSystem.AddComponent<CreatureSpawnManager>();

        GameObject proximitySystemObject = CreateChild(systems, "CreatureProximitySystem");
        CreatureProximitySystem proximitySystem =
            proximitySystemObject.AddComponent<CreatureProximitySystem>();

        GameObject debugSystem = CreateChild(systems, "DebugManager");
        ExplorationDebugPanel debugPanel = debugSystem.AddComponent<ExplorationDebugPanel>();

        GameObject playerMarker = new GameObject("PlayerMarker");
        playerMarker.transform.position = new Vector3(0f, 1f, 0f);

        GameObject playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerVisual.name = "PlayerVisual";
        playerVisual.transform.SetParent(playerMarker.transform);
        playerVisual.transform.localPosition = Vector3.zero;
        playerVisual.transform.localRotation = Quaternion.identity;
        playerVisual.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        Object.DestroyImmediate(playerVisual.GetComponent<Collider>());

        GameObject forwardNose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forwardNose.name = "ForwardNose";
        forwardNose.transform.SetParent(playerVisual.transform);
        forwardNose.transform.localPosition = new Vector3(0f, 0.25f, 0.65f);
        forwardNose.transform.localScale = new Vector3(0.25f, 0.2f, 0.6f);
        Object.DestroyImmediate(forwardNose.GetComponent<Collider>());

        GameObject worldRoot = new GameObject("WorldRoot");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(worldRoot.transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(7f, 1f, 7f);
        SetMaterial(ground, groundMaterial);

        CreateCube(
            "NorthRoad",
            worldRoot.transform,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(3f, 0.08f, 70f),
            roadMaterial
        );
        CreateCube(
            "EastRoad",
            worldRoot.transform,
            new Vector3(0f, 0.04f, 0f),
            new Vector3(70f, 0.08f, 3f),
            roadMaterial
        );
        CreateCube(
            "NorthTwelveMeterMarker",
            worldRoot.transform,
            new Vector3(0f, 0.35f, 12f),
            new Vector3(2f, 0.7f, 0.2f),
            landmarkMaterial
        );

        GameObject creatureTargets = CreateChild(worldRoot, "CreatureTargets");
        GameObject aquariaInstance = (GameObject)PrefabUtility.InstantiatePrefab(aquariaPrefab);
        aquariaInstance.name = "Aquaria_DebugTarget";
        aquariaInstance.transform.SetParent(creatureTargets.transform);
        CreatureExplorationTarget aquariaTarget =
            aquariaInstance.GetComponent<CreatureExplorationTarget>();
        SetEnum(aquariaTarget, "creatureType", (int)CreatureType.Aquaria);
        SetBool(aquariaTarget, "useDebugPosition", true);
        SetFloat(aquariaTarget, "debugEast", 0f);
        SetFloat(aquariaTarget, "debugNorth", 12f);
        SetFloat(aquariaTarget, "height", 0.6f);
        aquariaTarget.ApplyDebugPositionIfEnabled();

        GameObject aquarioInstance = (GameObject)PrefabUtility.InstantiatePrefab(aquarioPrefab);
        aquarioInstance.name = "Aquario_DebugTarget";
        aquarioInstance.transform.SetParent(creatureTargets.transform);
        CreatureExplorationTarget aquarioTarget =
            aquarioInstance.GetComponent<CreatureExplorationTarget>();
        SetEnum(aquarioTarget, "creatureType", (int)CreatureType.Aquario);
        SetBool(aquarioTarget, "useDebugPosition", true);
        SetFloat(aquarioTarget, "debugEast", 18f);
        SetFloat(aquarioTarget, "debugNorth", 0f);
        SetFloat(aquarioTarget, "height", 0.6f);
        aquarioTarget.ApplyDebugPositionIfEnabled();

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject feedbackGroup = CreatePanel(
            "CreatureDetectionFeedback",
            canvasObject.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(560f, 170f)
        );
        TextMeshProUGUI feedbackText = CreateText(
            "FeedbackText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, 42f),
            28f,
            "Creature Signal: Out of Range"
        );
        TextMeshProUGUI signalText = CreateText(
            "SignalText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -66f),
            new Vector2(-32f, 36f),
            24f,
            "Signal: Out of Range"
        );
        TextMeshProUGUI creatureNearbyText = CreateText(
            "CreatureNearbyText",
            feedbackGroup.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -108f),
            new Vector2(-32f, 36f),
            24f,
            "No Creature Nearby"
        );

        GameObject debugPanelRoot = CreatePanel(
            "DebugPanel",
            canvasObject.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(24f, -220f),
            new Vector2(620f, -260f)
        );
        TextMeshProUGUI debugText = CreateText(
            "DebugText",
            debugPanelRoot.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, -292f),
            20f,
            "Debug ready"
        );

        TextMeshProUGUI gpsStatusText = CreateDebugField(debugPanelRoot.transform, "GPSSimulationStatus", 12f, "GPS/simulation status: Waiting");
        TextMeshProUGUI currentDisplacementText = CreateDebugField(debugPanelRoot.transform, "CurrentDisplacement", 42f, "Current displacement: (0, 0, 0)");
        TextMeshProUGUI eastDisplacementText = CreateDebugField(debugPanelRoot.transform, "EastDisplacement", 72f, "East displacement: 0.00 m");
        TextMeshProUGUI northDisplacementText = CreateDebugField(debugPanelRoot.transform, "NorthDisplacement", 102f, "North displacement: 0.00 m");
        TextMeshProUGUI headingText = CreateDebugField(debugPanelRoot.transform, "Heading", 132f, "Heading: 0.0");
        TextMeshProUGUI nearestCreatureText = CreateDebugField(debugPanelRoot.transform, "NearestCreature", 162f, "Nearest creature: None");
        TextMeshProUGUI distanceText = CreateDebugField(debugPanelRoot.transform, "DistanceToNearestCreature", 192f, "Distance to nearest creature: 0.0 m");
        TextMeshProUGUI proximityStateText = CreateDebugField(debugPanelRoot.transform, "ProximityState", 222f, "Proximity state: OutOfRange");
        TextMeshProUGUI signalStrengthText = CreateDebugField(debugPanelRoot.transform, "SignalStrength", 252f, "Signal strength: 0.00");
        TextMeshProUGUI encounterStateText = CreateDebugField(debugPanelRoot.transform, "EncounterState", 282f, "Encounter state: None");

        GameObject encounterGroup = CreatePanel(
            "EncounterReadyStatus",
            canvasObject.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            new Vector2(460f, 96f)
        );
        RectTransform encounterRect = encounterGroup.GetComponent<RectTransform>();
        encounterRect.pivot = new Vector2(1f, 1f);
        TextMeshProUGUI encounterStatusText = CreateText(
            "EncounterStatusText",
            encounterGroup.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, -16f),
            new Vector2(-32f, -32f),
            28f,
            "No Encounter"
        );

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        SetObjectReference(gpsManager, "gpsPositionSource", gpsPositionSource);
        SetObjectReference(sourceSelector, "gpsPositionSource", gpsPositionSource);
        SetObjectReference(sourceSelector, "editorPositionSource", editorPositionSource);
        SetObjectReference(sourceSelector, "explorationController", explorationController);
        SetObjectReference(sourceSelector, "proximitySystem", proximitySystem);
        SetObjectReference(explorationController, "positionSource", editorPositionSource);
        SetObjectReference(explorationController, "worldRoot", worldRoot.transform);
        SetObjectReference(explorationController, "playerMarker", playerMarker.transform);
        SetObjectReference(headingController, "playerVisual", playerVisual.transform);
        SetObjectReference(spawnManager, "targetsRoot", creatureTargets.transform);
        SetObjectReference(proximitySystem, "positionSource", editorPositionSource);
        SetObjectReference(proximitySystem, "spawnManager", spawnManager);
        SetObjectReference(proximitySystem, "feedbackText", feedbackText);
        SetObjectReference(proximitySystem, "signalText", signalText);
        SetObjectReference(proximitySystem, "creatureNearbyText", creatureNearbyText);
        SetObjectReference(proximitySystem, "encounterStatusText", encounterStatusText);
        SetObjectReference(debugPanel, "debugPanelRoot", debugPanelRoot);
        SetObjectReference(debugPanel, "debugText", debugText);
        SetObjectReference(debugPanel, "positionSourceSelector", sourceSelector);
        SetObjectReference(debugPanel, "positionSource", editorPositionSource);
        SetObjectReference(debugPanel, "explorationController", explorationController);
        SetObjectReference(debugPanel, "headingController", headingController);
        SetObjectReference(debugPanel, "proximitySystem", proximitySystem);
        SetObjectReference(debugPanel, "gpsSimulationStatusText", gpsStatusText);
        SetObjectReference(debugPanel, "currentDisplacementText", currentDisplacementText);
        SetObjectReference(debugPanel, "eastDisplacementText", eastDisplacementText);
        SetObjectReference(debugPanel, "northDisplacementText", northDisplacementText);
        SetObjectReference(debugPanel, "headingText", headingText);
        SetObjectReference(debugPanel, "nearestCreatureText", nearestCreatureText);
        SetObjectReference(debugPanel, "nearestCreatureDistanceText", distanceText);
        SetObjectReference(debugPanel, "proximityStateText", proximityStateText);
        SetObjectReference(debugPanel, "signalStrengthText", signalStrengthText);
        SetObjectReference(debugPanel, "encounterStateText", encounterStateText);

        SetFloat(gpsPositionSource, "maximumHorizontalAccuracy", 20f);
        SetFloat(gpsPositionSource, "minimumMovementDistance", 2.5f);
        SetFloat(gpsPositionSource, "gpsSmoothingSpeed", 2f);
        SetBool(gpsPositionSource, "useAccuracyWeightedSmoothing", true);
        SetFloat(gpsPositionSource, "poorAccuracySmoothingMultiplier", 0.35f);
        SetBool(editorPositionSource, "simulationEnabled", true);
        SetFloat(editorPositionSource, "simulationSpeed", 3f);
        SetFloat(explorationController, "smoothingSpeed", 3f);
        SetBool(headingController, "editorSimulationEnabled", true);
        SetFloat(headingController, "simulationTurnSpeed", 90f);
        SetFloat(proximitySystem, "detectionRange", 30f);
        SetFloat(proximitySystem, "strongSignalRange", 12f);
        SetFloat(proximitySystem, "encounterRange", 3f);

        spawnManager.CollectTargets();

        EditorSceneManager.SaveScene(scene, CreatureDetectionScenePath);
        AddSceneToBuildSettings(CreatureDetectionScenePath);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private static GameObject CreateCube(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material
    )
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        SetMaterial(cube, material);
        return cube;
    }

    private static CreatureExplorationTarget CreateCreatureTarget(
        string name,
        Transform parent,
        CreatureType creatureType,
        Vector3 localPosition,
        Material material
    )
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = name;
        target.transform.SetParent(parent);
        target.transform.localPosition = localPosition;
        target.transform.localScale = Vector3.one * 1.2f;
        SetMaterial(target, material);
        CreatureExplorationTarget creatureTarget =
            target.AddComponent<CreatureExplorationTarget>();
        SetEnum(creatureTarget, "creatureType", (int)creatureType);
        return creatureTarget;
    }

    private static GameObject CreateAquariaCreaturePrefab(Material material)
    {
        return CreateCreaturePrefab(
            AquariaCreaturePrefabPath,
            "AquariaCreature",
            CreatureType.Aquaria,
            material,
            0f,
            12f
        );
    }

    private static GameObject CreateCreaturePrefab(
        string prefabPath,
        string prefabName,
        CreatureType creatureType,
        Material material,
        float debugEast,
        float debugNorth
    )
    {
        EnsureFolder("Assets/Prefabs");

        GameObject existingPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (existingPrefab != null)
        {
            return existingPrefab;
        }

        GameObject root = new GameObject(prefabName);
        CreatureExplorationTarget target = root.AddComponent<CreatureExplorationTarget>();
        SetEnum(target, "creatureType", (int)creatureType);
        SetBool(target, "useDebugPosition", true);
        SetFloat(target, "debugEast", debugEast);
        SetFloat(target, "debugNorth", debugNorth);
        SetFloat(target, "height", 0.6f);

        GameObject visualRoot = CreateChild(root, "VisualRoot");
        visualRoot.transform.localPosition = Vector3.zero;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(visualRoot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.15f, 0.8f, 1.15f);
        SetMaterial(body, material);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject crest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crest.name = "Crest";
        crest.transform.SetParent(visualRoot.transform);
        crest.transform.localPosition = new Vector3(0f, 0.45f, 0.1f);
        crest.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
        SetMaterial(crest, material);
        Object.DestroyImmediate(crest.GetComponent<Collider>());

        GameObject leftFin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftFin.name = "LeftFin";
        leftFin.transform.SetParent(visualRoot.transform);
        leftFin.transform.localPosition = new Vector3(-0.65f, 0f, 0f);
        leftFin.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        leftFin.transform.localScale = new Vector3(0.12f, 0.5f, 0.65f);
        SetMaterial(leftFin, material);
        Object.DestroyImmediate(leftFin.GetComponent<Collider>());

        GameObject rightFin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightFin.name = "RightFin";
        rightFin.transform.SetParent(visualRoot.transform);
        rightFin.transform.localPosition = new Vector3(0.65f, 0f, 0f);
        rightFin.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        rightFin.transform.localScale = new Vector3(0.12f, 0.5f, 0.65f);
        SetMaterial(rightFin, material);
        Object.DestroyImmediate(rightFin.GetComponent<Collider>());

        GameObject signalRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        signalRing.name = "SignalRing";
        signalRing.transform.SetParent(visualRoot.transform);
        signalRing.transform.localPosition = new Vector3(0f, -0.48f, 0f);
        signalRing.transform.localScale = new Vector3(1.8f, 0.03f, 1.8f);
        SetMaterial(signalRing, material);
        Object.DestroyImmediate(signalRing.GetComponent<Collider>());

        CreaturePresentation presentation = root.AddComponent<CreaturePresentation>();
        SetObjectReference(presentation, "target", target);
        SetObjectReference(presentation, "visualRoot", visualRoot.transform);
        SetObjectReference(presentation, "bobRoot", visualRoot.transform);
        SetObjectReference(presentation, "pulseRoot", signalRing.transform);
        SetObjectReference(presentation, "signalEffectRoot", signalRing);
        SetRendererArray(presentation, "fadeRenderers", root.GetComponentsInChildren<Renderer>(true));
        SetFloat(presentation, "fadeDuration", 0.35f);
        SetFloat(presentation, "weakSignalVisibility", 0f);
        SetFloat(presentation, "visibleRendererThreshold", 0.5f);
        SetFloat(presentation, "bobHeight", 0.35f);
        SetFloat(presentation, "bobSpeed", 2.4f);
        SetFloat(presentation, "minimumPulseScale", 1.25f);
        SetFloat(presentation, "maximumPulseScale", 5.5f);
        SetFloat(presentation, "pulseSpeed", 4f);
        SetFloat(presentation, "weakSignalPulseIntensity", 0.2f);
        SetFloat(presentation, "encounterPulseBoost", 0.85f);

        GameObject savedPrefab =
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        return panel;
    }

    private static TextMeshProUGUI CreateDebugField(
        Transform parent,
        string name,
        float topOffset,
        string initialText
    )
    {
        return CreateText(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -topOffset),
            new Vector2(-32f, 26f),
            19f,
            initialText
        );
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        string initialText
    )
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        return text;
    }

    private static Material CreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void SetMaterial(GameObject gameObject, Material material)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }
        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }
        property.enumValueIndex = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRendererArray(Object target, string propertyName, Renderer[] renderers)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            Debug.LogWarning($"Missing serialized renderer array {propertyName} on {target.name}");
            return;
        }

        property.arraySize = renderers.Length;

        for (int index = 0; index < renderers.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
            {
                scene.enabled = true;
                EditorBuildSettings.scenes = scenes;
                return;
            }
        }

        ArrayUtility.Add(
            ref scenes,
            new EditorBuildSettingsScene(scenePath, true)
        );
        EditorBuildSettings.scenes = scenes;
    }
}
