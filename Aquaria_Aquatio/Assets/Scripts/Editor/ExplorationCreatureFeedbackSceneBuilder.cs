/*
ExplorationCreatureFeedbackSceneBuilder.cs

Purpose:
Builds the creature-feedback exploration scene and configures creature visual
feedback prefabs/components from a Unity Editor menu command.

Responsibilities:
- Update creature prefabs with CreaturePresentation settings.
- Copy the creature-detection scene into the feedback scene.
- Find creature targets and the proximity system in the copied scene.
- Create or reuse the encounter prompt UI.
- Wire CreaturePresentation references and tuning values.
- Add the generated scene to Unity build settings.

Architecture:
Editor-only scene preparation utility for Exploration_03_CreatureFeedback. It
configures runtime presentation components but does not run during gameplay.

Dependencies:
- UnityEditor and UnityEditor.SceneManagement
- CreatureExplorationTarget
- CreaturePresentation
- CreatureProximitySystem
- CanvasGroup and TextMeshProUGUI

Data Flow:
Unity Editor menu item
    -> Prefab and scene serialized values
    -> Runtime CreaturePresentation reads proximity state during play

Editor / Runtime:
Located under Assets/Scripts/Editor and depends on UnityEditor APIs, so it is
editor-only and excluded from runtime builds.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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
    private const string AquarioCreaturePrefabPath = "Assets/Prefabs/AquarioCreature.prefab";

    [MenuItem("Aquaria/Build Exploration 03 Creature Feedback Scene")]
    public static void BuildScene()
    {
        UpdateCreaturePrefab(AquariaCreaturePrefabPath);
        UpdateCreaturePrefab(AquarioCreaturePrefabPath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, FeedbackScenePath))
        {
            Debug.LogWarning($"Could not copy {SourceScenePath}; opening existing stage 03 scene if present.");
        }

        Scene scene = EditorSceneManager.OpenScene(FeedbackScenePath, OpenSceneMode.Single);

        CreatureProximitySystem proximitySystem =
            UnityEngine.Object.FindAnyObjectByType<CreatureProximitySystem>();
        CreatureExplorationTarget[] creatureTargets =
            UnityEngine.Object.FindObjectsByType<CreatureExplorationTarget>(
                FindObjectsInactive.Include
            );

        if (proximitySystem == null || creatureTargets.Length == 0)
        {
            throw new InvalidOperationException(
                "Exploration_03_CreatureFeedback requires the duplicated creature detection systems."
            );
        }

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        CanvasGroup encounterPrompt = CreateEncounterPrompt(canvas);

        foreach (CreatureExplorationTarget creatureTarget in creatureTargets)
        {
            ConfigurePresentation(
                creatureTarget,
                proximitySystem,
                encounterPrompt
            );
        }

        EditorSceneManager.SaveScene(scene, FeedbackScenePath);
        AddSceneToBuildSettings(FeedbackScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateCreaturePrefab(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabAsset == null)
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            CreatureExplorationTarget target = prefabRoot.GetComponent<CreatureExplorationTarget>();

            if (target != null)
            {
                ConfigurePresentation(target, null, null);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ConfigurePresentation(
        CreatureExplorationTarget creatureTarget,
        CreatureProximitySystem proximitySystem,
        CanvasGroup encounterPrompt
    )
    {
        CreaturePresentation presentation =
            creatureTarget.GetComponent<CreaturePresentation>() ??
            creatureTarget.gameObject.AddComponent<CreaturePresentation>();
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
