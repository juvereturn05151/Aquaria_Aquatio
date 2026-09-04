/*
CesiumRenderingDiagnostics.cs

Purpose:
Logs the exploration scene camera and Cesium rendering state so disappearing
tiles can be diagnosed without changing the GPS or map architecture.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using System.Text;
using CesiumForUnity;
using UnityEngine;

[DisallowMultipleComponent]
public class CesiumRenderingDiagnostics : MonoBehaviour
{
    [Header("Diagnostics")]
    [SerializeField]
    private bool enableDiagnostics = true;
    [SerializeField]
    private bool logOnStart = true;
    [SerializeField]
    private float logIntervalSeconds;

    [Header("Runtime Guards")]
    [SerializeField]
    private bool forceMainCameraOnlyForCesium = true;
    [SerializeField]
    private bool disableGameplayCameraOcclusionCulling = true;

    private float nextLogTime;

    public bool EnableDiagnostics
    {
        get => enableDiagnostics;
        set => enableDiagnostics = value;
    }

    private void Start()
    {
        ApplyRuntimeGuards();

        if (enableDiagnostics && logOnStart)
        {
            LogState("startup");
        }

        nextLogTime = Time.time + Mathf.Max(0f, logIntervalSeconds);
    }

    private void Update()
    {
        if (!enableDiagnostics || logIntervalSeconds <= 0f || Time.time < nextLogTime)
        {
            return;
        }

        LogState("interval");
        nextLogTime = Time.time + logIntervalSeconds;
    }

    private void ApplyRuntimeGuards()
    {
        Camera gameplayCamera = Camera.main;

        if (disableGameplayCameraOcclusionCulling && gameplayCamera != null && gameplayCamera.useOcclusionCulling)
        {
            gameplayCamera.useOcclusionCulling = false;
        }

        if (!forceMainCameraOnlyForCesium)
        {
            return;
        }

        CesiumCameraManager[] cameraManagers = FindObjectsByType<CesiumCameraManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CesiumCameraManager cameraManager in cameraManagers)
        {
            cameraManager.useMainCamera = true;
            cameraManager.useSceneViewCameraInEditor = false;
        }
    }

    private void LogState(string reason)
    {
        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        CesiumGeoreference[] georeferences = FindObjectsByType<CesiumGeoreference>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Cesium3DTileset[] tilesets = FindObjectsByType<Cesium3DTileset>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int enabledCameraCount = 0;
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.isActiveAndEnabled)
            {
                enabledCameraCount++;
            }
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Cesium rendering diagnostics ({reason})");
        builder.AppendLine($"MainCamera: {FormatCamera(Camera.main)}");
        builder.AppendLine($"Enabled Camera components: {enabledCameraCount} / {cameras.Length}");

        foreach (Camera camera in cameras)
        {
            builder.AppendLine($"Camera: {FormatCamera(camera)}");
        }

        foreach (CesiumGeoreference georeference in georeferences)
        {
            Transform transformToLog = georeference.transform;
            builder.AppendLine(
                $"CesiumGeoreference: {GetPath(transformToLog)}, active={georeference.gameObject.activeInHierarchy}, " +
                $"pos={transformToLog.position}, rot={transformToLog.rotation.eulerAngles}, scale={transformToLog.lossyScale}"
            );
        }

        foreach (Cesium3DTileset tileset in tilesets)
        {
            Transform transformToLog = tileset.transform;
            builder.AppendLine(
                $"Cesium3DTileset: {GetPath(transformToLog)}, active={tileset.gameObject.activeInHierarchy}, " +
                $"enabled={tileset.enabled}, pos={transformToLog.position}, rot={transformToLog.rotation.eulerAngles}, " +
                $"scale={transformToLog.lossyScale}"
            );
        }

        Debug.Log(builder.ToString());
    }

    private string FormatCamera(Camera camera)
    {
        if (camera == null)
        {
            return "none";
        }

        Transform cameraTransform = camera.transform;
        float distanceFromOrigin = cameraTransform.position.magnitude;

        return $"{GetPath(cameraTransform)}, tag={camera.tag}, cameraEnabled={camera.enabled}, " +
            $"objectActive={camera.gameObject.activeInHierarchy}, position={cameraTransform.position}, " +
            $"rotation={cameraTransform.rotation.eulerAngles}, distanceFromOrigin={distanceFromOrigin:F2}, " +
            $"cullingMask={camera.cullingMask}, occlusionCulling={camera.useOcclusionCulling}, " +
            $"near={camera.nearClipPlane:F2}, far={camera.farClipPlane:F2}";
    }

    private string GetPath(Transform target)
    {
        if (target == null)
        {
            return "none";
        }

        StringBuilder builder = new StringBuilder(target.name);
        Transform parent = target.parent;

        while (parent != null)
        {
            builder.Insert(0, parent.name + "/");
            parent = parent.parent;
        }

        return builder.ToString();
    }
}
