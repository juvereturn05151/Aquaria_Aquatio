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
        SetFloat(aquariaTarget, "discoveryRadius", 30f);
        SetFloat(aquariaTarget, "encounterRadius", 3f);
        SetFloat(aquarioTarget, "discoveryRadius", 30f);
        SetFloat(aquarioTarget, "encounterRadius", 3f);

        spawnManager.CollectTargets();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
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
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.enumValueIndex = value;
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
