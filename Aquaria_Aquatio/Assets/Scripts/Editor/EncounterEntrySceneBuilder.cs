/*
EncounterEntrySceneBuilder.cs

Purpose:
Builds the encounter-entry exploration scene and the AR search scene from Unity
Editor menu commands.

Responsibilities:
- Duplicate the creature-feedback exploration scene into the encounter-entry scene.
- Add and wire ExplorationEncounterEntry, ExplorationEncounterFlow, and virtual controls.
- Generate the AR search scene with AR session objects, creature spawning, UI, and return button.
- Create required materials, UI objects, and AR helper objects.
- Add generated scenes to Unity build settings.

Architecture:
Editor-only scene assembly utility. It writes scene setup and serialized
references so runtime encounter scripts can operate without manual Inspector wiring.

Dependencies:
- UnityEditor and UnityEditor.SceneManagement
- UnityEngine.UI and TextMeshProUGUI
- XR Origin, ARSession, ARPlaneManager, and ARRaycastManager
- ExplorationEncounterEntry and ExplorationEncounterFlow
- ARCreatureSearchController and AR helper components

Data Flow:
Unity Editor menu item
    -> Generated/updated exploration and AR scenes
    -> Runtime encounter flow uses the serialized scene references

Editor / Runtime:
Located under Assets/Scripts/Editor and depends on UnityEditor APIs, so it is
editor-only and not included in runtime builds.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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
    private const string ARLookAroundCreaturePrefabPath =
        "Assets/Prefabs/Creature/ARLookAroundCreature.prefab";
    private const string AquariaCreaturePrefabPath =
        "Assets/Prefabs/Creature/Encounter/AquariaCreature_Encounter.prefab";
    private const string AquarioCreaturePrefabPath =
        "Assets/Prefabs/Creature/Encounter/AquarioCreature_Encounter.prefab";

    [MenuItem("Aquaria/Build Exploration 04 Encounter Entry And AR Search")]
    public static void BuildScenes()
    {
        BuildEncounterEntryScene();
        BuildARSearchScene();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Aquaria/Build AR Search Scene Only")]
    public static void BuildARSearchSceneOnly()
    {
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
        ExplorationEncounterFlow encounterFlow =
            systems.GetComponent<ExplorationEncounterFlow>() ??
            systems.AddComponent<ExplorationEncounterFlow>();
        EncounterEntryVirtualController virtualController =
            systems.GetComponent<EncounterEntryVirtualController>() ??
            systems.AddComponent<EncounterEntryVirtualController>();
        EditorKeyboardPositionSource editorPositionSource =
            UnityEngine.Object.FindAnyObjectByType<EditorKeyboardPositionSource>();
        DeviceHeadingController headingController =
            UnityEngine.Object.FindAnyObjectByType<DeviceHeadingController>();

        SetObjectReference(entry, "encounterFlow", encounterFlow);
        SetObjectReference(entry, "encounterPrompt", encounterPrompt);
        SetObjectReference(entry, "encounterButton", encounterButton);
        SetObjectReference(entry, "promptText", promptText);
        SetObjectReference(entry, "promptBackground", promptBackground);
        SetObjectReference(entry, "promptRectTransform", promptRectTransform);

        SetObjectReference(encounterFlow, "proximitySystem", proximitySystem);
        SetString(encounterFlow, "encounterSceneName", "Encounter_01_ARSearch");
        SetInt(encounterFlow, "aquarioCountToCatch", 3);

        SetObjectReference(virtualController, "positionSource", editorPositionSource);
        SetObjectReference(virtualController, "headingController", headingController);

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
        AREditorCameraInputController editorCameraInput =
            cameraOffset.AddComponent<AREditorCameraInputController>();

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
        ARCreatureSearchController searchController =
            systems.AddComponent<ARCreatureSearchController>();
        ARCreatureSpawner creatureSpawner = systems.AddComponent<ARCreatureSpawner>();
        ARCreatureVisibilityDetector visibilityDetector =
            systems.AddComponent<ARCreatureVisibilityDetector>();
        ARSearchUIController uiController = systems.AddComponent<ARSearchUIController>();
        AquariaUnionAnimation unionAnimation = systems.AddComponent<AquariaUnionAnimation>();

        GameObject guidanceRoot = new GameObject("ARSearchGuidance");
        GameObject directionArrowObject = CreateDirectionArrow(
            guidanceRoot.transform,
            CreateMaterial(
                "Assets/Resources/ARSearch_DirectionArrow.mat",
                new Color(0f, 0.85f, 1f, 0.95f)
            )
        );
        ARDirectionArrow directionArrow = guidanceRoot.AddComponent<ARDirectionArrow>();

        Canvas canvas = CreateCanvas();
        GameObject searchInstruction = CreatePanel(
            "SearchInstruction",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -48f),
            new Vector2(-64f, 96f)
        );
        TextMeshProUGUI instructionText = CreateText(
            "InstructionText",
            searchInstruction.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-32f, -20f),
            32f,
            "Find the creature",
            TextAlignmentOptions.Center
        );
        GameObject foundPanel = CreatePanel(
            "FoundPanel",
            canvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(620f, 180f)
        );
        RectTransform foundRect = foundPanel.GetComponent<RectTransform>();
        foundRect.pivot = new Vector2(0.5f, 0.5f);
        foundPanel.SetActive(false);
        TextMeshProUGUI foundText = CreateText(
            "FoundText",
            foundPanel.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-40f, -28f),
            44f,
            "Creature Found!",
            TextAlignmentOptions.Center
        );
        GameObject debugPanel = CreatePanel(
            "DebugPanel",
            canvas.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(24f, 24f),
            new Vector2(640f, 300f)
        );
        RectTransform debugPanelRect = debugPanel.GetComponent<RectTransform>();
        debugPanelRect.pivot = new Vector2(0f, 0f);
        TextMeshProUGUI debugText = CreateText(
            "DebugText",
            debugPanel.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(16f, 16f),
            new Vector2(-32f, -32f),
            19f,
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
            AssetDatabase.LoadAssetAtPath<GameObject>(ARLookAroundCreaturePrefabPath);
        GameObject aquariaCreaturePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(AquariaCreaturePrefabPath);
        GameObject aquarioCreaturePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(AquarioCreaturePrefabPath);

        SetObjectReference(searchController, "arSession", arSession);
        SetObjectReference(searchController, "arCamera", arCamera);
        SetObjectReference(searchController, "creatureSpawner", creatureSpawner);
        SetObjectReference(searchController, "visibilityDetector", visibilityDetector);
        SetObjectReference(searchController, "directionArrow", directionArrow);
        SetObjectReference(searchController, "uiController", uiController);
        SetObjectReference(searchController, "unionAnimation", unionAnimation);
        SetString(searchController, "returnSceneName", "Exploration_04_EncounterEntry");
        SetFloat(searchController, "returnDelayAfterFound", 2.5f);

        SetObjectReference(editorCameraInput, "moveRoot", cameraOffset.transform);
        SetObjectReference(editorCameraInput, "yawRoot", cameraOffset.transform);
        SetObjectReference(editorCameraInput, "pitchRoot", cameraObject.transform);
        SetFloat(editorCameraInput, "moveSpeed", 4f);
        SetFloat(editorCameraInput, "sprintMultiplier", 3f);
        SetFloat(editorCameraInput, "verticalMoveSpeed", 3f);
        SetFloat(editorCameraInput, "mouseSensitivity", 2.5f);

        SetObjectReference(creatureSpawner, "planeManager", planeManager);
        SetObjectReference(creatureSpawner, "raycastManager", raycastManager);
        SetObjectReference(creatureSpawner, "creaturePrefab", creaturePrefab);
        SetObjectReference(creatureSpawner, "aquariaCreaturePrefab", aquariaCreaturePrefab);
        SetObjectReference(creatureSpawner, "aquarioCreaturePrefab", aquarioCreaturePrefab);
        SetObjectReference(creatureSpawner, "creatureParent", creatureParent.transform);
        SetString(creatureSpawner, "spawnedCreatureName", "AREncounterCreature");
        SetFloat(creatureSpawner, "minimumSpawnDistance", 18f);
        SetFloat(creatureSpawner, "maximumSpawnDistance", 30f);
        SetFloat(creatureSpawner, "minimumSpawnAngleFromForward", 60f);
        SetFloat(creatureSpawner, "maximumSpawnAngleFromForward", 160f);
        SetFloat(creatureSpawner, "creatureHeightOffset", 0.5f);
        SetBool(creatureSpawner, "useDetectedPlaneHeight", false);
        SetBool(creatureSpawner, "allowFallbackPlacementWithoutPlane", true);

        SetObjectReference(visibilityDetector, "arCamera", arCamera);
        SetObjectReference(visibilityDetector, "playerViewpoint", cameraObject.transform);
        SetFloat(visibilityDetector, "requiredVisibleTime", 0.75f);
        SetFloat(visibilityDetector, "requiredDistance", 2f);
        SetBool(visibilityDetector, "showDebugDistance", false);

        SetObjectReference(directionArrow, "arCamera", arCamera);
        SetObjectReference(directionArrow, "arrowTransform", directionArrowObject.transform);

        SetObjectReference(uiController, "instructionText", instructionText);
        SetObjectReference(uiController, "foundPanel", foundPanel);
        SetObjectReference(uiController, "foundText", foundText);
        SetObjectReference(uiController, "debugPanel", debugPanel);
        SetObjectReference(uiController, "debugText", debugText);
        SetObjectReference(unionAnimation, "canvas", canvas);

        returnButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(returnButton.onClick, searchController.ReturnToPreviousScene);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, ARSearchScenePath);
        AddSceneToBuildSettings(ARSearchScenePath);
    }

    private static GameObject CreateDirectionArrow(Transform parent, Material material)
    {
        GameObject arrowRoot = new GameObject("DirectionArrow");
        arrowRoot.transform.SetParent(parent);
        arrowRoot.transform.localPosition = Vector3.zero;
        arrowRoot.transform.localRotation = Quaternion.identity;
        arrowRoot.transform.localScale = new Vector3(0.25f, 0.25f, 0.55f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "ArrowShaft";
        shaft.transform.SetParent(arrowRoot.transform);
        shaft.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        shaft.transform.localRotation = Quaternion.identity;
        shaft.transform.localScale = new Vector3(0.26f, 0.12f, 0.58f);
        SetMaterial(shaft, material);
        UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "ArrowHead";
        head.transform.SetParent(arrowRoot.transform);
        head.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        head.transform.localScale = new Vector3(0.38f, 0.14f, 0.38f);
        SetMaterial(head, material);
        UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());

        return arrowRoot;
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
        rectTransform.pivot = new Vector2(
            Mathf.Lerp(anchorMin.x, anchorMax.x, 0.5f),
            Mathf.Lerp(anchorMin.y, anchorMax.y, 0.5f)
        );
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0.22f, 0.28f, 0.72f);
        return panel;
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

    private static Material CreateMaterial(string path, Color color)
    {
        string folder = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");

        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder(folder);
        }

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

    private static void SetInt(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }

        property.intValue = value;
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
