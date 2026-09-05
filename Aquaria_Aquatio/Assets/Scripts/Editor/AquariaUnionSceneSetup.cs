using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AquariaUnionSceneSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Encounter_01_ARSearch.unity",
        "Assets/Scenes/Production/AREncounter_Production.unity",
    };

    [MenuItem("Aquaria/Setup Union UI In Encounter Scenes")]
    public static void SetupUnionUiInEncounterScenes()
    {
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SetupScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void SetupScene(Scene scene)
    {
        Canvas canvas = FindInScene<Canvas>(scene);
        ARCreatureSearchController searchController = FindInScene<ARCreatureSearchController>(scene);

        if (canvas == null || searchController == null)
        {
            Debug.LogWarning($"Could not set up Aquaria union UI in {scene.path}.");
            return;
        }

        AquariaUnionAnimation unionAnimation =
            searchController.GetComponent<AquariaUnionAnimation>() ??
            searchController.gameObject.AddComponent<AquariaUnionAnimation>();

        RectTransform overlay = FindDirectChild(canvas.transform, "AquariaUnionOverlay") ??
            CreateRectTransform("AquariaUnionOverlay", canvas.transform);
        overlay.SetAsLastSibling();
        StretchToParent(overlay);

        Image background = GetOrAdd<Image>(overlay.gameObject);
        background.color = new Color(0f, 0.1f, 0.16f, 0.82f);
        background.raycastTarget = true;

        CanvasGroup overlayGroup = GetOrAdd<CanvasGroup>(overlay.gameObject);
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        TextMeshProUGUI titleText = GetOrCreateText(
            overlay,
            "UnionTitle",
            "Aquaria and Aquario are uniting",
            new Vector2(0f, 170f),
            new Vector2(720f, 96f),
            38f
        );

        RectTransform aquariaMarker = GetOrCreateMarker(
            overlay,
            "AquariaMarker",
            new Color(0f, 0.75f, 0.95f, 1f),
            new Vector2(-190f, 0f),
            132f
        );
        RectTransform aquarioMarker = GetOrCreateMarker(
            overlay,
            "AquarioMarker",
            new Color(0.95f, 0.56f, 0.18f, 1f),
            new Vector2(190f, 0f),
            132f
        );
        RectTransform unitedMarker = GetOrCreateMarker(
            overlay,
            "UnitedMarker",
            new Color(0.8f, 1f, 0.85f, 0.95f),
            Vector2.zero,
            150f
        );
        unitedMarker.gameObject.SetActive(false);

        SerializedObject unionObject = new SerializedObject(unionAnimation);
        unionObject.FindProperty("overlayGroup").objectReferenceValue = overlayGroup;
        unionObject.FindProperty("aquariaMarker").objectReferenceValue = aquariaMarker;
        unionObject.FindProperty("aquarioMarker").objectReferenceValue = aquarioMarker;
        unionObject.FindProperty("unitedMarker").objectReferenceValue = unitedMarker;
        unionObject.FindProperty("titleText").objectReferenceValue = titleText;
        unionObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerObject = new SerializedObject(searchController);
        controllerObject.FindProperty("unionAnimation").objectReferenceValue = unionAnimation;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T FindInScene<T>(Scene scene) where T : Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RectTransform FindDirectChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static TextMeshProUGUI GetOrCreateText(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize
    )
    {
        RectTransform rectTransform = FindDirectChild(parent, name) ??
            CreateRectTransform(name, parent);
        CenterAnchored(rectTransform, anchoredPosition, size);

        TextMeshProUGUI textComponent = GetOrAdd<TextMeshProUGUI>(rectTransform.gameObject);
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static RectTransform GetOrCreateMarker(
        RectTransform parent,
        string name,
        Color color,
        Vector2 anchoredPosition,
        float size
    )
    {
        RectTransform rectTransform = FindDirectChild(parent, name) ??
            CreateRectTransform(name, parent);
        CenterAnchored(rectTransform, anchoredPosition, new Vector2(size, size));

        Image image = GetOrAdd<Image>(rectTransform.gameObject);
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static void CenterAnchored(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }
}
