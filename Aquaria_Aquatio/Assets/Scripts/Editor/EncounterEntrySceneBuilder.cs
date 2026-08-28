using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public static class EncounterEntrySceneBuilder
{
    private const string SourceExplorationScenePath = "Assets/Scenes/Exploration_03_CreatureFeedback.unity";
    private const string EncounterEntryScenePath = "Assets/Scenes/Exploration_04_EncounterEntry.unity";
    private const string ARSearchScenePath = "Assets/Scenes/Encounter_01_ARSearch.unity";
    private const string AquariaCreaturePrefabPath = "Assets/Prefabs/AquariaCreature.prefab";

    [MenuItem("Aquaria/Build Exploration 04 Encounter Entry And AR Search")]
    public static void BuildScenes()
    {
        BuildEncounterEntryScene();
        BuildARSearchScene();
        AssetDatabase.SaveAssets();
    }

    private static void BuildEncounterEntryScene()
    {
        ReplaceCopiedAsset(SourceExplorationScenePath, EncounterEntryScenePath);

        Scene scene = EditorSceneManager.OpenScene(EncounterEntryScenePath, OpenSceneMode.Single);
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        CreatureProximitySystem proximitySystem =
            UnityEngine.Object.FindAnyObjectByType<CreatureProximitySystem>();

        if (canvas == null || proximitySystem == null)
        {
            throw new InvalidOperationException(
                "Encounter entry scene requires the duplicated exploration canvas and proximity system."
            );
        }

        CanvasGroup encounterPrompt = CreateEncounterPrompt(canvas.transform);
        Button encounterButton = encounterPrompt.GetComponentInChildren<Button>(true);
        TextMeshProUGUI promptText = encounterPrompt.GetComponentInChildren<TextMeshProUGUI>(true);
        Image promptBackground = encounterPrompt.GetComponent<Image>();
        RectTransform promptRectTransform = encounterPrompt.GetComponent<RectTransform>();
        GameObject systems = GameObject.Find("Systems") ?? new GameObject("Systems");
        ExplorationEncounterEntry entry =
            systems.GetComponent<ExplorationEncounterEntry>() ??
            systems.AddComponent<ExplorationEncounterEntry>();

        SetObjectReference(entry, "proximitySystem", proximitySystem);
        SetObjectReference(entry, "encounterPrompt", encounterPrompt);
        SetObjectReference(entry, "encounterButton", encounterButton);
        SetObjectReference(entry, "promptText", promptText);
        SetObjectReference(entry, "promptBackground", promptBackground);
        SetObjectReference(entry, "promptRectTransform", promptRectTransform);
        SetString(entry, "encounterSceneName", "Encounter_01_ARSearch");

        encounterButton.onClick.RemoveAllListeners();

        EditorSceneManager.SaveScene(scene, EncounterEntryScenePath);
        AddSceneToBuildSettings(EncounterEntryScenePath);
    }

    private static void BuildARSearchScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject arSessionObject = new GameObject("AR Session");
        ARSession arSession = arSessionObject.AddComponent<ARSession>();
        arSessionObject.AddComponent<ARInputManager>();

        GameObject xrOriginObject = new GameObject("XR Origin");
        XROrigin xrOrigin = xrOriginObject.AddComponent<XROrigin>();
        ARPlaneManager planeManager = xrOriginObject.AddComponent<ARPlaneManager>();
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        ARRaycastManager raycastManager = xrOriginObject.AddComponent<ARRaycastManager>();

        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOriginObject.transform);
        cameraOffset.transform.localPosition = Vector3.zero;
        cameraOffset.transform.localRotation = Quaternion.identity;

        GameObject cameraObject = new GameObject("AR Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraOffset.transform);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera arCamera = cameraObject.AddComponent<Camera>();
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARCameraBackground>();

        xrOrigin.Camera = arCamera;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;

        GameObject directionalLightObject = new GameObject("Directional Light");
        Light directionalLight = directionalLightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1f;
        directionalLightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject creatureParent = new GameObject("AR Creature Root");
        GameObject systems = new GameObject("Systems");
        AREncounterPlacementController placementController =
            systems.AddComponent<AREncounterPlacementController>();

        Canvas canvas = CreateCanvas();
        TextMeshProUGUI instructionText = CreateText(
            "ScanInstructionText",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -48f),
            new Vector2(-64f, 80f),
            32f,
            "Move phone to scan the area",
            TextAlignmentOptions.Center
        );
        TextMeshProUGUI debugText = CreateText(
            "ARDebugText",
            canvas.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(24f, 24f),
            new Vector2(560f, 180f),
            20f,
            "AR debug ready",
            TextAlignmentOptions.BottomLeft
        );
        Button returnButton = CreateButton(
            "ReturnButton",
            canvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-32f, -32f),
            new Vector2(220f, 72f),
            "Return"
        );

        GameObject creaturePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(AquariaCreaturePrefabPath);

        SetObjectReference(placementController, "arSession", arSession);
        SetObjectReference(placementController, "planeManager", planeManager);
        SetObjectReference(placementController, "raycastManager", raycastManager);
        SetObjectReference(placementController, "arCamera", arCamera);
        SetObjectReference(placementController, "creaturePrefab", creaturePrefab);
        SetObjectReference(placementController, "creatureParent", creatureParent.transform);
        SetObjectReference(placementController, "instructionText", instructionText);
        SetObjectReference(placementController, "debugText", debugText);
        SetBool(placementController, "useDebugPlacement", true);
        SetFloat(placementController, "debugSpawnYaw", 120f);
        SetFloat(placementController, "debugSpawnDistance", 3f);
        SetFloat(placementController, "minSpawnDistance", 2f);
        SetFloat(placementController, "maxSpawnDistance", 4f);
        SetFloat(placementController, "minSpawnYawOffset", 70f);
        SetFloat(placementController, "maxSpawnYawOffset", 160f);
        SetFloat(placementController, "heightOffset", 0f);
        SetBool(placementController, "allowFallbackPlacementWithoutPlane", true);
        SetString(placementController, "returnSceneName", "Exploration_04_EncounterEntry");

        returnButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(returnButton.onClick, placementController.ReturnToExploration);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, ARSearchScenePath);
        AddSceneToBuildSettings(ARSearchScenePath);
    }

    private static CanvasGroup CreateEncounterPrompt(Transform canvas)
    {
        Transform existing = canvas.Find("EncounterEntryPrompt");

        if (existing != null)
        {
            return existing.GetComponent<CanvasGroup>() ??
                existing.gameObject.AddComponent<CanvasGroup>();
        }

        GameObject promptObject = new GameObject("EncounterEntryPrompt");
        promptObject.transform.SetParent(canvas);
        RectTransform rectTransform = promptObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 140f);
        rectTransform.sizeDelta = new Vector2(620f, 128f);
        Image background = promptObject.AddComponent<Image>();
        background.color = new Color(0f, 0.72f, 0.9f, 0.95f);
        CanvasGroup group = promptObject.AddComponent<CanvasGroup>();

        Button button = promptObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.9f, 0.9f, 1f);
        button.colors = colors;

        CreateText(
            "EncounterPromptText",
            promptObject.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-36f, -28f),
            34f,
            "START AR ENCOUNTER",
            TextAlignmentOptions.Center
        );

        return group;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        string initialText,
        TextAlignmentOptions alignment
    )
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = alignment;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(
            Mathf.Lerp(anchorMin.x, anchorMax.x, 0.5f),
            Mathf.Lerp(anchorMin.y, anchorMax.y, 0.5f)
        );
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        string text
    )
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent);
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0f, 0.28f, 0.34f, 0.86f);
        Button button = buttonObject.AddComponent<Button>();

        CreateText(
            "Label",
            buttonObject.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-20f, -12f),
            28f,
            text,
            TextAlignmentOptions.Center
        );

        return button;
    }

    private static void ReplaceCopiedAsset(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath) != null)
        {
            AssetDatabase.DeleteAsset(destinationPath);
        }

        if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
        {
            throw new InvalidOperationException($"Could not copy {sourcePath} to {destinationPath}.");
        }
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
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

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
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

    private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
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

    private static void SetString(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }

        property.stringValue = value;
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

        ArrayUtility.Add(ref scenes, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes;
    }
}
