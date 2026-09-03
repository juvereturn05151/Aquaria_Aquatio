/*
SoundManagerPrefabBuilder creates the editable prototype SoundManager prefab.
It is editor-only and does not participate in runtime audio playback.
*/

using UnityEditor;
using UnityEngine;

public static class SoundManagerPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Systems/SoundManager.prefab";

    [MenuItem("Aquaria/Audio/Create SoundManager Prefab")]
    public static void CreateSoundManagerPrefab()
    {
        GameObject root = new("SoundManager");
        SoundManager soundManager = root.AddComponent<SoundManager>();

        AudioSource musicAudioSource = CreateAudioSource(root.transform, "MusicAudioSource", true);
        AudioSource loopAudioSource = CreateAudioSource(root.transform, "LoopAudioSource", true);
        AudioSource sfxAudioSource = CreateAudioSource(root.transform, "SFXAudioSource", false);

        SetObjectReference(soundManager, "musicAudioSource", musicAudioSource);
        SetObjectReference(soundManager, "loopAudioSource", loopAudioSource);
        SetObjectReference(soundManager, "sfxAudioSource", sfxAudioSource);

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Systems");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created SoundManager prefab at {PrefabPath}");
    }

    private static AudioSource CreateAudioSource(Transform parent, string name, bool loop)
    {
        GameObject audioSourceObject = new(name);
        audioSourceObject.transform.SetParent(parent);
        audioSourceObject.transform.localPosition = Vector3.zero;
        audioSourceObject.transform.localRotation = Quaternion.identity;
        audioSourceObject.transform.localScale = Vector3.one;

        AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private static void SetObjectReference(
        Object target,
        string propertyName,
        Object value
    )
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
}
