using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ExplorationCreatureFeedbackSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Exploration_02_CreatureDetection.unity";
    private const string FeedbackScenePath = "Assets/Scenes/Exploration_03_CreatureFeedback.unity";
    private const string AquariaCreaturePrefabPath = "Assets/Prefabs/AquariaCreature.prefab";

    [MenuItem("Aquaria/Build Exploration 03 Creature Feedback Scene")]
    public static void BuildScene()
    {
        UpdateAquariaCreaturePrefab();

        if (!AssetDatabase.CopyAsset(SourceScenePath, FeedbackScenePath))
        {
            Debug.LogWarning($"Could not copy {SourceScenePath}; opening existing stage 03 scene if present.");
        }

        Scene scene = EditorSceneManager.OpenScene(FeedbackScenePath, OpenSceneMode.Single);

        CreatureProximitySystem proximitySystem =
            UnityEngine.Object.FindAnyObjectByType<CreatureProximitySystem>();
        CreatureExplorationTarget creatureTarget =
            UnityEngine.Object.FindAnyObjectByType<CreatureExplorationTarget>();

        if (proximitySystem == null || creatureTarget == null)
        {
            throw new InvalidOperationException(
                "Exploration_03_CreatureFeedback requires the duplicated creature detection systems."
            );
        }

        CreaturePresentation presentation =
            creatureTarget.GetComponent<CreaturePresentation>() ??
            creatureTarget.gameObject.AddComponent<CreaturePresentation>();

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        CanvasGroup encounterPrompt = CreateEncounterPrompt(canvas);

        Transform visualRoot = creatureTarget.transform.Find("VisualRoot");
        Transform pulseRoot = creatureTarget.transform.Find("VisualRoot/SignalRing");

        SetObjectReference(presentation, "target", creatureTarget);
        SetObjectReference(presentation, "proximitySystem", proximitySystem);
        SetObjectReference(presentation, "visualRoot", visualRoot);
        SetObjectReference(presentation, "bobRoot", visualRoot);
        SetObjectReference(presentation, "pulseRoot", pulseRoot);
        SetObjectReference(
            presentation,
            "signalEffectRoot",
            pulseRoot != null ? pulseRoot.gameObject : null
        );
        SetObjectReference(presentation, "encounterPrompt", encounterPrompt);
        SetRendererArray(presentation, "fadeRenderers", creatureTarget.GetComponentsInChildren<Renderer>(true));
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

        EditorSceneManager.SaveScene(scene, FeedbackScenePath);
        AddSceneToBuildSettings(FeedbackScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateAquariaCreaturePrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AquariaCreaturePrefabPath);

        try
        {
            CreaturePresentation presentation =
                prefabRoot.GetComponent<CreaturePresentation>() ??
                prefabRoot.AddComponent<CreaturePresentation>();
            CreatureExplorationTarget target = prefabRoot.GetComponent<CreatureExplorationTarget>();
            Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
            Transform pulseRoot = prefabRoot.transform.Find("VisualRoot/SignalRing");

            SetObjectReference(presentation, "target", target);
            SetObjectReference(presentation, "visualRoot", visualRoot);
            SetObjectReference(presentation, "bobRoot", visualRoot);
            SetObjectReference(presentation, "pulseRoot", pulseRoot);
            SetObjectReference(
                presentation,
                "signalEffectRoot",
                pulseRoot != null ? pulseRoot.gameObject : null
            );
            SetRendererArray(presentation, "fadeRenderers", prefabRoot.GetComponentsInChildren<Renderer>(true));
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

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, AquariaCreaturePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static CanvasGroup CreateEncounterPrompt(Canvas canvas)
    {
        if (canvas == null)
        {
            throw new InvalidOperationException("Exploration_03_CreatureFeedback requires a Canvas.");
        }

        Transform existing = canvas.transform.Find("CreatureFeedbackEncounterPrompt");

        if (existing != null)
        {
            CanvasGroup existingGroup = existing.GetComponent<CanvasGroup>();
            return existingGroup != null ? existingGroup : existing.gameObject.AddComponent<CanvasGroup>();
        }

        GameObject promptObject = new GameObject("CreatureFeedbackEncounterPrompt");
        promptObject.transform.SetParent(canvas.transform);

        RectTransform rectTransform = promptObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 96f);
        rectTransform.sizeDelta = new Vector2(620f, 132f);

        Image image = promptObject.AddComponent<Image>();
        image.color = new Color(0.0f, 0.28f, 0.34f, 0.82f);

        CanvasGroup canvasGroup = promptObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameObject promptTextObject = new GameObject("PromptText");
        promptTextObject.transform.SetParent(promptObject.transform);
        TextMeshProUGUI promptText = promptTextObject.AddComponent<TextMeshProUGUI>();
        promptText.text = "Aquaria signal locked";
        promptText.fontSize = 36f;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-36f, -24f);

        return canvasGroup;
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

    private static void SetRendererArray(
        UnityEngine.Object target,
        string propertyName,
        Renderer[] renderers
    )
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
