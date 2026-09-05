/*
ARCameraTrackedPoseDriverSceneSetup.cs

Purpose:
Adds the Input System Tracked Pose Driver required for AR Foundation cameras to
receive device position and rotation updates.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class ARCameraTrackedPoseDriverSceneSetup
{
    private static readonly string[] EncounterScenes =
    {
        "Assets/Scenes/Encounter_01_ARSearch.unity",
        "Assets/Scenes/Production/AREncounter_Production.unity",
    };

    [MenuItem("Aquaria/Setup AR Camera Tracked Pose Drivers")]
    public static void SetupEncounterScenes()
    {
        foreach (string scenePath in EncounterScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera arCamera = FindARCamera();

            if (arCamera == null)
            {
                Debug.LogWarning($"No AR camera found in {scenePath}.");
                continue;
            }

            ConfigureTrackedPoseDriver(arCamera.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Configured Tracked Pose Driver on {arCamera.name} in {scenePath}.");
        }
    }

    private static Camera FindARCamera()
    {
        foreach (ARCameraManager cameraManager in Object.FindObjectsByType<ARCameraManager>(FindObjectsInactive.Include))
        {
            Camera camera = cameraManager.GetComponent<Camera>();

            if (camera != null)
            {
                return camera;
            }
        }

        return Camera.main;
    }

    private static void ConfigureTrackedPoseDriver(GameObject cameraObject)
    {
        TrackedPoseDriver trackedPoseDriver = cameraObject.GetComponent<TrackedPoseDriver>();

        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
        }

        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        trackedPoseDriver.ignoreTrackingState = false;
        trackedPoseDriver.positionInput = new InputActionProperty(CreatePositionAction());
        trackedPoseDriver.rotationInput = new InputActionProperty(CreateRotationAction());
    }

    private static InputAction CreatePositionAction()
    {
        InputAction action = new(
            "Position",
            InputActionType.Value,
            "<XRHMD>/centerEyePosition",
            expectedControlType: "Vector3"
        );
        action.AddBinding("<HandheldARInputDevice>/devicePosition");
        return action;
    }

    private static InputAction CreateRotationAction()
    {
        InputAction action = new(
            "Rotation",
            InputActionType.Value,
            "<XRHMD>/centerEyeRotation",
            expectedControlType: "Quaternion"
        );
        action.AddBinding("<HandheldARInputDevice>/deviceRotation");
        return action;
    }
}
