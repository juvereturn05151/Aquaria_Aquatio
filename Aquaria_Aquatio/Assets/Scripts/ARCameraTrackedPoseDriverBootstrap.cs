/*
ARCameraTrackedPoseDriverBootstrap.cs

Purpose:
Ensures the AR Foundation camera has a tracked pose driver in player builds so
the camera Transform follows the device pose.
*/

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class ARCameraTrackedPoseDriverBootstrap : MonoBehaviour
{
    [SerializeField] private bool enableInEditorSimulation;

    private void Awake()
    {
        EnsureTrackedPoseDriver();
    }

    private void OnEnable()
    {
        EnsureTrackedPoseDriver();
    }

    private void EnsureTrackedPoseDriver()
    {
#if ENABLE_INPUT_SYSTEM
        if (Application.isEditor && !enableInEditorSimulation)
        {
            return;
        }

        TrackedPoseDriver trackedPoseDriver = GetComponent<TrackedPoseDriver>();

        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = gameObject.AddComponent<TrackedPoseDriver>();
        }

        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        trackedPoseDriver.ignoreTrackingState = false;
        trackedPoseDriver.positionInput = new InputActionProperty(CreatePositionAction());
        trackedPoseDriver.rotationInput = new InputActionProperty(CreateRotationAction());
#else
        UnityEngine.SpatialTracking.TrackedPoseDriver trackedPoseDriver =
            GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();

        if (trackedPoseDriver == null)
        {
            trackedPoseDriver =
                gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        }

        trackedPoseDriver.SetPoseSource(
            UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRDevice,
            UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.Center
        );
        trackedPoseDriver.trackingType =
            UnityEngine.SpatialTracking.TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType =
            UnityEngine.SpatialTracking.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
#endif
    }

#if ENABLE_INPUT_SYSTEM
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
#endif
}
